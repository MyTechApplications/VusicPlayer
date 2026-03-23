using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Path = System.IO.Path;

namespace VusicPlayer
{
    public class ProgramManager
    {
        [STAThread]
        static int Main(string[] args)
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();
            bool isRedirect = DecideRedirection();

            if (!isRedirect)
            {
                Application.Start((p) =>
                {
                    var context = new DispatcherQueueSynchronizationContext(
                        DispatcherQueue.GetForCurrentThread());
                    SynchronizationContext.SetSynchronizationContext(context);
                    _ = new App();
                });
            }

            return 0;
        }
        private static void OnActivated(object sender, AppActivationArguments args)
        {
            if (App.MainWindowInstance == null) return;

            App.MainWindowInstance.DispatcherQueue.TryEnqueue(() =>
            {
                if (args.Kind == ExtendedActivationKind.File)
                {
                    var fileArgs = (FileActivatedEventArgs)args.Data;
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
                            HomeWindow.HideWindow();

                            return;
                        }
                        else if (audioExtensions.Contains(extension))
                        {
                            var home = HomeWindow.ShowWindow();
                            QueueService.PlayMedia(new ObservableCollection<string> { filePath });
                            return;
                        }
                    }
                }
             

                
                App.MainWindowInstance.Activate();
            });
        }
        private static void HandleActivationArgs(AppActivationArguments args)
        {
            if (args.Kind == ExtendedActivationKind.File)
            {
                var fileArgs = args.Data as Windows.ApplicationModel.Activation.IFileActivatedEventArgs;
                if (fileArgs != null)
                {
                    // Get the first file (or loop through fileArgs.Files)
                    var file = fileArgs.Files.FirstOrDefault();
                    if (file != null)
                    {
                        // TODO: Call a method in your App or ViewModel to play the file
                        // Example: ((App)Application.Current).PlayFile(file.Path);
                        Debug.WriteLine($"File received: {file.Path}");
                    }
                }
            }
        }
        private static bool DecideRedirection()
        {
            bool isRedirect = false;
            AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();
            AppInstance keyInstance = AppInstance.FindOrRegisterForKey("VusicPlayer_Music_App_Key");

            if (keyInstance.IsCurrent)
            {
                keyInstance.Activated += OnActivated;
            }
            else
            {
                isRedirect = true;
                // The redirection happens here, but we'll ensure the Main instance 
                // receives the signal.
                RedirectActivationTo(args, keyInstance);
            }

            return isRedirect;
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateEvent(
    IntPtr lpEventAttributes, bool bManualReset,
    bool bInitialState, string lpName);

        [DllImport("kernel32.dll")]
        private static extern bool SetEvent(IntPtr hEvent);

        [DllImport("ole32.dll")]
        private static extern uint CoWaitForMultipleObjects(
            uint dwFlags, uint dwMilliseconds, ulong nHandles,
            IntPtr[] pHandles, out uint dwIndex);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        private static IntPtr redirectEventHandle = IntPtr.Zero;

        // Do the redirection on another thread, and use a non-blocking
        // wait method to wait for the redirection to complete.
        public static void RedirectActivationTo(AppActivationArguments args,
                                                AppInstance keyInstance)
        {
            redirectEventHandle = CreateEvent(IntPtr.Zero, true, false, null);
            Task.Run(() =>
            {
                keyInstance.RedirectActivationToAsync(args).AsTask().Wait();
                SetEvent(redirectEventHandle);
            });

            uint CWMO_DEFAULT = 0;
            uint INFINITE = 0xFFFFFFFF;
            _ = CoWaitForMultipleObjects(
               CWMO_DEFAULT, INFINITE, 1,
               [redirectEventHandle], out uint handleIndex);

            // Bring the window to the foreground
            Process process = Process.GetProcessById((int)keyInstance.ProcessId);
            SetForegroundWindow(process.MainWindowHandle);
        }

    }
}
