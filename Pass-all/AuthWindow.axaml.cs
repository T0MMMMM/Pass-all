using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Passall.Modeles;

namespace Passall;

public partial class AuthWindow : Window
{
    private bool _loginPasswordVisible;
    private bool _regPasswordVisible;
    private bool _regConfirmVisible;
    private bool _isAnimating;

    public AuthWindow()
    {
        InitializeComponent();
    }

    // ── Navigation login ↔ register ──────────────────────────────────────────

    private async void GoToRegisterClick(object? sender, RoutedEventArgs e)
    {
        if (_isAnimating) return;
        _isAnimating = true;

        // 1. Fade out login
        LoginPanel.Opacity = 0;
        await Task.Delay(220);

        // 2. Swap visibilité (retire du layout le panel caché)
        LoginPanel.IsVisible = false;
        RegErrorBox.IsVisible = false;
        RegisterPanel.IsVisible = true;
        RegisterPanel.IsHitTestVisible = true;

        // 3. Un frame pour que Avalonia calcule le layout avant de déclencher la transition
        await Task.Delay(16);
        RegisterPanel.Opacity = 1;

        await Task.Delay(220);
        _isAnimating = false;
    }

    private async void GoToLoginClick(object? sender, RoutedEventArgs e)
    {
        if (_isAnimating) return;
        _isAnimating = true;

        RegisterPanel.Opacity = 0;
        await Task.Delay(220);

        RegisterPanel.IsVisible = false;
        LoginErrorBox.IsVisible = false;
        LoginPanel.IsVisible = true;
        LoginPanel.IsHitTestVisible = true;

        await Task.Delay(16);
        LoginPanel.Opacity = 1;

        await Task.Delay(220);
        _isAnimating = false;
    }

    // ── Boutons œil ──────────────────────────────────────────────────────────

    private void ToggleLoginPasswordClick(object? sender, RoutedEventArgs e)
    {
        _loginPasswordVisible = !_loginPasswordVisible;
        LoginPasswordBox.PasswordChar = _loginPasswordVisible ? '\0' : '●';
        LoginEyeShow.IsVisible = !_loginPasswordVisible;
        LoginEyeHide.IsVisible = _loginPasswordVisible;
    }

    private void ToggleRegPasswordClick(object? sender, RoutedEventArgs e)
    {
        _regPasswordVisible = !_regPasswordVisible;
        RegPasswordBox.PasswordChar = _regPasswordVisible ? '\0' : '●';
        RegPasswordEyeShow.IsVisible = !_regPasswordVisible;
        RegPasswordEyeHide.IsVisible = _regPasswordVisible;
    }

    private void ToggleRegConfirmClick(object? sender, RoutedEventArgs e)
    {
        _regConfirmVisible = !_regConfirmVisible;
        RegConfirmBox.PasswordChar = _regConfirmVisible ? '\0' : '●';
        RegConfirmEyeShow.IsVisible = !_regConfirmVisible;
        RegConfirmEyeHide.IsVisible = _regConfirmVisible;
    }

    // ── Connexion ─────────────────────────────────────────────────────────────

    private void LoginClick(object? sender, RoutedEventArgs e)
    {
        string username = LoginUsernameBox.Text?.Trim() ?? "";
        string password = LoginPasswordBox.Text ?? "";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowLoginError("Veuillez remplir tous les champs.");
            return;
        }

        string hashed = Utils.Utils.Hash(password) ?? "";

        using var db = new DataContext();
        var user = db.User.FirstOrDefault(u => u.Login == username && u.Password == hashed);

        if (user == null)
        {
            ShowLoginError("Identifiant ou mot de passe incorrect.");
            return;
        }

        new MainWindow { CurrentUserId = user.Id }.Show();
        Close();
    }

    private void ShowLoginError(string message)
    {
        LoginErrorText.Text = message;
        LoginErrorBox.IsVisible = true;
    }

    // ── Inscription ───────────────────────────────────────────────────────────

    private void RegisterClick(object? sender, RoutedEventArgs e)
    {
        string name     = RegNameBox.Text?.Trim() ?? "";
        string email    = RegEmailBox.Text?.Trim() ?? "";
        string username = RegUsernameBox.Text?.Trim() ?? "";
        string password = RegPasswordBox.Text ?? "";
        string confirm  = RegConfirmBox.Text ?? "";

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) ||
            string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowRegError("Veuillez remplir tous les champs.");
            return;
        }

        if (password != confirm)
        {
            ShowRegError("Les mots de passe ne correspondent pas.");
            return;
        }

        using var db = new DataContext();

        if (db.User.Any(u => u.Login == username))
        {
            ShowRegError("Cet identifiant est déjà utilisé.");
            return;
        }

        var settings = new DBSettings { Id = Guid.NewGuid(), Version = Utils.Constantes.Settings_DefaultVersion };
        var user = new DBUser
        {
            Id         = Guid.NewGuid(),
            Name       = name,
            Email      = email,
            Login      = username,
            Password   = Utils.Utils.Hash(password) ?? "",
            SettingsId = settings.Id,
            Settings   = settings
        };

        db.Settings.Add(settings);
        db.User.Add(user);
        db.SaveChanges();

        new MainWindow { CurrentUserId = user.Id }.Show();
        Close();
    }

    private void ShowRegError(string message)
    {
        RegErrorText.Text = message;
        RegErrorBox.IsVisible = true;
    }

    private void HelpClick(object? sender, RoutedEventArgs e)
    {
        var uri = new Uri(Path.Combine(AppContext.BaseDirectory, "aide", "index.html")).AbsoluteUri + "#auth";
        Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
    }
}
