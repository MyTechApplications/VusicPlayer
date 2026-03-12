using ABI.Microsoft.UI.Xaml;
using FlyleafLib;
using FlyleafLib.MediaPlayer;
using LibVLCSharp.Shared;
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

//Vusic Player Version 1.1.0.0 Build 06.03.2026
//Development Reset  - 27/02/2026
//Switch to FlyLeaf Media Engine from LibVLCsharp due to failures with rendering of playbacks and audio output
//Code Cleanup Initiated
//FFmpeg DLLs to be shipped with the app

namespace VusicPlayer
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    ///DEVELOPMENT
    public sealed partial class HomeWindow : Window
    {

        public HomeWindow()
        {
            InitializeComponent();

            txtPreviewBuild.Text = $"Vusic Player Version {Appversionstrings.AppVersion + Environment.NewLine} {Appversionstrings.VersionType} Build {Appversionstrings.BuildNumber}";
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

                await ScanAllFoldersAsync();
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
            // This checks which app is currently the default for .mp3
            var result = await Launcher.FindFileHandlersAsync(".mp3");

            foreach (var handler in result)
            {
                // Check if the handler's Package Family Name matches yours
                if (handler.PackageFamilyName == Windows.ApplicationModel.Package.Current.Id.FamilyName)
                {
                    // Note: This only tells you if your app IS an option. 
                    // Finding if it is the SPECIFIC default is more complex due to privacy.
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
            if(currentSettings.ShowDefaultMessage == false)
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

            // Reattach acrylic if needed

        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // Make sure any Mica/Acrylic controller is disposed
            configurationSource = null;
            if (player != null)
            {
                player.Stop();
                player.Dispose();
            }

            acrylicController?.Dispose();
            acrylicController = null;

            Activated -= Window_Activated;
            configurationSource = null;
            if (player != null)
            {
                player.Stop();
                player.Dispose();
            }
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
            if (player == null) return;
            double newPosition = sldMain.Value / sldMain.Maximum;
            player.CurTime = TimeSpan.FromSeconds(sldMain.Value).Ticks;
            var curTime = TimeSpan.FromTicks(player.CurTime);
            txtRunningDuration.Text = curTime.ToString(@"hh\:mm\:ss");
            /*   if (_mediaPlayer != null)
               {
                   if (_mediaPlayer.State != VLCState.Stopped)
                   {
                       _mediaPlayer.Position = (float)newPosition;
                       long currentTimeMs = _mediaPlayer.Time;
                       txtRunningDuration.Text = TimeSpan.FromMilliseconds(currentTimeMs).ToString(@"hh\:mm\:ss");

                       _mediaPlayer.Mute = false;
                       if (stateofplay == "playing")
                           maintimer.Start();
                   }
                   else
                   {
                   }*/
            _isDragging = false;
            maintimer.Start();
            /*      _mediaPlayer.Play();
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


        private void SldMain_DragStarted()
        {

            _isDragging = true;
            maintimer.Stop();
        }
        Player? player;
        public async void LoadFileFromPath(ObservableCollection<string> path)
        {
            _pendingPath = path[currentVideoIndex];
            if (File.Exists(_pendingPath))
            {
                currentVideoPath = _pendingPath;
                PlaybackState.CurrentlyPlayingPath = _pendingPath;

                if (player == null)
                {
                    player = new Player(new Config());
                    mediaEngine.Player = player;
                }
                player.Open(path[currentVideoIndex]);
                player.Play();
                stateofplay = "playing";
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
                StorageFile file = await StorageFile.GetFileFromPathAsync(_pendingPath);
                var musicProps = await file.Properties.GetMusicPropertiesAsync();

                TimeSpan duration = musicProps.Duration;

                // Set slider max from metadata
                sldMain.Maximum = duration.TotalSeconds;

                // Set total duration label
                txtTotalDuration.Text = duration.ToString(@"hh\:mm\:ss");

                player.Audio.Volume = (int)sldVolume.Value;
                originalvolume = player.Audio.Volume;
                if (originalvolume.HasValue)
                {
                    currentvol = originalvolume.Value.ToString();
                }
                maintimer.Start();
                _ = SaveRecents();
            }
        }

        private void PlayNext()
        {
            if (currentVideoIndex + 1 < queuepaths.Count)
            {
                PlayVideoAtIndex(currentVideoIndex + 1);
            }
            player?.Stop();
            maintimer.Stop();
            //    _mediaPlayer.Position = 0;
            sldMain.Value = 0;
            txtRunningDuration.Text = "00:00:00";

            imgPlayPause.Source =
                new BitmapImage(new Uri("ms-appx:///Assets/play.png"));
            stateofplay = "paused";
        }
        private async Task SaveRecents()
        {
            var settings = await SettingsHelper.LoadSettingsAsync();
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
                // Insert at the start of the list
                settings.RecentMusic.Insert(0, NewRecent);
            }

            await SettingsHelper.SaveSettingsAsync(settings);
        }
        string currentVideoPath = "";
        int currentVideoIndex = 0;
        bool mediaended;
        private async void CheckForFileArguments()
        {
            var activatedArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
            if (activatedArgs.Kind == ExtendedActivationKind.File)
            {
                var fileArgs = (FileActivatedEventArgs)activatedArgs.Data;
                var file = fileArgs.Files.FirstOrDefault();

                if (file != null)
                {
                    string filePath = file.Path;

                    string extension = Path.GetExtension(filePath).ToLower();

                    string[] videoExtensions = { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm" };
                    string[] audioExtensions = { ".mp3", ".wav", ".aac", ".flac", ".m4a", ".ogg", ".wma" };

                    if (videoExtensions.Contains(extension))
                    {
                        var videoItems = new ObservableCollection<VideoItem>();
                        videoItems.Add(new VideoItem { FilePath = filePath });

                        var playerWindow = new MainWindow(videoItems, filePath, 0, true);

                        playerWindow.Activate();
                        App.SetCurrentMainWindow(playerWindow);
                        App.VideoPlayerWindowInstance = playerWindow;
                        return;
                    }
                    else if (audioExtensions.Contains(extension))
                    {
                        var home = HomeWindow.ShowWindow();
                        home.LoadFileFromPath(new ObservableCollection<string> { filePath });
                        return;
                    }
                }

            }
        }
        private static HomeWindow? instance;
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

            //     _mediaPlayer.Media = media;
            //       _mediaPlayer.Play();
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

            queuepaths.Clear();
            queuepaths = path;
            if (queuepaths.Contains(currentVideoPath))
            {
                currentVideoIndex = queuepaths.IndexOf(currentVideoPath);
            }

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
        // The "Is the engine started?" flag
        private string _pendingPath = "";
        private async Task PreloadLibVLCAsync(LibVLCSharp.Platforms.Windows.InitializedEventArgs e)
        {
            await Task.Run(() =>
            {
                _libVLC = new LibVLC(enableDebugLogs: true, e.SwapChainOptions);
                _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
            });
        }

        private LibVLC? _libVLC;
        private LibVLCSharp.Shared.MediaPlayer? _mediaPlayer;
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
        private void nvgMain_SelectionChanged(
            NavigationView sender,
            NavigationViewSelectionChangedEventArgs args)
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
        private async void btnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (player == null) return;
            if (stateofplay == "playing")
            {
                player.Pause();
                stateofplay = "paused";
                maintimer.Stop();
                imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/play.png"));
            }
            else
            {
                player.Play();
                stateofplay = "playing";
                maintimer.Start();
                imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/pause.png"));
            }
            /*   if (_mediaPlayer == null || _isProcessing)
                   return;

               _isProcessing = true;

               try
               {
                   if (_mediaPlayer.State == VLCState.Stopped)
                   {
                       float seekTo = (float)(sldMain.Value / sldMain.Maximum);
                       _mediaPlayer.Play();
                       await Task.Delay(100);
                       _mediaPlayer.Position = seekTo;

                       stateofplay = "playing";
                       maintimer.Start();
                       UpdatePlayUI(true);
                       return;
                   }

                   _ = SaveRecents(); // run in background

                   if (stateofplay == "playing")
                   {
                       _mediaPlayer.SetPause(true);
                       stateofplay = "paused";
                       maintimer.Stop();
                   }
                   else
                   {
                       _mediaPlayer.SetPause(false);
                       stateofplay = "playing";
                       maintimer.Start();
                   }

                   UpdatePlayUI(stateofplay == "playing");

                   await Task.Delay(100);
               }
               finally
               {
                   _isProcessing = false;
               }*/
        }

        /// <summary>
        /// Updates the Play/Pause button icon and tooltip based on state.
        /// </summary>
        private void UpdatePlayUI(bool isPlaying)
        {
            string iconName = isPlaying ? "pause" : "play";
            string toolTipText = isPlaying ? "Pause" : "Play";

            imgPlayPause.Source = new BitmapImage(new Uri($"ms-appx:///Assets/{iconName}.png"));
            ToolTipService.SetToolTip(btnPlayPause, toolTipText);
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
            if (player != null && maintimer != null)
            {
                //       player.Speed = 1.5;
                //  player?.Pause();
                //   maintimer.Stop();
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
        }
        private ObservableCollection<SongModel> AllAvailableSongs = new ObservableCollection<SongModel>();

        private async Task ScanAllFoldersAsync()
        {
            AllAvailableSongs.Clear();

            string[] searchPaths =
            {
        UserDataPaths.GetDefault().Music,
        UserDataPaths.GetDefault().Downloads,
        UserDataPaths.GetDefault().Pictures,
        UserDataPaths.GetDefault().Videos,
    };

            var extensions = new[] { ".mp3", ".flac", ".m4a" };

            foreach (var path in searchPaths)
            {
                try
                {
                    var files = Directory.EnumerateFiles(
                        path,
                        "*.*",
                        SearchOption.AllDirectories);

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

                            AllAvailableSongs.Add(new SongModel
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
                            });
                        }
                        catch
                        {
                            // Some files may fail metadata extraction
                        }
                    }
                }
                catch
                {
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
                Logger.Log($"Failed to save rating: {ex.Message}", "HomeWindow", Logger.LogLevelType.Error);
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
                DataPackage dt = new();
                dt.SetText(button.Content as string);
                Clipboard.SetContent(dt);
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
    }
}

