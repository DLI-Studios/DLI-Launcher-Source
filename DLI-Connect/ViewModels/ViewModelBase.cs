using CommunityToolkit.Mvvm.ComponentModel;

namespace DLI.Connect.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            SetProperty(ref _isBusy, value);
            OnPropertyChanged(nameof(IsIdle));
        }
    }

    public bool IsIdle => !_isBusy;

    public virtual void OnNavigatedTo() { }
    public virtual void OnNavigatedFrom() { }
}
