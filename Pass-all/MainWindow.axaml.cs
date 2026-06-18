using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using DllPass_all;
using Microsoft.EntityFrameworkCore;
using Passall.Modeles;
using Passall.Utils;
using Constantes = Passall.Utils.Constantes;

namespace Passall
{
    public partial class MainWindow : Window
    {
        public Guid CurrentUserId { get; set; }

        private bool? _isSuperAdmin;

        private bool _profilePasswordVisible;
        private bool _profileAddOpen;
        private Guid? _profileEditId;

        private System.Collections.Generic.List<DBUserProfile> _allProfiles = new();

        private bool _passwordCheckEnabled = true;
        private static readonly HttpClient _httpClient = new();

        private bool _categoryAddOpen;
        private Guid? _categoryEditId;
        private string _selectedCategoryColor = "#CC4285F4";

        private static readonly string[] PresetColors =
        {
            "#CC4285F4", "#CC1DB954", "#CCFF9900", "#CCE50914",
            "#CC5865F2", "#CC00A8D4", "#CCFF6B9D", "#CC8B5CF6",
        };

        public MainWindow()
        {
            InitializeComponent();
            BuildColorSwatches(NewCategoryColorSwatches, SelectCategoryColor);
            this.Loaded += (s, e) => _ = LoadProfiles();
        }

        // ── Profils — liste ────────────────────────────────────────────────────

        private async Task<bool> IsSuperAdminAsync()
        {
            if (_isSuperAdmin.HasValue) return _isSuperAdmin.Value;
            await using var db = new DataContext();
            var user = await db.User.FirstOrDefaultAsync(u => u.Id == CurrentUserId);
            _isSuperAdmin = user?.Login == Constantes.SuperAdmin_Login;
            return _isSuperAdmin.Value;
        }

        private async Task LoadProfiles()
        {
            var isAdmin = await IsSuperAdminAsync();
            await using var db = new DataContext();

            IQueryable<DBUserProfile> q = db.UserProfile.Include(p => p.Category);
            if (isAdmin) q = q.Include(p => p.User);
            else q = q.Where(p => p.UserId == CurrentUserId);

            var profiles = await q.ToListAsync();

            if (isAdmin)
            {
                foreach (var p in profiles)
                {
                    p.ShowOwner = true;
                    p.OwnerLabel = "par " + (p.User?.Name ?? p.User?.Login ?? "?");
                }
            }

            if (_passwordCheckEnabled)
                await CheckPasswordBreachesAsync(profiles);

            _allProfiles = profiles;
            ApplyFilterAndSort();
        }

        private void ApplyFilterAndSort()
        {
            if (ProfileItemsControl == null) return;

            var query = SearchInput?.Text?.Trim() ?? string.Empty;
            System.Collections.Generic.IEnumerable<DBUserProfile> view = _allProfiles;

            if (!string.IsNullOrEmpty(query))
            {
                view = view.Where(p =>
                    (p.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.Login?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.Email?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.Url?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.Category?.Label?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            view = (SortCombo?.SelectedIndex ?? 0) switch
            {
                1 => view.OrderByDescending(p => p.Id),
                2 => view.OrderBy(p => p.Category?.Label ?? string.Empty).ThenBy(p => p.Name),
                _ => view.OrderBy(p => p.Name),
            };

            var list = view.ToList();
            ProfileItemsControl.ItemsSource = list;
            var count = list.Count;
            ProfileCountText.Text = count == 1 ? "1 compte" : $"{count} comptes";
        }

        private void SearchInputChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e)
            => ApplyFilterAndSort();

        private void SortComboChanged(object? sender, SelectionChangedEventArgs e)
            => ApplyFilterAndSort();

        private async Task CheckPasswordBreachesAsync(System.Collections.Generic.List<DBUserProfile> profiles)
        {
            var tasks = profiles.Select(async profile =>
            {
                try
                {
                    var plain = Cryptage.DllDecrypt(profile.Password) ?? string.Empty;
                    if (string.IsNullOrEmpty(plain)) return;

                    var sha1 = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(plain)));
                    var prefix = sha1[..5];
                    var suffix = sha1[5..];

                    var response = await _httpClient.GetStringAsync(
                        $"https://api.pwnedpasswords.com/range/{prefix}");

                    var matched = false;
                    foreach (var line in response.Split('\n'))
                    {
                        var parts = line.Trim().Split(':');
                        if (parts.Length == 2 &&
                            parts[0].Equals(suffix, StringComparison.OrdinalIgnoreCase) &&
                            int.TryParse(parts[1], out var n))
                        {
                            profile.BreachCount = n;
                            profile.IsBreached = n > 0;
                            profile.IsSafe = n == 0;
                            matched = true;
                            break;
                        }
                    }
                    if (!matched)
                        profile.IsSafe = true;
                }
                catch { }
            });

            await Task.WhenAll(tasks);
        }

