using ABI.Microsoft.UI.Xaml;
using LibVLCSharp.Shared;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Search;
using DispatcherTimer = Microsoft.UI.Xaml.DispatcherTimer;
using RoutedEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;
using Window = Microsoft.UI.Xaml.Window;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicPlayer
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomeWindow : Window
    {

        public HomeWindow()
        {

            InitializeComponent();
            this.ExtendsContentIntoTitleBar = true;
            this.Title = "Vusic Player";
            loadingRing.IsActive = true;
            loadingRing.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            frmMain.Navigated += FrmMain_Navigated;

            Task.Run(() => videoView.Initialized += VideoView_Initialized);
            sldMain.DragStarted += SldMain_DragStarted;

            sldMain.DragCompleted += SldMain_DragCompleted;
            if (nvgMain.MenuItems.Count > 0)
            {
                nvgMain.SelectedItem = nvgMain.MenuItems[0];
            }

        }
        #region Fields

        ObservableCollection<string> queuepaths = new();
        bool _isDragging = false;
        #endregion
        private void SldMain_DragCompleted()
        {
            double newPosition = sldMain.Value / sldMain.Maximum;
            if (_mediaPlayer != null)
            {
                if (_mediaPlayer.State != VLCState.Stopped)
                {
                    _mediaPlayer.Position = (float)newPosition;

                    long currentTimeMs = _mediaPlayer.Time;
                    txtRunningDuration.Text = TimeSpan.FromMilliseconds(currentTimeMs).ToString(@"hh\:mm\:ss");
                    _isDragging = false;
                    _mediaPlayer.Mute = false;
                    if (stateofplay == "playing")
                        maintimer.Start();
                }
                /*  else
                  {
                      _mediaPlayer.Play();
                      _mediaPlayer.Position = (float)newPosition;
                      _mediaPlayer.Pause();
                      maintimer.Stop();
                      stateofplay = "paused";
                      long currentTimeMs = _mediaPlayer.Time;
                      txtRunningDuration.Text = TimeSpan.FromMilliseconds(currentTimeMs).ToString(@"hh\:mm\:ss");
                      _isDragging = false;
                      _mediaPlayer.Mute = false;
                  }*/
            }
        }

        private void SldMain_DragStarted()
        {
            if (_mediaPlayer == null)
                return;

            _mediaPlayer.Mute = true;
            _isDragging = true;
            maintimer.Stop();
        }
        public async void LoadFileFromPath(ObservableCollection<string> path)
        {

            if (!_isLibVLCReady)
            {
                _originalOrder.Clear();
                _originalOrder = path.ToList();
                currentVideoIndex = 0;
                queuepaths = path;
                _pendingPath = path[currentVideoIndex];
                Debug.WriteLine(_pendingPath);
                currentVideoPath = _pendingPath;
                PlaybackState.CurrentlyPlayingPath = _pendingPath;
                if (frmMain.Content is IUpdateableMusicPage activePage)
                {
                    // C# now treats 'activePage' as something that definitely has the method
                    activePage.UpdateCurrentListhere(_pendingPath);
                }
                Debug.WriteLine("Initialize");
                if (_libVLC == null)
                    return;
                using var media = new Media(_libVLC, _pendingPath, FromType.FromPath);
                // Force 16-bit high-quality output (prevents floating-point artifacts)
                media.AddOption(":avcodec-hw=none");
                media.AddOption(":audio-resampler=soxr");
                media.AddOption(":no-audio-time-stretch");
                media.AddOption(":stereo-mode=1");
                media.AddOption(":file-caching=3000");
                media.AddOption(":audio-channels=1");
                if (_mediaPlayer == null) return;
                _mediaPlayer.Media = media;
                _mediaPlayer?.Play(media);
                stateofplay = "playing";
                // In your initialization

                imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/pause.png"));
                imgThumbnailCover.Source = await GetFileThumbnailAsync(_pendingPath);
                txtSongName.Text = Path.GetFileName(_pendingPath);
                txtRunningDuration.Text = "00:00:00";
                sldMain.Value = 0;
                ToolTipService.SetToolTip(txtSongName, txtSongName.Text);
                btnPlayPause.IsEnabled = true;
                sldMain.IsEnabled = true;
                maintimer = new DispatcherTimer();
                maintimer.Interval = TimeSpan.FromMilliseconds(250);
                maintimer.Tick += Maintimer_Tick;
                if (_mediaPlayer == null)
                    return;

                StorageFile file = await StorageFile.GetFileFromPathAsync(_pendingPath);
                var musicProps = await file.Properties.GetMusicPropertiesAsync();

                TimeSpan duration = musicProps.Duration;

                // Set slider max from metadata
                sldMain.Maximum = duration.TotalSeconds;

                // Set total duration label
                txtTotalDuration.Text = duration.ToString(@"hh\:mm\:ss");

                sldVolume.Value = _mediaPlayer.Volume;
                maintimer.Start();

                _mediaPlayer.EndReached += (sender, e) =>
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        PlayNext();
                    });
                };
                string TeachingTipContent = "Media '" + Path.GetFileName(_pendingPath) + "' Opened" + Environment.NewLine + _pendingPath;
                ttMediaOpened.Content = TeachingTipContent;
                ttMediaOpened.IsOpen = true;
                await Task.Delay(4000);
                ttMediaOpened.IsOpen = false;
            }
            SaveRecents();
        }
        private void PlayNext()
        {
            if (currentVideoIndex + 1 < queuepaths.Count)
            {
                PlayVideoAtIndex(currentVideoIndex + 1);
            }
            _mediaPlayer.Stop();
            maintimer.Stop();
            _mediaPlayer.Position = 0;
            sldMain.Value = 0;
            txtRunningDuration.Text = "00:00:00";

            imgPlayPause.Source =
                new BitmapImage(new Uri("ms-appx:///Assets/play.png"));
            stateofplay = "paused";
        }
        private async void SaveRecents()
        {
            await SettingsHelper.LoadSettingsAsync();
            var settings = await SettingsHelper.LoadSettingsAsync();
            var unfinishedItems = settings.RecentMusic;
            var NewRecent = new RecentMusic
            {
                SongName = Path.GetFileName(_pendingPath),
                SongPath = _pendingPath,
                FolderName = new DirectoryInfo(
    Path.GetDirectoryName(_pendingPath) ?? string.Empty
).Name,
            };
            bool alreadyExists = settings.RecentMusic
              .Any(x => x.SongPath == _pendingPath);

            if (!alreadyExists)
            {
                settings.RecentMusic.Add(NewRecent);
            }
            await SettingsHelper.SaveSettingsAsync(settings);
        }
        string currentVideoPath = "";
        int currentVideoIndex = 0;
        bool mediaended;
        private void CheckForFileArguments()
        {
            string[] commandArgs = Environment.GetCommandLineArgs();

            if (commandArgs.Length > 1)
            {
                string filePath = commandArgs[1].Trim('"');

                if (System.IO.File.Exists(filePath))
                {
                    var paths = new ObservableCollection<string> { filePath };
                    this.LoadFileFromPath(paths);
                }
            }
        }
        private async void PlayVideoAtIndex(int index)
        {
            if (index < 0 || index >= queuepaths.Count)

                return; // out of range

            // Safety check for empty lists
            if (queuepaths.Count == 0) return;

            currentVideoIndex = index;
            currentVideoPath = queuepaths[index];
            PlaybackState.CurrentlyPlayingPath = _pendingPath;
            if (frmMain.Content is IUpdateableMusicPage activePage)
            {
                // C# now treats 'activePage' as something that definitely has the method
                activePage.UpdateCurrentListhere(_pendingPath);
            }
            if (_libVLC == null)
                return;
            var media = new Media(_libVLC, currentVideoPath, FromType.FromPath);
            media?.Parse(MediaParseOptions.ParseLocal);
            stateofplay = "playing";
            imgThumbnailCover.Source = await GetFileThumbnailAsync(currentVideoPath);
            txtSongName.Text = Path.GetFileName(currentVideoPath);
            ToolTipService.SetToolTip(txtSongName, txtSongName.Text);
            if (_mediaPlayer == null)
                return;

            _mediaPlayer.Media = media;
            _mediaPlayer.Play();
            sldMain.Value = 0;
            txtRunningDuration.Text = "00:00:00";
            maintimer.Start();
            imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/pause.png"));
        }
        private Random rdm = new Random();
        private List<string> _originalOrder = new List<string>();
        public void ShuffleRemaining()
        {

            if (queuepaths.Count <= 1) return;

            // 1. Identify the currently playing path
            string currentPath = currentVideoPath; // Your string variable

            // 2. Shuffle the ENTIRE list (starting from index 0)
            for (int i = queuepaths.Count - 1; i > 0; i--)
            {
                int j = rdm.Next(0, i + 1);
                var temp = queuepaths[i];
                queuepaths[i] = queuepaths[j];
                queuepaths[j] = temp;
            }

            // 3. Find where the current song moved to after the shuffle
            int newIdx = queuepaths.IndexOf(currentPath);

            // 4. Move the current song to the top (Index 0)
            if (newIdx != -1)
            {
                queuepaths.Move(newIdx, 0);
                currentVideoIndex = 0; // The current song is now always at the start
            }
        }
        public void RestoreOriginalOrder(ObservableCollection<string> path)
        {
            foreach (string item in path)
            {
                Debug.WriteLine("das: " + item);
            }
            Debug.WriteLine(currentVideoPath);
            queuepaths.Clear();
            queuepaths = path;
            if (queuepaths.Contains(currentVideoPath))
            {
                currentVideoIndex = queuepaths.IndexOf(currentVideoPath);
            }

        }
        private void Maintimer_Tick(object? sender, object e)
        {
            if (!_isDragging && _mediaPlayer != null)
            {
                long currentTimeMs = _mediaPlayer.Time;

                var time = TimeSpan.FromMilliseconds(currentTimeMs);

                txtRunningDuration.Text = time.ToString(@"hh\:mm\:ss");
                sldMain.Value = time.TotalSeconds;
            }
        }

        DispatcherTimer maintimer = new();
        public async Task<BitmapImage> GetFileThumbnailAsync(string path)
        {
            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(path);

                // GetScaledImageAsThumbnailAsync allows for higher resolution than the disk cache
                // Use a larger requested size (e.g., 320 or 640) for better quality
                using var thumbnail = await file.GetScaledImageAsThumbnailAsync(
                    ThumbnailMode.VideosView,
                    640,
                    ThumbnailOptions.UseCurrentScale);

                if (thumbnail != null)
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(thumbnail);
                    return bitmapImage;
                }
            }
            catch { /* Handle errors */ }

            return new BitmapImage(new Uri("ms-appx:///Assets/Placeholder.png"));
        }
        private bool _isLibVLCReady = false; // The "Is the engine started?" flag
        private string _pendingPath = "";
        private void VideoView_Initialized(object? sender, LibVLCSharp.Platforms.Windows.InitializedEventArgs e)
        {
            Core.Initialize();

            _libVLC = new LibVLC(enableDebugLogs: true, e.SwapChainOptions);
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);

            // These options fix the "ringing/bell" sound (aliasing) 
            // Note: Media options use a colon (:) instead of double dashes (--)

            _mediaPlayer.Play();

            _mediaPlayer.AspectRatio = "16:9";

            _mediaPlayer.Volume = 100;
            if (_libVLC != null && _pendingPath != "")
            {
                Debug.WriteLine("NotNull");
                using var media = new Media(_libVLC, _pendingPath, FromType.FromPath);
                _mediaPlayer?.Play(media);
            }
            this.DispatcherQueue.TryEnqueue(() =>
            {
                videoView.MediaPlayer = _mediaPlayer;
                // Hide the ring
                loadingRing.IsActive = false;
                loadingRing.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

                // Now you can safely call videoView.Initialized logic
                Debug.WriteLine("VLC is fully loaded and ready!");
            });
            Debug.WriteLine("VLC Engine is Ready!");

            CheckForFileArguments();
        }

        private LibVLC? _libVLC;
        private LibVLCSharp.Shared.MediaPlayer? _mediaPlayer;
        private async void FrmMain_Navigated(object sender, NavigationEventArgs e)
        {
            await ScanAllFoldersAsync();

            if (e.SourcePageType == typeof(HomePage))
                nvgMain.Header = "Home";

            else if (e.SourcePageType == typeof(MusicLibrary))
                nvgMain.Header = "Music Library";

            else if (e.SourcePageType == typeof(SettingsPage))
                nvgMain.Header = "App Settings";

            else if (e.SourcePageType == typeof(SearchResults))
                nvgMain.Header = Headersearch;

            else
                nvgMain.Header = "";
        }
        private void nvgMain_SelectionChanged(
            NavigationView sender,
            NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                frmMain.Navigate(typeof(SettingsPage));
                return;
            }

            if (args.SelectedItemContainer == null)
                return;

            Type pageType = null;

            if (args.SelectedItemContainer == nvgitHome)
                pageType = typeof(HomePage);

            else if (args.SelectedItemContainer == nvgitMusic)
                pageType = typeof(MusicLibrary);

            if (pageType != null && frmMain.CurrentSourcePageType != pageType)
            {
                frmMain.Navigate(pageType, null, new DrillInNavigationTransitionInfo());
            }
        }
        string stateofplay = "paused";
        string Headersearch = "Search results";
        public void PlayPausePublic(string statesofplay)
        {
            stateofplay = statesofplay;
            if (stateofplay == "playing")
            {
                stateofplay = "paused";
                _mediaPlayer?.Pause();
                maintimer.Stop();
                imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/play.png"));

            }
            else
            {
                stateofplay = "playing";
                maintimer.Start();
                _mediaPlayer?.Play();
                imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/pause.png"));
            }
        }
        private void btnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            SaveRecents();
            if (stateofplay == "playing")
            {
                stateofplay = "paused";
                _mediaPlayer?.Pause();
                maintimer.Stop();
                imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/play.png"));

            }
            else
            {
                stateofplay = "playing";
                maintimer.Start();
                _mediaPlayer?.Play();
                imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/pause.png"));
            }
            if (frmMain.Content is IUpdateableMusicPage activePage)
            {
                // C# now treats 'activePage' as something that definitely has the method
                activePage.UpdateCurrentState(stateofplay);
            }
        }

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (currentVideoIndex <= queuepaths.Count)
            {
                PlayVideoAtIndex(currentVideoIndex - 1);
                if (frmMain.Content is IUpdateableMusicPage activePage)
                {
                    // C# now treats 'activePage' as something that definitely has the method
                    activePage.UpdateCurrentListhere(currentVideoPath);
                }
            }
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (currentVideoIndex <= queuepaths.Count)
            {
                PlayVideoAtIndex(currentVideoIndex + 1);
                if (frmMain.Content is IUpdateableMusicPage activePage)
                {
                    // C# now treats 'activePage' as something that definitely has the method
                    activePage.UpdateCurrentListhere(currentVideoPath);
                }
            }
        }
        void SeekRelative(int seconds)
        {
            if (_mediaPlayer == null || _mediaPlayer.Length <= 0)
                return;

            // Calculate new time in milliseconds
            long newTime = _mediaPlayer.Time + seconds * 1000;

            // Clamp between 0 and total length
            newTime = Math.Max(0, Math.Min(newTime, _mediaPlayer.Length));

            // Apply the new time to the media player
            _mediaPlayer.Time = newTime;

            // Update slider (in seconds)
            sldMain.Value = newTime / 1000.0;

            // Update running duration text
            txtRunningDuration.Text = TimeSpan.FromMilliseconds(newTime).ToString(@"hh\:mm\:ss");
        }

        private void nvgMain_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (frmMain.CanGoBack)
            {
                frmMain.GoBack();
            }
        }

        private void sldVolume_ValueChanged(double obj)
        {
            if (_mediaPlayer == null)
                return;

            int vol = (int)obj;

            _mediaPlayer.Volume = vol;
            txtVolume.Text = vol.ToString() + "%";
        }

        private void btnSkipForward_Click(object sender, RoutedEventArgs e)
        {
            SeekRelative(+10);

        }
        public void RestoreBackOriginalWindow(long time)
        {

        }
        private void btnSkipBack_Click(object sender, RoutedEventArgs e)
        {
            SeekRelative(-10);
        }
        string videospeed = "1";
        private void btnSpeedfly_Click(object sender, RoutedEventArgs e)
        {
            ttSpeedCustom.IsOpen = false;
            var menuflyoutitem = (RadioMenuFlyoutItem)sender;

            string speed = menuflyoutitem.Text;
            videospeed = speed;
            if (_mediaPlayer != null && maintimer != null)
            {
                _mediaPlayer?.Pause();
                maintimer.Stop();
                if (menuflyoutitem != null)
                {

                    if (float.TryParse(speed, System.Globalization.CultureInfo.InvariantCulture, out float speedfloat))
                    {
                        _mediaPlayer?.SetRate(speedfloat);
                        _mediaPlayer?.Play();
                        maintimer.Start();
                    }
                }
            }
        }

        private void customSpeed_Click(object sender, RoutedEventArgs e)
        {
            nmbSpeedCustom.Value = Convert.ToDouble(videospeed);
            ttSpeedCustom.IsOpen = true;
        }

        private void btnEffects_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void btnInfo_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(PlaybackState.CurrentlyPlayingPath))
            {
                ShowSongDetails(PlaybackState.CurrentlyPlayingPath);
            }
        }

        private void btnMiniPlayer_Click(object sender, RoutedEventArgs e)
        {
            var miniplayer = new MiniPlayerWind();
            if (_mediaPlayer == null)
                return;

            PlaybackState.CurrentPosition = _mediaPlayer.Position;
            PlaybackState.CurrentSliderPosition = _mediaPlayer.Time / 1000;
            PlaybackState.TotalDuration = _mediaPlayer.Length / 1000;
            maintimer.Stop();

            Debug.WriteLine("Total: " + PlaybackState.TotalDuration.ToString() + " Current: " + PlaybackState.CurrentSliderPosition.ToString());
            if (stateofplay == "paused")
            {
                PlaybackState.CurrentState = false;
            }
            else
            {
                _mediaPlayer?.Pause();
                PlaybackState.CurrentState = true;
            }
            App.SetCurrentMainWindow(miniplayer);
            miniplayer.Closed += (s, args) =>
            {

                this.AppWindow.Show();
                if (_mediaPlayer == null)
                    return;

                _mediaPlayer.Position = PlaybackState.CurrentPosition;
                if (PlaybackState.CurrentState == true)
                {
                    _mediaPlayer.Play();
                    maintimer.Start();
                    stateofplay = "playing";
                    long currentTimeMs = _mediaPlayer.Time;
                    sldMain.Value = currentTimeMs / 1000.0;
                    imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/pause.png"));
                }
                else
                {
                    imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/play.png"));
                    stateofplay = "paused";
                    maintimer.Stop(); long currentTimeMs = _mediaPlayer.Time;
                    sldMain.Value = currentTimeMs / 1000.0;
                }
            };
            miniplayer.Activate();
            this.AppWindow.Hide();
        }
        private ObservableCollection<SongModel> AllAvailableSongs = new ObservableCollection<SongModel>();

        private async Task ScanAllFoldersAsync()
        {
            AllAvailableSongs.Clear();

            string[] searchPaths = {
        UserDataPaths.GetDefault().Music,
        UserDataPaths.GetDefault().Downloads,
        UserDataPaths.GetDefault().Documents,
        UserDataPaths.GetDefault().Videos
    };

            foreach (var path in searchPaths)
            {
                try
                {
                    StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);
                    var queryOptions = new QueryOptions(CommonFileQuery.OrderByMusicProperties, new[] { ".mp3", ".flac", ".m4a" });
                    var query = folder.CreateFileQueryWithOptions(queryOptions);
                    var files = await query.GetFilesAsync();

                    foreach (var file in files)
                    {
                        // This is the part that fills the Artist/Album/Duration
                        var props = await file.Properties.GetMusicPropertiesAsync();

                        AllAvailableSongs.Add(new SongModel
                        {
                            Title = file.DisplayName,
                            Artist = props.Artist,
                            AlbumName = props.Album,
                            SongDuration = props.Duration,
                            FilePath = file.Path
                        });
                    }
                }
                catch { /* Access Denied */ }
            }
        }
        private void btnSetCustomSpeed_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer != null && maintimer != null)
            {
                _mediaPlayer?.Pause();
                maintimer.Stop();

                if (!double.IsNaN(nmbSpeedCustom.Value))
                {
                    string speed = nmbSpeedCustom.Value.ToString();
                    videospeed = speed;
                    if (float.TryParse(speed, System.Globalization.CultureInfo.InvariantCulture, out float speedfloat))
                    {
                        _mediaPlayer?.SetRate(speedfloat);
                        _mediaPlayer?.Play();
                        maintimer.Start();
                    }
                }
            }
        }



        private void hypGoToLocation_Click(object sender, RoutedEventArgs e)
        {
            string filePath = txtInfoFilePath.Text;

            if (File.Exists(filePath))
            {
                // This opens explorer and HIGHLIGHTS the specific file
                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            }

        }

        private async void txtInfoRating_ValueChanged(RatingControl sender, object args)
        {
            if (PlaybackState.CurrentlyPlayingPath == null) return;

            // Convert 1-5 back to Windows 0-99
            uint shellRating = sender.Value switch
            {
                5 => 99,
                4 => 75,
                3 => 50,
                2 => 25,
                1 => 1,
                _ => 0
            };

            StorageFile file = await StorageFile.GetFileFromPathAsync(PlaybackState.CurrentlyPlayingPath);
            var propertiesToSave = new Dictionary<string, object>
    {
        { "System.Rating", shellRating }
    };

            try
            {
                await file.Properties.SavePropertiesAsync(propertiesToSave);
            }
            catch (Exception ex)
            {
                // If the file is still 'Blocked' by Windows Security, this will fail
                Debug.WriteLine($"Failed to save rating: {ex.Message}");
            }
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var package = new DataPackage();
            package.SetText(txtInfoFilePath.Text);
            Clipboard.SetContent(package);

        }
        public async void ShowSongDetails(string FilePath)
        {
            if (FilePath == null) return;

            StorageFile file = await StorageFile.GetFileFromPathAsync(FilePath);
            var musicProps = await file.Properties.GetMusicPropertiesAsync();
            var basicProps = await file.GetBasicPropertiesAsync();
            var propertyKeys = new List<string>
{
    "System.Rating",
    "System.Audio.SampleRate",
    "System.Audio.ChannelCount",
    "System.Music.Composer",
    "System.Music.Conductor",
    "System.Comment",
    "System.Music.Artist" // Contributing Artists
};
            IDictionary<string, object> extraProps = await file.Properties.RetrievePropertiesAsync(propertyKeys);

            // 2. DIALOG & FILE HEADER INFO
            dlgFileInfo.Title = $"Information on '{Path.GetFileName(FilePath)}'";
            txtInfoFileName.Text = !string.IsNullOrWhiteSpace(musicProps.Title) ? musicProps.Title : file.DisplayName;
            txtInfoFilePath.Text = FilePath;
            txtInfoFileType.Text = file.FileType;

            // 3. MUSIC METADATA (Artist, Album, etc.)
            txtInfoAlbum.Text = musicProps.Album;
            txtInfoArtist.Text = musicProps.Artist;
            txtInfoTrack.Text = musicProps.TrackNumber.ToString();
            txtInfoYear.Text = musicProps.Year.ToString();
            txtInfoGenre.Text = musicProps.Genre.Any() ? string.Join("; ", musicProps.Genre) : "Unknown Genre";

            // Contributing Artists (Fallback to main artist if null)
            if (extraProps.TryGetValue("System.Music.Artist", out object? artistObj) && artistObj is string[] artists)
                txtInfoContributingArtists.Text = string.Join("; ", artists);
            else
                txtInfoContributingArtists.Text = musicProps.Artist;

            // 4. TECHNICAL SPECS (Bitrate, Sample Rate, Duration)
            txtInfoBitrate.Text = musicProps.Bitrate > 0 ? $"{musicProps.Bitrate / 1000} kbps" : "Unknown Bitrate";
            txtInfoAudioSampleRate.Text = extraProps.TryGetValue("System.Audio.SampleRate", out object? sr) ? $"{sr} Hz" : "Unknown";
            txtInfoChannels.Text = extraProps.TryGetValue("System.Audio.ChannelCount", out object? ch) ? ch.ToString() : "Unknown";
            txtInfoSpeed.Text = videospeed;

            // Duration Formatting
            txtInfoDuration.Text = musicProps.Duration.TotalHours >= 1
                ? musicProps.Duration.ToString(@"h\:mm\:ss")
                : musicProps.Duration.ToString(@"m\:ss");

            // 5. ADDITIONAL DETAILS (Rating, Comments, Dates)
            // Rating Conversion
            if (extraProps.TryGetValue("System.Rating", out object? rateObj) && rateObj is uint rawRating)
            {
                txtInfoRating.Value = rawRating switch
                {
                    >= 99 => 5,
                    >= 75 => 4,
                    >= 50 => 3,
                    >= 25 => 2,
                    >= 1 => 1,
                    _ => 0
                };
            }
            else { txtInfoRating.Value = 0; }

            // Comments
            txtInfoComments.Text = extraProps.TryGetValue("System.Comment", out object? commObj) ? commObj.ToString() : "";

            // Composers / Conductors
            txtInfoComposers.Text = extraProps.TryGetValue("System.Music.Composer", out object? compObj) && compObj is string[] comps ? string.Join("; ", comps) : "-";
            txtInfoConductors.Text = extraProps.TryGetValue("System.Music.Conductor", out object? condObj) && condObj is string[] conds ? string.Join("; ", conds) : "-";

            // Timestamps
            txtInfoCreatedOn.Text = file.DateCreated.ToString("G");
            txtInfoLastModified.Text = basicProps.DateModified.ToString("G");

            // 6. SHOW DIALOG
            await dlgFileInfo.ShowAsync();
        }
        private double _lastVolume = 100;
        private void btnVolume_Click(object sender, RoutedEventArgs e)
        {
            if (sldVolume.Value > 0)
            {
                // --- STATE: MUTING ---
                _lastVolume = sldVolume.Value; // Save current volume
                sldVolume.Value = 0;           // Set slider to 0
                txtVolume.Text = "0%";
                VolumeIcon.Glyph = "\uE74F";      // Mute glyph
            }
            else
            {
                // --- STATE: UNMUTING ---
                // If the last saved volume was 0 (e.g. app started muted), 
                // default to a audible level like 50.
                sldVolume.Value = _lastVolume > 0 ? _lastVolume : 50;
                int vollast = Convert.ToInt32(sldVolume.Value);
                txtVolume.Text = vollast.ToString() + "%";
                VolumeIcon.Glyph = "\uE767";      // Sound glyph
            }
        }

        private async void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var searchTerm = sender.Text.ToLower().Trim();

                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    sender.ItemsSource = null;
                }
                else
                {
                    var suggestions = AllAvailableSongs
                 .Where(s => s.Title != null &&
            s.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                   .OrderByDescending(s =>
    s.Title?.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase) == true)
                        .ThenBy(s => s.Title)
                        .ToList();

                    sender.ItemsSource = suggestions;
                }
            }

        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            string query = args.QueryText.Trim();

            if (string.IsNullOrEmpty(query)) return;

            var results = AllAvailableSongs
            .Where(s => s.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            // If the user picked a specific suggestion from the list, 
            // it might be better to just send that ONE song instead of a filtered list.
            if (args.ChosenSuggestion != null)
            {
                if (args.ChosenSuggestion is SongModel selectedSong)
                {
                    frmMain.Navigate(typeof(SearchResults),
                        new List<SongModel> { selectedSong });

                    nvgMain.Header = $"Results for '{selectedSong.Title}'";
                }
            }
            else if (results.Any())
            {
                frmMain.Navigate(typeof(SearchResults), results);
                nvgMain.Header = $"Results for '{query}'";
                Headersearch = $"Results for '{query}'";
            }
            else
            {
                frmMain.Navigate(typeof(SearchResults), "no");
                nvgMain.Header = $"No results for '{query}'";
                Headersearch = $"No results for '{query}'";
            }
        }

        private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.Text = ((SongModel)args.SelectedItem).Title;
            if (args.SelectedItem is SongModel sngl)
            {
                sender.Text = sngl.Title;
            }
            //       ObservableCollection<string> temp = new();
            //      temp.Add(((SongModel)args.SelectedItem).FilePath);
            //        LoadFileFromPath(temp);

        }
    }
}

