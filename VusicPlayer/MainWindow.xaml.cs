using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicPlayer
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow(ObservableCollection<VideoItem> videos, string selectedVideoPath, double CURRENTDURATION, bool NEWinstance)
        {
            InitializeComponent();
            this.Closed += MainWindow_Closed;
    
            Mainframe.Content = new VideoLib(new VideoPlayerPageParams
            {
                Videos = videos,
                SelectedVideoPath = selectedVideoPath,
                CurrentRunningDuration = CURRENTDURATION,
                newinst = NEWinstance
              
            });
            this.Title = "Vusic Player - "+Path.GetFileName(selectedVideoPath);
            this.ExtendsContentIntoTitleBar = true;
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.SetIcon("Assets/appicon.ico");
            appWindow.SetTaskbarIcon("Assets/appicon.ico");
            appWindow.SetTitleBarIcon("Assets/appicon.ico");
        }

        public static event EventHandler PlayerClosed;
        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            if (Mainframe.Content is VideoLib playerPage)
            {
                playerPage.CleanupPlayer();
            }

            // BROADCAST: Tell whoever is listening that we are done
            PlayerClosed?.Invoke(this, EventArgs.Empty);
        }
    }
}