        private void ToggleProfilePasswordClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (((Button)sender!).Tag is not DBUserProfile profile) return;
            var btn = (Button)sender;
            if (btn.Parent is not StackPanel actions) return;
            if (actions.Parent is not Grid grid) return;

            var pwdBlock = grid.Children
                .OfType<TextBlock>()
                .FirstOrDefault(tb => Grid.GetColumn(tb) == 3);
            if (pwdBlock == null) return;

            bool isHidden = pwdBlock.Text == "••••••••••••";
            pwdBlock.Text = isHidden
                ? Cryptage.DllDecrypt(profile.Password) ?? "—"
                : "••••••••••••";

            if (btn.Content is Avalonia.Controls.Panel panel)
            {
                var icons = panel.Children.OfType<ContentControl>().ToList();
                if (icons.Count >= 2)
                {
                    icons[0].IsVisible = !isHidden;
                    icons[1].IsVisible = isHidden;
                }
            }
        }

        private async void CopyProfilePasswordClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (((Button)sender!).Tag is not DBUserProfile profile) return;
            var decrypted = Cryptage.DllDecrypt(profile.Password) ?? string.Empty;
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
                await clipboard.SetTextAsync(decrypted);
        }

        // ── Profils — formulaire inline ────────────────────────────────────────

        private Border? _openProfileCard;

        private void ProfileCardPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (IsInsideButton(e.Source as Avalonia.Visual)) return;
            // Ignore les clics qui viennent du formulaire d'édition (ComboBox, TextBox…) —
            // sinon ouvrir la liste déroulante de la catégorie ferme la carte.
            if (IsInsideTaggedElement(e.Source as Avalonia.Visual, "edit-form")) return;
            if (sender is not Border border || border.Tag is not DBUserProfile profile) return;
            if (border == _openProfileCard) { CloseProfileInlineCard(); return; }
            _ = OpenProfileInlineEdit(border, profile);
        }

        private async Task OpenProfileInlineEdit(Border card, DBUserProfile profile)
        {
            CloseProfileInlineCard();
            if (_profileAddOpen) CloseProfileForm();

            var editForm = FindChildByTag<Border>(card, "edit-form");
            if (editForm == null) return;

            _openProfileCard = card;
            _profileEditId = profile.Id;

            // Fill the inline form fields
            SetTextBoxByTag(card, "Name", profile.Name);
            SetTextBoxByTag(card, "Login", profile.Login);
            SetTextBoxByTag(card, "Email", profile.Email);
            SetTextBoxByTag(card, "Url", profile.Url);
            SetTextBoxByTag(card, "Password", Cryptage.DllDecrypt(profile.Password) ?? string.Empty);

            // Reset password to hidden
            var pwdBox = FindTextBoxByTag(card, "Password");
            if (pwdBox != null) pwdBox.PasswordChar = '●';
            var eyeShow = FindChildByTag<ContentControl>(card, "eye-show");
            var eyeHide = FindChildByTag<ContentControl>(card, "eye-hide");
            if (eyeShow != null) eyeShow.IsVisible = true;
            if (eyeHide != null) eyeHide.IsVisible = false;

            // Load category combo
            var categoryCombo = FindChildByTag<ComboBox>(card, "Category");
            if (categoryCombo != null)
            {
                var isAdmin = await IsSuperAdminAsync();
                await using var db = new DataContext();
                IQueryable<DBProfileCategory> cq = db.ProfileCategory;
                if (!isAdmin) cq = cq.Where(c => c.UserId == CurrentUserId);
                var categories = await cq.OrderBy(c => c.Label).ToListAsync();
                categoryCombo.ItemsSource = categories;
                categoryCombo.SelectedItem = categories.FirstOrDefault(c => c.Id == profile.CategoryId);
            }

            editForm.MaxHeight = 700;
        }

        private void CloseProfileInlineCard()
        {
            if (_openProfileCard != null)
            {
                var editForm = FindChildByTag<Border>(_openProfileCard, "edit-form");
                if (editForm != null) editForm.MaxHeight = 0;
                _openProfileCard = null;
                _profileEditId = null;
            }
        }

        private async void ToggleProfileAddClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_profileAddOpen) { CloseProfileForm(); return; }
            CloseProfileInlineCard();

            _profileEditId = null;

            var isAdmin = await IsSuperAdminAsync();
            await using var db = new DataContext();
            IQueryable<DBProfileCategory> cq = db.ProfileCategory;
            if (!isAdmin) cq = cq.Where(c => c.UserId == CurrentUserId);
            var categories = await cq.OrderBy(c => c.Label).ToListAsync();
            ProfileCategoryComboBox.ItemsSource = categories;
            ProfileCategoryComboBox.SelectedIndex = categories.Count > 0 ? 0 : -1;

            ProfileNameInput.Text = string.Empty;
            ProfileLoginInput.Text = string.Empty;
            ProfileEmailInput.Text = string.Empty;
            ProfileUrlInput.Text = string.Empty;
            ProfilePasswordInput.Text = string.Empty;
            ProfilePasswordInput.PasswordChar = '●';
            _profilePasswordVisible = false;
            ProfileFormEyeShow.IsVisible = true;
            ProfileFormEyeHide.IsVisible = false;

            ProfileAddForm.MaxHeight = 700;
            _profileAddOpen = true;
        }

        private async void SaveProfileFormClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var name = ProfileNameInput.Text?.Trim();
            if (string.IsNullOrEmpty(name)) return;

            var category = ProfileCategoryComboBox.SelectedItem as DBProfileCategory;
            if (category == null) return;

            await using var db = new DataContext();

            var userId = CurrentUserId;
            if (userId == Guid.Empty)
            {
                var user = await db.User.FirstOrDefaultAsync();
                if (user == null) return;
                userId = user.Id;
            }
            db.UserProfile.Add(new DBUserProfile
            {
                Id = Guid.NewGuid(),
                Name = name,
                Login = ProfileLoginInput.Text?.Trim() ?? string.Empty,
                Email = ProfileEmailInput.Text?.Trim() ?? string.Empty,
                Url = ProfileUrlInput.Text?.Trim() ?? string.Empty,
                Password = Cryptage.DllEncrypt(ProfilePasswordInput.Text) ?? string.Empty,
                UserId = userId,
                CategoryId = category.Id,
            });
            await db.SaveChangesAsync();

            CloseProfileForm();
            await LoadProfiles();
        }

        // ── Inline card edit: Save / Cancel / Delete ─────────────────────────

        private async void SaveProfileInlineClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_openProfileCard == null || !_profileEditId.HasValue) return;

            var name = FindTextBoxByTag(_openProfileCard, "Name")?.Text?.Trim();
            if (string.IsNullOrEmpty(name)) return;

            var categoryCombo = FindChildByTag<ComboBox>(_openProfileCard, "Category");
            var category = categoryCombo?.SelectedItem as DBProfileCategory;
            if (category == null) return;

            await using var db = new DataContext();
            var entity = await db.UserProfile.FindAsync(_profileEditId.Value);
            if (entity != null)
            {
                entity.Name = name;
                entity.Login = FindTextBoxByTag(_openProfileCard, "Login")?.Text?.Trim() ?? string.Empty;
                entity.Email = FindTextBoxByTag(_openProfileCard, "Email")?.Text?.Trim() ?? string.Empty;
                entity.Url = FindTextBoxByTag(_openProfileCard, "Url")?.Text?.Trim() ?? string.Empty;
                var pwd = FindTextBoxByTag(_openProfileCard, "Password")?.Text;
                if (!string.IsNullOrEmpty(pwd))
                    entity.Password = Cryptage.DllEncrypt(pwd) ?? entity.Password;
                entity.CategoryId = category.Id;
                await db.SaveChangesAsync();
            }

            CloseProfileInlineCard();
            await LoadProfiles();
        }

        private void CancelProfileInlineClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            CloseProfileInlineCard();
        }

        private async void DeleteProfileInlineClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (!_profileEditId.HasValue) return;

            await using var db = new DataContext();
            var entity = await db.UserProfile.FindAsync(_profileEditId.Value);
            if (entity != null)
            {
                db.UserProfile.Remove(entity);
                await db.SaveChangesAsync();
            }

            CloseProfileInlineCard();
            await LoadProfiles();
        }

        private void ToggleCardPasswordClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            // Walk up to find the card
            var card = btn.FindAncestorOfType<Border>();
            while (card != null && card.Tag is not DBUserProfile)
                card = card.FindAncestorOfType<Border>();
            if (card == null) return;

            var pwdBox = FindTextBoxByTag(card, "Password");
            if (pwdBox == null) return;

            bool isHidden = pwdBox.PasswordChar == '●';
            pwdBox.PasswordChar = isHidden ? '\0' : '●';

            var eyeShow = FindChildByTag<ContentControl>(card, "eye-show");
            var eyeHide = FindChildByTag<ContentControl>(card, "eye-hide");
            if (eyeShow != null) eyeShow.IsVisible = !isHidden;
            if (eyeHide != null) eyeHide.IsVisible = isHidden;
        }

        private void CloseProfileAddClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
            => CloseProfileForm();

        private void CloseProfileForm()
        {
            ProfileAddForm.MaxHeight = 0;
            _profileAddOpen = false;
        }

        private void ToggleProfileFormPasswordClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _profilePasswordVisible = !_profilePasswordVisible;
            ProfilePasswordInput.PasswordChar = _profilePasswordVisible ? '\0' : '●';
            ProfileFormEyeShow.IsVisible = !_profilePasswordVisible;
            ProfileFormEyeHide.IsVisible = _profilePasswordVisible;
        }

        private async void GeneratePasswordClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            await using var db = new DataContext();
            var words = await db.Dictionary.Select(d => d.Word).ToListAsync();
            var password = PasswordGenerator.GenerateFromWords(words);
            if (password == null) return;

            ProfilePasswordInput.Text = password;
            ProfilePasswordInput.PasswordChar = '\0';
            _profilePasswordVisible = true;
            ProfileFormEyeShow.IsVisible = false;
            ProfileFormEyeHide.IsVisible = true;
        }

        // ── Indicateur de force du mot de passe ────────────────────────────────

        private void ProfilePasswordChanged(object? sender, TextChangedEventArgs e)
        {
            var segments = new[]
            {
                ProfileStrengthSeg1, ProfileStrengthSeg2, ProfileStrengthSeg3, ProfileStrengthSeg4
            };
            UpdateStrengthMeter(segments, ProfileStrengthLabel, ProfilePasswordInput.Text);
        }

        private void InlinePasswordChanged(object? sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox box) return;

            var card = box.FindAncestorOfType<Border>();
            while (card != null && card.Tag is not DBUserProfile)
                card = card.FindAncestorOfType<Border>();
            if (card == null) return;

            var segments = new[]
            {
                FindChildByTag<Border>(card, "Strength1"),
                FindChildByTag<Border>(card, "Strength2"),
                FindChildByTag<Border>(card, "Strength3"),
                FindChildByTag<Border>(card, "Strength4"),
            };
            var label = FindChildByTag<TextBlock>(card, "StrengthLabel");
            UpdateStrengthMeter(segments, label, box.Text);
        }

        private static void UpdateStrengthMeter(Border?[] segments, TextBlock? label, string? password)
        {
            var result = PasswordStrength.Evaluate(password);
            var active = (int)result.Level; // None=0 .. Strong=4
            var onBrush = new SolidColorBrush(Color.Parse(result.Color));
            var offBrush = new SolidColorBrush(Color.Parse(PasswordStrength.OffColor));

            for (var i = 0; i < segments.Length; i++)
            {
                if (segments[i] != null)
                    segments[i]!.Background = i < active ? onBrush : offBrush;
            }

            if (label != null)
            {
                label.Text = result.Label;
                label.Foreground = onBrush;
            }
        }

        // ── Navigation ─────────────────────────────────────────────────────────

        private void OpenCategoriesClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            SettingsOverlay.IsVisible = false;
            MainView.IsVisible = false;
            CategoryView.IsVisible = true;
            _ = LoadCategories();
        }

        private void BackClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            CloseCategoryForm();
            CategoryView.IsVisible = false;
            MainView.IsVisible = true;
        }

        // ── Overlays ───────────────────────────────────────────────────────────

        private void HelpClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var anchor = CategoryView.IsVisible ? "#categories" : "#accounts";
            OpenHelp(anchor);
        }

        internal static void OpenHelp(string anchor)
        {
            var uri = new Uri(Path.Combine(AppContext.BaseDirectory, "Help", "index.html")).AbsoluteUri + anchor;
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    Process.Start(new ProcessStartInfo("xdg-open", uri) { UseShellExecute = false });
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    Process.Start(new ProcessStartInfo("open", uri) { UseShellExecute = false });
                else
                    // Windows : passer par `cmd /c start` préserve mieux le fragment "#anchor"
                    // que ShellExecute direct (Edge ignore parfois le hash sur file://).
                    Process.Start(new ProcessStartInfo("cmd", $"/c start \"\" \"{uri}\"") { CreateNoWindow = true, UseShellExecute = false });
            }
            catch { }
        }

        private void CloseHelpClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
            => HelpOverlay.IsVisible = false;

        private void SettingsClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
            => SettingsOverlay.IsVisible = true;

        private void CloseSettingsClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
            => SettingsOverlay.IsVisible = false;

        private void LogoutClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            new AuthWindow().Show();
            this.Close();
        }

        // ── Catégories — liste ─────────────────────────────────────────────────

        private async Task LoadCategories()
        {
            var isAdmin = await IsSuperAdminAsync();
            await using var db = new DataContext();

            IQueryable<DBProfileCategory> q = db.ProfileCategory;
            if (isAdmin) q = q.Include(c => c.User);
            else q = q.Where(c => c.UserId == CurrentUserId);

            var categories = await q.OrderBy(c => c.Label).ToListAsync();

            if (isAdmin)
            {
                foreach (var c in categories)
                {
                    c.ShowOwner = true;
                    c.OwnerLabel = "par " + (c.User?.Name ?? c.User?.Login ?? "?");
                }
            }

            CategoryItemsControl.ItemsSource = categories;
            var count = categories.Count;
            CategoryCountText.Text = count == 1 ? "1 catégorie" : $"{count} catégories";
        }

        // ── Catégories — formulaire inline ─────────────────────────────────────

        private Border? _openCategoryCard;

        private void CategoryCardPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (IsInsideButton(e.Source as Avalonia.Visual)) return;
            if (IsInsideTaggedElement(e.Source as Avalonia.Visual, "cat-edit-form")) return;
            if (sender is not Border border || border.Tag is not DBProfileCategory cat) return;
            if (border == _openCategoryCard) { CloseCategoryInlineCard(); return; }
            OpenCategoryInlineEdit(border, cat);
        }

        private void OpenCategoryInlineEdit(Border card, DBProfileCategory cat)
        {
            CloseCategoryInlineCard();
            if (_categoryAddOpen) CloseCategoryForm();

            var editForm = FindChildByTag<Border>(card, "cat-edit-form");
            if (editForm == null) return;

            _openCategoryCard = card;
            _categoryEditId = cat.Id;

            SetTextBoxByTag(card, "CatLabel", cat.Label);

            // Select color in inline swatches
            var swatchPanel = FindChildByTag<WrapPanel>(card, "cat-color-swatches");
            if (swatchPanel != null)
            {
                if (swatchPanel.Children.Count == 0)
                {
                    BuildColorSwatches(swatchPanel, c =>
                    {
                        _selectedCategoryColor = c;
                        foreach (var child in swatchPanel.Children.OfType<Border>())
                        {
                            var isSelected = child.Tag is string tag && tag == c;
                            child.BorderThickness = new Avalonia.Thickness(isSelected ? 2.5 : 0);
                            child.BorderBrush = isSelected ? Brushes.White : null;
                        }
                    });
                }

                _selectedCategoryColor = cat.Color;
                foreach (var child in swatchPanel.Children.OfType<Border>())
                {
                    var isSelected = child.Tag is string tag && tag == cat.Color;
                    child.BorderThickness = new Avalonia.Thickness(isSelected ? 2.5 : 0);
                    child.BorderBrush = isSelected ? Brushes.White : null;
                }
            }

            editForm.MaxHeight = 400;
        }

        private void CloseCategoryInlineCard()
        {
            if (_openCategoryCard != null)
            {
                var editForm = FindChildByTag<Border>(_openCategoryCard, "cat-edit-form");
                if (editForm != null) editForm.MaxHeight = 0;
                _openCategoryCard = null;
                _categoryEditId = null;
            }
        }

        private void ToggleCategoryAddClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_categoryAddOpen) { CloseCategoryForm(); return; }
            CloseCategoryInlineCard();

            NewCategoryLabelInput.Text = string.Empty;
            SelectCategoryColor(PresetColors[0]);

            CategoryAddForm.MaxHeight = 400;
            _categoryAddOpen = true;
        }

        private async void SaveCategoryFormClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var label = NewCategoryLabelInput.Text?.Trim();
            if (string.IsNullOrEmpty(label)) return;

            await using var db = new DataContext();
            var userId = CurrentUserId;
            if (userId == Guid.Empty)
            {
                var user = await db.User.FirstOrDefaultAsync();
                if (user == null) return;
                userId = user.Id;
            }

            db.ProfileCategory.Add(new DBProfileCategory
            {
                Id = Guid.NewGuid(),
                Label = label,
                Color = _selectedCategoryColor,
                UserId = userId
            });
            await db.SaveChangesAsync();

            CloseCategoryForm();
            await LoadCategories();
        }

        private async void SaveCategoryInlineClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_openCategoryCard == null || !_categoryEditId.HasValue) return;

            var label = FindTextBoxByTag(_openCategoryCard, "CatLabel")?.Text?.Trim();
            if (string.IsNullOrEmpty(label)) return;

            await using var db = new DataContext();
            var entity = await db.ProfileCategory.FindAsync(_categoryEditId.Value);
            if (entity != null)
            {
                entity.Label = label;
                entity.Color = _selectedCategoryColor;
                await db.SaveChangesAsync();
            }

            CloseCategoryInlineCard();
            await LoadCategories();
        }

        private void CancelCategoryInlineClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            CloseCategoryInlineCard();
        }

        private async void DeleteCategoryInlineClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (!_categoryEditId.HasValue) return;

            await using var db = new DataContext();
            var entity = await db.ProfileCategory.FindAsync(_categoryEditId.Value);
            if (entity != null)
            {
                db.ProfileCategory.Remove(entity);
                await db.SaveChangesAsync();
            }

            CloseCategoryInlineCard();
            await LoadCategories();
        }

        private void CloseNewCategoryClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
            => CloseCategoryForm();

        private void CloseCategoryForm()
        {
            CategoryAddForm.MaxHeight = 0;
            _categoryAddOpen = false;
        }

        // ── Couleurs ───────────────────────────────────────────────────────────

        private void BuildColorSwatches(WrapPanel panel, Action<string> onSelect)
        {
            foreach (var color in PresetColors)
            {
                var c = color;
                var swatch = new Border
                {
                    Width = 28,
                    Height = 28,
                    CornerRadius = new Avalonia.CornerRadius(14),
                    Background = new SolidColorBrush(Color.Parse(c)),
                    Margin = new Avalonia.Thickness(0, 0, 8, 8),
                    Cursor = new Cursor(StandardCursorType.Hand),
                    Tag = c,
                };
                swatch.PointerPressed += (_, _) => onSelect(c);
                panel.Children.Add(swatch);
            }
        }

        private void SelectCategoryColor(string color)
        {
            _selectedCategoryColor = color;
            foreach (var child in NewCategoryColorSwatches.Children.OfType<Border>())
            {
                var isSelected = child.Tag is string tag && tag == color;
                child.BorderThickness = new Avalonia.Thickness(isSelected ? 2.5 : 0);
                child.BorderBrush = isSelected ? Brushes.White : null;
            }
        }

        // ── Utilitaires ────────────────────────────────────────────────────────

        private static bool IsInsideButton(Avalonia.Visual? v)
        {
            var current = v;
            while (current != null)
            {
                if (current is Button) return true;
                current = current.GetVisualParent() as Avalonia.Visual;
            }
            return false;
        }

        private static bool IsInsideTaggedElement(Avalonia.Visual? v, string tag)
        {
            var current = v;
            while (current != null)
            {
                if (current is Control ctrl && ctrl.Tag is string t && t == tag) return true;
                current = current.GetVisualParent() as Avalonia.Visual;
            }
            return false;
        }

        /// <summary>Finds the first descendant of type T whose Tag equals the given string.</summary>
        private static T? FindChildByTag<T>(Avalonia.Visual parent, string tag) where T : Control
        {
            foreach (var child in parent.GetVisualDescendants())
            {
                if (child is T ctrl && ctrl.Tag is string t && t == tag)
                    return ctrl;
            }
            return null;
        }

        /// <summary>Finds a TextBox inside a visual whose Tag equals the given string.</summary>
        private static TextBox? FindTextBoxByTag(Avalonia.Visual parent, string tag)
            => FindChildByTag<TextBox>(parent, tag);

        /// <summary>Sets the Text of a TextBox found by Tag inside the given parent.</summary>
        private static void SetTextBoxByTag(Avalonia.Visual parent, string tag, string? value)
        {
            var box = FindTextBoxByTag(parent, tag);
            if (box != null) box.Text = value ?? string.Empty;
        }

        // ── Thème ─────────────────────────────────────────────────────────────

        private void ThemeToggleChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ThemeManager.SetTheme(ThemeToggle.IsChecked == true);
        }

        private void PasswordCheckToggleChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _passwordCheckEnabled = PasswordCheckToggle.IsChecked == true;
            _ = LoadProfiles();
        }

        // ── Test logger ───────────────────────────────────────────────────────

        private void TestErrorClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                int zero = 0;
                _ = 1 / zero;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        // ── Import dictionnaire ────────────────────────────────────────────────

        private async void ImportDictionaryClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Importer un CSV de mots",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("CSV") { Patterns = new[] { "*.csv", "*.txt" } }
                }
            });

            if (files.Count == 0) return;

            await using var stream = await files[0].OpenReadAsync();
            using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var content = await reader.ReadToEndAsync();

            var words = content
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.Trim())
                .Where(w => w.Length > 0 && Regex.IsMatch(w, @"^[a-zA-Z0-9\s\-_']+$"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (words.Count == 0) return;

            await using var db = new DataContext();
            var entries = words.Select(w => new DBDictionary { Id = Guid.NewGuid(), Word = w }).ToList();
            await db.Dictionary.AddRangeAsync(entries);
            await db.SaveChangesAsync();

            ImportSuccessOverlay.IsVisible = true;
            await Task.Delay(16);
            ImportSuccessOverlay.Opacity = 1;
            await Task.Delay(1200);
            ImportSuccessOverlay.Opacity = 0;
            await Task.Delay(300);
            ImportSuccessOverlay.IsVisible = false;
        }
    }
}
