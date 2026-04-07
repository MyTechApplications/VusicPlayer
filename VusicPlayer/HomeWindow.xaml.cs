using ABI.Microsoft.UI.Xaml;
using CommunityToolkit.WinUI;
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
            //  LoadTheme();
            if (App.HomeWindowInstance != null)
            {
                var rootElement = (FrameworkElement)App.HomeWindowInstance.Content;
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
       

            this.DispatcherQueue.TryEnqueue(async () =>
            {
                //  await CheckAndDownloadUpdate();

                //  await ScanAllFoldersAsync();
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
                FFmpegPath = ":FFmpeg",
            });
        
            SplashComplete();
            //  CheckForFileArguments();
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
            frmNavigate.Visibility = Visibility.Visible;
            frmNavigate.Navigate(typeof(MasterPage), null);
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

        private async void Window_Closed(object sender, WindowEventArgs args)
        {
            var currentSettings = await SettingsHelper.LoadSettingsAsync();
            var playlsits = currentSettings.SavedPlaylists;
            foreach (var item in playlsits)
            {
                item.PlaylistNowPlaying = "";
            }
            await SettingsHelper.SaveSettingsAsync(currentSettings);
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
        public async void ReattachUI()
        {

            //  PlayerService.AttachUI(txtRunningDuration, sldMain, txtTotalDuration);
        }
        
        public async void Create()
        {
            PlayerService.CreatePlayer();
        }

        private async void PlayVideoPath(string path)
        {

            if (PlayerService.MasterPlayer == null)
            {
                PlayerService.CreatePlayer();
            }
            PlayerService.PlayFile(path);

            PlaybackState.CurrentlyPlayingPath = path;

            //if (player != null)
            //{
            //    player.Audio.Volume = (int)sldVolume.Value;
            //    originalvolume = player.Audio.Volume;
            //}
            //if (originalvolume.HasValue)
            //{
            //    currentvol = originalvolume.Value.ToString();
            //}
           
        }

    
   
        private static HomeWindow? instance;
        public static HomeWindow? HideWindow()
        {
            if (instance != null)
            {
                PlayerService.Pause();
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
            //    instance.CheckForFileArguments();
            return instance;
        }



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




      

   
    }
}

