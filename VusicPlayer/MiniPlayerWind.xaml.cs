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
      
            stateofplay = "playing";
            this.Closed += MiniPlayerWind_Closed;
            sldMain.DragStarted += SldMain_DragStarted;
            sldMain.DragCompleted += SldMain_DragCompleted;
 
            txtTitle.Text = Path.GetFileName(PlaybackState.CurrentlyPlayingPath); 
          //  videoView.Initialized += VideoView_Initialized;
        }

        private void MiniPlayerWind_Closed(object sender, WindowEventArgs args)
        {
           
        }

        string stateofplay = "paused";
        private void SldMain_DragCompleted()
        {
            }

        private void SldMain_DragStarted()
        {
        }

     

        private void VideoView_Initialized(object? sender, LibVLCSharp.Platforms.Windows.InitializedEventArgs e)
        {
            
        }
        bool _isDragging = false;
        private void Maintimer_Tick(object? sender, object e)
        {
           
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
