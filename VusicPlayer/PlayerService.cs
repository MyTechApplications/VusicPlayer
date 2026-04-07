using FlyleafLib.MediaPlayer;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.System;
using Windows.UI;
using Brush = Microsoft.UI.Xaml.Media.Brush;
using Color = Windows.UI.Color;
using DispatcherTimer = Microsoft.UI.Xaml.DispatcherTimer;

namespace VusicPlayer
{

    public static class PlayerService
    {
        public static Player? MasterPlayer { get; set; }
        public static DispatcherTimer? maintimer { get; set; }
        public static string currentvol = "0";
        public static string volumeglyph = "\uE767";
        public static Brush volForeground = new SolidColorBrush(Colors.White);

        public static int? originalvolume;
        public static void VolumeChange(double obj)
        {
            UIController.VolumeString = ((int)obj).ToString() + "%";
            if (MasterPlayer == null) return;

            int vol = (int)obj;
            if (vol != 0)
            {
                originalvolume = vol;
            }
            currentvol = vol.ToString();
            UIController.VolumeString = currentvol + "%";
            MasterPlayer.Audio.Volume = vol;

            // 1. Determine the Glyph
            volumeglyph = vol switch
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

            volForeground = new SolidColorBrush(iconColor);
        }

        public static void CreatePlayer()
        {
            if (MasterPlayer == null)
            {
                MasterPlayer = new Player();
            }
            if (maintimer == null)
            {
                maintimer = new DispatcherTimer();
                maintimer.Interval = TimeSpan.FromMilliseconds(250);
                maintimer.Tick += Maintimer_Tick;
            }
            MasterPlayer.PlaybackStopped += MasterPlayer_PlaybackStopped;

            maintimer.Start();

        }
        public static async void PlayFile(string path)
        {

            PlaybackState.CurrentlyPlayingPath = path;


            StorageFile file = await StorageFile.GetFileFromPathAsync(path);
            var musicProps = await file.Properties.GetMusicPropertiesAsync();

            TimeSpan duration = musicProps.Duration;
            //    sldMain!.Maximum = duration.TotalSeconds;
            UIController.TotalDuration = duration.TotalSeconds;
            UIController.TotalDurationString = duration.ToString(@"hh\:mm\:ss");
            UIController.SongDisplayName = Path.GetFileName(path);
            string album = !string.IsNullOrWhiteSpace(musicProps.Album) ? musicProps.Album : "Unknown Album";
            string artist = !string.IsNullOrWhiteSpace(musicProps.Artist) ? musicProps.Artist : "Unknown Artist";
            UIController.AlbumDisplayName = album;
            UIController.ArtistDisplayName = artist;
            await LoadMediaAsync(path);

            MasterPlayer?.Open(path);
            MediaCompleted = false;
            Play();
            App.HomeWindowInstance?.DispatcherQueue.TryEnqueue(async () =>
            {
                var settings = await SettingsHelper.LoadSettingsAsync();
                var existingSong = settings.RecentMusic.FirstOrDefault(x => x.SongPath == path);
                if (existingSong == null)
                {

                    var newRecent = new RecentMusic
                    {
                        SongName = Path.GetFileName(path),
                        SongPath = path,
                        FolderName = new DirectoryInfo(Path.GetDirectoryName(path) ?? string.Empty).Name,
                        PlayCount = 1
                    };
                    settings.RecentMusic.Insert(0, newRecent);
                }
                else
                {
                    existingSong.PlayCount++;
                    settings.RecentMusic.Remove(existingSong);
                    settings.RecentMusic.Insert(0, existingSong);
                }


                await SettingsHelper.SaveSettingsAsync(settings);
            });
        }
        public static bool MediaCompleted { get; set; }
        private static void MasterPlayer_PlaybackStopped(object? sender, PlaybackStoppedArgs e)
        {
            if (UIController.TotalDurationString == UIController.RunningDurationString)
            {
                MediaCompleted = true;
                App.HomeWindowInstance?.DispatcherQueue.TryEnqueue(async () =>
                {

                    var bitm = new BitmapImage(new Uri("ms-appx:///Assets/play.png"));
                    maintimer?.Stop();
                    UIController.CurrentPosition = 0;
                    UIController.PlayPauseToolTipSer = "Play";
                    UIController.Thumbnail = bitm;



                }); 
            

                QueueHandler.PlayNext();
            }
        }


