using NAudio.Wave;
using NAudio.WaveFormRenderer;
using RPGGamerAnywhere.Helpers;
using RPGGamerAnywhere.Model.Database;
using RPGGamerAnywhere.Model.Settings;
using RPGGamerAnywhere.WPF.ViewModel.Commands;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace RPGGamerAnywhere.WPF.ViewModel
{
    public partial class MainVM : ViewModelBase
    {
        public const string ROOT = "http://www.rpgamers.net/radio/data/";

        private readonly WaveFormRenderer _waveFormRenderer = new();
        public ImageSource? WaveFormSource { get => _waveFormSource; set => SetProperty(ref _waveFormSource, value); }
        public double FillWidth { get => _fillWidth; set => SetProperty(ref _fillWidth, value); }

        private string _status = string.Empty;
        public string Status { get => _status; set => SetProperty(ref _status, value.Length == 0 ? "Ready" : value); }

        private string _songCount = string.Empty;
        public string SongCount { get => _songCount; set => SetProperty(ref _songCount, value.Length == 0 ? "No Songs" : value); }

        private string _duration = string.Empty;
        public string Duration { get => _duration; set => SetProperty(ref _duration, value); }

        private double _volume = 0.5;
        public double Volume
        {
            get => _volume;
            set
            {
                SetProperty(ref _volume, value);
                SetVolume();
            }
        }

        private string _search = string.Empty;
        public string Search
        {
            get => _search;
            set
            {
                SetProperty(ref _search, value);
                Query();
            }
        }

        private Song? _selectedSong = null;
        public Song? SelectedSong
        {
            get => _selectedSong;
            set
            {
                SetProperty(ref _selectedSong, value);
                if (value != null)
                    PlaySong(value);
            }
        }

        private Playlist? _selectedPlaylist;
        public Playlist? SelectedPlaylist { get => _selectedPlaylist; set => SetProperty(ref _selectedPlaylist, value); }

        private bool _isPlaying;
        public bool IsPlaying { get => _isPlaying; set => SetProperty(ref _isPlaying, value); }

        private bool _isRequesting = true;
        public bool IsRequesting { get => _isRequesting; set => SetProperty(ref _isRequesting, value); }

        #region Commands
        public SaveVolumeCommand SaveVolumeCommand { get; }
        public SearchLinksCommand SearchLinksCommand { get; }
        public DownloadCommand DownloadCommand { get; }
        public DownloadAllCommand DownloadAllCommand { get; }
        public PreviousCommand PreviousCommand { get; }
        public NextCommand NextCommand { get; }
        public PauseCommand PauseCommand { get; }
        public CreatePlaylistCommand CreatePlaylistCommand { get; }
        public FixTitleCommand FixTitleCommand { get; }
        public ClearRequestsCommand ClearRequestsCommand { get; }
        #endregion

        #region ObservableCollections
        public ObservableCollection<Song> FoundLinks { get; }
        public ObservableCollection<Playlist> Playlists { get; }
        public ObservableCollection<Song> RequestedSongs { get; }
        #endregion

        private List<Song> _filteredSongs = [];
        public List<Song> FilteredSongs { get => _filteredSongs; set => SetProperty(ref _filteredSongs, value); }

        public Stack<Song> previousSongs = new();

        public MainVM()
        {
            Status = string.Empty;

            FoundLinks = [];
            RequestedSongs = [];
            Playlists = [];

            SaveVolumeCommand = new(this);
            SearchLinksCommand = new(this);
            DownloadCommand = new(this);
            PreviousCommand = new(this);
            NextCommand = new(this);
            PauseCommand = new(this);
            CreatePlaylistCommand = new(this);
            DownloadAllCommand = new(this);
            FixTitleCommand = new(this);
            ClearRequestsCommand = new(this);

            DatabaseHelper.InitializeFolder();

            ReadSongs();
            ReadPlaylists();
            StartTimer();
            ReadPreferences();

            //Connect to twitch
            //BotManager.Init(TwitchInfo.Token, TwitchInfo.Name, this, TwitchInfo.Channel);
        }

        public void SongRequest(string request)
        {
            if (!IsRequesting) return;
            var possibleSongs = FoundLinks.Where(x => x.Title?.ToLower().Contains(request, StringComparison.CurrentCultureIgnoreCase) == true);
            if (possibleSongs.Any())
                AddSongRequest(possibleSongs.First());
        }

        private void AddSongRequest(Song song)
        {
            _popups.Push("Song added to requests");
            Application.Current.Dispatcher.Invoke(() => RequestedSongs.Add(song));
        }

        public void ClearRequests() => RequestedSongs.Clear();

        public void FixTitle()
        {
            new Thread(async () =>
            {
                foreach (var song in DatabaseHelper.Read<Song>(DatabaseHelper.Target.Database))
                    await FixSongInfo(song);
                ReadSongs();
            }).Start();
        }

        public static async Task FixSongInfo(Song? song)
        {
            if (song == null) return;

            const string filePath = @"D:\temp.mp3";
            File.Delete(filePath);
            HttpClient client = new();
            await using var stream = await client.GetStreamAsync(song.Url);
            await using var fileStream = new FileStream(filePath, FileMode.CreateNew);
            await stream.CopyToAsync(fileStream);
            fileStream.Close();
            TagLib.File tagFile = TagLib.File.Create(filePath);
            string title = tagFile.Tag.Title;
            string game = tagFile.Tag.Album;

            song.Title = title;
            song.Game = game;
            DatabaseHelper.Update(song, DatabaseHelper.Target.Database);

            //using HttpClient client = new();
            //HttpResponseMessage response = await client.GetAsync(song.Url);
            //Stream streamToReadFrom = await response.Content.ReadAsStreamAsync();
            //using StreamReader sr = new(streamToReadFrom, Encoding.UTF8);
            //string? info = sr.ReadLine();
            //info += sr.ReadLine();
            //info = Decode(info);
            ////MessageBox.Show(info);

            //var gameSplit = info.Split("TALB", StringSplitOptions.None);
            //string game = string.Empty;
            //if (gameSplit.Length < 2)
            //    game = gameSplit[0];
            //else
            //    game = gameSplit[1].Split("TPE1", StringSplitOptions.None)[0];

            //var titleSplit = info.Split("TIT2", StringSplitOptions.None);
            //string title = string.Empty;
            //if (titleSplit.Length < 2)
            //    title = titleSplit[0];
            //else
            //    title = titleSplit[1].Split("TRCK", StringSplitOptions.None)[0];

            //string removeAtStart = "ID3vTALB";
            //if (title.Contains(removeAtStart))
            //    title = title.Replace(removeAtStart, "");
            //if (game.Contains(removeAtStart))
            //    game = game.Replace(removeAtStart, "");

            //string[] cut = { "TALB", "TXXX", "TRCK", "TPE1", "TYER", "OS", "Xing", "WXXX", "TCON", "TDRC", "COMM" };
            //foreach(var item in cut)
            //{
            //    if(title.Contains(item))
            //        title = title.Split(item, StringSplitOptions.None)[0];
            //    if (game.Contains(item))
            //        game = game.Split(item, StringSplitOptions.None)[0];
            //}

            ////var message = $"Game = {game}\n" +
            ////              $"Title = {title}";
            ////if (MessageBox.Show(message, "Question", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            ////{
            //    song.Title = title;
            //    song.Game = game;
            //    DatabaseHelper.Update(song, DatabaseHelper.Target.Database);
            //    //ReadSongs();
            ////}
        }

        private void ReadPreferences()
        {
            var preferences = DatabaseHelper.Read<UserPreference>(DatabaseHelper.Target.Preferences);
            double? volume = preferences.FirstOrDefault(x => x?.Name == SettingNames.Volume, null)?.Percent;
            Volume = volume != null ? (double)volume : 0.25;
        }

        private void ReadPlaylists()
        {
            Playlists.Clear();
            var allPlaylists = DatabaseHelper.Read<Playlist>(DatabaseHelper.Target.Preferences);
            if (allPlaylists.Count == 0)
                return;
            foreach (var item in allPlaylists)
                Playlists.Add(item);
        }

        private void ReadSongs()
        {
            FoundLinks.Clear();
            var allSongs = DatabaseHelper.Read<Song>(DatabaseHelper.Target.Database);
            if (allSongs.Count == 0)
            {
                DatabaseHelper.ImportFromOnlineAsync();
                allSongs = DatabaseHelper.Read<Song>(DatabaseHelper.Target.Database);
            }
            if (allSongs.Count == 0)
                return;
            foreach (var item in allSongs)
                FoundLinks.Add(item);
            Query();
        }

        public void Query()
        {
            FilteredSongs = [.. FoundLinks.
                    Where(x => x.Game?.ToLower().Contains(Search, StringComparison.CurrentCultureIgnoreCase) == true).OrderBy(s => s.Url)];
            SongCount = $"{FilteredSongs.Count} songs";
        }

        public Task LookForLinksAsync()
        {
            string dataUrl = Path.Combine(ROOT, "data_");
            for (int i = 0; i < 4000; i++)
            {
                string digit = i.ToString();
                char dataNumber = digit[0];
                while (digit.Length < 4)
                    digit = "0" + digit;
                string url = $"{dataUrl}{dataNumber}/{digit}.dat";
                _ = ReadSongInfoAsync(url, i);
            }
            return Task.CompletedTask;
        }

        private async Task ReadSongInfoAsync(string url, int id)
        {
            using HttpClient client = new();
            HttpResponseMessage response = await client.GetAsync(url);
            Stream streamToReadFrom = await response.Content.ReadAsStreamAsync();
            using StreamReader sr = new(streamToReadFrom, Encoding.UTF8);
            string line = sr.ReadLine() ?? "";

            line = Decode(line);

            if (line.Contains("DOCTYPE"))
                return;

            var gameSplit = line.Split("TALB", StringSplitOptions.None);
            string game = string.Empty;
            if (gameSplit.Length < 2)
                game = gameSplit[0];
            else
                game = gameSplit[1].Split("TPE1", StringSplitOptions.None)[0];

            var titleSplit = line.Split("TIT2", StringSplitOptions.None);
            string title = string.Empty;
            if (titleSplit.Length < 2)
                title = titleSplit[0];
            else
                title = titleSplit[1].Split("TRCK", StringSplitOptions.None)[0];

            Song song = new()
            {
                Id = id,
                Url = url,
                Title = title,
                Game = game
            };

            var allSongs = DatabaseHelper.Read<Song>(DatabaseHelper.Target.Database);
            if (!allSongs.Select(x => x.Url).Contains(url))
                DatabaseHelper.Insert(song, DatabaseHelper.Target.Database);
            ReadSongs();
        }

        private static string Decode(string input) => MyRegex().Replace(input, string.Empty);

        private bool _subscribed;

        private void PlaySong(Song? song) => PlaySong(song, false);

        private void PlaySong(Song? song, bool isPrevious)
        {
            //new Thread(async () =>
            //{
            //    await LogSongInfoAsync(song);
            //}).Start();

            if (song == null || song.Url == null) return;
            if (((MainWindow)Application.Current.MainWindow).MyPlayer is MediaElement element)
            {
                element.Source = new Uri(song.Url);
                element.Play();
                SetVolume();
                if (!isPrevious)
                    previousSongs.Push(song);
                CheckHistory();
                Status = $"{song.Game} | {song.Title}";

                IsPlaying = true;
                if (!_subscribed)
                {
                    element.MediaEnded += Element_MediaEnded;
                    _subscribed = true;
                }
                WaveFormSource = new BitmapImage();
                new Thread(() =>
                {
                    using var waveStream = new WaveFileReader(song.Url);
                    var image = _waveFormRenderer.Render(waveStream, new MaxPeakProvider(), new StandardWaveFormRendererSettings() { Width = 1650 });
                    if (image == null) return;
                    using var ms = new MemoryStream();
                    image.Save(ms, ImageFormat.Bmp);
                    ms.Seek(0, SeekOrigin.Begin);

                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = ms;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    Dispatcher.CurrentDispatcher.Invoke(() => WaveFormSource = bitmapImage);
                }).Start();
            }
        }

        //private static async Task LogSongInfoAsync(Song? song)
        //{
        //    if(song == null) return;
        //    using HttpClient client = new();
        //    HttpResponseMessage response = await client.GetAsync(song.Url);
        //    Stream streamToReadFrom = await response.Content.ReadAsStreamAsync();
        //    using StreamReader sr = new(streamToReadFrom, Encoding.UTF8);
        //    string info = sr.ReadLine();
        //    info += sr.ReadLine();
        //    //info += sr.ReadLine();
        //    info = Decode(info);
        //    using StreamWriter sw = new (@"D:\\log.txt", true);
        //    sw.WriteLine("NEW");
        //    sw.WriteLine(info);
        //}

        private void CheckHistory()
        {
            if (previousSongs.Count < 50) return;
            Stack<Song> temp = new();
            for (int i = 0; i < 10; i++)
                temp.Push(previousSongs.Pop());
            previousSongs.Clear();
            for (int i = 0; i < 10; i++)
                previousSongs.Push(temp.Pop());
        }

        private void SetVolume()
        {
            if (((MainWindow)Application.Current.MainWindow).MyPlayer is MediaElement element)
                element.Volume = Volume;
        }

        public void SetVolumePreference()
        {
            SetPreference(SettingNames.Volume, Volume);
            _popups.Push("Volume Level Saved");
        }

        private void StartTimer()
        {
            DispatcherTimer timer = new()
            {
                Interval = TimeSpan.FromMilliseconds(20)
            };
            timer.Tick += TimerTick;
            timer.Start();
            DispatcherTimer updater = new()
            {
                Interval = TimeSpan.FromMilliseconds(10)
            };
            updater.Tick += Update;
            updater.Start();
        }

        private void TimerTick(object? sender, EventArgs e)
        {
            if (Application.Current.MainWindow != null
                && ((MainWindow)Application.Current.MainWindow).MyPlayer is MediaElement element
                && element.Source != null
                && element.NaturalDuration.HasTimeSpan)
            {
                Duration = string.Format("{0} / {1}", element.Position.ToString(@"mm\:ss"), element.NaturalDuration.TimeSpan.ToString(@"mm\:ss"));
                FillWidth = element.Position.TotalMilliseconds / element.NaturalDuration.TimeSpan.TotalMilliseconds * 800;
            }
        }

        private void Element_MediaEnded(object sender, RoutedEventArgs e) => PlayRandomSong();

        public void PlayRandomSong()
        {
            if (RequestedSongs.Count > 0)
            {
                var song = RequestedSongs.First();

                if (FilteredSongs.Any(s => s.Id == song.Id))
                {
                    SelectedSong = FilteredSongs.First(s => s.Id == song.Id);
                    RequestedSongs.Remove(RequestedSongs.First());
                }
            }
            else
            {
                Random rand = new();
                SelectedSong = FilteredSongs[rand.Next(FilteredSongs.Count)];
            }
            PlaySong(SelectedSong);
        }

        public void PlayPrevious()
        {
            if (previousSongs.Count == 0) return;
            PlaySong(previousSongs.Pop(), true);
        }

        public static async Task SaveSong(Song? song)
        {
            if (song == null) return;
            string userRoot = Environment.GetEnvironmentVariable("USERPROFILE") ?? "C:\\";
            string downloadFolder = Path.Combine(userRoot, "Downloads", $"{song.Title}.mp3");

            MessageBox.Show($"Downloading to:\n{downloadFolder}");
            HttpClient client = new();
            await using Stream stream = await client.GetStreamAsync(song.Url);
            await using FileStream fileStream = new(downloadFolder, FileMode.CreateNew);
            await stream.CopyToAsync(fileStream);
        }

        public void Pause()
        {
            if (((MainWindow)Application.Current.MainWindow).MyPlayer is MediaElement element)
            {
                if (IsPlaying)
                {
                    element.Pause();
                    IsPlaying = false;
                }
                else
                {
                    if (SelectedSong == null) PlayRandomSong();
                    element.Play();
                    IsPlaying = true;
                }
            }
        }

        public void CreatePlaylist()
        {
            DatabaseHelper.Insert(new Playlist { Name = "New playlist" }, DatabaseHelper.Target.Preferences);
            ReadPlaylists();
        }

        #region SetPreferences

        private static void SetPreference(string preferenceName, bool value)
        {
            var target = DatabaseHelper
                .Read<UserPreference>(DatabaseHelper.Target.Preferences)
                .FirstOrDefault(x => x.Name == preferenceName, CreatePreference(preferenceName));
            if (target == null) return;
            target.IsTrue = value;
            DatabaseHelper.Update(target, DatabaseHelper.Target.Preferences);
        }

        private static void SetPreference(string preferenceName, double value)
        {
            var target = DatabaseHelper
                .Read<UserPreference>(DatabaseHelper.Target.Preferences)
                .FirstOrDefault(x => x.Name == preferenceName, CreatePreference(preferenceName));
            if (target == null) return;
            target.Percent = value;
            DatabaseHelper.Update(target, DatabaseHelper.Target.Preferences);
        }

        private static void SetPreference(string preferenceName, int value)
        {
            var target = DatabaseHelper
                .Read<UserPreference>(DatabaseHelper.Target.Preferences)
                .FirstOrDefault(x => x.Name == preferenceName, CreatePreference(preferenceName));
            if (target == null) return;
            target.Value = value;
            DatabaseHelper.Update(target, DatabaseHelper.Target.Preferences);
        }

        private static UserPreference CreatePreference(string name)
        {
            var preferences = new UserPreference { Name = name };
            DatabaseHelper.Insert(preferences, DatabaseHelper.Target.Preferences);
            return preferences;
        }
        #endregion SetPreferences

        public double PopupHeight { get => _popupHeight; set => SetProperty(ref _popupHeight, value); }
        public double PopupFontSize { get => _popupFontSize; set => SetProperty(ref _popupFontSize, value); }
        public string PopupText { get => _popupText; set => SetProperty(ref _popupText, value); }

        private readonly Stack<string> _popups = new();

        private enum PopupState
        {
            Waiting,
            Expanding,
            Collapsing,
            Display
        }

        private PopupState _popupState = PopupState.Waiting;
        private double _popupHeight = -40;
        private double _popupFontSize = 1;
        private string _popupText = string.Empty;
        private int _popupDisplayTick = 0;
        private double _fillWidth;
        private ImageSource? _waveFormSource;

        private void Update(object? sender, EventArgs e)
        {
            if (_popupState == PopupState.Waiting && _popups.Count != 0)
            {
                _popupState = PopupState.Expanding;
                PopupText = _popups.Pop();
            }
            else if (_popupState == PopupState.Expanding)
            {
                PopupHeight += 1.2;
                PopupFontSize += 0.5;
                if (PopupHeight >= 0)
                    _popupState = PopupState.Display;
            }
            else if (_popupState == PopupState.Collapsing)
            {
                PopupHeight -= 1.2;
                PopupFontSize -= 0.5;
                if (PopupHeight <= -30)
                    _popupState = PopupState.Waiting;
            }
            else if (_popupState == PopupState.Display)
            {
                if (_popupDisplayTick++ == 20)
                {
                    _popupState = PopupState.Collapsing;
                    _popupDisplayTick = 0;
                }
            }
        }

        public void DownloadAll()
        {
            new Thread(async () =>
            {
                await DatabaseHelper.Refresh();
                ReadSongs();
            }).Start();
        }

        //public void DownloadAll()
        //{
        //    Status = "Download all songs....";
        //    new Thread(async () =>
        //    {
        //        HttpClient client = new();
        //        string userRoot = @"D:\\";
        //        int count = 0;
        //        foreach (var song in FoundLinks)
        //        {
        //            if (song == null) return;

        //            string downloadFolder = Path.Combine(userRoot, "Radio", $"song_{count++}.mpeg");

        //            //MessageBox.Show($"Downloading to:\n{downloadFolder}");

        //            using var stream = await client.GetStreamAsync(song.Url);
        //            using var fileStream = new FileStream(downloadFolder, FileMode.CreateNew);
        //            await stream.CopyToAsync(fileStream);
        //        }
        //    }).Start();
        //    Status = string.Empty;
        //}

        [GeneratedRegex("[^\\u0020-\\u007E]")]
        private static partial Regex MyRegex();
    }
}