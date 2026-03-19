using ABI.Microsoft.UI.Xaml;
using CommunityToolkit.WinUI;
using CSCore.Codecs;
using CSCore.Codecs.WAV;
using CSCore.DMO.Effects;
using FlyleafLib;
using FlyleafLib.MediaFramework.MediaDecoder;
using FlyleafLib.MediaPlayer;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Win32;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Contacts;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Search;
using Windows.System;
using Windows.UI;
using WinRT;
using WinRT.Interop;
using static System.Runtime.InteropServices.JavaScript.JSType;
using DispatcherTimer = Microsoft.UI.Xaml.DispatcherTimer;
using FrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using LogLevel = FlyleafLib.LogLevel;
using RoutedEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;
using Window = Microsoft.UI.Xaml.Window;
using WindowActivatedEventArgs = Microsoft.UI.Xaml.WindowActivatedEventArgs;
using WindowEventArgs = Microsoft.UI.Xaml.WindowEventArgs;
using XamlRoot = Microsoft.UI.Xaml.XamlRoot;
//Vusic Player - OceanCyan Tech - Version 1.1.0.0 

namespace VusicPlayer
{
    ///DEVELOPMENT
    public sealed partial class HomeWindow : Window
    {

        public HomeWindow()
        {
            InitializeComponent();

            txtPreviewBuild.Text = $"Vusic Player {Strings.VersionText} {Appversionstrings.AppVersion + Environment.NewLine} {Appversionstrings.VersionType} {Strings.BuildText} {Appversionstrings.BuildNumber}";
            //  LoadTheme();
            if (App.MainWindowInstance != null)
            {
                var rootElement = (FrameworkElement)App.MainWindowInstance.Content;
                rootElement.RequestedTheme = ElementTheme.Dark;
            }
            TrySetAcrylicBackdrop(true); DispatcherQueue.EnsureSystemDispatcherQueue();
            Mainframe.Navigate(typeof(SplashScreen));
            this.ExtendsContentIntoTitleBar = true;
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.SetIcon("Assets/appicon.ico");
            appWindow.SetTaskbarIcon("Assets/appicon.ico");
            appWindow.SetTitleBarIcon("Assets/appicon.ico");
            this.Title = "Vusic Player";
            loadingRing.IsActive = true;

            loadingRing.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            frmMain.Navigated += FrmMain_Navigated;

            rootgrid.Visibility = Visibility.Collapsed;
            sldMain.DragStarted += SldMain_DragStarted;

            sldMain.DragCompleted += SldMain_DragCompleted;

            this.DispatcherQueue.TryEnqueue(async () =>
            {
                //  await CheckAndDownloadUpdate();

                //     await ScanAllFoldersAsync();
            });
            Engine.Start(new EngineConfig()
            {
#if DEBUG
                LogOutput = ":debug",
                LogLevel = LogLevel.Debug,
                FFmpegLogLevel = Flyleaf.FFmpeg.LogLevel.Warn,
#endif

                UIRefresh = false, // For Activity Mode usage
                                   //   PluginsPath = ":Plugins",
                FFmpegPath = Path.Combine(AppContext.BaseDirectory, "FFmpegDLLs")
            });
            if (nvgMain.MenuItems.Count > 0)
            {
                nvgMain.SelectedItem = nvgMain.MenuItems[0];
            }
            SplashComplete();
            CheckForDefaultNess();
            //  CheckForFileArguments();
        }
        public async Task<bool> IsAppDefault()
        {

            var result = await Launcher.FindFileHandlersAsync(".mp3");

            foreach (var handler in result)
            {

                if (handler.PackageFamilyName == Windows.ApplicationModel.Package.Current.Id.FamilyName)
                {

                    return true;
                }
            }
            return false;
        }
        public static bool IsDefaultForMp4()
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.mp4\UserChoice");

            if (key == null)
                return false;

            var progId = key.GetValue("ProgId")?.ToString();

            if (progId == null)
                return false;

            string pfn = Package.Current.Id.FamilyName;

