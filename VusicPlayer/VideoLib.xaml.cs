#region Namespaces 
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Animations;
using FlyleafLib;
using FlyleafLib.MediaPlayer;
using LibVLCSharp.Shared;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.WindowManagement;
using WinRT.Interop;
using AppWindow = Microsoft.UI.Windowing.AppWindow;
using Path = System.IO.Path;
using WindowId = Microsoft.UI.WindowId;
#endregion


namespace VusicPlayer
{

    public sealed partial class VideoLib : Page
    {
        #region Fields
        #region Media & Playlist
        private ObservableCollection<VideoItem>? videos;
        private List<string> _allVideoNames = new List<string>();
        private List<string> _allSubtitleNames = new List<string>();
        private List<string> listsubtitlespaths = new();

        private string currentVideoPath = "";
        private string filepathtemp = "";
        private int currentVideoIndex = 0;

        private bool issubtitlesenabled = false;
        private string snapshotdirectorypath = KnownFolders.PicturesLibrary.Path;
        #endregion

        #region Playback State
        private string stateofplay = "paused";
        private string currentvol = "0";
        private string volumestate = "1";
        private string? originalvolume;

        private double currentdur = 0;
        private long totaldurationtemp;
        private double totalTime = 20; // seconds to show full progress
        private double elapsedTime = 0;

        private bool newinstance = false;
        private bool _isDragging = false;
        #endregion

        #region UI & Controls
        private Microsoft.UI.Windowing.AppWindow? _appWindow;
        private InputCursor? originalInputCursor;

        bool _loadedvisible;
        private bool isglowenabled = true;
        private bool isfullscreen = false;
        private bool _controlsVisible = true;
        private bool seektxtvisible = false;
        private bool ispinned = false;

        private int showcontrols = 0;
        private int showloaded = 0;
        private int seektimercount;
        private int nextbuttonwidthtimer = 0;
        #endregion

        #region Timers
        private DispatcherTimer? MainTimer;
        private DispatcherTimer? seektimer;
        private DispatcherTimer? nextVideoTimer;
        private DispatcherTimer? _hideTimer;
        private DispatcherTimer? _hideLoadedTimer;

        private DispatcherQueueTimer? _saveTimer;
        private bool isSaving = false;
        public void StopAutoSave() => _saveTimer?.Stop();
        #endregion

        #region Animations (Storyboards)
        private Storyboard? _fadeIn;
        private Storyboard? _fadeIn2;
        private Storyboard? _fadeIn3;
        private Storyboard? _fadeOut;
        private Storyboard? _fadeOut2;
        private Storyboard? _fadeOut3;
        #endregion

        #region Media Engine
        Player? player;
        #endregion
        #endregion
        #region Initialization
        private async void LoadComponents(VideoPlayerPageParams parameter)
        {
            this.RequestedTheme = ElementTheme.Dark; //Dark theme always true
            //Slider events
            sldMain.DragStarted += SldMain_DragStarted;
            sldMain.DragCompleted += SldMain_DragCompleted;

            this.Content.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(Global_KeyDown), true);

            //Load videos from parameters or settings
            videos = parameter!.Videos!;
            currentVideoPath = parameter!.SelectedVideoPath!;
            newinstance = parameter!.newinst;
            currentdur = parameter!.CurrentRunningDuration;
            LoadFlyLeafEngine();
            if (currentVideoPath != null)
            {
                PlayMedia(currentVideoPath);
                Load.Visibility = Visibility.Collapsed;
                var settings = await SettingsHelper.LoadSettingsAsync();
                var item = settings.SavedItems.FirstOrDefault(x => x.FilePath == currentVideoPath);
                if (item != null && player != null)
                {
                    player.CurTime = (long)item.CurrentDuration;
                    UpdatePlayPauseUI("play");
                    var curTime = TimeSpan.FromTicks(player.CurTime);
                    txtRunningDuration.Text = curTime.ToString(@"hh\:mm\:ss");
                    sldMain.Value = curTime.TotalSeconds;
                    txtLoaded.Text = $"Loaded playback of {txtFileName.Text} at {txtRunningDuration.Text}";
                }
            }
        }
        private void LoadFlyLeafEngine()
        {
            Engine.Start(new EngineConfig()
            {
                LogOutput = ":debug",
                UIRefresh = true,
                LogLevel = FlyleafLib.LogLevel.Debug,
                FFmpegLogLevel = Flyleaf.FFmpeg.LogLevel.Warn,


                FFmpegPath = Path.Combine(AppContext.BaseDirectory, "FFmpegDLLs")
            });
            mediaPlayerMain.Player = player;
        }
        private void Global_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (player == null) return;

