using NAudio.WaveFormRenderer;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RPGGamerAnywhere.WPF.ViewModel.Helpers;

public static class AudioHelper
{
    public static readonly string SamplePath = Path.Combine(Environment.CurrentDirectory, @"View\Sample\sample-3s.mp3");

    public static ImageSource UpdateGraphic()
    {
        //selectedFile = song.Url;
        RenderWaveform();
        return source;
    }

    private static ImageSource source;
    private static readonly string selectedFile = SamplePath;
    private static string imageFile;
    private static readonly WaveFormRenderer waveFormRenderer = new();
    private static readonly WaveFormRendererSettings standardSettings = new StandardWaveFormRendererSettings();

    private static IPeakProvider GetPeakProvider()
    {
        //switch (comboBoxPeakCalculationStrategy.SelectedIndex)
        //{
        //    case 0:
        //        return new MaxPeakProvider();
        //    case 1:
        //        return new RmsPeakProvider((int)upDownBlockSize.Value);
        //    case 2:
        //        return new SamplingPeakProvider((int)upDownBlockSize.Value);
        //    case 3:
        //        return new AveragePeakProvider(4);
        //    default:
        //        throw new InvalidOperationException("Unknown calculation strategy");
        //}
        return new MaxPeakProvider();
    }

    private static WaveFormRendererSettings GetRendererSettings()
    {
        //var settings = (WaveFormRendererSettings)comboBoxRenderSettings.SelectedItem;
        //settings.TopHeight = (int)upDownTopHeight.Value;
        //settings.BottomHeight = (int)upDownBottomHeight.Value;
        //settings.Width = (int)upDownWidth.Value;
        //settings.DecibelScale = checkBoxDecibels.Checked;
        return standardSettings;
    }

    private static void RenderWaveform()
    {
        if (selectedFile == null) return;
        var settings = GetRendererSettings();
        if (imageFile != null)
        {
            settings.BackgroundImage = new Bitmap(imageFile);
        }
        //pictureBox1.Image = null;
        //labelRendering.Visible = true;
        //Enabled = false;
        var peakProvider = GetPeakProvider();
        Task.Factory.StartNew(() => RenderThreadFunc(peakProvider, settings));
    }

    private static void RenderThreadFunc(IPeakProvider peakProvider, WaveFormRendererSettings settings)
    {
        Image? image = null;
        try
        {
            using var waveStream = new NAudio.Wave.WaveFileReader(selectedFile);
            image = waveFormRenderer.Render(waveStream, new MaxPeakProvider(), new StandardWaveFormRendererSettings());
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