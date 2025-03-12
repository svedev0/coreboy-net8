using System.Net;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using coreboy.avalonia.ViewModels;

namespace coreboy.avalonia.Views;

public partial class MainWindow : Window
{
	public MainWindow()
	{
		InitializeComponent();
	}

	private async void LoadRom_Click(object _, RoutedEventArgs e)
	{
		IStorageProvider? storage = GetTopLevel(this)?.StorageProvider;
		if (storage is null)
		{
			return;
		}

		FilePickerOpenOptions fpOptions = new()
		{
			Title = "Select a ROM",
			FileTypeFilter =
			[
				new FilePickerFileType("Roms")
				{
					Patterns = ["*.gb", "*.gbc"],
				}
			],
			AllowMultiple = false
		};

		IStorageFile[] paths = [.. await storage.OpenFilePickerAsync(fpOptions)];
		if (paths is null || paths.Length == 0)
		{
			return;
		}

		string? absolutePath = paths[0].Path?.AbsolutePath;
		if (string.IsNullOrEmpty(absolutePath))
		{
			return;
		}

		if (DataContext is MainViewModel context)
		{
			await context.LoadRom(WebUtility.UrlDecode(absolutePath));
		}
	}

	private void Quit_Click(object _, RoutedEventArgs e)
	{
		Close();
	}

	private void PlayPause_Click(object _, RoutedEventArgs e)
	{
		if (DataContext is MainViewModel context)
		{
			context.Pause();

			if ($"{PlayPauseMenuItem.Header}".Contains("Pause"))
			{
				PlayPauseMenuItem.Header = "_Play";
			}
			else
			{
				PlayPauseMenuItem.Header = "_Pause";
			}
		}
	}

	private void SetSpeed1x_Click(object _, RoutedEventArgs e)
	{
		if (DataContext is MainViewModel context)
		{
			context.SetEmulationSpeed(1);
		}
	}


	private void SetSpeed2x_Click(object _, RoutedEventArgs e)
	{
		if (DataContext is MainViewModel context)
		{
			context.SetEmulationSpeed(2);
		}
	}

	private void SetSpeed3x_Click(object _, RoutedEventArgs e)
	{
		if (DataContext is MainViewModel context)
		{
			context.SetEmulationSpeed(3);
		}
	}

	private async void Screenshot_Click(object _, RoutedEventArgs e)
	{
		IStorageProvider? storage = GetTopLevel(this)?.StorageProvider;
		if (storage is null)
		{
			return;
		}

		if (DataContext is not MainViewModel context)
		{
			return;
		}
		context.Pause();

		FilePickerSaveOptions fpOptions = new()
		{
			Title = "Image Path",
			DefaultExtension = ".png",
			FileTypeChoices = [
				new FilePickerFileType("PNG")
				{
					Patterns = ["*.png"],
					MimeTypes = ["image/png"]
				}]
		};

		IStorageFile? path = await storage.SaveFilePickerAsync(fpOptions);
		if (path is null)
		{
			return;
		}

		string? absolutePath = path.Path?.AbsolutePath;
		if (string.IsNullOrEmpty(absolutePath))
		{
			return;
		}

		await context.Screenshot(WebUtility.UrlDecode(absolutePath));
		context.Pause();
	}

	protected override void OnClosing(WindowClosingEventArgs e)
	{
		if (DataContext is MainViewModel context)
		{
			context.Close();
			base.OnClosing(e);
		}
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		if (DataContext is MainViewModel context)
		{
			context.KeyDown(e);
			base.OnKeyDown(e);
		}
	}

	protected override void OnKeyUp(KeyEventArgs e)
	{
		if (DataContext is MainViewModel context)
		{
			context.KeyUp(e);
			base.OnKeyUp(e);
		}
	}
}
