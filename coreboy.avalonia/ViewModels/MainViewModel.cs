using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using coreboy.controller;
using coreboy.gui;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Button = coreboy.controller.Button;

namespace coreboy.avalonia.ViewModels;

public partial class MainViewModel : ObservableObject, IController
{
    [ObservableProperty]
    private SKBitmap bitmap;

    private readonly Emulator emulator;
    private readonly GameboyOptions gbOptions;
    private CancellationTokenSource cancellationSource;
    private IButtonListener listener;
    private readonly object updateLock = new();
    private readonly Dictionary<Key, Button> controls;


    public MainViewModel()
    {
        cancellationSource = new CancellationTokenSource();
        gbOptions = new GameboyOptions();
        emulator = new Emulator(gbOptions);

        controls = new Dictionary<Key, Button>
        {
            { Key.A,  Button.Left },
            { Key.D, Button.Right },
            { Key.W,    Button.Up },
            { Key.S,  Button.Down },
            { Key.E,     Button.A },
            { Key.R,     Button.B },
            { Key.Enter, Button.Start },
            { Key.Back,  Button.Select }
        };

        ConnectEmulatorToPanel();
    }

    public void SetButtonListener(IButtonListener buttonListener)
    {
        listener = buttonListener;
    }

    private void ConnectEmulatorToPanel()
    {
        emulator.Controller = this;
        emulator.Display.OnFrameProduced += UpdateDisplay;

        //KeyDown += EmulatorSurface_KeyDown!;
        //KeyUp += EmulatorSurface_KeyUp!;
        //Closed += (_, _) => { cancellationSource.Cancel(); };
    }

    private void UpdateDisplay(object _, byte[] frameBytes)
    {
        if (!Monitor.TryEnter(updateLock))
        {
            return;
        }

        try
        {
            Bitmap = SKBitmap.Decode(frameBytes);

            // this here might be better?
            //int width = 256;
            //int height = 256;
            //int bytesPerPixel = 4; // For SKColorType.Bgra8888
            //int rowBytes = width * bytesPerPixel;
            //int totalBytes = height * rowBytes;

            //SKImageInfo info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

            //// Create an SKBitmap using the image info
            //SKBitmap bitmap = new SKBitmap(info);
            //// Copy the byte array into the bitmap's pixel buffer
            //IntPtr pixels = bitmap.GetPixels();
            //Marshal.Copy(frameBytes, 0, pixels, totalBytes);

            //Bitmap = bitmap;

        }
        catch
        {
            // YOLO
        }
        finally
        {
            Monitor.Exit(updateLock);
        }
    }

    internal async Task LoadRom(string fileName)
    {
        if (emulator.Active)
        {
            emulator.Stop(cancellationSource);
            cancellationSource = new CancellationTokenSource();
            await Task.Delay(100);
        }

        gbOptions.Rom = fileName;
        emulator.Run(cancellationSource.Token);
    }

    public async Task ScreenShot(string path)
    {
        if (Bitmap == null) return;
        using SKImage image = SKImage.FromBitmap(Bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        await File.WriteAllBytesAsync(path, data.ToArray());
    }

    [RelayCommand]
    public void Pause()
    {
        emulator?.TogglePause();
    }

    internal void KeyDown(KeyEventArgs e)
    {
        if (controls.TryGetValue(e.Key, out var button))
        {
            listener.OnButtonPress(button);
        }
    }

    internal void KeyUp(KeyEventArgs e)
    {
        if (controls.TryGetValue(e.Key, out var button))
        {
            listener.OnButtonRelease(button);
        }
    }

    internal void Close()
    {
        cancellationSource.Cancel();
    }
}
