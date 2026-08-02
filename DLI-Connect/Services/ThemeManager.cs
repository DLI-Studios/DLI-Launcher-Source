using System;
using Microsoft.Win32;
using System.Windows;
using DLI.Connect.Services.Interfaces;

namespace DLI.Connect.Services;

public class ThemeManager : IThemeManager
{
    private const string DarkColors = "Themes/Colors.xaml";
    private const string LightColors = "Themes/ColorsLight.xaml";

    public string CurrentTheme { get; private set; } = "dark";

    public event Action? ThemeChanged;

    public void Apply(string theme)
    {
        CurrentTheme = theme;
        var useDark = ResolveDark(theme);

        var app = Application.Current;
        if (app == null) return;

        var merged = app.Resources.MergedDictionaries;
        if (merged.Count == 0) return;

        var colorsUri = useDark ? DarkColors : LightColors;

        // Replace the colors dictionary (index 0); keep styles dictionary.
        merged[0] = new ResourceDictionary { Source = new Uri(colorsUri, UriKind.Relative) };

        ThemeChanged?.Invoke();
    }

    private static bool ResolveDark(string theme) => theme switch
    {
        "light" => false,
        "system" => !SystemUsesLightTheme(),
        _ => true
    };

    private static bool SystemUsesLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int i && i == 1;
        }
        catch
        {
            return false;
        }
    }
}
