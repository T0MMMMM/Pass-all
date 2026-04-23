using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Microsoft.EntityFrameworkCore;
using Passall.Modeles;
using Passall.Utils;

namespace Passall
{
    public partial class MainWindow : Window
    {
        public Guid CurrentUserId { get; set; }

        private bool _profilePasswordVisible;
        private Guid? _editingCategoryId;
        private string _selectedColor = "#CC4285F4";

        private static readonly string[] PresetColors =
        {
            "#CC4285F4", "#CC1DB954", "#CCFF9900", "#CCE50914",
            "#CC5865F2", "#CC00A8D4", "#CCFF6B9D", "#CC8B5CF6",
        };

        public MainWindow()
        {
            InitializeComponent();
            BuildColorSwatches();
            _ = LoadProfiles();
        }

        // ── Profils — liste ────────────────────────────────────────────────────

        private async Task LoadProfiles()
        {
            await using var db = new DataContext();
            var profiles = await db.UserProfile
                .Include(p => p.Category)
                .Where(p => CurrentUserId == Guid.Empty || p.UserId == CurrentUserId)
                .OrderBy(p => p.Name)
                .ToListAsync();

            ProfileItemsControl.ItemsSource = profiles;
            var count = profiles.Count;
            ProfileCountText.Text = count == 1 ? "1 compte" : $"{count} comptes";
        }

        private void ToggleProfilePasswordClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (((Button)sender!).Tag is not DBUserProfile profile) return;
            var btn = (Button)sender;
            if (btn.Parent is not StackPanel actions) return;
            if (actions.Parent is not Grid grid) return;

            var pwdBlock = grid.Children
                .OfType<TextBlock>()
                .FirstOrDefault(tb => Grid.GetColumn(tb) == 2);
            if (pwdBlock == null) return;

            bool isHidden = pwdBlock.Text == "••••••••••••";
            pwdBlock.Text = isHidden
                ? Passall.Utils.Utils.Decrypt(profile.Password) ?? "—"
                : "••••••••••••";

            if (btn.Content is Panel panel)
            {
                var icons = panel.Children.OfType<ContentControl>().ToList();
                if (icons.Count >= 2)
                {
                    icons[0].IsVisible = !isHidden;  // Eye : visible quand caché
                    icons[1].IsVisible = isHidden;   // EyeClosed : visible quand affiché
                }
            }
        }

        private async void CopyProfilePasswordClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (((Button)sender!).Tag is not DBUserProfile profile) return;
            var decrypted = Passall.Utils.Utils.Decrypt(profile.Password) ?? string.Empty;
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
                await clipboard.SetTextAsync(decrypted);
        }

        // ── Profils — formulaire ───────────────────────────────────────────────

        private async void AddProfileClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            await using var db = new DataContext();
            var categories = await db.ProfileCategory.OrderBy(c => c.Label).ToListAsync();

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

            ProfileFormOverlay.IsVisible = true;
        }

        private async void SaveProfileClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
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
                Password = Passall.Utils.Utils.Encrypt(ProfilePasswordInput.Text) ?? string.Empty,
                UserId = userId,
                CategoryId = category.Id,
            });
            await db.SaveChangesAsync();

            ProfileFormOverlay.IsVisible = false;
            await LoadProfiles();
        }

        private void CloseProfileFormClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
            => ProfileFormOverlay.IsVisible = false;

        private void ToggleProfileFormPasswordClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _profilePasswordVisible = !_profilePasswordVisible;
            ProfilePasswordInput.PasswordChar = _profilePasswordVisible ? '\0' : '●';
            ProfileFormEyeShow.IsVisible = !_profilePasswordVisible;
            ProfileFormEyeHide.IsVisible = _profilePasswordVisible;
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
            CategoryView.IsVisible = false;
            MainView.IsVisible = true;
        }

        // ── Overlays ───────────────────────────────────────────────────────────

        private void HelpClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
            => HelpOverlay.IsVisible = true;

        private void CloseHelpClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
            => HelpOverlay.IsVisible = false;

        private void SettingsClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
            => SettingsOverlay.IsVisible = true;

        private void CloseSettingsClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
            => SettingsOverlay.IsVisible = false;

        // ── Catégories — liste ─────────────────────────────────────────────────

        private async Task LoadCategories()
        {
            await using var db = new DataContext();
            var categories = await db.ProfileCategory.OrderBy(c => c.Label).ToListAsync();

            CategoryItemsControl.ItemsSource = categories;

            var count = categories.Count;
            CategoryCountText.Text = count == 1 ? "1 catégorie" : $"{count} catégories";
        }

        private void EditCategoryClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (((Button)sender!).Tag is not DBProfileCategory cat) return;
            _editingCategoryId = cat.Id;
            CategoryFormTitle.Text = "Modifier la catégorie";
            CategoryLabelInput.Text = cat.Label;
            SelectColor(cat.Color);
            CategoryFormOverlay.IsVisible = true;
        }

        private async void DeleteCategoryClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (((Button)sender!).Tag is not DBProfileCategory cat) return;

            await using var db = new DataContext();
            var entity = await db.ProfileCategory.FindAsync(cat.Id);
            if (entity != null)
            {
                db.ProfileCategory.Remove(entity);
                await db.SaveChangesAsync();
            }
            await LoadCategories();
        }

        // ── Catégories — formulaire ────────────────────────────────────────────

        private void AddCategoryClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _editingCategoryId = null;
            CategoryFormTitle.Text = "Nouvelle catégorie";
            CategoryLabelInput.Text = string.Empty;
            SelectColor(PresetColors[0]);
            CategoryFormOverlay.IsVisible = true;
        }

        private async void SaveCategoryClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var label = CategoryLabelInput.Text?.Trim();
            if (string.IsNullOrEmpty(label)) return;

            await using var db = new DataContext();

            if (_editingCategoryId.HasValue)
            {
                var entity = await db.ProfileCategory.FindAsync(_editingCategoryId.Value);
                if (entity != null)
                {
                    entity.Label = label;
                    entity.Color = _selectedColor;
                    await db.SaveChangesAsync();
                }
            }
            else
            {
                db.ProfileCategory.Add(new DBProfileCategory
                {
                    Id = Guid.NewGuid(),
                    Label = label,
                    Color = _selectedColor,
                });
                await db.SaveChangesAsync();
            }

            CategoryFormOverlay.IsVisible = false;
            await LoadCategories();
        }

        private void CloseCategoryFormClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
            => CategoryFormOverlay.IsVisible = false;

        // ── Couleurs ───────────────────────────────────────────────────────────

        private void BuildColorSwatches()
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
                    Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                    Tag = c,
                };
                swatch.PointerPressed += (_, _) => SelectColor(c);
                CategoryColorSwatches.Children.Add(swatch);
            }
        }

        private void SelectColor(string color)
        {
            _selectedColor = color;
            foreach (var child in CategoryColorSwatches.Children.OfType<Border>())
            {
                var isSelected = child.Tag is string tag && tag == color;
                child.BorderThickness = new Avalonia.Thickness(isSelected ? 2.5 : 0);
                child.BorderBrush = isSelected ? Brushes.White : null;
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