            return progId.Contains(pfn);
        }
        private async void CheckForDefaultNess()
        {
            bool isRegistered = await IsAppDefault();
            if (!isRegistered)
            {
                ttDefaultAppSet.IsOpen = true;
            }
            if (!IsDefaultForMp4())
            {
                ttDefaultAppSet.IsOpen = true;
            }
            var currentSettings = await SettingsHelper.LoadSettingsAsync();
            if (currentSettings.ShowDefaultMessage == false)
            {
                ttDefaultAppSet.IsOpen = false;
            }
        }
        private async void LoadTheme()
        {
            try
            {
                var currentSettings = await SettingsHelper.LoadSettingsAsync();

                if (App.MainWindowInstance != null)
                {
                    var rootElement = (FrameworkElement)App.MainWindowInstance.Content;
                    var personalization = currentSettings.UserSettings[0];
                    if (personalization.Theme == "Light")
                    {
                        rootElement.RequestedTheme = ElementTheme.Light;
                    }
                    else if (personalization.Theme == "Dark")
                    {
                        rootElement.RequestedTheme = ElementTheme.Dark;
                    }
                    else
                    {
                        rootElement.RequestedTheme = ElementTheme.Default;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, "HomeWindow.LoadTheme", Logger.LogLevelType.Error);
            }
        }

        private async void SplashComplete()
        {
            await Task.Delay(2000);
            Mainframe.Visibility = Visibility.Collapsed;
            rootgrid.Visibility = Visibility.Visible;
        }
        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            if (configurationSource != null)
            {
                SetConfigurationSourceTheme();
            }
        }
        Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController? acrylicController;
        Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration? configurationSource;
        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (configurationSource != null)
            {
                configurationSource.IsInputActive =
                    args.WindowActivationState != WindowActivationState.Deactivated;
            }

        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            configurationSource = null;
            if (PlayerService.MasterPlayer != null)
            {
                PlayerService.MasterPlayer.Stop();
                PlayerService.MasterPlayer.Dispose();
                PlayerService.maintimer?.Stop();
            }
            acrylicController?.Dispose();
            acrylicController = null;
            Activated -= Window_Activated;
            configurationSource = null;
        }
        private void SetConfigurationSourceTheme()

        {
            if (configurationSource == null) return;
            switch (((FrameworkElement)this.Content).ActualTheme)
            {
                case ElementTheme.Dark: configurationSource.Theme = SystemBackdropTheme.Dark; break;
                case ElementTheme.Light: configurationSource.Theme = SystemBackdropTheme.Light; break;
                case ElementTheme.Default: configurationSource.Theme = SystemBackdropTheme.Default; break;
            }
        }

        bool TrySetAcrylicBackdrop(bool useAcrylicThin)
        {
            if (DesktopAcrylicController.IsSupported())
            {
                DispatcherQueue.EnsureSystemDispatcherQueue();

                // Hooking up the policy object
                configurationSource = new SystemBackdropConfiguration();
                Activated += Window_Activated;
                Closed += Window_Closed;
                ((FrameworkElement)Content).ActualThemeChanged += Window_ThemeChanged;

                // Initial configuration state.
                configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                acrylicController = new DesktopAcrylicController();
                acrylicController.Kind = useAcrylicThin ? DesktopAcrylicKind.Thin : DesktopAcrylicKind.Base;

                // Enable the system backdrop.

                acrylicController.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
                acrylicController.SetSystemBackdropConfiguration(configurationSource);

                return true; // Succeeded.
            }

            return false; // Acrylic is not supported on this system.
        }


        #region Fields

        ObservableCollection<string> queuepaths = new();
        Version currentVersion = new Version(VersionStringApp.VersionText);
        #endregion

        private async Task CheckAndDownloadUpdate()
        {
            // 1. DEFINE YOUR CURRENT VERSION
            Version currentVersion = new Version("1.0.1.5");

            try
            {
                using var client = new HttpClient();

                // 2. Replace with your actual Pastebin RAW URL
                string pastebinContent = await client.GetStringAsync("https://pastebin.com/raw/YjGbNMpc");

                var parts = pastebinContent.Split('|');
                if (parts.Length < 2) return;

                Version latestVersion = Version.Parse(parts[0]);
                string zipUrl = parts[1].Trim();

                // 3. Compare versions
                if (latestVersion > currentVersion)
                {
                    Logger.Log(latestVersion.ToString() + " version available for update. Update will be carried out the next time app is opened.", "HomeWindow", Logger.LogLevelType.Information);

                    string root = AppContext.BaseDirectory;
                    string stagingDir = Path.Combine(root, "UpdateStaging");
                    string flagFile = Path.Combine(root, "update.ready");

                    Directory.CreateDirectory(stagingDir);

                    // 4. Download the ZIP
                    string zipPath = Path.Combine(stagingDir, "updates_dynamics.zip");
                    var response = await client.GetAsync(zipUrl, HttpCompletionOption.ResponseHeadersRead);

                    if (response.IsSuccessStatusCode)
                    {
                        byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();
                        await File.WriteAllBytesAsync(zipPath, fileBytes);

                        // 5. Create the flag
                        await File.WriteAllTextAsync(flagFile, latestVersion.ToString());
                        Logger.Log("Download Success: ZIP and Flag created.", "HomeWindow", Logger.LogLevelType.Success);
                    }
                    else
                    {
                        Logger.Log($"Download Failure: {(int)response.StatusCode} {response.ReasonPhrase}", "HomeWindow", Logger.LogLevelType.Error);
                        Logger.Log($"Error Response: URL attempted: {zipUrl}", "HomeWindow", Logger.LogLevelType.Error);
                    }

                    Logger.Log("Update downloaded and flag created!", "HomeWindow", Logger.LogLevelType.Success);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Update Error: " + ex.Message, "HomeWindow", Logger.LogLevelType.Error);
            }
        }
        public async Task<string> GetVersionFromGist()
        {
            string gistId = "e2c93cec660817d0a6bfe6605da1ea81";
            string token = "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"; // Put your token here
            string url = $"https://api.github.com/gists/{gistId}";

            using var client = new HttpClient();

            // GitHub API strictly requires a User-Agent and Authorization for secret gists
            client.DefaultRequestHeaders.UserAgent.ParseAdd("VusicPlayer");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                // 1. Get the JSON response from GitHub
                var response = await client.GetStringAsync(url);

                // 2. Parse the JSON
                using var doc = JsonDocument.Parse(response);

                // 3. Navigate to: files -> versionvusic.txt -> content
                // This extracts the "1.0.0.0|https://..." string
                var content = doc.RootElement
                    .GetProperty("files")
                    .GetProperty("versionvusic.txt")
                    .GetProperty("content")
                    .GetString();

                return content?.Trim() ?? string.Empty;
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                Logger.Log($"Gist Fetch Error: {ex.Message}", "HomeWindow", Logger.LogLevelType.Error);
                return string.Empty;
            }
        }
        bool _isDragging = false;
        private void SldMain_DragCompleted()
        {
            //if (player == null) return;
            //double newPosition = sldMain.Value / sldMain.Maximum;
            //player.CurTime = TimeSpan.FromSeconds(sldMain.Value).Ticks;
            //var curTime = TimeSpan.FromTicks(player.CurTime);
            //txtRunningDuration.Text = curTime.ToString(@"hh\:mm\:ss");

            //_isDragging = false;

        }


        private void SldMain_DragStarted()
        {

            //  _isDragging = true;
            //maintimer.Stop();
        }
        Player? player;
        public async void ReattachUI()
        {
            PlayerService.AttachUI(txtRunningDuration, sldMain);
        }
        public async void LoadFileFromPath(ObservableCollection<string> path)
        {
            currentVideoIndex = 0;
            queuepaths = path;
            currentVideoPath = path[currentVideoIndex];
            if (File.Exists(currentVideoPath))
            {
                sldMain.IsEnabled = true;
                PlaybackState.CurrentlyPlayingPath = currentVideoPath;
                PlayVideoPath(currentVideoPath);
            }
        }
        public async void UpdateQueuePath(ObservableCollection<string> path)
        {
            queuepaths = path;
        }
        private void PlayNext()
        {
            // PlayVideoAtIndex already handles the bounds check, 
            // so it will simply return if (currentVideoIndex + 1) is invalid.
            PlayVideoAtIndex(currentVideoIndex + 1);
        }

        private void PlayPrevious()
        {
            PlayVideoAtIndex(currentVideoIndex - 1);
        }
        private async void PlayVideoPath(string path)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(currentVideoPath);
            var musicProps = await file.Properties.GetMusicPropertiesAsync();

            TimeSpan duration = musicProps.Duration;


            sldMain.Maximum = duration.TotalSeconds;
            txtTotalDuration.Text = duration.ToString(@"hh\:mm\:ss");
            if (player == null)
            {
                PlayerService.AttachUI(txtRunningDuration, sldMain);
                PlayerService.CreatePlayer();
                player = PlayerService.MasterPlayer;
                mediaEngine.Player = player;
            }

            player?.Open(path);
            PlayerService.Play();
            stateofplay = "playing";
            imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/pause.png"));
            imgThumbnailCover.Source = await GetFileThumbnailAsync(currentVideoPath);
            txtSongName.Text = Path.GetFileName(currentVideoPath);

            ToolTipService.SetToolTip(txtSongName, txtSongName.Text);
            btnPlayPause.IsEnabled = true;
            if (player != null)
            {
                player.Audio.Volume = (int)sldVolume.Value;
                originalvolume = player.Audio.Volume;
            }
            if (originalvolume.HasValue)
            {
                currentvol = originalvolume.Value.ToString();
            }
            _ = SaveRecents();
        }
        private async void PlayVideoAtIndex(int index)
        {
            if (index < 0 || index >= queuepaths.Count)

                return; 
            if (queuepaths.Count == 0) return;

            currentVideoIndex = index;
            currentVideoPath = queuepaths[index];

            PlayVideoPath(currentVideoPath);
        }

        private async Task SaveRecents()
        {
            var settings = await SettingsHelper.LoadSettingsAsync();
            var NewRecent = new RecentMusic
            {
                SongName = Path.GetFileName(currentVideoPath),
                SongPath = currentVideoPath,
                FolderName = new DirectoryInfo(
                    Path.GetDirectoryName(currentVideoPath) ?? string.Empty
                ).Name,
            };

            bool alreadyExists = settings.RecentMusic
                .Any(x => x.SongPath == currentVideoPath);

            if (!alreadyExists)
            {
                // Insert at the start of the list
                settings.RecentMusic.Insert(0, NewRecent);
            }

            await SettingsHelper.SaveSettingsAsync(settings);
        }
        string currentVideoPath = "";
        int currentVideoIndex = 0;
        public void PausePlayer()
        {
            if (player == null) return;
            if (stateofplay == "playing")
            {
                player.Pause();
                stateofplay = "paused";
                maintimer.Stop();
                imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/play.png"));
            }
        }
        private static HomeWindow? instance;
        public static HomeWindow? HideWindow()
        {
            if (instance != null)
            {
                instance.PausePlayer();
                instance.AppWindow.Hide();
            }
            return instance;
        }

        public static HomeWindow ShowWindow()
        {
            if (instance == null)
            {
                instance = new HomeWindow();
                //       instance.Closed += (_, __) => instance = null; // Reset when closed
                instance.Activate();
            }
            else
            {
                instance.Activate(); // Bring existing window to front
            }
            App.CurrentActiveWindow = instance;
            App.MainWindowInstance = instance;
            App.MainWindowInstance2 = instance;
            App.HomeWindowInstance = instance;
            //  instance.CheckForFileArguments();
            return instance;
        }

        private void Maintimer_Tick(object? sender, object e)
        {
            if (!_isDragging && player != null)
            {
              
                var curTime = TimeSpan.FromTicks(player.CurTime);
                txtRunningDuration.Text = curTime.ToString(@"hh\:mm\:ss");
                sldMain.Value = curTime.TotalSeconds;
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


        private async void FrmMain_Navigated(object sender, NavigationEventArgs e)
        {

            if (e.SourcePageType == typeof(HomePage))
                nvgMain.Header = "Home";

            else if (e.SourcePageType == typeof(MusicLibrary))
                nvgMain.Header = "Music Library";

            else if (e.SourcePageType == typeof(VideoLibrary))
                nvgMain.Header = "Video Library";

            else if (e.SourcePageType == typeof(QueuePage))
                nvgMain.Header = "Play Queue";

            else if (e.SourcePageType == typeof(SettingsPage))
                nvgMain.Header = "App Settings";
            else if (e.SourcePageType == typeof(LogPage))
                nvgMain.Header = "App Logs";

            else if (e.SourcePageType == typeof(SearchResults))
                nvgMain.Header = Headersearch;


            else
                nvgMain.Header = "";
        }
        private void nvgMain_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            MusicPlayerMaster.Visibility = Visibility.Visible;
            if (args.IsSettingsSelected)
            {
                frmMain.Navigate(typeof(SettingsPage));
                return;
            }

            if (args.SelectedItemContainer == null)
                return;

            Type? pageType = null;

            if (args.SelectedItemContainer == nvgitHome)
                pageType = typeof(HomePage);

            else if (args.SelectedItemContainer == nvgitMusic)
                pageType = typeof(MusicLibrary);

            else if (args.SelectedItemContainer == nvgitVideo)
                pageType = typeof(VideoLibrary);

            else if (args.SelectedItemContainer == nvgitQueue)
            {
                TransposeMediaDetails mediaDetails = new TransposeMediaDetails { MediaPath = currentVideoPath, CurrentDur = txtRunningDuration.Text };
                frmMain.Navigate(typeof(QueuePage), mediaDetails, new DrillInNavigationTransitionInfo());
                MusicPlayerMaster.Visibility = Visibility.Collapsed;
            }

            if (pageType != null && frmMain.CurrentSourcePageType != pageType)
            {
                frmMain.Navigate(pageType, null, new DrillInNavigationTransitionInfo());
            }
        }
        string stateofplay = "paused";
        string Headersearch = "Search results";
        
        private async void btnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (player == null) return;
            if (stateofplay == "playing")
            {
                PlayerService.Pause();
                stateofplay = "paused";
                imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/play.png"));
            }
            else
            {
                PlayerService.Play();
                stateofplay = "playing";
                imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/pause.png"));
            }

        }


        private void UpdatePlayUI(bool isPlaying)
        {
            string iconName = isPlaying ? "pause" : "play";
            string toolTipText = isPlaying ? "Pause" : "Play";

            imgPlayPause.Source = new BitmapImage(new Uri($"ms-appx:///Assets/{iconName}.png"));
            ToolTipService.SetToolTip(btnPlayPause, toolTipText);
        }
        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            PlayPrevious();
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            PlayNext();
        }
        
        

        private void nvgMain_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (frmMain.CanGoBack)
            {
                frmMain.GoBack();
            }
        }
        private void VolumeChange(double obj)
        {
            txtVolume.Text = ((int)obj).ToString() + "%";
            if (player == null) return;

            int vol = (int)obj;
            if (vol != 0)
            {
                originalvolume = vol;
            }
            currentvol = vol.ToString();
            txtVolume.Text = currentvol + "%";
            player.Audio.Volume = vol;

            // 1. Determine the Glyph
            VolumeIcon.Glyph = vol switch
            {
                0 => "\uE74F", // Mute
                < 10 => "\uE992", // Low
                < 40 => "\uE993", // Med-Low
                < 80 => "\uE994", // Med
                _ => "\uE995"  // High / Max
            };

            // 2. Determine the Color (Defaults to White)
            Color iconColor = vol switch
            {
                >= 115 => Colors.Orange,
                > 100 => Colors.Yellow,
                _ => Colors.White
            };

            VolumeIcon.Foreground = new SolidColorBrush(iconColor);
        }
        private string currentvol = "0";
        private string volumestate = "1";
        private int? originalvolume;
        private void sldVolume_ValueChanged(double obj)
        {
            VolumeChange(obj);
        }

        private void btnSkipForward_Click(object sender, RoutedEventArgs e)
        {
         
        }
        public void RestoreBackOriginalWindow(long time)
        {

        }
        private void btnSkipBack_Click(object sender, RoutedEventArgs e)
        {
        }
        string videospeed = "1";
        private void btnSpeedfly_Click(object sender, RoutedEventArgs e)
        {
            ttSpeedCustom.IsOpen = false;
            var menuflyoutitem = (RadioMenuFlyoutItem)sender;

            string speed = menuflyoutitem.Text;
            videospeed = speed;
            if (player != null && maintimer != null)
            {
             
                if (menuflyoutitem != null)
                {

                    if (double.TryParse(speed, System.Globalization.CultureInfo.InvariantCulture, out double speedfloat))
                    {
                        player.Speed = speedfloat;
                    }
                }

            }
        }

        private void customSpeed_Click(object sender, RoutedEventArgs e)
        {
            nmbSpeedCustom.Value = Convert.ToDouble(videospeed);
            ttSpeedCustom.IsOpen = true;
        }

        private async void btnEffects_Click(object sender, RoutedEventArgs e)
        {
            await dlgEffects.ShowAsync();
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
        }
        private ObservableCollection<SongModel> AllAvailableSongs = new ObservableCollection<SongModel>();

        private async Task ScanAllFoldersAsync()
        {
            AllAvailableSongs.Clear();

            var userPaths = UserDataPaths.GetDefault();

            string[] searchPaths =
            {
        userPaths.Music,
        userPaths.Downloads,
        userPaths.Pictures,
        userPaths.Videos
    };

            var extensions = new HashSet<string>
    {
        ".mp3", ".wav", ".aac", ".flac", ".m4a", ".ogg", ".wma",
        ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm"
    };

            foreach (var path in searchPaths)
            {
                if (!Directory.Exists(path))
                    continue;

                IEnumerable<string> files;

                try
                {
                    files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories);
                }
                catch
                {
                    continue;
                }

                foreach (var file in files)
                {
                    string ext = Path.GetExtension(file).ToLower();

                    if (!extensions.Contains(ext))
                        continue;

                    try
                    {
                        StorageFile storageFile =
                            await StorageFile.GetFileFromPathAsync(file);

                        MusicProperties props =
                            await storageFile.Properties.GetMusicPropertiesAsync();

                        var song = new SongModel
                        {
                            Title = string.IsNullOrEmpty(props.Title)
                                ? Path.GetFileNameWithoutExtension(file)
                                : props.Title,

                            Artist = string.IsNullOrEmpty(props.Artist)
                                ? "Unknown"
                                : props.Artist,

                            AlbumName = props.Album ?? "",

                            SongDuration = props.Duration,

                            FilePath = file
                        };

                        await DispatcherQueue.EnqueueAsync(() =>
                        {
                            AllAvailableSongs.Add(song);
                        });
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex.Message, "HomeWindow", Logger.LogLevelType.Error);
                    }
                }
            }
        }
        private void btnSetCustomSpeed_Click(object sender, RoutedEventArgs e)
        {
            if (player != null && maintimer != null)
            {


                if (!double.IsNaN(nmbSpeedCustom.Value))
                {
                    string speed = nmbSpeedCustom.Value.ToString();
                    videospeed = speed;
                    if (double.TryParse(speed, System.Globalization.CultureInfo.InvariantCulture, out double speedfloat))
                    {
                        player.Speed = speedfloat;
                    }
                }
            }
        }



        private void hypGoToLocation_Click(object sender, RoutedEventArgs e)
        {
            NavigateToExplorer.GoToLocation(txtInfoFilePath.Text);
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
                Logger.Log($"Failed to save rating: {ex.Message}", "HomeWindow", Logger.LogLevelType.Error);
            }
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            CopyToClipboard.CopyStringToClipboard(txtInfoFilePath.Text);
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
            if (currentvol == "0")
            {
                double orig = Convert.ToDouble(originalvolume);
                sldVolume.Value = orig;
                VolumeChange(orig);

            }
            else
            {
                sldVolume.Value = 0;
                VolumeChange(0);
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
                    Headersearch = $"Results for '{selectedSong.Title}'";
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
        TextBlock currentinfoedit = new();
        string old = "";
        private void txtInfoAlbum_PointerPressed(object sender, PointerRoutedEventArgs e)
        {

        }



        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var Buttond = sender as Button;
            if (Buttond == null) return;
            var txtBlock = Buttond.Content as TextBlock;
            if (txtBlock != null)
            {
                ttEditInfo.Target = txtBlock;
                ttEditInfo.IsOpen = true;
                ttEditInfo.Title = "Edit " + txtBlock.Tag + " info";
                currentinfoedit = txtBlock;
                txtEditTT.Tag = txtBlock.Tag;
                txtEditTT.Text = txtBlock.Text;
                old = txtBlock.Text;
            }
        }
        string oldpath = "";
        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            //Relocate missing file
            if (frmMain.Content is MusicLibrary activePage)
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                picker.FileTypeFilter.Add("*");
                WinRT.Interop.InitializeWithWindow.Initialize(
                    picker,
                    WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance)
                );

                StorageFile newFile = await picker.PickSingleFileAsync();
                if (newFile != null)
                {
                    activePage.UpdatePath(oldpath, newFile.Path);
                    ttFileMissing.IsOpen = false;
                    iBFileMissing.Severity = InfoBarSeverity.Success;
                    iBFileMissing.Title = "";
                    iBFileMissing.Message = "File relocated successfully";
                    ttFileMissing.IsOpen = true;
                    ObservableCollection<string> str = new();
                    str.Add(newFile.Path);
                    LoadFileFromPath(str);
                }
            }
        }

        private void txtEditTT_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtEditTT.Text == "")
            {
                txtEditTT.Text = old;
            }
            string tagtoedit = txtEditTT.Tag?.ToString() ?? "";
            if (tagtoedit == null) return;
            currentinfoedit.Text = txtEditTT.Text;
        }

        private void mnftEditProperty_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuFlyoutItem)sender;

            var flyout = menuItem.Parent as MenuFlyout;

            var button = flyout?.Target as Button;

            if (button != null)
            {

                if (button == null) return;
                var txtBlock = button.Content as TextBlock;
                if (txtBlock != null)
                {
                    ttEditInfo.Target = txtBlock;
                    ttEditInfo.IsOpen = true;
                    ttEditInfo.Title = "Edit " + txtBlock.Tag + " info";
                    currentinfoedit = txtBlock;
                    txtEditTT.Tag = txtBlock.Tag;
                    txtEditTT.Text = txtBlock.Text;
                    old = txtBlock.Text;
                }
            }

        }

        private void mnftCopyProperty_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuFlyoutItem)sender;

            var flyout = menuItem.Parent as MenuFlyout;

            var button = flyout?.Target as Button;

            if (button != null)
            {
                string textToCopy = button.Content?.ToString() ?? string.Empty;

                CopyToClipboard.CopyStringToClipboard(textToCopy);
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {

            string pfn = Package.Current.Id.FamilyName;

            await Launcher.LaunchUriAsync(
                new Uri($"ms-settings:defaultapps?registeredAppUser={pfn}"));
        }

        private void hypGoToSettings_Click(object sender, RoutedEventArgs e)
        {
            frmMain.Navigate(typeof(SettingsPage), "WinRelate");

        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox chck && chck.IsChecked == true)
            {
                txtUserInfoDefaultApp.Visibility = Visibility.Visible;
                hypGoToSettings.Visibility = Visibility.Visible;
            }
        }
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
           
        }

        private async void ttDefaultAppSet_CloseButtonClick(TeachingTip sender, object args)
        {
            var currentSettings = await SettingsHelper.LoadSettingsAsync();
            if (chckShowDefault.IsChecked == true)
            {
                currentSettings.ShowDefaultMessage = true;
            }
            else
            {
                currentSettings.ShowDefaultMessage = false;
            }
            await SettingsHelper.SaveSettingsAsync(currentSettings);

        }

        private void btnEffects_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void btnSetReverb_Click(object sender, RoutedEventArgs e)
        {
            if (player == null) return;
            if (player.Config.Audio.Filters == null)
                player.Config.Audio.Filters = new List<Filter>();

            player.Config.Audio.Filters.Clear();

            // Step 1: Add filter BEFORE opening media
            player.Config.Audio.Filters.Add(new Filter()
            {
                Name = "freeverb",
                Args = "roomsize=0.9:damp=0.5:wet=0.3:dry=0.8"
            });

            // Step 2: Open media (this builds pipeline WITH filter)
            player.Open(currentVideoPath);

            // Step 3: (optional) tweak AFTER playback starts
            player.Config.Audio.UpdateFilter("freeverb", "roomsize", "0.9");

            foreach (var f in player.Config.Audio.Filters)
            {
                Debug.WriteLine(f.Name + " | " + f.Args);
            }
            player.Config.Audio.Filters.Clear();

            player.Config.Audio.Filters.Add(new Filter()
            {
                Name = "volume",
                Args = "volume=0.1"
            });

            // Force rebuild
            player.Stop();
            player.Open(currentVideoPath);
        }
    }
}

