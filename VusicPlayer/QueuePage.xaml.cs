using Flyleaf.FFmpeg;
using FlyleafLib.MediaFramework.MediaDecoder;
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
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using Windows.System.Power;
using Windows.UI.Text;
using Application = Microsoft.UI.Xaml.Application;
using Button = Microsoft.UI.Xaml.Controls.Button;
using Filter = FlyleafLib.MediaFramework.MediaDecoder.Filter;
using FrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using RoutedEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;
using TextBox = Microsoft.UI.Xaml.Controls.TextBox;
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
            var status = PowerManager.EnergySaverStatus;
            if (status == EnergySaverStatus.On)
            {
                mssgBarMain.Title = "Energy Saver Mode";
                mssgBarMain.Message = "Energy saver is enabled on Windows. Frosted effects may not work as expected.";
                mssgBarMain.Severity = InfoBarSeverity.Warning;
                mssgBarMain.IsOpen = true;
            }
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
            PageState.IsQueuePage = false;
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
            PageState.IsQueuePage = true;
            CallValues();
            Main.CollectionChanged += Main_CollectionChanged;

        }
        bool _isbusy = false;
        private void Main_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Important: Don't let your "isBusy" flag block these updates 
            // unless you are currently loading the whole list from a database.
            if (_isbusy) return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null && e.OldItems[0] is SongModel removedTrack)
                    {
                        // Step 1: Remove from the Global list regardless of where it was
                        QueueListHolder.VusicQueue.Remove(removedTrack);
                        //       Debug.WriteLine($"Sync: Removed {removedTrack.Title} from Global");
                    }
                    break;

                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null && e.NewItems[0] is SongModel addedTrack)
                    {
                        // Step 2: Insert into Global based on its NEW position in the Local list
                        int localIndex = e.NewStartingIndex;

                        if (localIndex + 1 < Main.Count)
                        {
                            // Find the track that is now AFTER it in the local list
                            var trackAfter = Main[localIndex + 1];
                            int globalIndex = QueueListHolder.VusicQueue.IndexOf(trackAfter);

                            if (globalIndex != -1)
                            {
                                QueueListHolder.VusicQueue.Insert(globalIndex, addedTrack);
                            }
                            else
                            {
                                // Fallback if trackAfter isn't found
                                QueueListHolder.VusicQueue.Add(addedTrack);
                            }
                        }
                        else
                        {
                            // It's at the end of the local list, so put it at the end of global
                            QueueListHolder.VusicQueue.Add(addedTrack);
                        }
                        //    Debug.WriteLine($"Sync: Added {addedTrack.Title} to Global at relative index");
                    }
                    break;
            }
        }
        public async void UpdateQueue(string CurrentMediaPath)
        {
            ChangeCurrent();
        }
        private void LoadQueue()
        {
            try
            {
                _isbusy = true;
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
                    lstViewPlaylist.ItemsSource = Main;
                    //   lstViewQueue.LoadMedia(Main, this.Frame);
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
            finally
            {
                _isbusy = false;
            }
        }
        public ObservableCollection<SongModel> Main = new();
        private void ChangeCurrent()
        {

        }
        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            QueueHandler.PlayPrevious();

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
            try
            {
                _isbusy = true;
                var totalTime = TimeSpan.Zero;


                Main.Clear();
                foreach (var item in QueueListHolder.VusicQueue)
                {

                    Main.Add(item);
                    totalTime += item.SongDuration ?? TimeSpan.Zero;

                }
                if (grdNoSearchResults.Visibility == Visibility.Collapsed)
                {
                    lstViewPlaylist.ItemsSource = Main;
                    //             lstViewQueue.LoadMedia(Main, this.Frame);
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
            finally
            {
                _isbusy = false;
            }
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
            if (txtArtistDisplay.Text == "Unknown Artist") return;
            // 3. Navigate to the Album page
            if (PlaybackState.CurrentlyPlayingPath == "") return;
            StorageFile file = await StorageFile.GetFileFromPathAsync(PlaybackState.CurrentlyPlayingPath);
            MusicProperties properties = await file.Properties.GetMusicPropertiesAsync();

            string album = !string.IsNullOrWhiteSpace(properties.Artist) ? properties.Artist : "Unknown Artist";
            var clickeditem = new SongModel { Artist = album };
            this.Frame?.Navigate(typeof(ArtistInfo), clickeditem);
        }

        private async void txtAlbum_Click(object sender, RoutedEventArgs e)
        {
            if (txtAlbumDisplay.Text == "Unknown Album") return;
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
            if (App.HomeWindowInstance == null) return;
            AllSongs.CollectionChanged += AllSongs_CollectionChanged;
            OceanContentDialog.ClearSubscribers();

            // 2. Now add back only the current listener
            OceanContentDialog.PrimaryRequested += Dlg_PrimaryRequested;
            //           await PlaylistDialog.LoadPlaylistCreationDialog(true, new PlaylistProperties { PlaylistName = "New Playlist" }, this.Frame);
            OceanContentDialog.Show("Save Queue to New Playlist", "Create", "", "Cancel", OceanContentDialogDefault.Primary, contentsNewPlaylist, this.XamlRoot, 600, 760, OceanContentDialogType.Elevated, App.HomeWindowInstance, "addicon", "", "");
            AllSongs.Clear();
            foreach (var item in QueueListHolder.VusicQueue)
            {
                AllSongs.Add(item);
            }
            lstViewPlaylistAddedSongs.ItemsSource = AllSongs;
        }

        private void AllSongs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {

            txtNullAddedSongs.Visibility = AllSongs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            txtAddedSongs.Text = "Added Songs: " + $"{AllSongs.Count} {(AllSongs.Count == 1 ? "item" : "items")}";
        }
        private bool _isSavingPlaylist = false;
        private async void Dlg_PrimaryRequested()
        {
            Debug.WriteLine($"CreationCalled | Object ID: {this.GetHashCode()} | Playlist: {txtEditPlaylistName.Text}");
            if (_isSavingPlaylist) return;
            try
            {
                Debug.WriteLine("CreationCalled");
                var currentSettings = await SettingsHelper.LoadSettingsAsync();

                string baseName = txtEditPlaylistName.Text.Trim();
                if (string.IsNullOrEmpty(baseName)) baseName = "Queue";

                string finalName = baseName;
                int counter = 1;
                while (currentSettings.SavedPlaylists.Any(p =>
                    string.Equals(p.PlaylistName, finalName, StringComparison.OrdinalIgnoreCase)))
                {
                    finalName = $"{baseName} ({counter++})";
                }
                Uri defaultPath = new Uri("ms-appx:///Assets/playlistdefaultdark.png");


                if (playlistcoverpath != "")
                {
                    defaultPath = new Uri(playlistcoverpath);
                }
                else
                {
                    Uri darkIcon = new Uri("ms-appx:///Assets/playlistdefaultdark.png");

                    // Set your initial default (e.g., based on current theme)
                    defaultPath = darkIcon;
                }
                string playlistID = Guid.NewGuid().ToString("N");
                var newPlaylist = new PlaylistProperties
                {
                    PlaylistName = finalName,
                    PlaylistId = playlistID,
                    PlaylistCount = $"{AllSongs.Count} {(AllSongs.Count == 1 ? "item" : "items")}",
                    PlaylistNowPlaying = "",
                    PlaylistGenre = txtEditGenre.Text,
                    SongsPaths = AllSongs
        .Select(s => s.FilePath)
        .Where(path => path != null)
        .ToHashSet()!,
                    Thumbnail = defaultPath,
                    DateCreation = DateTime.Now.Date,
                };

                txtCreatedInfo.Text = $"Queue saved to {newPlaylist.PlaylistName}";
                tempid = newPlaylist.PlaylistId;
                currentSettings.SavedPlaylists.Add(newPlaylist);
                await SettingsHelper.SaveSettingsAsync(currentSettings);

                OceanContentDialog.HideDlg();
                HomeWindow.ShowWindow();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                ttCreatedInfo.IsOpen = true;
                await Task.Delay(3000);
                ttCreatedInfo.IsOpen = false;
                // Allow the button to be used again only after everything is done
                _isSavingPlaylist = false;
            }
        }
        ObservableCollection<SongModel> searchresults = new();

        private void asbFindQueue_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (asbFindQueue.Text == "")
            {
                searchresults.Clear();
                asbFindQueue.ItemsSource = null;
                grdNoSearchResults.Visibility = Visibility.Collapsed;
                lstViewPlaylist.ItemsSource = Main;
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
                    lstViewPlaylist.ItemsSource = searchresults;
                    //    lstViewQueue.LoadMedia(searchresults, Frame);
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
                lstViewPlaylist.ItemsSource = searchresults;
                //       lstViewQueue.LoadMedia(searchresults, Frame);
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
            if (lstViewQueue.SelectedItems.Count == 0) return;
            foreach (var item in lstViewQueue.SelectedItems)
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
        private void txtTitle_Click(object sender, RoutedEventArgs e)
        {
            var menuFlyoutItem = sender as HyperlinkButton;
            var selectedsong = menuFlyoutItem?.DataContext as SongModel;
            if (selectedsong != null)
            {
                selectedSong = selectedsong;
                PlaySelection();
            }
            UpdatePlaylistState.UpdatePlaylistPlayState();
        }
        private void GoToAlbum(object sender)
        {

            if (sender is FrameworkElement clickedElement)
            {
                if (clickedElement.DataContext is SongModel clickedItem)
                {
                    this.Frame.Navigate(typeof(Album), clickedItem);
                }
            }
        }

        SongModel selectedSong = new();
        private void txtArtistHyp_Click(object sender, RoutedEventArgs e)
        {
            var clickedArtist = sender as HyperlinkButton;

            var clickedItem = clickedArtist?.DataContext as SongModel;
            if (clickedArtist != null)
            {

                this.Frame?.Navigate(typeof(ArtistInfo), clickedItem);
            }
        }

        private void txtAlbumHyp_Click(object sender, RoutedEventArgs e)
        {

            GoToAlbum(sender);
        }

        private async void btnRemoveSelections_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = lstViewPlaylist.SelectedItems.Cast<SongModel>().ToList();

            foreach (var item in selectedItems)
            {
                Main.Remove(item);
            }
            if (btnRemoveSelections.Flyout is Flyout f)
            {
                f.Hide();
            }

        }
        private void PlaySelection()
        {
            if (selectedSong.FilePath != null)
            {
                if (File.Exists(selectedSong.FilePath))
                {
                    ObservableCollection<SongModel> temp = new();
                    temp.Add(selectedSong);
                    PlayerService.CreatePlayer();
                    QueueHandler.PlayMedia(temp, false, false);
                }
            }
        }
        public IList<object> SelectedItems => lstViewPlaylist.SelectedItems;
        private void lstViewPlaylist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstViewPlaylist.SelectedItems.Count > 0)
            {
                stkMultiOptions.Visibility = Visibility.Visible;
            }
            else
            {
                stkMultiOptions.Visibility = Visibility.Collapsed;
            }
        }

        private void btnEditAlbumMass_Click(object sender, RoutedEventArgs e)
        {
            if (App.HomeWindowInstance == null) return;
            OceanContentDialog.Show("Properties", "Save", "", "Cancel", OceanContentDialogDefault.Primary, MassEditgrd, this.XamlRoot, 800, 800, OceanContentDialogType.Elevated, App.HomeWindowInstance, "saveicon", "", "");
            OceanContentDialog.PrimaryRequested += OceanContentDialog_PrimaryRequested1;
            lstViewEdit.ItemsSource = Main;
            lstViewEdit.SelectedItems.Clear();
            foreach (var item in lstViewPlaylist.SelectedItems)
            {
                lstViewEdit.SelectedItems.Add(item);
            }
            tbviAlbum.IsSelected = true;

            txtEditAlbum.Text = Main[0].AlbumName;

        }

        private void OceanContentDialog_PrimaryRequested1()
        {
            foreach (var item in lstViewEdit.SelectedItems)
            {
                lstViewPlaylist.SelectedItems.Add(item);
            }
            if (tbviAlbum.IsSelected == true)
            {

                foreach (SongModel item in lstViewPlaylist.SelectedItems)
                {
                    try
                    {
                        item.AlbumName = txtEditAlbum.Text;
                        var file = TagLib.File.Create(item.FilePath);
                        file.Tag.Album = txtEditAlbum.Text;
                        file.Save();

                    }
                    catch (COMException ex)
                    {
                        btnFixFile.Visibility = Visibility.Collapsed;
                        FileStatusInfoBar.IsOpen = true;
                        FileStatusInfoBar.Title = "Error";
                        FileStatusInfoBar.Message = "An unexpected error occured while setting Album property. Check log page for more details under App Settings";
                        Logger.Log(ex.Message, "ListViewMedia.AlbumSetMultiple", Logger.LogLevelType.Error);
                    }
                }


            }
            else if (tbviArtist.IsSelected == true)
            {
                foreach (SongModel item in lstViewPlaylist.SelectedItems)
                {
                    try
                    {
                        item.Artist = txtEditArtist.Text;
                        var file = TagLib.File.Create(item.FilePath);
                        file.Tag.AlbumArtists = new[] { txtEditArtist.Text };

                        file.Save();
                    }
                    catch (IOException ex) when (IsFileLocked(ex))
                    {
                        btnFixFile.Visibility = Visibility.Collapsed;
                        FileStatusInfoBar.IsOpen = true;
                        FileStatusInfoBar.Title = "Error";
                        FileStatusInfoBar.Message = "The file is in use by another process. Check log page for more details under App Settings";
                        Logger.Log(ex.Message, "ListViewMedia.ArtistSetMultiple", Logger.LogLevelType.Error);

                    }
                    catch (COMException ex)
                    {
                        btnFixFile.Visibility = Visibility.Collapsed;
                        FileStatusInfoBar.IsOpen = true;
                        FileStatusInfoBar.Title = "Error";
                        FileStatusInfoBar.Message = "An unexpected error occured while setting Artist property. Check log page for more details under App Settings";
                        Logger.Log(ex.Message, "ListViewMedia.ArtistSetMultiple", Logger.LogLevelType.Error);
                    }
                    //Check for blocked files
                }
            }
            OceanContentDialog.HideDlg();
            HomeWindow.ShowWindow();
        }
        private bool IsFileLocked(IOException exception)
        {
            int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(exception) & ((1 << 16) - 1);
            return errorCode == 32 || errorCode == 33; // 32 = Sharing Violation, 33 = Lock Violation
        }
        private void btnEditArtistMass_Click(object sender, RoutedEventArgs e)
        {
            if (App.HomeWindowInstance == null) return;
            OceanContentDialog.Show("Properties", "Save", "", "Cancel", OceanContentDialogDefault.Primary, MassEditgrd, this.XamlRoot, 600, 600, OceanContentDialogType.Elevated, App.HomeWindowInstance, "saveicon", "", "");
            OceanContentDialog.PrimaryRequested += OceanContentDialog_PrimaryRequested1;

            tbviArtist.IsSelected = true;
            lstViewEdit.ItemsSource = Main;
            txtEditArtist.Text = Main[0].Artist;

            foreach (var item in lstViewPlaylist.SelectedItems)
            {
                lstViewEdit.SelectedItems.Add(item);
            }
            tbviAlbum.IsSelected = true;
        }

        private void btnAddtoPlaylistMass_Click(object sender, RoutedEventArgs e)
        {
            if (App.HomeWindowInstance == null) return;
            OceanContentDialog.Show("Properties", "Save", "", "Cancel", OceanContentDialogDefault.Primary, MassEditgrd, this.XamlRoot, 600, 600, OceanContentDialogType.Elevated, App.HomeWindowInstance, "saveicon", "", "");
            tbviAddToPlaylist.IsSelected = true;
        }

        private async void TabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tbviAddToPlaylist.IsSelected)
            {
                var currentSettings = await SettingsHelper.LoadSettingsAsync();
                var Playlists = currentSettings.SavedPlaylists;
                foreach (var item in Playlists)
                {
                    if (item == null) return;
                    if (item.PlaylistName == null) return;
                    List<string> playlistitems = new();
                    playlistitems!.Add(item.PlaylistName);
                    lstViewAddToPlaylists.ItemsSource = playlistitems;

                }
            }
        }

        private void removesongfromplaylistcreation_Click(object sender, RoutedEventArgs e)
        {
            var clickedMenuFlyout = sender as MenuFlyoutItem;

            var clickedItem = clickedMenuFlyout?.DataContext as SongModel;
            if (clickedItem != null)
            {
                Main.Remove(clickedItem);
            }

        }

        private void mnftUnselectItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftSelectitem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftSetAlbumName_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftSetArtistName_Click(object sender, RoutedEventArgs e)
        {

        }

        private void lstViewAddToPlaylists_ItemClick(object sender, ItemClickEventArgs e)
        {

        }

        private void btnClearSelection_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnRemoveSelectionsFromFavourites_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnSelectAll_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void btnFavourite_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var song = btn?.DataContext as SongModel;
            if (btn == null) return;
            if (song != null)
            {
                // 1. Toggle the data
                song.IsFavourite = !song.IsFavourite;

                // 2. Update your Settings/Database
                var settings = await SettingsHelper.LoadSettingsAsync();
                var favourites = settings.Favourites;
                var alreadyexisting = favourites.FirstOrDefault(f => f.FilePath == song.FilePath);
                if (alreadyexisting != null)
                {
                    favourites.Remove(alreadyexisting);
                    ToolTipService.SetToolTip(btn, "Add to favourites");
                    song.FavOpacity = 0;
                    song.FavString = "Add to Favourites";
                }
                else
                {
                    favourites.Add(new FavouritesModel { FilePath = song.FilePath });
                    ToolTipService.SetToolTip(btn, "Remove from favourites");
                    song.FavOpacity = 1;
                    song.FavString = "Remove from Favourites";
                }
                await SettingsHelper.SaveSettingsAsync(settings);

                // 3. Trigger animation
                var fillHeart = btn.FindName("FillHeart") as FontIcon;
                if (fillHeart == null) return;
                if (song.IsFavourite)
                    AnimateHeart.AnimateHeartIcon(fillHeart, 1.0, 1.0);
                else
                    AnimateHeart.AnimateHeartIcon(fillHeart, 0.0, 0.0);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftTools_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftMovetobottom_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftMovetotop_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftMovedown_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftMoveup_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void mnftAddToFavourites_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuFlyoutItem;
            var song = menuItem?.DataContext as SongModel;

            if (song != null)
            {
                // 1. Toggle the data

                song.IsFavourite = !song.IsFavourite;
                song.FavOpacity = song.IsFavourite ? 1 : 0;
                song.FavString = song.IsFavourite ? "Remove from Favourites" : "Add to Favourites";

                var settings = await SettingsHelper.LoadSettingsAsync();
                var favourites = settings.Favourites;
                var alreadyexisting = favourites.FirstOrDefault(f => f.FilePath == song.FilePath);
                if (alreadyexisting != null)
                {
                    favourites.Remove(alreadyexisting);
                }
                else
                {
                    favourites.Add(new FavouritesModel { FilePath = song.FilePath });
                }
                await SettingsHelper.SaveSettingsAsync(settings);


            }
        }


        private void mnftGoToAlbum_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftGoToArtist_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftAddtoQueue_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftSongDetails_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftAddtoPlaylist_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void mnftPlaySongNext_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftPlaySong_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftContext_Opened(object sender, object e)
        {
            var flyout = sender as MenuFlyout;
            var header = flyout?.Items
        .OfType<MenuFlyoutItem>()
        .FirstOrDefault(x => x.Name == "txtHeaderContext");
            var selectedsong = header?.DataContext as SongModel;
            if (selectedsong == null) return;
            if (header == null)
                return;
            header.Text = selectedsong.Title;
        }

        private void lstViewPlaylist_ItemClick(object sender, ItemClickEventArgs e)
        {

        }

        private void btnAddtoQueue_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void mnftEditInfo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnFixFile_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {

        }

        #region CreatePlaylistCodeBehind
        public ObservableCollection<SongModel> AllSongs { get; set; } = new();
        string playlistcoverpath = "";

        private void btnActualReset_Click(object sender, RoutedEventArgs e)
        {
            txtEditPlaylistName.Text = "";

            txtEditGenre.Text = "";
            AllSongs.Clear();
            imgPlaylistCov.Source = new BitmapImage(new Uri("ms-appx:///Assets/playlistdefaultdark.png"));
            CoverOptions.Visibility = Visibility.Collapsed;
            btnAddPlaylistCover.IsEnabled = true;
        }

        private void imgPlaylistCov_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            //SHOW LARGE VIEW OF IMAGE
        }

        private async void btnAddPlaylistCover_Click(object sender, RoutedEventArgs e)
        {
            if (App.OceanDialogInstance == null)
            {
                mssgBar.IsOpen = true;
                mssgBar.Title = "Error";
                mssgBar.Message = "An unexpected error occured. Check log details in Settings Page.";
                mssgBar.Severity = InfoBarSeverity.Error;
                Logger.Log("Error code 0x0012oc. Refer the github page for more details.", "PlaylistCreation", Logger.LogLevelType.Error);
                return;
            }
            var file = await PickFiles.PickSingleImageFileAsync(App.OceanDialogInstance, "Choose Image");

            if (file != null)
            {
                CoverOptions.Visibility = Visibility.Visible;
                ToolTipService.SetToolTip(imgPlaylistCov, Path.GetFileName(file.Path));
                imgPlaylistCov.Source = new BitmapImage(new Uri(file.Path));
                playlistcoverpath = file.Path;
            }
        }

        private void txtEditPlaylistName_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.DispatcherQueue.TryEnqueue(() =>
                {
                    textBox.SelectAll();
                });
            }
        }

        private async void btnAddSongs_Click(object sender, RoutedEventArgs e)
        {
            if (App.OceanDialogInstance == null)
            {
                mssgBar.IsOpen = true;
                mssgBar.Title = "Error";
                mssgBar.Message = "An unexpected error occured. Check log details in Settings Page.";
                mssgBar.Severity = InfoBarSeverity.Error;
                Logger.Log("Error code 0x0012oc. Refer the github page for more details.", "PlaylistCreation", Logger.LogLevelType.Error);
                return;
            }
            var files = await PickFiles.PickMultipleAudioFilesAsync(App.OceanDialogInstance, "Select Files");
            if (files == null) return;

            foreach (var file in files)
            {
                if (!AllSongs.Any(s => s.FilePath == file.Path))
                {
                    var musicProps = await file.Properties.GetMusicPropertiesAsync();

                    string duration = FormatTimeSpanDuration.Format(musicProps.Duration);
                    AllSongs.Add(new SongModel
                    {
                        Title = Path.GetFileNameWithoutExtension(file.Path),
                        SongDuration = musicProps.Duration,
                        FilePath = file.Path

                    });
                }
            }
            lstViewPlaylistAddedSongs.StartBringIntoView();
            lstViewPlaylistAddedSongs.ItemsSource = AllSongs;
        }

        private void asbSearchSongs_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {

        }

        private void asbSearchSongs_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {

        }

        private void asbSearchSongs_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {

        }

        private void mnftRemoveSongFromPlaylistCreation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is SongModel song)
            {
                AllSongs.Remove(song);
            }
            if (lstViewPlaylistAddedSongs.Items.Count == 0)
            {
                txtNullAddedSongs.Visibility = Visibility.Visible;
            }
        }

        private void btnRemovePlaylistCover_Click(object sender, RoutedEventArgs e)
        {
            ToolTipService.SetToolTip(imgPlaylistCov, "");
            CoverOptions.Visibility = Visibility.Collapsed;
            btnAddPlaylistCover.IsEnabled = true;
            imgPlaylistCov.Source = new BitmapImage(new Uri("ms-appx:///Assets/playlistdefaultdark.png"));
        }

        private async void btnOpenMediaFile_Click(object sender, RoutedEventArgs e)
        {
            if (App.HomeWindowInstance == null) return;
            var files = await PickFiles.PickMultipleAudioFilesAsync(App.HomeWindowInstance, "Choose files");
            if (files != null)
            {



                foreach (var singlefile in files)
                {
                    if (File.Exists(singlefile.Path))
                    {
                        StorageFile file = await StorageFile.GetFileFromPathAsync(singlefile.Path);
                        MusicProperties properties = await file.Properties.GetMusicPropertiesAsync();

                        string title = !string.IsNullOrWhiteSpace(properties.Title) ? properties.Title : file.DisplayName;
                        string album = !string.IsNullOrWhiteSpace(properties.Album) ? properties.Album : "Unknown Album";
                        string artist = !string.IsNullOrWhiteSpace(properties.Artist) ? properties.Artist : "Unknown Artist";
                        ObservableCollection<SongModel> temp = new();
                        temp.Add(new SongModel
                        {
                            Title = title,
                            AlbumName = album,
                            Artist = artist,
                            SongDuration = properties.Duration,
                            FilePath = file.Path,
                        });
                        if (QueueListHolder.VusicQueue.Count == 0)
                        {
                            PlayerService.CreatePlayer();
                            QueueHandler.PlayMedia(temp, false, false);
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

                        }
                 
                        else
                        {
                            var newItems = temp.Where(t => !QueueListHolder.VusicQueue.Any(v => v.FilePath == t.FilePath));

                            foreach (var item in newItems)
                            {
                                QueueListHolder.VusicQueue.Add(item);
                            }

                        }
                        bool isFull = btnViewQueue.IsChecked == true;
                        if (isFull) LoadFullQueue(); else LoadQueue();
                        txtUpNext.Text = isFull ? "Full queue" : "Up Next";
                    }

                }
            }
        }

        private void mnftPitch_Click(object sender, RoutedEventArgs e)
        {
            ttPitch.IsOpen = true;
        }

        private void sldPitch_ValueChanged(double obj)
        {
            if (PlayerService.MasterPlayer != null)
            {
                Debug.WriteLine("CHECK1");
                string pitchString = obj.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                if (PlayerService.MasterPlayer.Config.Audio.Filters == null)
                    PlayerService.MasterPlayer.Config.Audio.Filters = new();

                var pitchFilter = new Filter()
                {
                    Name = "pitch_control",
                    Args = "asetrate=44100,atempo=1.0"
                };
                int newRate = (int)(44100 * obj);

                // We also have to adjust atempo to counteract the speed change
                // If pitch is 1.5x, we set tempo to 1/1.5 (0.66) to stay at normal speed
                double compensationTempo = 1.0 / obj;
                string tempoStr = compensationTempo.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

                // Update both keys in your named filter
                PlayerService.MasterPlayer.Config.Audio.UpdateFilter("pitch_control", "sample_rate", newRate.ToString());
                PlayerService.MasterPlayer.Config.Audio.UpdateFilter("pitch_control", "atempo", tempoStr);
            }
        }
        string tempid = "";
        private void btnFavourite_DataContextChanged(FrameworkElement sender, Microsoft.UI.Xaml.DataContextChangedEventArgs args)
        {
          
        }

        private async void btnViewPlaylist_Click(object sender, RoutedEventArgs e)
        {
            var currentsettings = await SettingsHelper.LoadSettingsAsync();
            var playlist = currentsettings.SavedPlaylists.FirstOrDefault(p => p.PlaylistId == tempid);
            PlaybackState.currentPlaylist = playlist;
            if (playlist != null)
            {
                this.Frame.Navigate(typeof(Playlist), playlist);
            }
        }
    }
        #endregion

}