            if (e.Key == Windows.System.VirtualKey.Space)
            {
                // Check if the focus is in a TextBox (we don't want to pause while typing)
                var focusedElement = FocusManager.GetFocusedElement(this.XamlRoot);
                e.Handled = true;
                // 1. Skip if typing in a TextBox
                // 2. Skip if the PlayPauseButton is focused (let the button's own Click handler take over)
                if (focusedElement is TextBox || focusedElement == btnPlayPause)
                {
                    return;
                }
                PlayPause();
                ShowControls();
                e.Handled = true;
            }
        }
        #endregion

        //Vusic Player Version 1.1.0.0 Build 06.03.2026
        //Development Reset  - 27/02/2026
        //Switch to FlyLeaf Media Engine from LibVLCsharp due to failures with rendering of playbacks and audio output
        //Code Cleanup Initiated
        //FFmpeg DLLs to be shipped with the app
        public VideoLib(VideoPlayerPageParams param)
        {
            InitializeComponent();
            LoadComponents(param);
        }
        #region Slider Events
        private void SldMain_DragStarted()
        {
            if (player != null)
            {
                _isDragging = true;
                MainTimer?.Stop();
            }
        }
        private void SldMain_DragCompleted()
        {
            if (player != null)
            {
                player.CurTime = (long)(sldMain.Value * TimeSpan.TicksPerSecond);
                txtRunningDuration.Text = TimeSpan.FromSeconds(sldMain.Value).ToString(@"hh\:mm\:ss"); //Format
                _isDragging = false;
                MainTimer?.Start();
            }
        }
        #endregion

        //Video Control
        #region Video Control Events
        private void LoadCustomAspectRatioDialog()
        {
            if (App.VideoPlayerWindowInstance == null) { return; }
            var dlg = VideoOptionsWindow.ShowDialog(customAspectRatio, 600, 600, true);
            dlg.CloseRequested += () =>
            {
                dlg.HideDialog();
            };
            tbviVideoOptions.IsSelected = true;
            sbiAspectRatio.IsSelected = true;
        }

        private void AspectRatioSetCustom()
        {

        }

        private void SetAspectRatio(string ratio)
        {
            if (player == null) return;
            switch (ratio)
            {
                case "Default":
                    player.Config.Video.AspectRatio = AspectRatio.Keep; // Default
                    break;
                case "16:9":
                    player.Config.Video.AspectRatio = "16:9";
                    break;
                case "4:3":
                    player.Config.Video.AspectRatio = "4:3";
                    break;
                case "1:1":
                    player.Config.Video.AspectRatio = "1:1";
                    break;
                case "21:9":
                    player.Config.Video.AspectRatio = "21:9";
                    break;
                case "2.35:1":
                    player.Config.Video.AspectRatio = "2.35:1";
                    break;
                case "Custom":
                    LoadCustomAspectRatioDialog();
                    break;
                default:
                    break;
            }
        }
        #endregion

        //Audio Control
        #region Audio Control Events
        private void VolumeChange(double obj)
        {
            if (player == null) return;

            int vol = (int)obj;
            currentvol = vol.ToString();
            txtVolumepercent.Text = currentvol;
            player.Audio.Volume = vol;

            // 1. Determine the Glyph
            fntIcon.Glyph = vol switch
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

            fntIcon.Foreground = new SolidColorBrush(iconColor);
        }
        #endregion

        //Media Control Events
        #region Media Control Events
        private void PlayPause()
        {
            if (player == null) return;
            if (stateofplay == "playing")
            {
                player.Pause();

                _saveTimer?.Stop();
                UpdatePlayPauseUI("pause");
            }
            else
            {
                player.Play();
                _saveTimer?.Start();
                UpdatePlayPauseUI("play");
            }

        }
        void SeekRelative(int seconds)
        {
            if (player == null) return;
            player.Seek(seconds * 1000);
            var curTime = TimeSpan.FromTicks(player.CurTime);
            txtRunningDuration.Text = curTime.ToString(@"hh\:mm\:ss");
            sldMain.Value = curTime.TotalSeconds;
        }

        private void UpdatePlayPauseUI(string state)
        {
            if (player == null) return;
            string icon = "play";
            if (state == "play")
            {
                stateofplay = "playing";
                icon = "pause";
            }
            else
            {
                stateofplay = "paused";
                icon = "play";
            }
            imgPlayPause.Source = new BitmapImage(new Uri($"ms-appx:///Assets/{icon}.png"));
        }
        private void PlayNext()
        {
            if (player == null) return;
            if (videos != null)
            {
                if (currentVideoIndex + 1 < videos.Count)
                {
                    PlayVideoAtIndex(currentVideoIndex + 1);
                }
                player?.Stop();
                MainTimer?.Stop();
                sldMain.Value = 0;
                txtRunningDuration.Text = "00:00:00";
                UpdatePlayPauseUI("play");
            }
        }
        private void PlayNextVideo()
        {
            int nextIndex = currentVideoIndex + 1;
            if (videos != null)
                if (nextIndex >= videos.Count)
                    nextIndex = 0;

            ResetNextUI();
            PlayVideoAtIndex(nextIndex);
        }

        private async void PlayMedia(string path)
        {
            player = new Player(new Config());
            mediaPlayerMain.Player = player;
            player.Open(path);
            player.Play();
            UpdatePlayPauseUI("play");
            sldMain.Value = 0;
            MainTimer = new DispatcherTimer();
            MainTimer.Interval = TimeSpan.FromMilliseconds(250);
            MainTimer.Tick += MainTimer_Tick;

            MainTimer.Start();

            txtFileName.Text = Path.GetFileName(path);
            txtLoaded.Text = $"Loaded media '{txtFileName.Text}'";
            StorageFile file = await StorageFile.GetFileFromPathAsync(currentVideoPath);
            var props = await file.Properties.GetMusicPropertiesAsync();
            double totalSeconds = props.Duration.TotalSeconds;
            txtTotalDuration.Text = props.Duration.ToString(@"hh\:mm\:ss");
            sldMain.Maximum = totalSeconds;
            _hideTimer = new DispatcherTimer();
            _hideTimer.Interval = TimeSpan.FromSeconds(1);
            _hideTimer.Tick += _hideTimer_Tick; ;
            _hideTimer.Start();
            DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            StartAutoSave();
            seektimer = new DispatcherTimer();
            seektimer.Interval = TimeSpan.FromSeconds(2);
            seektimer.Tick += Seektimer_Tick;
            InitializeAllAnimations();
            ShowLoaded();
            _hideLoadedTimer = new DispatcherTimer();
            _hideLoadedTimer.Interval = TimeSpan.FromSeconds(1);
            _hideLoadedTimer.Tick += _hideLoadedTimer_Tick;
            _hideLoadedTimer.Start();
            stateofplay = "playing";
            RootGrid.Focus(FocusState.Programmatic);
            btnFullScreen.IsEnabled = true;
            sldVol.IsEnabled = true;
            if (player != null)
            {
                sldVol.Value = player.Audio.Volume;
                txtVolumepercent.Text = player.Audio.Volume.ToString();
            }
        }
        public void CleanupPlayer()
        {
            if (player != null)
            {
                _saveTimer?.Stop();
                MainTimer?.Stop();
                player.Stop();
                player.Dispose();
            }
        }

        private async void PlayVideoAtIndex(int index)
        {
            if (videos == null || index < 0 || index >= videos.Count) return;

            var selectedVideo = videos[index];
            if (selectedVideo?.FilePath == null)
            {
                Logger.Log("Error: Video item or FilePath is null when playing at next index.", "VideoPlayer", Logger.LogLevelType.Error);
                return;
            }

            currentVideoIndex = index;
            currentVideoPath = selectedVideo.FilePath;
            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(currentVideoPath);
                var props = await file.Properties.GetMusicPropertiesAsync();

                double totalSeconds = props.Duration.TotalSeconds;

                sldMain.Maximum = totalSeconds;
                txtTotalDuration.Text = props.Duration.ToString(@"hh\:mm\:ss");
                totaldurationtemp = (long)props.Duration.TotalMilliseconds;

                string fileName = Path.GetFileName(currentVideoPath);
                txtFileName.Text = fileName;
                txtLoaded.Text = $"Loaded media '{fileName}'";

                btnNextVideo.Visibility = Visibility.Collapsed;
                PlayMedia(currentVideoPath);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error loading video metadata when Playing at next Index: {ex.Message}", "VideoPlayer", Logger.LogLevelType.Error);
            }
        }
        #endregion

        //IO Pickers
        #region IO Pickers
        private async Task<StorageFile> PickMediaFileAsync()
        {
            FileOpenPicker picker = new FileOpenPicker();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.VideoPlayerWindowInstance);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.FileTypeFilter.Add(".mp4");
            picker.FileTypeFilter.Add(".mp3");

            return await picker.PickSingleFileAsync();
        }
        private async Task<StorageFile> PickSubtitles()
        {
            FileOpenPicker picker = new FileOpenPicker();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App. VideoPlayerWindowInstance);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.FileTypeFilter.Add(".srt");
            picker.FileTypeFilter.Add(".ass");
            picker.FileTypeFilter.Add(".vtt");

            return await picker.PickSingleFileAsync();
        }
        #endregion

        //Timer Events
        #region Timer Events
        private async void MainTimer_Tick(object? sender, object e)
        {
            if (!_isDragging && player != null)
            {
                var curTime = TimeSpan.FromTicks(player.CurTime);
                txtRunningDuration.Text = curTime.ToString(@"hh\:mm\:ss");
                sldMain.Value = curTime.TotalSeconds;
                if (videos == null || videos.Count <= 1)
                {
                    ResetNextUI();
                    return;
                }

                long remainingMs = player.Duration - player.CurTime;
                if (remainingMs <= 20000 && remainingMs > 0)
                {
                    if (btnNextVideo.Visibility != Visibility.Visible)
                        btnNextVideo.Visibility = Visibility.Visible;
                    if (remainingMs <= 2000)
                    {
                        nextVideoProgress.Value = 2000 - remainingMs;
                    }
                    else
                    {
                        nextVideoProgress.Value = 0;
                    }
                }
                else
                {
                    ResetNextUI();
                }

                if (remainingMs <= 0)
                {
                    PlayNextVideo();
                }
            }
        }
        private void _hideLoadedTimer_Tick(object? sender, object e)
        {
            showloaded++;
            if (showloaded == 4)
            {
                showloaded = 0;
                HideLoadedText();
                _hideLoadedTimer?.Stop();
            }
        }
        public void StartAutoSave()
        {
            if (player == null) return;

            _saveTimer?.Stop();

            _saveTimer = this.DispatcherQueue.CreateTimer();
            _saveTimer.Interval = TimeSpan.FromSeconds(4);

            _saveTimer.Tick += async (s, e) =>
            {
                if (string.IsNullOrEmpty(currentVideoPath) || player == null) return;

                var settings = await SettingsHelper.LoadSettingsAsync();
                var item = settings.SavedItems.FirstOrDefault(x => x.FilePath == currentVideoPath);

                if (item != null)
                {
                    item.CurrentDuration = player.CurTime;
                    item.TotalDuration = player.Duration;
                }
                else
                {
                    settings.SavedItems.Add(new VideoProgress
                    {
                        FileName = Path.GetFileName(currentVideoPath),
                        FilePath = currentVideoPath,
                        CurrentDuration = player.CurTime,
                        TotalDuration = player.Duration
                    });
                }

                await SettingsHelper.SaveSettingsAsync(settings);
            };

            _saveTimer.Start();
        }
        private void Seektimer_Tick(object? sender, object e)
        {
            if (++seektimercount >= 2)
            {
                seektimercount = 0;
                HideSeekText();
                seektimer?.Stop();
            }
        }
        private void _hideTimer_Tick(object? sender, object e)
        {
            showcontrols++;
            if (showcontrols == 2)
            {
                showcontrols = 0;
                HideControls();
                GlowSurround.Visibility = Visibility.Collapsed;
                _hideTimer?.Stop();
            }
        }
        #endregion

        //Animation Events
        #region Animation Events
        private (Storyboard fadeIn, Storyboard fadeOut) CreateFadePair(DependencyObject target)
        {
            var fadeIn = new Storyboard();
            var fadeOut = new Storyboard();

            var fadeInAnim = new DoubleAnimation { To = 1, Duration = TimeSpan.FromMilliseconds(200) };
            var fadeOutAnim = new DoubleAnimation { To = 0, Duration = TimeSpan.FromMilliseconds(300) };

            Storyboard.SetTarget(fadeInAnim, target);
            Storyboard.SetTargetProperty(fadeInAnim, "Opacity");

            Storyboard.SetTarget(fadeOutAnim, target);
            Storyboard.SetTargetProperty(fadeOutAnim, "Opacity");

            fadeIn.Children.Add(fadeInAnim);
            fadeOut.Children.Add(fadeOutAnim);

            return (fadeIn, fadeOut);
        }
        void InitializeAllAnimations()
        {
            (_fadeIn, _fadeOut) = CreateFadePair(GlassRoot);
            (_fadeIn2, _fadeOut2) = CreateFadePair(txtLoaded);
            (_fadeIn3, _fadeOut3) = CreateFadePair(txtSeekForward);
        }
        private void SetVisibility(bool show, UIElement element, Storyboard? fadeIn, Storyboard? fadeOut, ref bool stateFlag)
        {
            if (show == stateFlag) return; // Already in the desired state

            stateFlag = show;
            element.IsHitTestVisible = show;

            if (show)
            {
                fadeOut?.Stop();
                fadeIn?.Begin();
            }
            else
            {
                fadeIn?.Stop();
                fadeOut?.Begin();
            }
        }
        void HideControls() => SetVisibility(false, RootPanel, _fadeIn, _fadeOut, ref _controlsVisible);
        void ShowControls() => SetVisibility(true, RootPanel, _fadeIn, _fadeOut, ref _controlsVisible);

        // Loaded Text
        void HideLoadedText() => SetVisibility(false, txtLoaded, _fadeIn2, _fadeOut2, ref _loadedvisible);
        void ShowLoaded() => SetVisibility(true, txtLoaded, _fadeIn2, _fadeOut2, ref _loadedvisible);

        // Seek Text (Special case because of the timer)
        void HideSeekText() => SetVisibility(false, txtSeekForward, _fadeIn3, _fadeOut3, ref seektxtvisible);

        void ShowSeekText()
        {
            SetVisibility(true, txtSeekForward, _fadeIn3, _fadeOut3, ref seektxtvisible);

            // Refresh the timer
            seektimer?.Stop();
            seektimercount = 0;
            seektimer?.Start();
        }
        #endregion

        //Window Events
        #region Window Events
        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(App.m_window);
            WindowId myWndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(myWndId);
        }

        private void FullScreen()
        {
            _appWindow ??= GetAppWindowForCurrentWindow();

            // 2. Toggle state and pick values
            isfullscreen = !isfullscreen;

            var targetPresenter = isfullscreen ? AppWindowPresenterKind.FullScreen : AppWindowPresenterKind.Default;
            var toolTipText = isfullscreen ? "Resize to Normal Window" : "Set Full Screen";

            // 3. Apply changes
            _appWindow.SetPresenter(targetPresenter);
            ToolTipService.SetToolTip(btnFullScreen, toolTipText);
        }

        #endregion

        //Subtitle Control
        #region Subtitle Control Events
        void PopulateSubtitleList()
        {
            //Populate subtitle list in the UI
        }
        void LoadFonts()
        {
            var fonts = CanvasTextFormat.GetSystemFontFamilies()
                                 .OrderBy(f => f)
                                 .ToList();

            cmbFonts.ItemsSource = fonts;

            cmbFonts.SelectedItem = "Segoe UI"; // default
        }
        #endregion


        //UI Events
        #region UI Click and Show Events
        private void Preset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Content is string ratio)
            {
                // Split "16:9" into [16, 9]
                var parts = ratio.Split(':');
                if (parts.Length == 2)
                {
                    WidthInput.Value = double.Parse(parts[0]);
                    HeightInput.Value = double.Parse(parts[1]);
                }
            }
        }
        private void CustomInput_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
        }
        private void SearchSubtitles_Click(object sender, RoutedEventArgs e)
        {
            ttSearch.IsOpen = true;
        }
        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
            }
            else
            {
                // Just pick the first result if they just hit enter
                var topResult = _allSubtitleNames.FirstOrDefault(n => n.ToLower().Contains(sender.Text.ToLower()));
                if (topResult != null)
                {

                }
            }
        }

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var searchTerm = sender.Text.ToLower();
                var results = _allSubtitleNames
                    .Where(name => name.ToLower().Contains(searchTerm))
                    .ToList();

                sender.ItemsSource = results;
            }
        }

        private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
        }

        private void btnVideo_Click(object sender, RoutedEventArgs e)
        {
        }
        private void Item_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftTakeSnapshot_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void mnftSnapshotDirectory_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();
            picker.FileTypeFilter.Add("*");
            var hwnd = WindowNative.GetWindowHandle(App.MainWindowInstance);
            InitializeWithWindow.Initialize(picker, hwnd);
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            StorageFolder folder = await picker.PickSingleFolderAsync();

            if (folder != null)
            {
                string folderPath = folder.Path;
                snapshotdirectorypath = folderPath;
                ToolTipService.SetToolTip(mnftSnapshotDirectory, snapshotdirectorypath);
            }
        }

        private void btnAudio_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SearchAudioTracks_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftAudioDevice_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftAudioOptions_Click(object sender, RoutedEventArgs e)
        {

        }
        private void btnVol_Click(object sender, RoutedEventArgs e)
        {

            if (volumestate == "0")
            {
                sldVol.Value = Convert.ToInt32(originalvolume);
                volumestate = "1";
            }
            else
            {
                originalvolume = currentvol;
                volumestate = "0";
                sldVol.Value = 0;
            }
            VolumeChange(sldVol.Value);
        }

        private void btnHome_Click(object sender, RoutedEventArgs e)
        {

        }


        private void GlassRoot_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ShowPanel();
            if (isglowenabled == true)
            {
                NeonPulseStoryboard.Begin();
            }
        }

        private void videoView_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            ShowPanel();
        }

        private void videoView_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ShowPanel();
        }
        private void sldVolume_ValueChanged_1(double obj)
        {
            VolumeChange(obj);
        }

        private void btnSubtitles_Click_1(object sender, RoutedEventArgs e)
        {
            issubtitlesenabled = true;
            PopulateSubtitleList();
        }

        private void ShowPanel()
        {
            if (ispinned == false)
            {
                ShowControls();
                if (isglowenabled)
                {
                    GlowSurround.Visibility = Visibility.Visible;
                }
                showcontrols = 0;
                if (originalInputCursor != null)
                {
                    RootGrid.InputCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
                }
                _hideTimer?.Start();
            }
        }
        private void GlassRoot_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            ShowPanel();
        }
        private void SubtitleClicked(object sender, RoutedEventArgs e)
        {
            if (player == null) return;
        }
        private void btnFullScreen_Click(object sender, RoutedEventArgs e)
        {
            FullScreen();
        }
        private void btnSeekAfter_Click(object sender, RoutedEventArgs e)
        {
            player.SeekForward();
            //      txtSeekForward.HorizontalAlignment = HorizontalAlignment.Right;
            //  txtSeekForward.Text = "+10";
            // SeekRelative(+10);
            // ShowSeekText();
        }
        private void btnSeekBefore_Click(object sender, RoutedEventArgs e)
        {
            txtSeekForward.HorizontalAlignment = HorizontalAlignment.Left;
            SeekRelative(-10);
            txtSeekForward.Text = "-10";
            ShowSeekText();
        }

        private void ResetNextUI()
        {
            nextVideoProgress.Value = 0;
            btnNextVideo.Visibility = Visibility.Collapsed;
        }
        private async void btnOpenVideo_Click(object sender, RoutedEventArgs e)
        {
            var file = await PickMediaFileAsync();
            if (file != null)
            {
                var stream = await file.OpenAsync(FileAccessMode.Read);
                var path = file.Path;
                if (player == null)
                    return;
                PlayMedia(path);
            }
        }
        private void AspectRatio_Click(object sender, RoutedEventArgs e)
        {
            //Set Aspect Ratio
            var menuItem = sender as MenuFlyoutItem;
            if (menuItem == null) return;
            if (player == null) return;
            string ratio = menuItem.Text;
            SetAspectRatio(ratio);
        }
        private async void btnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            PlayPause();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            LoadFonts();
            var result = await dialogmediaoptions.ShowAsync();
        }
        private void cmbFonts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbFonts.SelectedItem is string fontName)
            {
                txtSample.FontFamily = new FontFamily(fontName);
            }
        }

        private void numFontSize_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {

        }

        private void clrPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            txtSample.Foreground =
       new SolidColorBrush(args.NewColor);
            rctColor.Fill = new SolidColorBrush(args.NewColor);
        }

        private void dialogmediaoptions_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {

        }
        private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {


            }
            catch (Exception ex)
            {
                Logger.Log("Subtitle error:" + ex.Message, "VideoPlayer", Logger.LogLevelType.Error);
            }
        }
        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            int nextIndex = currentVideoIndex + 1;
            if (videos == null) return;
            if (nextIndex >= videos.Count)
            {
                nextIndex = 0; // loop back to first video (optional)
            }

            PlayVideoAtIndex(nextIndex);
            btnNextVideo.Visibility = Visibility.Collapsed;
        }
        #endregion


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

        }
        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            base.OnKeyDown(e);
            if (player == null) return;

            if (e.Key == Windows.System.VirtualKey.Left || e.Key == Windows.System.VirtualKey.Right)
            {
                bool isRight = e.Key == Windows.System.VirtualKey.Right;

                // 1. Set UI state based on direction
                txtSeekForward.HorizontalAlignment = isRight ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                txtSeekForward.Text = isRight ? "+10" : "-10";

                // 2. Perform logic
                SeekRelative(isRight ? 10 : -10);

                // 3. Update visibility and timers
                ShowSeekText(); // Assuming the refactored version from earlier
                _hideTimer?.Start();

                e.Handled = true;
            }
        }

        private void mnftHome_Click(object sender, RoutedEventArgs e)
        {
            if (stateofplay == "playing")
            {
                PlayPause();
                UpdatePlayPauseUI("pause");
            }
            HomeWindow.ShowWindow();
        }
        private bool _isPinningInProgress = false;
        private async void btnPin_Click(object sender, RoutedEventArgs e)
        {
            if (ispinned == false)
            {
                ispinned = true;
                Grid.SetRow(RootGrid, 1);
                RootGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
                RootPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
                ControlsOverlay.HorizontalAlignment = HorizontalAlignment.Stretch;
                RootGrid.VerticalAlignment = VerticalAlignment.Stretch;
                ControlsOverlay.VerticalAlignment = VerticalAlignment.Stretch;
                RootPanel.VerticalAlignment = VerticalAlignment.Stretch;
                GlassRoot.VerticalAlignment = VerticalAlignment.Stretch;
                GlassRoot.HorizontalAlignment = HorizontalAlignment.Stretch;
                GlassRoot.CornerRadius = new CornerRadius(0);
                GlassRoot.Margin = new Thickness(0, 0, 0, 0);
                _hideTimer?.Stop();
                txtFileName.Visibility = Visibility.Collapsed;
                txtPinFileName.Visibility = Visibility.Visible;
                btnPin.Visibility = Visibility.Collapsed;
                btnPin2.Visibility = Visibility.Visible;
                txtPinFileName.Text = txtFileName.Text;
                fntPin2.Glyph = "\uE77A";
                LinearGradientBrush myGradient = new LinearGradientBrush();
                myGradient.StartPoint = new Windows.Foundation.Point(0, 0);
                myGradient.EndPoint = new Windows.Foundation.Point(0, 1);

                // 2. Add the first GradientStop (#CC1A1A1A)
                myGradient.GradientStops.Add(new GradientStop()
                {
                    Color = ColorHelper.FromArgb(0xCC, 0x1A, 0x1A, 0x1A),
                    Offset = 0.0
                });

                // 3. Add the second GradientStop (#E60A0A0A)
                myGradient.GradientStops.Add(new GradientStop()
                {
                    Color = ColorHelper.FromArgb(0xE6, 0x0A, 0x0A, 0x0A),
                    Offset = 1.0
                });

                // 4. Apply to your Grid
                RootPanel.Background = myGradient;
            }
            else
            {
                ispinned = false;
                RootPanel.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
                RootGrid.HorizontalAlignment = HorizontalAlignment.Center;
                txtPinFileName.Visibility = Visibility.Collapsed;
                txtFileName.Visibility = Visibility.Visible;
                btnPin.Visibility = Visibility.Visible;
                btnPin2.Visibility = Visibility.Collapsed;
                ControlsOverlay.HorizontalAlignment = HorizontalAlignment.Center;
                RootGrid.VerticalAlignment = VerticalAlignment.Bottom;
                ControlsOverlay.VerticalAlignment = VerticalAlignment.Bottom;
                GlassRoot.VerticalAlignment = VerticalAlignment.Bottom;
                GlassRoot.HorizontalAlignment = HorizontalAlignment.Center;
                GlassRoot.CornerRadius = new CornerRadius(24);
                GlassRoot.Margin = new Thickness(0, 0, 0, 40);
                Grid.SetRow(RootGrid, 0);
                _hideTimer?.Start();
            }
            /*         if (_isPinningInProgress) return; // Ignore clicks if we are still processing
                     _isPinningInProgress = true;
                     try
                     {
                         if (ispinned == false)
                     {
                         fntPin.Glyph = "\uE77A";
                         ispinned = true;

                         // 1. Move the panel
                         roottpanel.Children.Remove(RootPanel);
                         grdPinnedPanel.Children.Add(RootPanel);

                         // 2. Adjust Layout
                         RootPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
                         GlassRoot.HorizontalAlignment = HorizontalAlignment.Stretch;
                         RootPanel.VerticalAlignment = VerticalAlignment.Stretch;
                         GlassRoot.VerticalAlignment = VerticalAlignment.Stretch;
                         GlassRoot.Margin = new Thickness(0);
                         GlassRoot.CornerRadius = new CornerRadius(0);

                         // 3. HANDLE GLOW: Instead of Collapsed, use Opacity
                         mnftGlowEffect.IsEnabled = false;
                         NeonGlow.CastTo = null;

                         // Wait for the UI thread to finish the 'Move' operation
                         RootPanel.Loaded += (s, e) => {
                             NeonGlow.CastTo = GlowSurround;
                             NeonGlow.Opacity = 0.7;
                         };
                         grdPinnedPanel.Visibility = Visibility.Visible;
                         _hideTimer?.Stop();
                         RootPanel.Opacity = 1;
                     }
                     else
                     {
                         mnftGlowEffect.IsEnabled = true;
                         fntPin.Glyph = "\uE718";

                         // 1. Move the panel back
                         grdPinnedPanel.Children.Remove(RootPanel);
                         roottpanel.Children.Add(RootPanel);

                         // 2. Reset Layout
                         GlassRoot.Margin = new Thickness(0, 0, 0, 40);
                         GlassRoot.CornerRadius = new CornerRadius(24);
                         ispinned = false;
                         grdPinnedPanel.Visibility = Visibility.Collapsed;

                         RootPanel.HorizontalAlignment = HorizontalAlignment.Center;
                         GlassRoot.HorizontalAlignment = HorizontalAlignment.Center;
                         RootPanel.VerticalAlignment = VerticalAlignment.Bottom;
                         GlassRoot.VerticalAlignment = VerticalAlignment.Bottom;

                         // 3. HANDLE GLOW: Re-activate target and opacity


                         _hideTimer?.Start(); 
                         GlowSurround.Visibility = Visibility.Visible;
                         NeonGlow.CastTo = null;
                             await Task.Delay(200); NeonGlow.CastTo = GlowSurround;
                             NeonGlow.Opacity = 0.7;
                             // Wait for the UI thread to finish the 'Move' operation
                             RootPanel.Loaded += (s, e) => {


                         };
                     }
                     }
                     finally
                     {
                         // Small buffer to let the shadow engine catch up before allowing another click
                         await Task.Delay(200);
                         _isPinningInProgress = false;
                     }*/
        }

        private void ControlsPanel_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (isglowenabled)
            {
                NeonPulseStoryboard.Stop();
                NeonGlow.Opacity = 0.7;
            }
        }

        private void mnftGlowEffect_Click(object sender, RoutedEventArgs e)
        {
            var glowcheck = sender as ToggleMenuFlyoutItem;
            if (glowcheck == null) return;
            if (glowcheck.IsChecked == true)
            {
                Debug.WriteLine("Enabled");
                isglowenabled = true;
                NeonPulseStoryboard.Begin();
                GlowSurround.Visibility = Visibility.Visible;
            }
            else
            {
                Debug.WriteLine("Disable");
                isglowenabled = false;
                GlowSurround.Visibility = Visibility.Collapsed;
            }
        }

        private void Grid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            ShowPanel();
        }

        private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ShowPanel();
        }

        private void asbSearchOptions_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {

        }

        private void asbSearchOptions_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {

        }

        private void voSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            SelectorBarItem selectedItem = sender.SelectedItem;
            int currentSelectedIndex = sender.Items.IndexOf(selectedItem);
            voGeneral.Visibility = Visibility.Collapsed;
            voFilters.Visibility = Visibility.Collapsed;
            voOrientation.Visibility = Visibility.Collapsed;

            voAspectRatio.Visibility = Visibility.Collapsed;
            switch (currentSelectedIndex)
            {
                case 0:
                    voGeneral.Visibility = Visibility.Visible;
                    break;

                case 1:
                    voFilters.Visibility = Visibility.Visible;
                    break;

                case 2:
                    voOrientation.Visibility = Visibility.Visible;
                    break;

                case 3:
                    voAspectRatio.Visibility = Visibility.Visible;
                    break;

            }
        }

        bool isDraggingRot = false;

        private void Dial_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            isDraggingRot = true;
            ((FrameworkElement)sender).CapturePointer(e.Pointer);
            UpdateDialAngle(e);
        }

        private void Dial_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (isDraggingRot)
            {
                UpdateDialAngle(e);
            }
        }

        private void Dial_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            isDraggingRot = false;
            ((FrameworkElement)sender).ReleasePointerCapture(e.Pointer);
        }
        public static readonly DependencyProperty RotationAngleProperty =
    DependencyProperty.Register("RotationAngle", typeof(double), typeof(MainWindow), new PropertyMetadata(0.0));

        public double RotationAngle
        {
            get => (double)GetValue(RotationAngleProperty);
            set => SetValue(RotationAngleProperty, value);
        }
        private void UpdateDialAngle(PointerRoutedEventArgs e)
        {
            var grid = DialControl;
            var pointerPoint = e.GetCurrentPoint(grid);

            // 1. Get the center of the dial control
            double centerX = grid.ActualWidth / 2;
            double centerY = grid.ActualHeight / 2;

            // 2. Calculate the difference between the mouse and the center
            double deltaX = pointerPoint.Position.X - centerX;
            double deltaY = pointerPoint.Position.Y - centerY;

            // 3. Get the angle in Radians and convert to Degrees
            // Math.Atan2 returns 0 at the 3 o'clock position, so we add 90 
            // to make 0 degrees start at the 12 o'clock position.
            double radians = Math.Atan2(deltaY, deltaX);
            double degrees = radians * (180 / Math.PI) + 90;

            // 4. Normalize the value to a 0-360 range
            if (degrees < 0) degrees += 360;
            if (degrees >= 360) degrees -= 360;

            double snapThreshold = 5.0;
            if (degrees % 90 < snapThreshold || degrees % 90 > (90 - snapThreshold))
            {
                degrees = Math.Round(degrees / 90.0) * 90;
                // Correct 360 back to 0 for consistency
                if (degrees == 360) degrees = 0;
            }

            // 6. Update the Dependency Property
            // This triggers the {x:Bind} for the Needle and Slider automatically
            RotationAngle = Math.Round(degrees);

            // Manual update to ensure the needle moves immediately
            NeedleRotation.Angle = RotationAngle;
            txtRotationValue.Text = $"{RotationAngle}°";
            sldRotation.Value = RotationAngle;
        }

        private void BtnRotateMinus_Click(object sender, RoutedEventArgs e)
        {
            double newAngle = RotationAngle - 1;
            if (newAngle < 0) newAngle = 359;

            UpdateManualRotation(newAngle);
        }

        private void BtnRotatePlus_Click(object sender, RoutedEventArgs e)
        {
            double newAngle = RotationAngle + 1;
            if (newAngle >= 360) newAngle = 0;

            UpdateManualRotation(newAngle);
        }

        // Helper to keep UI and Property in sync
        private void UpdateManualRotation(double angle)
        {
            RotationAngle = angle;
            txtRotationValue.Text = $"{RotationAngle}°";
            // If your slider isn't bound, update it here too:
            sldRotation.Value = RotationAngle;
        }

        private void voSelectorBarSubtitles_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            SelectorBarItem selectedItem = sender.SelectedItem;
            int currentSelectedIndex = sender.Items.IndexOf(selectedItem);
            grdSubTracks.Visibility = Visibility.Collapsed;
            grdSubtitlesCustomize.Visibility = Visibility.Collapsed;
            switch (currentSelectedIndex)
            {
                case 0:
                    grdSubTracks.Visibility = Visibility.Visible;
                    break;

                case 1:
                    grdSubtitlesCustomize.Visibility = Visibility.Visible;
                    break;


            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {

        }

        private void btnNewSubtitleCreate_Click(object sender, RoutedEventArgs e)
        {
                SaveSubtitleFile();
            txtboxMaster.Text = "";
            txtSubFileName.Text = "Untitled";
        }
        private async void SaveSubtitleFile()
        {
            if (filepathname != "")
            {
                savedfiletext = txtboxMaster.Text;
                txtSubFileName.Text.Replace("*", "");
                await File.WriteAllTextAsync(filepathname, txtboxMaster.Text);
            }
            else
            {
                var picker = new FileSavePicker();

                var hwnd = WindowNative.GetWindowHandle(App.HomeWindowInstance);
                InitializeWithWindow.Initialize(picker, hwnd);

                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.SuggestedFileName = "untitled";

                picker.FileTypeChoices.Add("SubRip Subtitle", new List<string>() { ".srt" });
                picker.FileTypeChoices.Add("ASS Subtitle", new List<string>() { ".ass" });
                picker.FileTypeChoices.Add("WebVTT Subtitle", new List<string>() { ".vtt" });

                StorageFile file = await picker.PickSaveFileAsync();

                if (file != null)
                {
                    await FileIO.WriteTextAsync(file, txtboxMaster.Text);
                }
            }
        }
        
        private void tglbtnTextEditor_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton? tgl = sender as ToggleButton;
            if (tgl != null)
            {
                if (tgl.Tag.ToString() == "TE")
                {
                    stkTextEditor.Visibility = Visibility.Visible;
                    stkVisualEditor.Visibility = Visibility.Collapsed;
                    tglbtnSidebyside.IsChecked = false;
                    tglbtnVisualEditor.IsChecked = false;
                    Grid.SetColumn(stkTextEditor, 0);
                }
                else if (tgl.Tag.ToString() == "VE")
                {
                    tglbtnSidebyside.IsChecked = false;
                    tglbtnTextEditor.IsChecked = false;
                    stkTextEditor.Visibility = Visibility.Collapsed;
                    stkVisualEditor.Visibility = Visibility.Visible;
                    Grid.SetColumn(stkVisualEditor, 0);
                }
                else
                {
                    tglbtnVisualEditor.IsChecked = false;
                    tglbtnTextEditor.IsChecked = false;
                    stkTextEditor.Visibility = Visibility.Visible;
                    stkVisualEditor.Visibility = Visibility.Visible;
                    Grid.SetColumn(stkTextEditor, 0);
                    Grid.SetColumn(stkVisualEditor, 1);

                }
            }
        }

        private void tglbtnTextEditor_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void btnCopyText_Click(object sender, RoutedEventArgs e)
        {
            var buttn = (Button)sender;
            if (buttn == null) return;
            string tag = buttn.Tag.ToString();
            if(tag == "Undo")
            {
                txtboxMaster.Undo();
            }
            else if(tag == "Redo")
            {
                txtboxMaster.Redo();
            }
            else if (tag == "Copy")
            {
                txtboxMaster.CopySelectionToClipboard();
            }
            else if(tag == "Cut")
            {
                txtboxMaster.CutSelectionToClipboard();
              
            }
            else if (tag == "Paste")
            {
                txtboxMaster.PasteFromClipboard();
            }
            else if (tag == "Delete")
            {
                txtboxMaster.SelectedText.Replace(txtboxMaster.SelectedText, "");
            }
            else if (tag == "SelAll")
            {
                txtboxMaster.SelectAll();
            }
        }

        private void btnReplace_Click(object sender, RoutedEventArgs e)
        {
            ttFindReplace.IsOpen = true;
            expReplace.IsExpanded = true;
        }

        private void btnZoomOut_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if (App.VideoPlayerWindowInstance == null) { return; }
            var dlg = VideoOptionsWindow.ShowDialog(grdSubtitleEditor, 900, 900, false);
            dlg.CloseRequested += () =>
            {
                dlg.HideDialog();
            };
        }
        string filepathname = "";
        private async void  btnImportSubtitleCreate_Click(object sender, RoutedEventArgs e)
        {
            var file = await PickSubtitles();
            if(file != null)
            {
                filepathname = file.Path;
                string content = await FileIO.ReadTextAsync(file);
                txtboxMaster.Text = content;
                txtSubFileName.Text = Path.GetFileName(filepathname);
            }
        
        }
        string savedfiletext = "";
        private void txtboxMaster_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtboxMaster.Text != savedfiletext)
            {
                txtSubFileName.Text = Path.GetFileName(filepathname) + "*";
            }
            else
            {
                txtSubFileName.Text.Replace("*", "");
            }
        }

        private async void btnSaveSubtitleCreate_Click(object sender, RoutedEventArgs e)
        {
            SaveSubtitleFile();
        }

        private void btnFindReplace_Click(object sender, RoutedEventArgs e)
        {
            ttFindReplace.IsOpen = true;
            expReplace.IsExpanded = false;
        }

       
        private void btnShowAll_Click_1(object sender, RoutedEventArgs e)
        {
            if(btnShowAll.IsChecked == true)
            {
                stkAllResults.Visibility = Visibility.Visible;
            }
            else
            {
                stkAllResults.Visibility = Visibility.Collapsed;
            }
        }
    }
}
