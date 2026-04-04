using Microsoft.UI.Xaml;
using Microsoft.Win32;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace VusicPlayer
{
    public partial class App : Application
    {
        public static Window? m_window { get; private set; }
        public static Window? MainWindowInstance { get; set; }
        public static Window? MainWindowInstance2 { get; set; }
        public static Window? HomeWindowInstance { get; set; }
        public static Window? OceanDialogInstance { get; set; }
        public static Window? VideoPlayerWindowInstance { get; set; }
        public static Window? CurrentActiveWindow { get; set; }
        public static ObservableCollection<VideoProgress> GlobalContinuePlaying { get; } = new();

        public App()
        {
            this.InitializeComponent();
         
            RegisterOnce();
        }

        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
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
                        HomeWindow.HideWindow();

                        return;
                    }
                    else if (audioExtensions.Contains(extension))
                    {

                        var home = HomeWindow.ShowWindow();

                        StorageFile file2 = await StorageFile.GetFileFromPathAsync(filePath);
                        MusicProperties properties = await file2.Properties.GetMusicPropertiesAsync();

                        string title = !string.IsNullOrWhiteSpace(properties.Title) ? properties.Title : file2.DisplayName;
                        string album = !string.IsNullOrWhiteSpace(properties.Album) ? properties.Album : "Unknown Album";
                        string artist = !string.IsNullOrWhiteSpace(properties.Artist) ? properties.Artist : "Unknown Artist";


                        var SongCollection = new ObservableCollection<SongModel>();
                        SongCollection.Add(new SongModel
                        {
                            Title = title,
                            AlbumName = album,
                            Artist = artist,
                            SongDuration = properties.Duration,
                            FilePath = file.Path,

                        });
                        home.Create();
                        QueueHandler.PlayMedia(SongCollection, false, false);
                        return;
                    }
                }
                this.UnhandledException += App_UnhandledException;

            }
                HomeWindow.ShowWindow();
            
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            Logger.Log(e.Message, "App", Logger.LogLevelType.Error);
        }

        public static void SetCurrentMainWindow(Window window)
        {
            MainWindowInstance = window;
            m_window = window;
        }
        private void RegisterOnce()
        {
            string? exePath = Environment.ProcessPath;

            // Safety check: If for some reason we can't find the exe, stop here.
            if (string.IsNullOrEmpty(exePath))
            {
                return;
            }

            RegisterFormat(".3g2", "VusicPlayer.3g2", "3G2 video (VusicPlayer)", exePath);
            RegisterFormat(".3gp", "VusicPlayer.3gp", "3GP video (VusicPlayer)", exePath);
            RegisterFormat(".asf", "VusicPlayer.asf", "ASF video (VusicPlayer)", exePath);
            RegisterFormat(".asx", "VusicPlayer.asx", "ASX video (VusicPlayer)", exePath);
            RegisterFormat(".avi", "VusicPlayer.avi", "AVI video (VusicPlayer)", exePath);
            RegisterFormat(".dat", "VusicPlayer.dat", "DAT video (VusicPlayer)", exePath);
            RegisterFormat(".divx", "VusicPlayer.divx", "DIVX video (VusicPlayer)", exePath);
            RegisterFormat(".dv", "VusicPlayer.dv", "DV video (VusicPlayer)", exePath);
            RegisterFormat(".f4v", "VusicPlayer.f4v", "F4V video (VusicPlayer)", exePath);
            RegisterFormat(".flv", "VusicPlayer.flv", "FLV video (VusicPlayer)", exePath);
            RegisterFormat(".m2t", "VusicPlayer.m2t", "M2T video (VusicPlayer)", exePath);
            RegisterFormat(".m2ts", "VusicPlayer.m2ts", "M2TS video (VusicPlayer)", exePath);
            RegisterFormat(".m4v", "VusicPlayer.m4v", "M4V video (VusicPlayer)", exePath);
            RegisterFormat(".mkv", "VusicPlayer.mkv", "MKV video (VusicPlayer)", exePath);
            RegisterFormat(".mov", "VusicPlayer.mov", "MOV video (VusicPlayer)", exePath);
            RegisterFormat(".mp4", "VusicPlayer.mp4", "MP4 video (VusicPlayer)", exePath);
            RegisterFormat(".mpeg", "VusicPlayer.mpeg", "MPEG video (VusicPlayer)", exePath);
            RegisterFormat(".mpg", "VusicPlayer.mpg", "MPG video (VusicPlayer)", exePath);
            RegisterFormat(".mts", "VusicPlayer.mts", "MTS video (VusicPlayer)", exePath);
            RegisterFormat(".nsv", "VusicPlayer.nsv", "NSV video (VusicPlayer)", exePath);
            RegisterFormat(".ogm", "VusicPlayer.ogm", "OGM video (VusicPlayer)", exePath);
            RegisterFormat(".ogv", "VusicPlayer.ogv", "OGV video (VusicPlayer)", exePath);
            RegisterFormat(".rm", "VusicPlayer.rm", "RM video (VusicPlayer)", exePath);
            RegisterFormat(".rmvb", "VusicPlayer.rmvb", "RMVB video (VusicPlayer)", exePath);
            RegisterFormat(".ts", "VusicPlayer.ts", "TS video (VusicPlayer)", exePath);
            RegisterFormat(".vob", "VusicPlayer.vob", "VOB video (VusicPlayer)", exePath);
            RegisterFormat(".webm", "VusicPlayer.webm", "WEBM video (VusicPlayer)", exePath);
            RegisterFormat(".wmv", "VusicPlayer.wmv", "WMV video (VusicPlayer)", exePath);

            // Audio Extensions
            RegisterFormat(".aac", "VusicPlayer.aac", "AAC audio (VusicPlayer)", exePath);
            RegisterFormat(".ac3", "VusicPlayer.ac3", "AC3 audio (VusicPlayer)", exePath);
            RegisterFormat(".adt", "VusicPlayer.adt", "ADT audio (VusicPlayer)", exePath);
            RegisterFormat(".adts", "VusicPlayer.adts", "ADTS audio (VusicPlayer)", exePath);
            RegisterFormat(".aif", "VusicPlayer.aif", "AIF audio (VusicPlayer)", exePath);
            RegisterFormat(".aifc", "VusicPlayer.aifc", "AIFC audio (VusicPlayer)", exePath);
            RegisterFormat(".aiff", "VusicPlayer.aiff", "AIFF audio (VusicPlayer)", exePath);
            RegisterFormat(".amr", "VusicPlayer.amr", "AMR audio (VusicPlayer)", exePath);
            RegisterFormat(".aob", "VusicPlayer.aob", "AOB audio (VusicPlayer)", exePath);
            RegisterFormat(".ape", "VusicPlayer.ape", "APE audio (VusicPlayer)", exePath);
            RegisterFormat(".caf", "VusicPlayer.caf", "CAF audio (VusicPlayer)", exePath);
            RegisterFormat(".cda", "VusicPlayer.cda", "CDA audio (VusicPlayer)", exePath);
            RegisterFormat(".dts", "VusicPlayer.dts", "DTS audio (VusicPlayer)", exePath);
            RegisterFormat(".flac", "VusicPlayer.flac", "FLAC audio (VusicPlayer)", exePath);
            RegisterFormat(".it", "VusicPlayer.it", "IT audio (VusicPlayer)", exePath);
            RegisterFormat(".m4a", "VusicPlayer.m4a", "M4A audio (VusicPlayer)", exePath);
            RegisterFormat(".m4p", "VusicPlayer.m4p", "M4P audio (VusicPlayer)", exePath);
            RegisterFormat(".mid", "VusicPlayer.mid", "MID audio (VusicPlayer)", exePath);
            RegisterFormat(".mka", "VusicPlayer.mka", "MKA audio (VusicPlayer)", exePath);
            RegisterFormat(".mlp", "VusicPlayer.mlp", "MLP audio (VusicPlayer)", exePath);
            RegisterFormat(".mod", "VusicPlayer.mod", "MOD audio (VusicPlayer)", exePath);
            RegisterFormat(".mp1", "VusicPlayer.mp1", "MP1 audio (VusicPlayer)", exePath);
            RegisterFormat(".mp2", "VusicPlayer.mp2", "MP2 audio (VusicPlayer)", exePath);
            RegisterFormat(".mp3", "VusicPlayer.mp3", "MP3 audio (VusicPlayer)", exePath);
            RegisterFormat(".mpc", "VusicPlayer.mpc", "MPC audio (VusicPlayer)", exePath);
            RegisterFormat(".oga", "VusicPlayer.oga", "OGA audio (VusicPlayer)", exePath);
            RegisterFormat(".ogg", "VusicPlayer.ogg", "OGG audio (VusicPlayer)", exePath);
            RegisterFormat(".oma", "VusicPlayer.oma", "OMA audio (VusicPlayer)", exePath);
            RegisterFormat(".opus", "VusicPlayer.opus", "OPUS audio (VusicPlayer)", exePath);
            RegisterFormat(".ra", "VusicPlayer.ra", "RA audio (VusicPlayer)", exePath);
            RegisterFormat(".rmi", "VusicPlayer.rmi", "RMI audio (VusicPlayer)", exePath);
            RegisterFormat(".s3m", "VusicPlayer.s3m", "S3M audio (VusicPlayer)", exePath);
            RegisterFormat(".spx", "VusicPlayer.spx", "SPX audio (VusicPlayer)", exePath);
            RegisterFormat(".tta", "VusicPlayer.tta", "TTA audio (VusicPlayer)", exePath);
            RegisterFormat(".voc", "VusicPlayer.voc", "VOC audio (VusicPlayer)", exePath);
            RegisterFormat(".vqf", "VusicPlayer.vqf", "VQF audio (VusicPlayer)", exePath);
            RegisterFormat(".w64", "VusicPlayer.w64", "W64 audio (VusicPlayer)", exePath);
            RegisterFormat(".wav", "VusicPlayer.wav", "WAV audio (VusicPlayer)", exePath);
            RegisterFormat(".wma", "VusicPlayer.wma", "WMA audio (VusicPlayer)", exePath);
            RegisterFormat(".wv", "VusicPlayer.wv", "WV audio (VusicPlayer)", exePath);
            RegisterFormat(".xa", "VusicPlayer.xa", "XA audio (VusicPlayer)", exePath);
            RegisterFormat(".xm", "VusicPlayer.xm", "XM audio (VusicPlayer)", exePath);
        }

        private void RegisterFormat(string extension, string progId, string description, string exePath)
        {
            // 1. Point extension to unique ProgID
            using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{extension}"))
            {
                key.SetValue("", progId);
            }

            // 2. Set the unique description for this specific ProgID
            using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}"))
            {
                key.SetValue("", description);
            }

            // 3. Set the command
            using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}\shell\open\command"))
            {
                key.SetValue("", $"\"{exePath}\" \"%1\"");
            }
        }
    }
}