using LibVLCSharp.Shared;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicPlayer
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MiniPlayerWind : Window
    {
        public MiniPlayerWind()
        {
            this.InitializeComponent();
            this.ExtendsContentIntoTitleBar = true;
            this.Title = "Vusic Player";
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            // 1. Set the size
            int width = 450;
            int height = 200;
            appWindow.Resize(new Windows.Graphics.SizeInt32(width, height));

            // 2. Get the monitor's work area (avoids Taskbar if it's at the top)
            var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);

            // 3. Calculate "Top Right" with a margin (e.g., 30 pixels from edges)
            int margin = 30;
            int x = displayArea.WorkArea.Width - width - margin;
            int y = displayArea.WorkArea.Y + margin; // .Y handles cases where taskbar is at the top

            appWindow.Move(new Windows.Graphics.PointInt32(x, y));
           
            // 4. Ensure it stays on top
            if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            {
                presenter.IsAlwaysOnTop = true;
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
            }
            if (PlaybackState.CurrentlyPlayingPath != null)
            {
                _Path = PlaybackState.CurrentlyPlayingPath;
            }
            stateofplay = "playing";
            this.Closed += MiniPlayerWind_Closed;
            sldMain.DragStarted += SldMain_DragStarted;
            sldMain.DragCompleted += SldMain_DragCompleted;
 
            txtTitle.Text = Path.GetFileName(PlaybackState.CurrentlyPlayingPath); 
          //  videoView.Initialized += VideoView_Initialized;
        }

        private void MiniPlayerWind_Closed(object sender, WindowEventArgs args)
        {
            PlaybackState.CurrentPosition = _mediaPlayer.Position;
            _mediaPlayer.Dispose();
            if (stateofplay == "paused")
            {
                PlaybackState.CurrentState = false;
            }
            else
            {
                PlaybackState.CurrentState = true;
            }
        }

        string stateofplay = "paused";
        private void SldMain_DragCompleted()
        {
            double newPosition = sldMain.Value / sldMain.Maximum;
            if (_mediaPlayer != null)
            {
                if (_mediaPlayer.State != VLCState.Stopped)
                {
                    _mediaPlayer.Position = (float)newPosition;

                    long currentTimeMs = _mediaPlayer.Time;
                    _isDragging = false;
                    _mediaPlayer.Mute = false;
                    if (stateofplay == "playing")
                        maintimer.Start();
                }
            }
            }

        private void SldMain_DragStarted()
        {
            _mediaPlayer.Mute = true;
            _isDragging = true;
            maintimer.Stop();
        }

        private LibVLC? _libVLC;
        private LibVLCSharp.Shared.MediaPlayer? _mediaPlayer;
        string _Path;

        private void VideoView_Initialized(object? sender, LibVLCSharp.Platforms.Windows.InitializedEventArgs e)
        {
            Core.Initialize();

            _libVLC = new LibVLC(enableDebugLogs: true, e.SwapChainOptions);
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);


            _mediaPlayer.AspectRatio = "16:9";

            _mediaPlayer.Volume = 100;
            if (_libVLC != null && _Path != "")
            {
                using var media = new Media(_libVLC, _Path, FromType.FromPath);
                _mediaPlayer.Media = media;
                EventHandler<EventArgs> seekHandler = null;
                seekHandler = (s, e) =>
                {
                    // Unsubscribe immediately so this doesn't fire every time you play/pause
                    _mediaPlayer.Playing -= seekHandler;

                    // Apply the saved position
                    _mediaPlayer.Position = PlaybackState.CurrentPosition; 

                    // If you wanted it to stay paused at that spot:
                    // _mediaPlayer.Pause(); 
                }; _mediaPlayer.Playing += seekHandler;
                maintimer = new DispatcherTimer();
                maintimer.Interval = TimeSpan.FromSeconds(1);
                maintimer.Tick += Maintimer_Tick; 

             
             
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    videoView.MediaPlayer = _mediaPlayer;
                    if (PlaybackState.CurrentState == true)
                    {
                        _mediaPlayer.Play();
                        maintimer.Start();
                        stateofplay = "playing";
                        imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/pause.png"));
                    }
                    else
                    {
                        _mediaPlayer.Pause();
                        stateofplay = "paused";
                        imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/play.png"));
                    }
                });
                sldMain.Maximum = PlaybackState.TotalDuration;
                sldMain.Value = PlaybackState.CurrentSliderPosition;
            }
        }
        bool _isDragging = false;
        private void Maintimer_Tick(object? sender, object e)
        {
            if (!_isDragging && _mediaPlayer != null)
            {
                long currentTimeMs = _mediaPlayer.Time;
                sldMain.Value = currentTimeMs / 1000.0;
                if(stateofplay == "paused")
                {
                    _mediaPlayer.Pause();
                    maintimer.Stop();
                }
            }
            }

        DispatcherTimer maintimer;
        private void btnSkipForward_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnPlayPause_Click(object sender, RoutedEventArgs e)
        {
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

        private void btnSkipBack_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnVolume_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnResizeBack_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
