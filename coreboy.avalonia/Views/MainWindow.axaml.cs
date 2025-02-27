using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using coreboy.avalonia.ViewModels;
using System.Linq;
using System.Net;

namespace coreboy.avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OpenFile_Click(object sender, RoutedEventArgs e)
    {

        var storage = TopLevel.GetTopLevel(this).StorageProvider;

        var path = await storage.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "Select a ROM",
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Roms")
                {
                    Patterns = new[] { "*.gb", "*.gbc" },
                }
            },
            AllowMultiple = false
        });

        if (!string.IsNullOrEmpty(path.FirstOrDefault()?.Path?.AbsolutePath))
        {
            var data = DataContext as MainViewModel;
            await data.LoadRom(WebUtility.UrlDecode(path.FirstOrDefault()?.Path?.AbsolutePath));
        }
    }

    private async void ScreenShot_Click(object sender, RoutedEventArgs e)
    {
        var storage = TopLevel.GetTopLevel(this).StorageProvider;

        var path = await storage.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = "Image Path",
            DefaultExtension = ".png",

        });

        if (!string.IsNullOrEmpty(path?.Path.AbsolutePath))
        {
            var data = DataContext as MainViewModel;
            await data.ScreenShot(WebUtility.UrlDecode(path.Path.AbsolutePath));
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        var data = DataContext as MainViewModel;
        data.Close();
        base.OnClosing(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var data = DataContext as MainViewModel;
        data.KeyDown(e);
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        var data = DataContext as MainViewModel;
        data.KeyUp(e);
        base.OnKeyUp(e);
    }
}
