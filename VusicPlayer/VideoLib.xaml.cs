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
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.WindowManagement;
using WinRT.Interop;
using AppWindow = Microsoft.UI.Windowing.AppWindow;
using Path = System.IO.Path;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicPlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class VideoLib : Page
    {
        public VideoLib(VideoPlayerPageParams param)
        {
            InitializeComponent();

            Load.Visibility = Visibility.Visible;
            sldMain.DragStarted += SldMain_DragStarted; ;
            sldMain.DragCompleted += SldMain_DragCompleted; ;
            videoView.Initialized += VideoView_Initialized;
            this.KeyDown += VideoLib_KeyDown;

            videos = param!.Videos!;
            currentVideoPath = param!.SelectedVideoPath!;
            newinstance = param!.newinst;
            currentdur = param!.CurrentRunningDuration;
        }
        bool newinstance;
        double currentdur;
        private void SldMain_DragCompleted()
        {
            double newPosition = sldMain.Value / sldMain.Maximum;
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Position = (float)newPosition;

                long currentTimeMs = _mediaPlayer.Time;
                txtRunningDuration.Text = TimeSpan.FromMilliseconds(currentTimeMs).ToString(@"hh\:mm\:ss");
                MainTimer?.Start();
            }
        }
        public async void LoadFileFromPath(string path)
        {
            try
            {
                // Convert the string path to a StorageFile
                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(path);

            }
            catch (Exception ex)
            {
                // Handle cases where the file might be moved or inaccessible
                Debug.WriteLine($"Error loading file: {ex.Message}");
            }
        }

        private void SldMain_DragStarted()
        {
            MainTimer?.Stop();

        }
        int endduration = 20;
        private void VideoLib_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (_mediaPlayer == null)
                return;

            if (e.Key == Windows.System.VirtualKey.Space)
            {
                _mediaPlayer.SetPause(_mediaPlayer.IsPlaying);
                ShowControls();

                e.Handled = true;
            }
        }

        private LibVLC? _libVLC;
        private LibVLCSharp.Shared.MediaPlayer? _mediaPlayer;
        private void VideoView_Initialized(object? sender, LibVLCSharp.Platforms.Windows.InitializedEventArgs e)
        {
            Core.Initialize();

            _libVLC = new LibVLC(enableDebugLogs: true, e.SwapChainOptions);
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);

            videoView.MediaPlayer = _mediaPlayer;
            _mediaPlayer.AspectRatio = "16:9";
            if (_libVLC != null)
            {
                if (currentVideoPath != null)
                {
                    using var media = new Media(_libVLC, currentVideoPath, FromType.FromPath);
                    _mediaPlayer?.Play(media);
                    imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/pause.png"));

                    _hideTimer = new DispatcherTimer();
                    _hideLoadedTimer = new DispatcherTimer();
                    _hideTimer.Interval = TimeSpan.FromSeconds(1);
                    _hideLoadedTimer.Interval = TimeSpan.FromSeconds(1);
                    _hideTimer.Tick += _hideTimer_Tick;
                    _hideLoadedTimer.Tick += _hideLoadedTimer_Tick;
                    _hideTimer.Start();
                    seektimer = new DispatcherTimer();
                    seektimer.Interval = TimeSpan.FromSeconds(1);
                    seektimer.Tick += Seektimer_Tick;
                    CreateAnimations();
                    CreateAnimations2(txtSeekForward);
                    CreateAnimations3();
                    stateofplay = "playing";
                    RootGrid.Focus(FocusState.Programmatic);
                    btnFullScreen.IsEnabled = true;
                    sldVol.IsEnabled = true;
                    _mediaPlayer!.TimeChanged += _mediaPlayer_TimeChanged;

                    filepathtemp = currentVideoPath;
                    currentVideoIndex = videos.FindIndex(v => v.FilePath == currentVideoPath);

                    media.Parse(MediaParseOptions.ParseLocal);

                    _mediaPlayer.LengthChanged += (sender, e) =>
                    {
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            long totalLengthMs = e.Length;
                            sldMain.Maximum = totalLengthMs / 1000.0;
                            txtTotalDuration.Text = TimeSpan.FromMilliseconds(totalLengthMs).ToString(@"hh\:mm\:ss");
                            totaldurationtemp = totalLengthMs;
                            txtFileName.Text = Path.GetFileName(currentVideoPath);
                            // Start slider timer after length is available
                            MainTimer = new DispatcherTimer();
                            MainTimer.Interval = TimeSpan.FromSeconds(1);
                            MainTimer.Tick += MainTimer_Tick;
                            txtLoaded.Text = "Loaded media '" + txtFileName.Text + "'";
                            sldVol.Value = _mediaPlayer.Volume;

                            txtVolumepercent.Text = sldVol.Value.ToString();
                            MainTimer.Start();
                            StartAutoSave();

                            Load.Visibility = Visibility.Collapsed;
                            RootGrid.Visibility = Visibility.Visible;
                            if (newinstance == false)
                            {
                                Load.Visibility = Visibility.Collapsed;
                                RootGrid.Visibility = Visibility.Visible;
                                _mediaPlayer.Position = (float)currentdur / _mediaPlayer.Length;
                                long currentTimeMs = _mediaPlayer.Time;
                                txtRunningDuration.Text = TimeSpan.FromMilliseconds(currentTimeMs).ToString(@"hh\:mm\:ss");
                                txtLoaded.Text = "Loaded previous playback at " + txtRunningDuration.Text;
                            }

                            ShowLoaded();
                            _hideLoadedTimer.Start();
                        });

                    };
                }

            }
        }
        int seektimercount;
        private void _hideLoadedTimer_Tick(object? sender, object e)
        {
            if (Load.Visibility == Visibility.Visible)
            {
                Load.Visibility = Visibility.Collapsed;
                RootGrid.Visibility = Visibility.Visible;
            }
            showloaded++;
            if (showloaded == 4)
            {
                showloaded = 0;
                HideLoadedText();

                _hideLoadedTimer?.Stop();

            }
        }

        private void PlayVideoAtIndex(int index)
        {
            if (index < 0 || index >= videos.Count)
                return; // out of range

            currentVideoIndex = index;
            currentVideoPath = videos[index].FilePath;

            var media = new Media(_libVLC, currentVideoPath, FromType.FromPath);

            // Parse to get length if needed
            media.Parse(MediaParseOptions.ParseLocal);

            _mediaPlayer.Media = media;
            _mediaPlayer.Play();

            // Update UI (play/pause button)
            imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/pause.png"));
        }
        private void _mediaPlayer_TimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
        {


        }
        public void CleanupPlayer()
        {
            if (_mediaPlayer != null)
            {

                _saveTimer.Stop();
                MainTimer?.Stop();
                _mediaPlayer.Stop();
                _mediaPlayer.Dispose();
                // Also dispose the LibVLC instance if you created one locally
                _libVLC?.Dispose();
            }
        }
        private int currentVideoIndex = 0;
        private async Task<StorageFile> PickMediaFileAsync()
        {
            FileOpenPicker picker = new FileOpenPicker();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.m_window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.FileTypeFilter.Add(".mp4");
            picker.FileTypeFilter.Add(".mp3");

            return await picker.PickSingleFileAsync();
        }
        private async Task<StorageFile> PickSubtitles()
        {
            FileOpenPicker picker = new FileOpenPicker();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.m_window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.FileTypeFilter.Add(".srt");
            picker.FileTypeFilter.Add(".ass");
            picker.FileTypeFilter.Add(".vtt");

            return await picker.PickSingleFileAsync();
        }
        private async void btnOpenVideo_Click(object sender, RoutedEventArgs e)
        {
            var file = await PickMediaFileAsync();
            if (file != null)
            {
                var stream = await file.OpenAsync(FileAccessMode.Read);
                var path = file.Path;
                if (_libVLC == null || _mediaPlayer == null)
                    return;

                using var media = new Media(_libVLC, path, FromType.FromPath);
                _mediaPlayer.Play(media);
                imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/pause.png"));
                sldMain.Value = 0;
                MainTimer = new DispatcherTimer();
                MainTimer.Interval = TimeSpan.FromSeconds(1);
                MainTimer.Tick += MainTimer_Tick;

                MainTimer.Start();
                long totalLengthMs = _mediaPlayer.Length;
                TimeSpan totalTime = TimeSpan.FromMilliseconds(totalLengthMs);
                txtTotalDuration.Text = totalTime.ToString(@"hh\:mm\:ss");
            }
            _hideTimer = new DispatcherTimer();
            _hideTimer.Interval = TimeSpan.FromSeconds(1);
            _hideTimer.Tick += _hideTimer_Tick; ;
            _hideTimer.Start();
            seektimer = new DispatcherTimer();
            seektimer.Interval = TimeSpan.FromSeconds(2);
            seektimer.Tick += Seektimer_Tick;
            CreateAnimations();
            CreateAnimations2(txtSeekForward);
            stateofplay = "playing";
            RootGrid.Focus(FocusState.Programmatic);
            btnFullScreen.IsEnabled = true;
            sldVol.IsEnabled = true;
            if (_mediaPlayer != null)
                sldVol.Value = _mediaPlayer.Volume;

        }
        private bool isSaving = false;
        int nextbuttonwidthtimer = 0;
        private SemaphoreSlim saveSemaphore = new SemaphoreSlim(1, 1);
        private async void MainTimer_Tick(object? sender, object e)
        {
            if (_mediaPlayer == null) return;

            long currentTimeMs = _mediaPlayer.Time;
            txtRunningDuration.Text = TimeSpan.FromMilliseconds(currentTimeMs).ToString(@"hh\:mm\:ss");
            sldMain.Value = currentTimeMs / 1000.0;

            // Show "Next" button 20 seconds before the end
            if (_mediaPlayer.Length > 0 && currentTimeMs >= _mediaPlayer.Length - 20000)
            {
                btnNextVideo.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                nextbuttonwidthtimer += 100;

                nextVideoProgress.Width += 1;
                if (nextbuttonwidthtimer == 2000)
                {
                    if (nextVideoProgress.Width >= grdMax.Width)
                    {
                        int nextIndex = currentVideoIndex + 1;

                        if (nextIndex >= videos.Count)
                        {
                            nextIndex = 0; // loop back to first video (optional)
                        }

                        PlayVideoAtIndex(nextIndex);
                        nextbuttonwidthtimer = 0;
                        btnNextVideo.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        nextbuttonwidthtimer -= 100;
                    }
                }

                // Stop timer and reset UI if video ends
                if (sldMain.Value >= sldMain.Maximum)
                {
                    Debug.WriteLine("MediaEnded");
                    _mediaPlayer?.Pause();
                    ToolTipService.SetToolTip(btnPlayPause, "Play");
                    stateofplay = "paused";
                    imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/play.png"));
                    MainTimer?.Stop();
                }

            }

        }
        string tempfilename;
        public ObservableCollection<VideoProgress> MyItems { get; set; } = new();
        public async void StartAutoSave()
        {
            if (_mediaPlayer != null)
            {
                var currentSettings = await SettingsHelper.LoadSettingsAsync();
                var currentPlaying = currentSettings.SavedItems;

                _saveTimer = this.DispatcherQueue.CreateTimer();
                _saveTimer.Interval = TimeSpan.FromSeconds(4);
                string pathnamee = Path.GetFileName(filepathtemp);

                _saveTimer.Tick += async (s, e) =>
                {

                    var existingItem = currentSettings.SavedItems
             .FirstOrDefault(x => x.FilePath == filepathtemp);

                    if (existingItem != null)
                    {
                        // 2. Update existing entry
                        existingItem.CurrentDuration = _mediaPlayer.Time;
                        existingItem.TotalDuration = _mediaPlayer.Length;
                    }
                    else
                    {
                        // 3. Add as a new entry if it wasn't found
                        var newProgress = new VideoProgress
                        {
                            FileName = pathnamee,
                            FilePath = filepathtemp,
                            CurrentDuration = _mediaPlayer.Time,
                            TotalDuration = _mediaPlayer.Length,
                        };
                        currentSettings.SavedItems.Add(newProgress);
                    }

                    // 4. Save the updated state
                    await SettingsHelper.SaveSettingsAsync(currentSettings);
                };

                _saveTimer.Start();
            }
        }
        public void StopAutoSave() => _saveTimer?.Stop();
        DispatcherQueueTimer _saveTimer;
        string filepathtemp = "";
        long totaldurationtemp;
        private DispatcherTimer? nextVideoTimer;
        private double totalTime = 20; // seconds to show full progress
        private double elapsedTime = 0;
        private InputCursor? originalInputCursor;
        private void Seektimer_Tick(object? sender, object e)
        {
            seektimercount++;
            if (seektimercount == 2)
            {
                seektimercount = 0;
                HideSeekText();

                seektimer?.Stop();

            }
        }
        int savecount = 0;
        void CreateAnimations()
        {
            _fadeIn = new Storyboard();
            _fadeOut = new Storyboard();

            var fadeInAnim = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200)
            };

            var fadeOutAnim = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            Storyboard.SetTarget(fadeInAnim, GlassRoot);
            Storyboard.SetTargetProperty(fadeInAnim, "Opacity");

            Storyboard.SetTarget(fadeOutAnim, GlassRoot);
            Storyboard.SetTargetProperty(fadeOutAnim, "Opacity");

            _fadeIn.Children.Add(fadeInAnim);
            _fadeOut.Children.Add(fadeOutAnim);
        }
        void CreateAnimations3()
        {
            _fadeIn2 = new Storyboard();
            _fadeOut2 = new Storyboard();

            var fadeInAnim = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200)
            };

            var fadeOutAnim = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            Storyboard.SetTarget(fadeInAnim, txtLoaded);
            Storyboard.SetTargetProperty(fadeInAnim, "Opacity");

            Storyboard.SetTarget(fadeOutAnim, txtLoaded);
            Storyboard.SetTargetProperty(fadeOutAnim, "Opacity");

            _fadeIn2.Children.Add(fadeInAnim);
            _fadeOut2.Children.Add(fadeOutAnim);
        }
        void CreateAnimations2(TextBlock txt)
        {
            _fadeIn3 = new Storyboard();
            _fadeOut3 = new Storyboard();

            var fadeInAnim = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200)
            };

            var fadeOutAnim = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            Storyboard.SetTarget(fadeInAnim, txt);
            Storyboard.SetTargetProperty(fadeInAnim, "Opacity");

            Storyboard.SetTarget(fadeOutAnim, txt);
            Storyboard.SetTargetProperty(fadeOutAnim, "Opacity");

            _fadeIn3.Children.Add(fadeInAnim);
            _fadeOut3.Children.Add(fadeOutAnim);
        }
        private List<VideoItem> videos;
        private string currentVideoPath;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            Load.Visibility = Visibility.Visible;
            videoView.Initialized += VideoView_Initialized;

            this.KeyDown += VideoLib_KeyDown; ;
        }

        int showcontrols = 0;
        int showloaded = 0;
        private void _hideTimer_Tick(object? sender, object e)
        {
            showcontrols++;
            if (showcontrols == 2)
            {
                showcontrols = 0;
                HideControls();
                originalInputCursor = RootGrid.InputCursor ?? InputSystemCursor.Create(InputSystemCursorShape.Arrow);
                // Hide the cursor by setting it to null or a "no" cursor
                RootGrid.InputCursor = null;
                _hideTimer?.Stop();

            }
        }

        void HideControls()
        {
            if (_controlsVisible)
            {
                _fadeIn?.Stop();
                _fadeOut?.Begin();
                GlassRoot.IsHitTestVisible = false;
                _controlsVisible = false;
            }
        }
        bool _loadedvisible;
        void HideLoadedText()
        {
            if (_loadedvisible)
            {
                _fadeIn2?.Stop();
                _fadeOut2?.Begin();
                txtLoaded.IsHitTestVisible = false;
                _loadedvisible = false;
            }
        }
        void HideSeekText()
        {
            if (seektxtvisible)
            {
                _fadeIn3?.Stop();
                _fadeOut3?.Begin();
                txtSeekForward.IsHitTestVisible = false;
                seektxtvisible = false;
            }
        }

        void ShowControls()
        {
            if (!_controlsVisible)
            {
                _fadeOut?.Stop();
                _fadeIn?.Begin();
                GlassRoot.IsHitTestVisible = true;
                _controlsVisible = true;
            }

        }
        void ShowLoaded()
        {
            if (!_loadedvisible)
            {
                _fadeOut2?.Stop();
                _fadeIn2?.Begin();
                txtLoaded.IsHitTestVisible = true;
                _loadedvisible = true;
            }

        }
        void ShowSeekText(TextBlock txt)
        {
            if (!seektxtvisible)
            {
                _fadeOut3?.Stop();
                _fadeIn3?.Begin();
                txt.IsHitTestVisible = true;
                seektxtvisible = true;
            }
            seektimer?.Stop();

            seektimercount = 0;
            seektimer?.Start();
        }
        string stateofplay = "paused";
        private void btnSeekBefore_Click(object sender, RoutedEventArgs e)
        {
            txtSeekForward.HorizontalAlignment = HorizontalAlignment.Left;
            SeekRelative(-10); txtSeekForward.Text = "-10";
            ShowSeekText(txtSeekForward);
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
        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            base.OnKeyDown(e);

            if (_mediaPlayer == null)
                return;

            switch (e.Key)
            {
                case Windows.System.VirtualKey.Space:
                    _mediaPlayer.SetPause(_mediaPlayer.IsPlaying);
                    ShowControls();
                    _hideTimer?.Start();
                    e.Handled = true;
                    break;

                case Windows.System.VirtualKey.Left:
                    txtSeekForward.HorizontalAlignment = HorizontalAlignment.Left;
                    txtSeekForward.Text = "-10";
                    SeekRelative(-10);
                    ShowSeekText(txtSeekForward);
                    ShowControls();
                    _hideTimer?.Start();
                    e.Handled = true;
                    break;

                case Windows.System.VirtualKey.Right:
                    txtSeekForward.HorizontalAlignment = HorizontalAlignment.Right;
                    txtSeekForward.Text = "+10";
                    SeekRelative(+10);
                    ShowSeekText(txtSeekForward);
                    ShowControls();
                    _hideTimer?.Start();
                    e.Handled = true;
                    break;
            }
        }
        private async void savesettings()
        {
            // Simply route the data to the Manager. 
            // It handles the loading, checking, updating/appending, and locking.
            await DataManager.SaveProgressAsync(
                filepathtemp,
                Path.GetFileName(filepathtemp),
                (double)_mediaPlayer.Time,
                (double)totaldurationtemp
            );

            Debug.WriteLine("Manual save completed via DataManager.");
        }
        private void btnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (stateofplay == "playing")
            {
                stateofplay = "paused";
                _mediaPlayer?.Pause();
                MainTimer?.Stop();

                _saveTimer.Stop();
                imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/play.png"));

            }
            else
            {
                MainTimer?.Start();
                stateofplay = "playing";
                _mediaPlayer?.Play();
                _saveTimer.Start();
                imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/pause.png"));
            }
        }
        DispatcherTimer? _hideTimer;
        DispatcherTimer? _hideLoadedTimer;
        DispatcherTimer? MainTimer;
        DispatcherTimer? seektimer;
        bool _controlsVisible = true;
        bool seektxtvisible = false;
        private void btnSeekAfter_Click(object sender, RoutedEventArgs e)
        {
            txtSeekForward.HorizontalAlignment = HorizontalAlignment.Right;
            txtSeekForward.Text = "+10";
            SeekRelative(+10);
            ShowSeekText(txtSeekForward);
        }
        private Microsoft.UI.Windowing.AppWindow? _appWindow;
        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(App.m_window);
            WindowId myWndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(myWndId);
        }
        bool isfullscreen = false;
        private void btnFullScreen_Click(object sender, RoutedEventArgs e)
        {
            if (_appWindow == null)
            {
                _appWindow = GetAppWindowForCurrentWindow();
            }

            // 2. Determine target state
            string toolTipText;
            AppWindowPresenterKind targetPresenter;

            if (isfullscreen)
            {
                // Switch to Windowed
                targetPresenter = AppWindowPresenterKind.Default;
                btnOpenVideo.Visibility = Visibility.Visible;
                toolTipText = "Set Full Screen";
                isfullscreen = false; // UPDATE STATE HERE
            }
            else
            {
                // Switch to FullScreen
                targetPresenter = AppWindowPresenterKind.FullScreen;
                btnOpenVideo.Visibility = Visibility.Collapsed;
                toolTipText = "Resize to Normal Window";
                isfullscreen = true; // UPDATE STATE HERE
            }

            // 3. Apply the changes
            _appWindow.SetPresenter(targetPresenter);
            ToolTipService.SetToolTip(btnFullScreen, toolTipText);
        }
        void PopulateSubtitleList()
        {

            if (_mediaPlayer == null)
                return;

            trackexistinglist.Items.Clear();
            _allSubtitleNames.Clear();
            // real subtitle tracks
            foreach (var track in _mediaPlayer.SpuDescription)
            {
                _allSubtitleNames.Add(track.Name);
                var item = new MenuFlyoutItem
                {
                    Text = track.Name,
                    Tag = track.Id
                };

                item.Click += OffItem_Click;

                trackexistinglist.Items.Add(item);
            }
        }
        private List<string> _allSubtitleNames = new List<string>();
        private List<string> _allVideoNames = new List<string>();
        private void OffItem_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer == null)
                return;

            var item = (MenuFlyoutItem)sender;
        
            int spuId = (int)item.Tag;
            _mediaPlayer.SetSpu(spuId);
        }

        bool issubtitlesenabled = false;

        private void GlassRoot_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            ShowPanel();
        }
        Storyboard? _fadeIn;
        Storyboard? _fadeIn2;
        Storyboard? _fadeIn3;
        Storyboard? _fadeOut;
        Storyboard? _fadeOut2;
        Storyboard? _fadeOut3;
        public static class CursorHelper
        {
            [DllImport("user32.dll")]
            private static extern bool ShowCursor(bool bShow);

            public static void Hide() => ShowCursor(false);
            public static void Show() => ShowCursor(true);
        }
        private void ShowPanel()
        {
            CursorHelper.Show();
            ShowControls();
            showcontrols = 0;
            if (originalInputCursor != null)
            {
                RootGrid.InputCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
            }
            _hideTimer?.Start();
        }
        private void GlassRoot_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ShowPanel();

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
            if (_mediaPlayer == null)
                return;

            int vol = (int)obj;

            _mediaPlayer.Volume = vol;
            txtVolumepercent.Text = vol.ToString();
        }

        private void btnSubtitles_Click_1(object sender, RoutedEventArgs e)
        {
         
                issubtitlesenabled = true;
                PopulateSubtitleList();
            
        }

        void LoadFonts()
        {
            var fonts = CanvasTextFormat.GetSystemFontFamilies()
                                 .OrderBy(f => f)
                                 .ToList();

            cmbFonts.ItemsSource = fonts;

            cmbFonts.SelectedItem = "Segoe UI"; // default
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
            if (sender.Value is double v)
                sender.Value = Math.Round(v);

            txtSample.FontSize = sender.Value;
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
        List<string> listsubtitlespaths = new();
        private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var file = await PickSubtitles();
                if (file == null) return;

                string subtitlePath = file.Path;
                if (listsubtitlespaths.Contains(subtitlePath)) return;
                listsubtitlespaths.Add(subtitlePath);

                if (_mediaPlayer?.Media == null)
                    return;
                Uri uri = new Uri(subtitlePath);
                string uriString = uri.AbsoluteUri;
                _mediaPlayer.AddSlave(MediaSlaveType.Subtitle, uriString, true);


                await Task.Delay(300);
                var tracks = _mediaPlayer.SpuDescription;
                if (tracks != null)
                {
                    foreach (var track in tracks)
                    {
                        bool exists = trackexistinglist.Items
                            .OfType<MenuFlyoutItem>()
                            .Any(item => item.Tag?.ToString() == track.Id.ToString());

                        if (!exists) // Only add if it doesn't exist
                        {
                            var item = new MenuFlyoutItem
                            {
                                Text = track.Name,
                                Tag = track.Id
                            };

                            item.Click += OffItem_Click;
                            trackexistinglist.Items.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Subtitle error:");
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }
        private void RootGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {

        }

        private void RootGrid_PointerExited(object sender, PointerRoutedEventArgs e)
        {

        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            int nextIndex = currentVideoIndex + 1;

            if (nextIndex >= videos.Count)
            {
                nextIndex = 0; // loop back to first video (optional)
            }

            PlayVideoAtIndex(nextIndex);
            btnNextVideo.Visibility = Visibility.Collapsed;
        }

        private void MenuFlyoutItem_Click_1(object sender, RoutedEventArgs e)
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
                // Handled by SuggestionChosen
            }
            else
            {
                // Just pick the first result if they just hit enter
                var topResult = _allSubtitleNames.FirstOrDefault(n => n.ToLower().Contains(sender.Text.ToLower()));
                if (topResult != null)
                {
                    var targetTrack = _mediaPlayer.SpuDescription.FirstOrDefault(t => t.Name == topResult);
                    _mediaPlayer.SetSpu(targetTrack.Id - 1);
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
            string selectedName = args.SelectedItem.ToString();

            // Find the item in the MenuFlyout and simulate a click, 
            // or call your selection logic directly:
            var targetTrack = _mediaPlayer.SpuDescription.FirstOrDefault(t => t.Name == selectedName);
            _mediaPlayer.SetSpu(targetTrack.Id);

        }

        private void btnVideo_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer == null)
                return;

            videotracks.Items.Clear();
            _allVideoNames.Clear();
            // real subtitle tracks
            foreach (var track in _mediaPlayer.VideoTrackDescription)
            {
                _allVideoNames.Add(track.Name);
                var item = new MenuFlyoutItem
                {
                    Text = track.Name,
                    Tag = track.Id
                };

                item.Click += Item_Click; ;

                videotracks.Items.Add(item);
            }
            ToolTipService.SetToolTip(mnftSnapshotDirectory, snapshotdirectorypath);
        }
        string snapshotdirectorypath = KnownFolders.PicturesLibrary.Path;
        private void Item_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer == null)
                return;

            var item = (MenuFlyoutItem)sender;

            int videoid = (int)item.Tag;
            _mediaPlayer.SetVideoTrack(videoid);
        }

        private void mnftTakeSnapshot_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer == null) return;
            _mediaPlayer.TakeSnapshot(0, snapshotdirectorypath, 1800, 1800);
        }

        private async void mnftSnapshotDirectory_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();

            // Required file type filter (MUST be added)
            picker.FileTypeFilter.Add("*");

            // Attach picker to window
            var hwnd = WindowNative.GetWindowHandle(App.MainWindowInstance);
            InitializeWithWindow.Initialize(picker, hwnd);
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            StorageFolder folder = await picker.PickSingleFolderAsync();

            if (folder != null)
            {
                string folderPath = folder.Path;
                snapshotdirectorypath = folderPath;  // example usage
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
    }
}
