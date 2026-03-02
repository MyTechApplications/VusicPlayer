using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WinRT.Interop;

namespace Updater
{
    public sealed partial class MainWindow : Window
    {
        private const string LogSource = "UpdaterService";

        public MainWindow()
        {
            InitializeComponent();

            ConfigureWindow();
        }

        #region Window Setup

        private void ConfigureWindow()
        {
            this.ExtendsContentIntoTitleBar = true;

            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);

            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
            }

            appWindow.SetIcon("Assets/appicon.ico");
            appWindow.SetTaskbarIcon("Assets/appicon.ico");
            appWindow.SetTitleBarIcon("Assets/appicon.ico");

            if (Content is FrameworkElement root)
                root.Loaded += MainWindow_Loaded;
        }

        #endregion

        #region Update Flow

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Delay(100);

            string root = AppContext.BaseDirectory;
            string playerFolder = Path.Combine(root, "Player");
            string flagFile = Path.Combine(playerFolder, "update.ready");
            string stagingFolder = Path.Combine(playerFolder, "UpdateStaging");
            string mainExePath = Path.Combine(playerFolder, "VusicPlayer.exe");

            Logger.Log("Updater started", LogSource, Logger.LogLevelType.Information);
            Logger.Log("Player executable path: " + mainExePath, LogSource, Logger.LogLevelType.Information);

            if (File.Exists(flagFile))
            {
                await PerformUpdateAsync(root, stagingFolder, flagFile);
            }
            else
            {
                Logger.Log("No update flag found. Skipping update.", LogSource, Logger.LogLevelType.Information);
            }

            LaunchPlayer(mainExePath);
        }

        private async Task PerformUpdateAsync(string root, string stagingFolder, string flagFile)
        {
            try
            {
                string? stagingZip = Directory.Exists(stagingFolder)
                    ? Directory.GetFiles(stagingFolder, "*.zip").FirstOrDefault()
                    : null;

                if (string.IsNullOrEmpty(stagingZip))
                {
                    Logger.Log("No update ZIP found in staging folder.", LogSource, Logger.LogLevelType.Warning);
                    return;
                }

                Logger.Log("Update package found: " + stagingZip, LogSource, Logger.LogLevelType.Information);

                await Task.Run(() => ExtractUpdate(root, stagingZip));

                File.Delete(stagingZip);
                Logger.Log("Deleted staging ZIP file.", LogSource, Logger.LogLevelType.Information);

                File.Delete(flagFile);
                Logger.Log("Deleted update flag file.", LogSource, Logger.LogLevelType.Information);

                Logger.Log("Update completed successfully.", LogSource, Logger.LogLevelType.Success);
            }
            catch (Exception ex)
            {
                Logger.Log("Update failed: " + ex.Message, LogSource, Logger.LogLevelType.Error);
            }
        }

        private void ExtractUpdate(string root, string stagingZip)
        {
            using var archive = ZipFile.OpenRead(stagingZip);

            int totalEntries = archive.Entries.Count;
            int current = 0;

            foreach (var entry in archive.Entries)
            {
                string destinationPath = Path.Combine(root, "Player", entry.FullName);
                string? directoryPath = Path.GetDirectoryName(destinationPath);

                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                if (!string.IsNullOrEmpty(entry.Name))
                {
                    bool success = false;
                    int attempts = 0;

                    while (!success && attempts < 3)
                    {
                        try
                        {
                            entry.ExtractToFile(destinationPath, overwrite: true);
                            success = true;

                            Logger.Log("Copied file: " + entry.FullName,
                                LogSource,
                                Logger.LogLevelType.Information);
                        }
                        catch (IOException)
                        {
                            attempts++;

                            // Kill running player if locked
                            foreach (var process in Process.GetProcessesByName("VusicPlayer"))
                            {
                                try
                                {
                                    process.Kill();
                                    process.WaitForExit(500);
                                }
                                catch { }
                            }

                            Thread.Sleep(500);

                            if (attempts == 3)
                                throw;
                        }
                    }
                }

                current++;
                double progress = (double)current / totalEntries * 100;

                DispatcherQueue.TryEnqueue(() =>
                {
                    UpdateProgressBar.Value = progress;
                });
            }
        }

        #endregion

        #region Launch Player

        private async void LaunchPlayer(string mainExePath)
        {
            if (!File.Exists(mainExePath))
            {
                Logger.Log("Player executable not found.", LogSource, Logger.LogLevelType.Error);
                return;
            }

            Process.Start(new ProcessStartInfo(mainExePath)
            {
                UseShellExecute = true
            });

            Logger.Log("Player launched successfully.", LogSource, Logger.LogLevelType.Success);

            await Task.Delay(500);
            Application.Current.Exit();
        }

        #endregion
    }
}