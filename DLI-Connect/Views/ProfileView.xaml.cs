using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace DLI.Connect.Views;

public partial class ProfileView : UserControl
{
    public ProfileView()
    {
        InitializeComponent();
    }

    private void OnChangeAvatarClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ViewModels.ProfileViewModel vm) return;

        var dialog = new OpenFileDialog
        {
            Title = "Avatar seç",
            Filter = "Resim dosyaları (*.png;*.jpg;*.jpeg;*.webp;*.bmp)|*.png;*.jpg;*.jpeg;*.webp;*.bmp"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var bytes = File.ReadAllBytes(dialog.FileName);
            vm.ApplyAvatarBytes(bytes);
        }
        catch
        {
            vm.ShowAvatarError();
        }
    }
}
