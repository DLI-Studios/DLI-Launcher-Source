using System.Windows;
using System.Windows.Controls;
using DLI.Connect.ViewModels;

namespace DLI.Connect.Views;

public partial class AddFriendsView : UserControl
{
    public AddFriendsView()
    {
        InitializeComponent();

        Loaded += (_, _) => SearchBox.Focus();
    }
}
