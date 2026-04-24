using System;
using System.Linq;
using Avalonia;
using Avalonia.Markup.Xaml.Styling;

namespace Passall.Utils;

public static class ThemeManager
{
    public static bool IsDark { get; private set; }

    /// <summary>
    /// Swaps the active theme resource dictionary at runtime.
    /// All DynamicResource bindings in styles.axaml update automatically.
    /// </summary>
    public static void SetTheme(bool isDark)
    {
        IsDark = isDark;
        var app = Application.Current!;

        // Remove the currently active theme dict (glass or dark)
        var old = app.Resources.MergedDictionaries
            .OfType<ResourceInclude>()
            .FirstOrDefault(r => r.Source != null &&
                (r.Source.AbsoluteUri.Contains("LightTheme") ||
                 r.Source.AbsoluteUri.Contains("DarkTheme")));
        if (old != null)
            app.Resources.MergedDictionaries.Remove(old);

        var uri = new Uri(isDark
            ? "avares://Pass-all/Styles/DarkTheme.axaml"
            : "avares://Pass-all/Styles/LightTheme.axaml");
        app.Resources.MergedDictionaries.Add(new ResourceInclude(uri) { Source = uri });
    }
}
