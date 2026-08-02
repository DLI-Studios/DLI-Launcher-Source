using System;

namespace DLI.Connect.Services.Interfaces;

public interface IThemeManager
{
    string CurrentTheme { get; }
    event Action? ThemeChanged;
    void Apply(string theme); // dark | light | system
}