        public static ObservableCollection<string> OriginalPaths = new();
        public static ObservableCollection<string> ReceivedPaths = new();
        public static void PlayQueue(bool isshuffleenabled, ObservableCollection<string> Paths)
        {
            ReceivedPaths.Clear();
            OriginalPaths.Clear();
            OriginalPaths = Paths;
            ReceivedPaths = Paths;
            if (isshuffleenabled == true)
            {
                var shuffled = OriginalPaths.OrderBy(a => Guid.NewGuid()).ToList();
                ReceivedPaths.Clear();
                foreach (var shuffleditem in shuffled)
                {
                    ReceivedPaths.Add(shuffleditem);
                }
            }
            if (App.HomeWindowInstance is HomeWindow wind)
            {
                QueueService.PlayMedia(ReceivedPaths);
            }
        }
        public static void UpdatePlayQueue(ObservableCollection<string> Paths)
        {
            if (App.HomeWindowInstance is HomeWindow wind)
            {
                QueueService.PlayMedia(Paths);
            }
        }
        public static void SldMain_DragCompleted(SliderReuse slider)
        {
            if (MasterPlayer == null) return;
            double newPosition = slider.Value / slider.Maximum;
            MasterPlayer.CurTime = TimeSpan.FromSeconds(slider.Value).Ticks;
            var curTime = TimeSpan.FromTicks(MasterPlayer.CurTime);

            _isDragging = false;
            maintimer?.Start();
        }
        public static async Task<BitmapImage> GetFileThumbnailAsync(string path)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(path);
            var thumbnail = await file.GetScaledImageAsThumbnailAsync(
                ThumbnailMode.VideosView, 640, ThumbnailOptions.UseCurrentScale);

            // 2. Jumping back to the UI Thread to create the actual Image object
            // We use the App's main dispatcher
            TaskCompletionSource<BitmapImage> tcs = new();

            App.HomeWindowInstance?.DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    if (thumbnail != null)
                    {
                        await bitmapImage.SetSourceAsync(thumbnail);
                        tcs.SetResult(bitmapImage);
                    }
                    else
                    {
                        tcs.SetResult(new BitmapImage(new Uri("ms-appx:///Assets/Placeholder.png")));
                    }
                }
                catch
                {
                    tcs.SetResult(new BitmapImage(new Uri("ms-appx:///Assets/Placeholder.png")));
                }
            });

            return await tcs.Task;
        }

        public static async Task LoadMediaAsync(string path)
        {
            // 1. Get the thumbnail (this runs in the background)
            var bitmap = await GetFileThumbnailAsync(path);

            // 2. Update the global state
            // The UI will "pick this up" automatically on whatever page is open
            UIController.Thumbnail2 = bitmap;
        }
        public static MediaPlaybackController UIController => MediaPlaybackController.instance;


        public static void SldMain_DragStarted()
        {
            if (MasterPlayer == null) return;
            _isDragging = true;
            maintimer?.Stop();
        }

        private static bool _isDragging = false;
        private static void Maintimer_Tick(object? sender, object e)
        {
            if (!_isDragging && MasterPlayer != null)
            {
                var curTime = TimeSpan.FromTicks(MasterPlayer.CurTime);
                UIController.CurrentPosition = curTime.TotalSeconds;
            }
        }
        public static void Play()
        {
            if (MasterPlayer == null) return;
            if (MediaCompleted == true)
            {
                MasterPlayer.CurTime = 0;
            }
            MasterPlayer.Play();
            CurrentPlayState?.Invoke("Play");
            App.HomeWindowInstance?.DispatcherQueue.TryEnqueue( () =>
            {
                var bitm = new BitmapImage(new Uri("ms-appx:///Assets/pause.png"));
                UIController.PlayPauseToolTipSer = "Pause";
                UIController.Thumbnail = bitm;
                maintimer?.Start();

            });
        }
        public static event Action<string?>? CurrentPlayState;
        public static void Pause()
        {
            if (MasterPlayer == null) return;
            MasterPlayer.Pause();
            CurrentPlayState?.Invoke("Paused");
            App.HomeWindowInstance?.DispatcherQueue.TryEnqueue(async () =>
            {
                var bitm = new BitmapImage(new Uri("ms-appx:///Assets/play.png"));
                UIController.PlayPauseToolTipSer = "Play";

                UIController.Thumbnail = bitm;
                maintimer?.Stop();

            });

        }
    }
}
