using System.Windows.Controls;

namespace DLI.Connect.Services.Interfaces;

public interface INavigationService
{
    Func<UserControl>? HostAccessor { get; set; }
    event Action<AppPage>? PageChanged;
    void Navigate(AppPage page);
}