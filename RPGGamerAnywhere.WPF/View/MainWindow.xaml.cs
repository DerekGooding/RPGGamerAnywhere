using RPGGamerAnywhere.WPF.ViewModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Controls;

namespace RPGGamerAnywhere.WPF;

public partial class MainWindow : Window
{
    public MainVM MainVM
    {
        set { DataContext = value; }
    }

    public MainWindow()
    {
        InitializeComponent();
        PrepareViewMenu();
        MainVM = new();
    }

    private void PrepareViewMenu()
    {
        foreach (TabItem item in MainTabControl.Items)
        {
            MenuItem menuItem = new()
            {
                Header = item.Header
            };
            menuItem.Click += SelectView_Click;
            menuItem.IsEnabled = item.IsEnabled;
            ViewMenu.Items.Add(menuItem);
        }
    }

    private void SelectView_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem)
            foreach (TabItem item in MainTabControl.Items)
                if (menuItem.Header == item.Header)
                    MainTabControl.SelectedItem = item;
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

    private void About_Click(object sender, RoutedEventArgs e)
    {
        string message = $"{Application.Current.MainWindow.Title}\n" +
                         $"Created by: Derek Gooding\n" +
                         $"©2023\n" +
                         $"Libertas Infinitum";

        MessageBox.Show(message);
    }

    private void GitHub_Click(object sender, RoutedEventArgs e) => OpenBrowser("https://github.com/DerekGooding/RPGGamers-Radio-Premium");

    private void RadioSite_Click(object sender, RoutedEventArgs e) => OpenBrowser("http://www.rpgamers.net/radio/");

    public static void OpenBrowser(string url)
    {
        try
        {
            Process.Start(url);
        }
        catch
        {
            // hack because of this: https://github.com/dotnet/corefx/issues/10361
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                throw;
            }
        }
    }

    private void ShowDownloadPath_Click(object sender, RoutedEventArgs e)
    {
        string userRoot = Environment.GetEnvironmentVariable("USERPROFILE") ?? "C:\\";
        string downloadFolder = Path.Combine(userRoot, "Downloads");
        MessageBox.Show(downloadFolder);
    }

    private void Rectangle_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is UIElement rectangle)
        {
            var mousePosition = e.MouseDevice.GetPosition(rectangle);
            if (((MainWindow)Application.Current.MainWindow).MyPlayer is MediaElement element)
                element.Position = element.NaturalDuration.TimeSpan / (800 / mousePosition.X);
        }
    }

    private void Fix_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        foreach (var item in Resources.Values)
        {
            if (item is MainVM vm)
            {
                new Thread(async () => await MainVM.FixSongInfo(vm.SelectedSong)).Start();
            }
        }
    }
}