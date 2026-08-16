using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using DLI.Connect.ViewModels;

namespace DLI.Connect.Views;

public partial class MessagesView : UserControl
{
    private MessagesViewModel? _vm;
    private double _olderScrollableBefore;

    public MessagesView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm != null)
        {
            _vm.PropertyChanged -= OnVmPropertyChanged;
            _vm.ScrollToBottomRequested -= OnScrollToBottomRequested;
            _vm.OlderMessagesLoaded -= OnOlderMessagesLoaded;
        }

        _vm = DataContext as MessagesViewModel;

        if (_vm != null)
        {
            _vm.PropertyChanged += OnVmPropertyChanged;
            _vm.ScrollToBottomRequested += OnScrollToBottomRequested;
            _vm.OlderMessagesLoaded += OnOlderMessagesLoaded;
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MessagesViewModel.HasOpened) && _vm != null && _vm.HasOpened)
        {
            OnScrollToBottomRequested(force: true);
        }
    }

    private void OnScrollToBottomRequested(bool force)
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            var sv = MessagesScroll;
            if (sv == null) return;

            if (!force && sv.ScrollableHeight > 0 && sv.ScrollableHeight - sv.VerticalOffset > 160)
            {
                return;
            }

            sv.ScrollToEnd();
        }));
    }

    private void OnOlderMessagesLoaded()
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            var sv = MessagesScroll;
            if (sv == null) return;

            var growth = sv.ScrollableHeight - _olderScrollableBefore;
            if (growth > 0)
            {
                sv.ScrollToVerticalOffset(sv.VerticalOffset + growth);
            }
        }));
    }

    private void OnLoadOlderClick(object sender, RoutedEventArgs e)
    {
        _olderScrollableBefore = MessagesScroll.ScrollableHeight;
        _vm?.LoadOlderMessagesCommand.Execute(null);
    }

    private void OnDeleteMessageClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: MessageItemViewModel item })
        {
            _vm?.DeleteMessageCommand.Execute(item);
        }
    }

    private void OnRetryMessageClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: MessageItemViewModel item })
        {
            _vm?.RetryMessageCommand.Execute(item);
        }
    }

    private void OnRetryTap(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: MessageItemViewModel item })
        {
            _vm?.RetryMessageCommand.Execute(item);
        }
    }

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && _vm != null)
        {
            _vm.SendMessageCommand.Execute(null);
            e.Handled = true;
        }
    }
}
