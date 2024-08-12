using NAudio.WaveFormRenderer;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RPGGamerAnywhere.WPF.ViewModel.Helpers;

public static class AudioHelper
{
    public static ImageSource UpdateGraphic(string filePath)
    {
        RenderWaveform(filePath);
        return source;
    }

    private static ImageSource source;
    private static string imageFile;
    private static readonly WaveFormRenderer _waveFormRenderer = new();
    private static readonly WaveFormRendererSettings _standardSettings = new StandardWaveFormRendererSettings();

    private static IPeakProvider GetPeakProvider() => new MaxPeakProvider();

    private static WaveFormRendererSettings GetRendererSettings() => _standardSettings;

    private static void RenderWaveform(string filePath)
    {
        var settings = GetRendererSettings();
        if (imageFile != null)
        {
            settings.BackgroundImage = new Bitmap(imageFile);
        }
        //pictureBox1.Image = null;
        //labelRendering.Visible = true;
        //Enabled = false;
        var peakProvider = GetPeakProvider();
        Task.Factory.StartNew(() => RenderThreadFunc(peakProvider, settings, filePath));
    }

    private static void RenderThreadFunc(IPeakProvider peakProvider, WaveFormRendererSettings settings, string filePath)
    {
        Image? image = null;
        try
        {
            using var waveStream = new NAudio.Wave.WaveFileReader(filePath);
            image = _waveFormRenderer.Render(waveStream, new MaxPeakProvider(), new StandardWaveFormRendererSettings());
        }
        catch { }
        new Thread(() => FinishedRender(image)).Start();
    }

    private static void FinishedRender(Image? image)
    {
        if (image == null) return;
        using var ms = new MemoryStream();
        image.Save(ms, ImageFormat.Bmp);
        ms.Seek(0, SeekOrigin.Begin);

        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.StreamSource = ms;
        bitmapImage.EndInit();

        source = bitmapImage;
        //labelRendering.Visible = false;
        //source = image;
        //Enabled = true;
    }
}