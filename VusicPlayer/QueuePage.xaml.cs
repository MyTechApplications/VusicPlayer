using Flyleaf.FFmpeg;
using FlyleafLib.MediaPlayer;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Vortice.Direct3D11;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Text;
using Application = Microsoft.UI.Xaml.Application;
using RoutedEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;
using TextDecorations = Windows.UI.Text.TextDecorations;
using Visibility = Microsoft.UI.Xaml.Visibility;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicPlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class QueuePage : Page
    {
        public QueuePage()
        {
            InitializeComponent();
            QueueHandler.QueueUpdated += QueueHandler_QueueUpdated;

        }

        private void QueueHandler_QueueUpdated(object? sender, string e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                bool isFull = btnViewQueue.IsChecked == true;

                if (isFull) LoadFullQueue(); else LoadQueue();
                txtUpNext.Text = isFull ? "Full queue" : "Up Next";
            });
        }

        private QueueHandler _queueHandler;


        private void NewSong_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SongModel.IsCompleted))
            {
                Debug.WriteLine("Just don't care");
                var updatedSong = sender as SongModel;

                DispatcherQueue.TryEnqueue(() =>
                {
                    if (btnViewQueue.IsChecked == true)
                    {
                        LoadFullQueue();
                        txtUpNext.Text = "Full queue";
                    }
                    else
                    {
                        LoadQueue();
                        txtUpNext.Text = "Up Next";
                    }
                });
            }
        }

        public void LoadQueueRefresh()
        {
            LoadQueue();
        }
        public MediaPlaybackController mediacontroller => MediaPlaybackController.instance;

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {

            base.OnNavigatedFrom(e);
        }
        public async void CallValues()
        {
            foreach (var item in QueueListHolder.VusicQueue)
            {
                Debug.WriteLine(item.FilePath + " DONAMAG");
            }

            txtSongName.Text = Path.GetFileName(PlaybackState.CurrentlyPlayingPath);
            if (PlaybackState.CurrentlyPlayingPath == "") return;
            btnPlayPause.IsEnabled = true;
            btnPrev.IsEnabled = true;
            btnFav.IsEnabled = true;
            btnShuffle.IsEnabled = true;
            btnVolume.IsEnabled = true;
            sldVolume.IsEnabled = true;
            btnNext.IsEnabled = true;
            var player = PlayerService.MasterPlayer;
            if (player == null) return;
            if (player.IsPlaying)
            {
                imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/pause.png"));
            }
            else
            {

                imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/play.png"));
            }
            txtVolume.Text = PlayerService.currentvol + "%";
            VolumeIcon.Foreground = PlayerService.volForeground;
            VolumeIcon.Glyph = PlayerService.volumeglyph;
            DispatcherQueue.TryEnqueue(() => LoadQueue());

        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {

            CallValues();

        }
        public async void UpdateQueue(string CurrentMediaPath)
        {
            ChangeCurrent();
        }
        private void LoadQueue()
        {
            var totalTime = TimeSpan.Zero;

            
            Main.Clear();
            foreach (var item in QueueListHolder.VusicQueue)
            {
                if (item.IsCompleted == false && item.FilePath != PlaybackState.CurrentlyPlayingPath)
                {
                    Main.Add(item);
                    totalTime += item.SongDuration ?? TimeSpan.Zero;
                }
            }
            if (grdNoSearchResults.Visibility == Visibility.Collapsed)
            {
                lstViewQueue.LoadMedia(Main, this.Frame);
            }

            // Update UI Labels
            txtTotalDurQueue.Text = $"• {totalTime:hh\\:mm\\:ss}";
            txtCount.Text = $"• {Main.Count} {(Main.Count == 1 ? "item" : "items")}";

            // Toggle UI State in one block
            bool hasItems = Main.Count > 0;

            grdEmptyQueue.Visibility = hasItems ? Visibility.Collapsed : Visibility.Visible;
            lstViewQueue.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed;
            // Bulk update IsEnabled
            btnClearQueue.IsEnabled = btnSaveQueue.IsEnabled = btnRemoveSelectionFromQueue.IsEnabled =
            asbFindQueue.IsEnabled = tglLoopQueue.IsEnabled = tglShuffleQueue.IsEnabled = hasItems;
            UpdateCurrentListhere();
        }
        public ObservableCollection<SongModel> Main = new();
        private void ChangeCurrent()
        {

        }
        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            QueueService.PlayPrevious();

        }
        public void UpdateCurrentListhere()
        {
            if (PlaybackState.CurrentlyPlayingPath == null) return;

            this.DispatcherQueue.TryEnqueue(() =>
            {
                // Get the system's standard text color for the current theme
                var normalBrush = Application.Current.Resources["TextFillColorPrimaryBrush"] as SolidColorBrush;
                var highlightBrush = new SolidColorBrush(Microsoft.UI.Colors.Cyan);
                var Playing = "\uE769";
                foreach (var item in QueueListHolder.VusicQueue)
                {
                    if (item is SongModel song)
                    {
                        if (song.FilePath == PlaybackState.CurrentlyPlayingPath)
                        {
                            song.TitleColor = highlightBrush;
                            song.Glyph = Playing;
                        }
                        else
                        {
                            // This will be Black in Light theme and White in Dark theme
                            song.TitleColor = normalBrush;
                            song.Glyph = "\uEC4F";
                        }
                    }
                }
            });
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            QueueHandler.PlayNext();
        }
        private void PlayPauseEvent()
        {
            var player = PlayerService.MasterPlayer;
            if (player == null) return;
            if (player.IsPlaying)
            {
                player.Pause();
                imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/play.png"));
            }
            else
            {
                player.Play();
                imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/pause.png"));
            }
        }
        private void btnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            PlayPauseEvent();
        }

        private void btnVolume_Click(object sender, RoutedEventArgs e)
        {
            if (PlayerService.currentvol == "0")
            {
                double orig = Convert.ToDouble(PlayerService.originalvolume);
                sldVolume.Value = orig;
                PlayerService.VolumeChange(orig);

            }
            else
            {
                sldVolume.Value = 0;
                PlayerService.VolumeChange(0);
            }
            txtVolume.Text = PlayerService.currentvol + "%";
            VolumeIcon.Foreground = PlayerService.volForeground;
            VolumeIcon.Glyph = PlayerService.volumeglyph;
        }

        private void sldMain_DragStarted()
        {
            PlayerService.SldMain_DragStarted();
        }

        private void sldMain_DragCompleted()
        {
            PlayerService.SldMain_DragCompleted(sldMain);
        }
        private async void LoadFullQueue()
        {
            var totalTime = TimeSpan.Zero;


            Main.Clear();
            foreach (var item in QueueListHolder.VusicQueue)
            {

                Main.Add(item);
                totalTime += item.SongDuration ?? TimeSpan.Zero;

            }
            if (grdNoSearchResults.Visibility == Visibility.Collapsed)
            {
                lstViewQueue.LoadMedia(Main, this.Frame);
            }

            // Update UI Labels
            txtTotalDurQueue.Text = $"• {totalTime:hh\\:mm\\:ss}";
            txtCount.Text = $"• {Main.Count} {(Main.Count == 1 ? "item" : "items")}";

            // Toggle UI State in one block
            bool hasItems = Main.Count > 0;

            grdEmptyQueue.Visibility = hasItems ? Visibility.Collapsed : Visibility.Visible;
            lstViewQueue.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed;
            // Bulk update IsEnabled
            btnClearQueue.IsEnabled = btnSaveQueue.IsEnabled = btnRemoveSelectionFromQueue.IsEnabled =
            asbFindQueue.IsEnabled = tglLoopQueue.IsEnabled = tglShuffleQueue.IsEnabled = hasItems;
            UpdateCurrentListhere();
        }
        private async void btnViewQueue_Checked(object sender, RoutedEventArgs e)
        {
            if (btnViewQueue.IsChecked == true)
            {
                DispatcherQueue.TryEnqueue(() => LoadFullQueue());
                txtUpNext.Text = "Full queue";

            }
            else
            {
                DispatcherQueue.TryEnqueue(() => LoadQueue());
                txtUpNext.Text = "Up Next";

            }
        }

        private void btnViewQueue_Unchecked(object sender, RoutedEventArgs e)
        {
            if (btnViewQueue.IsChecked == true)
            {
                DispatcherQueue.TryEnqueue(() => LoadFullQueue());
                txtUpNext.Text = "Full queue";

            }
            else
            {
                DispatcherQueue.TryEnqueue(() => LoadQueue());
                txtUpNext.Text = "Up Next";

            }
        }

        private async void txtArtist_Click(object sender, RoutedEventArgs e)
        {
            // 3. Navigate to the Album page
            StorageFile file = await StorageFile.GetFileFromPathAsync(PlaybackState.CurrentlyPlayingPath);
            MusicProperties properties = await file.Properties.GetMusicPropertiesAsync();

            string album = !string.IsNullOrWhiteSpace(properties.Artist) ? properties.Artist : "Unknown Artist";
            var clickeditem = new SongModel { Artist = album };
            this.Frame?.Navigate(typeof(ArtistInfo), clickeditem);
        }

        private async void txtAlbum_Click(object sender, RoutedEventArgs e)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(PlaybackState.CurrentlyPlayingPath);
            MusicProperties properties = await file.Properties.GetMusicPropertiesAsync();

            string album = !string.IsNullOrWhiteSpace(properties.Album) ? properties.Album : "Unknown Album";
            var clickeditem = new SongModel { AlbumName = album };
            this.Frame?.Navigate(typeof(Album), clickeditem);
        }

        private void btnClearQueue_Click(object sender, RoutedEventArgs e)
        {
            QueueListHolder.VusicQueue.Clear();
            Main.Clear();
            if (btnViewQueue.IsChecked == true)
            {
                DispatcherQueue.TryEnqueue(() => LoadFullQueue());
                txtUpNext.Text = "Full queue";

            }
            else
            {
                DispatcherQueue.TryEnqueue(() => LoadQueue());
                txtUpNext.Text = "Up Next";

            }
            if (Main.Count != 0)
            {
                grdEmptyQueue.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                lstViewQueue.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                btnClearQueue.IsEnabled = true;
                btnSaveQueue.IsEnabled = true;
                btnRemoveSelectionFromQueue.IsEnabled = true;
                asbFindQueue.IsEnabled = true;
                tglLoopQueue.IsEnabled = true;
                tglShuffleQueue.IsEnabled = true;
            }
            else
            {
                lstViewQueue.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                grdEmptyQueue.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                btnClearQueue.IsEnabled = false;
                btnSaveQueue.IsEnabled = false;
                btnRemoveSelectionFromQueue.IsEnabled = false;
                asbFindQueue.IsEnabled = false;
                tglLoopQueue.IsEnabled = false;
                tglShuffleQueue.IsEnabled = false;
            }
        }
        Random rng = new Random();

        public void ShuffleQueue()
        {
            var items = QueueListHolder.VusicQueue.ToList();
            int n = items.Count;


            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                var value = items[k];
                items[k] = items[n];
                items[n] = value;
            }
            DispatcherQueue.TryEnqueue(() =>
            {
                QueueListHolder.VusicQueue.Clear();
                foreach (var item in items)
                {
                    QueueListHolder.VusicQueue.Add(item);
                }

            });

        }
        ObservableCollection<SongModel> original = new();
        ObservableCollection<QueueSongModel> completedtemp = new();
        private void Shuffle()
        {
            if (tglShuffleQueue.IsChecked == true)
            {
                original.Clear();
                foreach (var item in QueueListHolder.VusicQueue)
                {
                    original.Add(item);
                }
                QueueHandler.ShuffleList();
            }
            else
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    QueueListHolder.VusicQueue.Clear();
                    foreach (var item in original)
                    {
                        QueueListHolder.VusicQueue.Add(item);
                    }
                });
                QueueHandler.ResetVideoIndex();
            }

            DispatcherQueue.TryEnqueue(() =>
            {
                if (btnViewQueue.IsChecked == true)
                {
                    LoadFullQueue();
                    txtUpNext.Text = "Full queue";
                }
                else
                {
                    LoadQueue();
                    txtUpNext.Text = "Up Next";
                }
            });
        }
        private void tglShuffleQueue_Checked(object sender, RoutedEventArgs e)
        {
            if (tglShuffleQueue.IsChecked == true)
            {
                btnShuffle.IsChecked = true;
            }
            else
            {
                btnShuffle.IsChecked = false;

            }
            Shuffle();
        }

        private void tglShuffleQueue_Unchecked(object sender, RoutedEventArgs e)
        {
            if (tglShuffleQueue.IsChecked == true)
            {
                btnShuffle.IsChecked = true;
            }
            else
            {
                btnShuffle.IsChecked = false;

            }
            Shuffle();
        }

        private async void btnSaveQueue_Click(object sender, RoutedEventArgs e)
        {
            //var currentSettings = await SettingsHelper.LoadSettingsAsync();

            //if (Main.Count != 0)
            //{
            //    var queueinlist = currentSettings.QueueSave;
            //    if (queueinlist != null)
            //    {
            //        queueinlist.ShuffleMode = btnShuffle.IsChecked ?? false;
            //        queueinlist.LoopMode = tglLoopQueue.IsChecked ?? false;
            //        queueinlist.MediaPaths?.Clear();
            //        foreach (var item in Main)
            //        {

            //            queueinlist.MediaPaths?.Add(item);
            //        }
            //        if (PlaybackState.CurrentlyPlayingPath != null)
            //        {
            //            queueinlist.CurrentMedia = PlaybackState.CurrentlyPlayingPath;
            //            queueinlist.CurrentMediaPosition = PlayerService.MasterPlayer!.CurTime;
            //        }
            //    }

            //    await SettingsHelper.SaveSettingsAsync(currentSettings);
            //}
        }
        ObservableCollection<SongModel> searchresults = new();

        private void asbFindQueue_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if(asbFindQueue.Text == "")
            {
                searchresults.Clear();
                asbFindQueue.ItemsSource = null;
                grdNoSearchResults.Visibility = Visibility.Collapsed;
                lstViewQueue.LoadMedia(Main, Frame);
                asbFindQueue.ItemsSource = null;
                lstViewQueue.Visibility = Visibility.Visible;

            }
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {

                searchresults.Clear();
                var rawQuery = asbFindQueue.Text.Trim();
                // 1. Extract time components
                var minMatch = Regex.Match(rawQuery, @"(\d+)\s*(?:min|m)", RegexOptions.IgnoreCase);
                var secMatch = Regex.Match(rawQuery, @"(\d+)\s*(?:sec|s)", RegexOptions.IgnoreCase);

                int searchSeconds = 0;
                if (minMatch.Success) searchSeconds += int.Parse(minMatch.Groups[1].Value) * 60;
                if (secMatch.Success) searchSeconds += int.Parse(secMatch.Groups[1].Value);

                // 2. Create a "Clean" query for text searching (removes the time parts)
                // This prevents searching for "3m" inside a song title if the user only meant 3 minutes.
                var textQuery = rawQuery;
                if (minMatch.Success) textQuery = textQuery.Replace(minMatch.Value, "");
                if (secMatch.Success) textQuery = textQuery.Replace(secMatch.Value, "");
                textQuery = textQuery.Trim().ToLower();

                // 3. Filter the list
                var results = Main.Where(s =>
                {
                    // Check if any text matches (only if textQuery isn't empty)
                    bool textMatch = !string.IsNullOrEmpty(textQuery) && (
                        (s.Title?.Contains(textQuery, StringComparison.OrdinalIgnoreCase) == true) ||
                        (s.Artist?.Contains(textQuery, StringComparison.OrdinalIgnoreCase) == true) ||
                        (s.AlbumName?.Contains(textQuery, StringComparison.OrdinalIgnoreCase) == true) ||
                        (s.Year.ToString().Contains(textQuery))
                    );

                    // Check if duration matches (within 2 seconds)
                    bool durationMatch = (searchSeconds > 0 && s.SongDuration.HasValue &&
                                         Math.Abs(s.SongDuration.Value.TotalSeconds - searchSeconds) < 2);

                    // Return true if either the text matches OR the duration matches
                    return textMatch || durationMatch;
                })
                .OrderByDescending(s => s.Title?.StartsWith(textQuery, StringComparison.OrdinalIgnoreCase) == true)
                .ThenBy(s => s.Title)
                .ToList();

                if (results.Any())
                {
                    asbFindQueue.ItemsSource = null;
                    foreach (var item in results)
                    {
                        searchresults.Add(item);
                    }
                    lstViewQueue.LoadMedia(searchresults, Frame);
                }
                else
                {
                    var noresult = new List<string>();
                    noresult.Add("No matches found!");
                    asbFindQueue.ItemsSource = null;
                    asbFindQueue.ItemsSource = noresult;
                }
            }
        }

        private async void asbFindQueue_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            searchresults.Clear();
            var query = asbFindQueue.Text.ToLower().Trim();
            var minMatch = Regex.Match(query, @"(\d+)\s*(?:min|m)");
            var secMatch = Regex.Match(query, @"(\d+)\s*(?:sec|s)");
            int searchSeconds = 0;
            if (minMatch.Success) searchSeconds += int.Parse(minMatch.Groups[1].Value) * 60;
            if (secMatch.Success) searchSeconds += int.Parse(secMatch.Groups[1].Value);
            var results = Main.Where(s =>
            (s.Title != null && s.Title.ToLower().Contains(query, StringComparison.OrdinalIgnoreCase)) ||
            (s.Artist != null && s.Artist.ToLower().Contains(query, StringComparison.OrdinalIgnoreCase)) ||
            (s.AlbumName != null && s.AlbumName.ToLower().Contains(query, StringComparison.OrdinalIgnoreCase)) ||
            (s.Year.ToString().ToLower().Contains(query, StringComparison.OrdinalIgnoreCase)) ||
            (searchSeconds > 0 && Math.Abs(s.SongDuration!.Value.TotalSeconds - searchSeconds) < 2)
         ).OrderByDescending(s =>
s.Title?.StartsWith(query, StringComparison.OrdinalIgnoreCase) == true)
                    .ThenBy(s => s.Title)
                    .ToList();

            if (results.Any())
            {
                foreach (var item in results)
                {
                    searchresults.Add(item);
                }
                lstViewQueue.LoadMedia(searchresults, Frame);
            }
            else
            {
                lstViewQueue.Visibility = Visibility.Collapsed;
                grdNoSearchResults.Visibility = Visibility.Visible;
                await Task.Delay(200);
                frmSearchResultsNOMATCH.Navigate(typeof(NoSearchResultsPage), null, new DrillInNavigationTransitionInfo());
            }
        }

        private void tglLoopQueue_Checked(object sender, RoutedEventArgs e)
        {
            LoopQueue();
        }

        private void tglLoopQueue_Unchecked(object sender, RoutedEventArgs e)
        {
            LoopQueue();
        }
        private void LoopQueue()
        {
            QueueHandler.Loop = tglLoopQueue.IsChecked ?? false;
        }

        private void btnCloseSearch_Click(object sender, RoutedEventArgs e)
        {
            asbFindQueue.Text = "";
            lstViewQueue.Focus(FocusState.Programmatic);
            asbFindQueue.ItemsSource = null;
        }

        private void btnShuffle_Checked(object sender, RoutedEventArgs e)
        {
            if (btnShuffle.IsChecked == true)
            {
                tglShuffleQueue.IsChecked = true;
            }
            else
            {
                tglShuffleQueue.IsChecked = false;

            }
        }

        private void btnShuffle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (btnShuffle.IsChecked == true)
            {
                tglShuffleQueue.IsChecked = true;
            }
            else
            {
                tglShuffleQueue.IsChecked = false;

            }
        }
        private readonly BitmapImage _heartGif = new BitmapImage(new Uri("ms-appx:///Assets/heartbeating.gif"));
        private readonly BitmapImage _heartStatic = new BitmapImage(new Uri("ms-appx:///Assets/favicon.png"));
        private void btnFav_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            imgFav.Source = _heartGif;

            // Ensure the GIF starts from the beginning
            if (_heartGif.IsAnimatedBitmap)
            {
                _heartGif.Play();
            }
        }

        private void btnFav_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            imgFav.Source = _heartStatic;
        }

        private void btnRemoveSelectionFromQueue_Click(object sender, RoutedEventArgs e)
        {
            foreach(var item in lstViewQueue.SelectedItems)
            {
                if (item is SongModel song)
                {
                    QueueListHolder.VusicQueue.Remove(song);
                }


            }
            bool isFull = btnViewQueue.IsChecked == true;

            if (isFull) LoadFullQueue(); else LoadQueue();
            txtUpNext.Text = isFull ? "Full queue" : "Up Next";
        }

        private void sldVolume_ValueChanged(double obj)
        {
            PlayerService.VolumeChange(obj);
            txtVolume.Text = PlayerService.currentvol + "%";
            VolumeIcon.Foreground = PlayerService.volForeground;
            VolumeIcon.Glyph = PlayerService.volumeglyph;
        }
    }
    }

