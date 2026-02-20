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
                string filePath = string.Empty;

                // 1. Try the most likely cast for Unpackaged 'Launch' Kind
                if (args.Data is Windows.ApplicationModel.Activation.ILaunchActivatedEventArgs launchArgs)
                {
                    filePath = launchArgs.Arguments;
                }
                // 2. Fallback for Command Line (if Windows decides to use it)
                else if (args.Data is Windows.ApplicationModel.Activation.ICommandLineActivatedEventArgs cmdArgs)
                {
                    filePath = cmdArgs.Operation.Arguments;
                }

                // 3. Process the file path
                if (!string.IsNullOrEmpty(filePath))
                {
                    // Unpackaged apps often get the full string: "C:\Path\To\File.mp3"
                    if (filePath.Contains(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        // Split by the " " delimiter that separates the EXE from the File
                        string[] parts = filePath.Split(new[] { "\" \"" }, StringSplitOptions.RemoveEmptyEntries);

                        if (parts.Length > 1)
                        {
                            // The second part is your actual file path
                            filePath = parts[1].Trim('"');
                        }
                        else
                        {
                            // If splitting failed, try to just take everything after the last .exe"
                            int exeIndex = filePath.LastIndexOf(".exe\"", StringComparison.OrdinalIgnoreCase);
                            if (exeIndex != -1)
                            {
                                filePath = filePath.Substring(exeIndex + 5).Trim().Trim('"');
                            }
                        }
                    }
                    else
                    {
                        // If it's just the path alone
                        filePath = filePath.Trim().Trim('"');
                    }
                    Debug.WriteLine("Path first: " + filePath);
                    ObservableCollection<string> strings = new();
                    strings.Add(filePath);
                    if(App.MainWindowInstance is HomeWindow wind)
                    {
                        wind.LoadFileFromPath(strings);
                    }

                    // Call your app's opening logic
                    // ((App)Application.Current).HandleFileOpening(filePath);
                }

                // Always bring window to front
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
