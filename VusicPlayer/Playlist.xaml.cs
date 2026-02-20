using ABI.Microsoft.UI.Xaml;
using LibVLCSharp.Shared;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Intrinsics.Arm;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.System;
using Windows.UI.Core;
using Application = Microsoft.UI.Xaml.Application;
using FileAttributes = System.IO.FileAttributes;
using FrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using Path = System.IO.Path;
using RoutedEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicPlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Playlist : Page, IUpdateableMusicPage
    {
        public Playlist()
        {
            InitializeComponent();
        }
        private PlaylistProperties? _currentPlaylist;
        public ObservableCollection<SongModel> SongCollection { get; } = new();
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // e.Parameter is the 'clickedPlaylist' we sent earlier
            if (e.Parameter is PlaylistProperties selectedPlaylist)
            {
                _currentPlaylist = selectedPlaylist;
                // Now you have the data! Update your UI:
                txtPlaylistName.Text = selectedPlaylist.PlaylistName;
                txtGenreCov.Text = "Genre: " + selectedPlaylist.PlaylistGenre;
                if (selectedPlaylist.PlaylistGenre == "")
                {
                    txtGenreCov.Text = "";
                }
                txtDateCreation.Text = selectedPlaylist.DateCreation.ToString("dd MMMM yyyy");
                txtItemCount.Text = selectedPlaylist.PlaylistCount;
                if (!string.IsNullOrEmpty(selectedPlaylist.Thumbnail))
                {
                    imgPlaylistCover.Source = new BitmapImage(new Uri(selectedPlaylist.Thumbnail));
                }
                SongCollection.Clear();
                foreach (string path in selectedPlaylist.SongsPaths)
                {
                    try
                    {
                        StorageFile file = await StorageFile.GetFileFromPathAsync(path);
                        MusicProperties properties = await file.Properties.GetMusicPropertiesAsync();
                        string title = !string.IsNullOrWhiteSpace(properties.Title)
               ? properties.Title
               : file.DisplayName; // Falls back to filename

                        string album = !string.IsNullOrWhiteSpace(properties.Album)
                                       ? properties.Album
                                       : "Unknown Album";

                        string artist = !string.IsNullOrWhiteSpace(properties.Artist)
                                        ? properties.Artist
                                        : "Unknown Artist";
                        ts += properties.Duration;
                        SongCollection.Add(new SongModel
                        {

                            Title = title,
                            AlbumName = album,
                            Artist = artist,
                            SongDuration = properties.Duration,
                            FilePath = file.Path
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.Write("Exception: " + ex.Message);
                    }
                }
            }
            lstViewPlaylist.ItemsSource = SongCollection;
            int count = SongCollection.Count;
            txtItemCount.Text = $"{count} {(count == 1 ? "item" : "items")}";
            SongCollection.CollectionChanged += SongCollection_CollectionChanged;
            UpdateCurrentListhere(PlaybackState.CurrentlyPlayingPath);
            string formatted = ts.TotalHours >= 1
       ? ts.ToString(@"h\:mm\:ss")
       : ts.ToString(@"m\:ss");
            txtTotalDuration.Text = formatted;
            if (SongCollection.Count == 0)
            {
                panelEmptyplaylists.Visibility = Visibility.Visible;
                txtPlaylistContentHeader.Visibility = Visibility.Collapsed;
                ListPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async void SongCollection_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var currentSettings = await SettingsHelper.LoadSettingsAsync();
            if (_currentPlaylist != null)
            {
                var playlistInMasterList = currentSettings.SavedPlaylists
               .FirstOrDefault(p => p.PlaylistName == _currentPlaylist.PlaylistName);
                if (playlistInMasterList != null)
                {
                    List<string> SongPathsModified = new();
                    foreach (var item in SongCollection)
                    {
                        if (item.FilePath != null)
                            SongPathsModified.Add(item.FilePath);
                    }
                    int cplount = SongCollection.Count;
                    playlistInMasterList.PlaylistCount = $"{cplount} {(cplount == 1 ? "item" : "items")}";
                    playlistInMasterList.SongsPaths = SongPathsModified;
                    await SettingsHelper.SaveSettingsAsync(currentSettings);
                }
            }
            if (SongCollection.Count == 0)
            {
                panelEmptyplaylists.Visibility = Visibility.Visible;
                txtPlaylistContentHeader.Visibility = Visibility.Collapsed;
                ListPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                panelEmptyplaylists.Visibility = Visibility.Collapsed;
                txtPlaylistContentHeader.Visibility = Visibility.Visible;
                ListPanel.Visibility = Visibility.Visible;
            }
            int count = SongCollection.Count;
            txtItemCount.Text = $"{count} {(count == 1 ? "item" : "items")}";
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (txtRename.Text == "")
            {
                txtRename.Text = original;
            }
            txtPlaylistName.Text = txtRename.Text;
            string newName = txtPlaylistName?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(newName) || _currentPlaylist == null) return;
            flyoutRename.Hide();
            // 1. Load the fresh master list from disk
            var currentSettings = await SettingsHelper.LoadSettingsAsync();

            // 2. Locate the playlist in the freshly loaded list
            // If you don't have a Guid ID, we match by the name it had BEFORE this edit
            var playlistInMasterList = currentSettings.SavedPlaylists
                .FirstOrDefault(p => p.PlaylistName == _currentPlaylist.PlaylistName);
            if (playlistInMasterList != null)
            {
                // 3. Update the Master List object (The one being saved to JSON)
                playlistInMasterList.PlaylistName = newName;

                // 4. Update the Local Reference (The one the UI/ListView is bound to)
                _currentPlaylist.PlaylistName = newName;

                // 5. Save the master list back to the file
                await SettingsHelper.SaveSettingsAsync(currentSettings);
            }
        }
        string? original = "";
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

            original = txtRename.Text;
            txtRename.Text = txtPlaylistName.Text;
        }

        private void txtRename_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Use the Dispatcher to run this after the click event fully completes
                textBox.DispatcherQueue.TryEnqueue(() =>
                {
                    textBox.SelectAll();
                });
            }
        }

        private void removesongfromplaylistcreation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is SongModel song)
            {
                // This single line will now update the UI automatically
                SongCollection.Remove(song);
            }

        }

        private void mnftSongDetails_Click(object sender, RoutedEventArgs e)
        {
            var menuFlyoutItem = sender as MenuFlyoutItem;

            // 2. The 'DataContext' of the menu item IS the SongModel for that row
            var selectedsong = menuFlyoutItem?.DataContext as SongModel;
            if (App.MainWindowInstance is HomeWindow wind)
            {
                wind.ShowSongDetails(selectedsong.FilePath);
            }
        }
        ObservableCollection<string> paths = new();
        private void PlaySelection()
        {
            if (selectedSong.FilePath != null)
            {
                if (App.MainWindowInstance is HomeWindow homeWindow)
                {
                    if (paths.Count != 0)
                    {
                        paths.Clear();
                    }
                    paths = new();
                    paths.Add(selectedSong.FilePath);
                    homeWindow.LoadFileFromPath(paths);

                }
            }
        }
        TimeSpan ts;
        SongModel selectedSong = new();
        private void mnftPlaySong_Click(object sender, RoutedEventArgs e)
        {
            var menuFlyoutItem = sender as MenuFlyoutItem;

            // 2. The 'DataContext' of the menu item IS the SongModel for that row
            var selectedsong = menuFlyoutItem?.DataContext as SongModel;
            if (selectedsong != null)
            {
                selectedSong = selectedsong;
                PlaySelection();
            }
        }

        private async void btnOpenFileLoc_Click(object sender, RoutedEventArgs e)
        {
            if (ITM != null)
            {
                try
                {
                    // 2. Get the actual StorageFile object from the path
                    StorageFile file = await StorageFile.GetFileFromPathAsync(ITM.FilePath);

                    // 3. Set up LauncherOptions to highlight the file
                    var options = new FolderLauncherOptions();
                    options.ItemsToSelect.Add(file);

                    // 4. Get the parent folder and launch it with the selection
                    StorageFolder parentFolder = await file.GetParentAsync();
                    await Launcher.LaunchFolderAsync(parentFolder, options);
                }
                catch (Exception ex)
                {
                    // Handle cases where the file might have been moved or deleted
                    System.Diagnostics.Debug.WriteLine($"Could not open location: {ex.Message}");
                }
            }
        }
        SongModel? ITM;
        private async void mnftEditInfo_Click(object sender, RoutedEventArgs e)
        {
            var menuFlyoutItem = sender as MenuFlyoutItem;

            // 2. The 'DataContext' of the menu item IS the SongModel for that row
            var selectedsong = menuFlyoutItem?.DataContext as SongModel;
            ITM = selectedsong;
            if (selectedsong == null) return;
            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(selectedsong.FilePath);
                MusicProperties props = await file.Properties.GetMusicPropertiesAsync();
                string title = !string.IsNullOrWhiteSpace(props.Title)
               ? props.Title
               : file.DisplayName;
                txtSongName.Text = title;
                txtAlbumName.Text = props.Album;
                txtArtistName.Text = props.Artist;
                txtTrack.Text = props.TrackNumber.ToString();
                string genreDisplay = string.Join("; ", props.Genre);

                // If the list is empty, provide a fallback
                string finalGenre = !string.IsNullOrEmpty(genreDisplay) ? genreDisplay : "Unknown Genre";
                txtGenre.Text = finalGenre;
                txtYear.Text = props.Year.ToString();

                // 2. Retrieve the extra properties from the file
                #region Contributing_Artists
                try
                {
                    var propertyKeys = new string[] { "System.Music.Artist" };
                    var extraProperties = await file.Properties.RetrievePropertiesAsync(propertyKeys);

                    if (extraProperties != null && extraProperties.ContainsKey("System.Music.Artist"))
                    {
                        string[]? contributingArtists = extraProperties["System.Music.Artist"] as string[];
                        txtContributingArtist.Text = contributingArtists != null ? string.Join("; ", contributingArtists) : "";
                    }
                }
                catch (ArgumentException ex)
                {
                    Debug.WriteLine("Property Retrieval Failed: " + ex.Message);
                    // Fallback: use the standard artist property if the deep dive fails
                    txtContributingArtist.Text = props.Artist;
                }
                #endregion

                ValidateFileAccessibility(ITM.FilePath);
                await dlgEditProperties.ShowAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception: " + ex.Message);
            }
        }

        private async void dlgEditProperties_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (ITM != null)
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(ITM.FilePath);

                    var propertiesToSave = new Dictionary<string, object>();

                // 1. Title and Album
                propertiesToSave["System.Title"] = txtSongName.Text;
                propertiesToSave["System.Music.AlbumTitle"] = txtAlbumName.Text;

                // 2. The Secret Sauce: Save to AlbumArtist for the main "Artist" display
                // This is a single string, not an array.
                propertiesToSave["System.Music.AlbumArtist"] = txtArtistName.Text;

                // 3. Save the array to System.Music.Artist (Contributing Artists)
                string[] artistsArray = txtContributingArtist.Text.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                                  .Select(a => a.Trim())
                                                                  .ToArray();
                propertiesToSave["System.Music.Artist"] = artistsArray;

                // ... (rest of your Year/Track/Genre code) ...

                await file.Properties.SavePropertiesAsync(propertiesToSave);

                // Update UI
                ITM.Title = txtSongName.Text;
                ITM.Artist = txtArtistName.Text;
                ITM.AlbumName = txtAlbumName.Text;

            }
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool DeleteFile(string lpFileName);
        bool notsavable =true;
        private void ValidateFileAccessibility(string filePath)
        {
            if (!File.Exists(filePath)) return;

            bool isBlocked = File.Exists($"{filePath}:Zone.Identifier");
            var attributes = File.GetAttributes(filePath);
            bool isReadOnly = attributes.HasFlag(FileAttributes.ReadOnly);

            if (isBlocked || isReadOnly)
            {
                FileStatusInfoBar.IsOpen = true;
                notsavable = true; // Prevent the crash

                if (isBlocked && isReadOnly)
                {
                    FileStatusInfoBar.Message = "File is blocked and Read-Only. Properties cannot be modified.";
                    btnFixFile.Content = "Fix Both";
                }
                else if (isBlocked)
                {
                    FileStatusInfoBar.Message = "This file is blocked (came from another PC). Unblock to modify properties.";
                    btnFixFile.Content = "Unblock";
                }
                else // isReadOnly
                {
                    FileStatusInfoBar.Message = "This file is marked as Read-Only.";
                    btnFixFile.Content = "Make Writable";
                }
            }
            else
            {
                FileStatusInfoBar.IsOpen = false;
                notsavable = false;
            }
        }
        private void mnftEditInfo_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private async void btnPlayAll_Click(object sender, RoutedEventArgs e)
        {
            if (paths.Count != 0)
            {
                paths.Clear();
            }
            paths = new();
            foreach (var itm in SongCollection)
            {
                if (itm.FilePath != null)
                {
                    paths.Add(itm.FilePath);
                    shuffled.Add(itm.FilePath);

                }
            }
            if (App.MainWindowInstance is HomeWindow homeWindow)
            {
                if (shuffleenabled)
                {
                    ShuffleEntireList();
                    homeWindow.LoadFileFromPath(shuffled);
                }
                else
                {
                    homeWindow.LoadFileFromPath(paths);
                }
            }
            UpdatePlaylistPlayState();
        }
        ObservableCollection<string> shuffled = new();
        public void ShuffleEntireList()
        {
            if (paths.Count <= 1) return;

            Random rdm = new Random();
            int n = shuffled.Count;
            // Start from 0 since we want the whole list randomized
            for (int i = n - 1; i > 0; i--)
            {
                int j = rdm.Next(0, i + 1);

                // Swap items
                var temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }
        }
        public void UpdateCurrentState(string currentstate)
        {
            if (currentstate == null) return;

            this.DispatcherQueue.TryEnqueue(() =>
            {

            
                var Playing = "\uE769";
                var Paused = "\uE768";
                foreach (var item in lstViewPlaylist.Items)
                {
                    if (item is SongModel song)
                    {
                        if (song.FilePath == PlaybackState.CurrentlyPlayingPath)
                        {
                            if (currentstate == "playing")
                            {
                                song.Glyph = Playing;
                            }
                            else
                            {
                                song.Glyph = Paused;
                            }
                        }
                      
                    }
                }
            });
        }

        public void UpdateCurrentListhere(string currentplaying)
        {
            if (currentplaying == null) return;

            this.DispatcherQueue.TryEnqueue(() =>
            {
                // Get the system's standard text color for the current theme
                var normalBrush = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
                var highlightBrush = new SolidColorBrush(Microsoft.UI.Colors.Cyan);
                var Playing = "\uE769";
                foreach (var item in lstViewPlaylist.Items)
                {
                    if (item is SongModel song)
                    {
                        if (song.FilePath == currentplaying)
                        {
                            song.TitleColor = highlightBrush;
                            song.Glyph = Playing;
                            lstViewPlaylist.ScrollIntoView(song);
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
        private void lstViewPlaylist_ItemClick(object sender, ItemClickEventArgs e)
        {
            var lstViewItem = sender as ListViewItem;

            // 2. The 'DataContext' of the menu item IS the SongModel for that row
            var selectedsong = lstViewItem?.DataContext as SongModel;

            if (selectedsong?.FilePath != null)
            {
                selectedSong = selectedsong;
                PlaySelection();
            }
        }

        private void lstViewPlaylist_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {

        }
        bool shuffleenabled;
        private void btnShuffle_Click(object sender, RoutedEventArgs e)
        {
            if (btnShuffle.IsChecked == true)
            {
                shuffleenabled = true;
                txtShuffled.Visibility = Visibility.Visible;
                if (App.MainWindowInstance is HomeWindow window)
                {
                    window.ShuffleRemaining();
                }
            }
            else
            {
                shuffleenabled = false;
                txtShuffled.Visibility = Visibility.Collapsed;
                if (App.MainWindowInstance is HomeWindow window)
                {
                    foreach (var item in SongCollection)
                    {
                        pats.Add(item.FilePath);
                        Debug.WriteLine(item.FilePath);
                    }
                    window.RestoreOriginalOrder(pats);
                }
            }
        }
        ObservableCollection<string> pats = new();
        private ObservableCollection<NewPlaylistSongProperty> loadedSongs = new ObservableCollection<NewPlaylistSongProperty>();
        private async void btnEditPlaylistInfo_Click(object sender, RoutedEventArgs e)
        {
            txtEditPlaylistName.Text = txtPlaylistName.Text;
            txtEditGenre.Text = txtGenreCov.Text.Replace("Genre: ", "");
            imgPlaylistCov.Source = imgPlaylistCover.Source;
            btnEditPlaylistCover.IsEnabled = false;
            CoverOptions.Visibility = Visibility.Visible;
            lstViewPlaylistAddedSongs.ItemsSource = SongCollection;
            await dlgEditPlaylist.ShowAsync();
        }

        private async void btnEditPlaylistCover_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();

            // Get the handle from the specific instance we know is alive
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);

            if (hwnd == IntPtr.Zero)
            {
                // If for some reason the main window is gone, try the current active one
                hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentActiveWindow);
            }
            picker.CommitButtonText = "Choose";
            // 2. Initialize the picker with the handle
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".ico");

            var file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                CoverOptions.Visibility = Visibility.Visible;
                btnEditPlaylistCover.IsEnabled = false;
                ToolTipService.SetToolTip(imgPlaylistCov, Path.GetFileName(file.Path));
                imgPlaylistCov.Source = new BitmapImage(new Uri(file.Path));
            }
        }



        private void btnAddSongs_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnRemovePlaylistCover_Click(object sender, RoutedEventArgs e)
        {
            ToolTipService.SetToolTip(imgPlaylistCov, "");
            CoverOptions.Visibility = Visibility.Collapsed;
            btnEditPlaylistCover.IsEnabled = true;
            imgPlaylistCov.Source = null;
        }

        private void btnEditAddSongs_Click(object sender, RoutedEventArgs e)
        {

        }

        private void txtEditPlaylistName_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Use the Dispatcher to run this after the click event fully completes
                textBox.DispatcherQueue.TryEnqueue(() =>
                {
                    textBox.SelectAll();
                });
            }
        }

        private async void dlgEditPlaylist_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var currentSettings = await SettingsHelper.LoadSettingsAsync();
            if (_currentPlaylist != null)
            {
                var playlistInMasterList = currentSettings.SavedPlaylists
               .FirstOrDefault(p => p.PlaylistName == _currentPlaylist.PlaylistName);
                if (playlistInMasterList != null)
                {
                    string playlistname = _currentPlaylist.PlaylistName;
                    string Genre = _currentPlaylist.PlaylistGenre;
                    playlistInMasterList.PlaylistName =
         string.IsNullOrEmpty(txtEditPlaylistName.Text)
             ? playlistname
             : txtEditPlaylistName.Text;

                    playlistInMasterList.PlaylistGenre = txtEditGenre.Text;

                    if (imgPlaylistCov.Source is BitmapImage bitmap && bitmap.UriSource != null)
                    {
                        playlistInMasterList.Thumbnail = bitmap.UriSource.AbsoluteUri;
                    }
                    else
                    {
                        playlistInMasterList.Thumbnail = isdarktheme
                            ? "ms-appx:///Assets/playlistdefaultdark.png"
                            : "ms-appx:///Assets/playlistdefaultlight.png";
                    }
                    await SettingsHelper.SaveSettingsAsync(currentSettings);
                }
            }
            txtPlaylistName.Text = txtEditPlaylistName.Text;
            txtGenreCov.Text = txtEditGenre.Text;
            imgPlaylistCover.Source = (imgPlaylistCov.Source as BitmapImage)
                        ?? new BitmapImage(new Uri(isdarktheme
                            ? "ms-appx:///Assets/playlistdefaultdark.png"
                            : "ms-appx:///Assets/playlistdefaultlight.png"));
        }
        private bool isdarktheme = true;
        private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void txtArtistHyp_Click(object sender, RoutedEventArgs e)
        {
            var clickedArtist = sender as HyperlinkButton;

            var clickedItem = clickedArtist?.DataContext as SongModel;
            if (clickedArtist != null)
            {

                this.Frame.Navigate(typeof(ArtistInfo), clickedItem);
            }
        }

        private void txtAlbumHyp_Click(object sender, RoutedEventArgs e)
        {
            GoToAlbum(sender);
        }
        private void GoToAlbum( object sender)
        {

            if (sender is FrameworkElement clickedElement)
            {
                // 2. Extract the DataContext (your SongModel)
                if (clickedElement.DataContext is SongModel clickedItem)
                {
                    // 3. Navigate to the Album page
                    this.Frame.Navigate(typeof(Album), clickedItem);
                }
            }
        }
        private void mnftGoToAlbum_Click(object sender, RoutedEventArgs e)
        {
            GoToAlbum(sender);
        }

        private async void txtTitle_Click(object sender, RoutedEventArgs e)
        {
            var menuFlyoutItem = sender as HyperlinkButton;

            // 2. The 'DataContext' of the menu item IS the SongModel for that row
            var selectedsong = menuFlyoutItem?.DataContext as SongModel;
            if (selectedsong != null)
            {
                selectedSong = selectedsong;
                PlaySelection();
            }
            UpdatePlaylistPlayState();
        }
        private async void UpdatePlaylistPlayState()
        {
            var currentSettings = await SettingsHelper.LoadSettingsAsync();
            if (_currentPlaylist != null)
            {
                foreach (var playlist in currentSettings.SavedPlaylists)
                {
                    playlist.PlaylistNowPlaying = string.Empty;
                }
                var playlistInMasterList = currentSettings.SavedPlaylists
               .FirstOrDefault(p => p.PlaylistName == _currentPlaylist.PlaylistName);
                if (playlistInMasterList != null)
                {
                    playlistInMasterList.PlaylistNowPlaying = "Now playing...";
                    await SettingsHelper.SaveSettingsAsync(currentSettings);
                }
            }
        }
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            // 2. The 'DataContext' of the menu item IS the SongModel for that row
            var selectedsong = button?.DataContext as SongModel;
            if (selectedsong.FilePath != PlaybackState.CurrentlyPlayingPath)
            {
                return; // Do absolutely nothing
            }
            else
            {
                if (selectedsong.Glyph == "\uE769")
                {
                    //if playing, then pause 
                    if (App.MainWindowInstance is HomeWindow homeWindow)
                    {
                        homeWindow.PlayPausePublic("playing");
                    }
                    selectedsong.Glyph = "\uE768";
                }
                else if (selectedsong.Glyph == "\uE768")
                {
                    if (App.MainWindowInstance is HomeWindow homeWindow)
                    {
                        homeWindow.PlayPausePublic("paused");
                    }
                    //if paused, then play
                    selectedsong.Glyph = "\uE769";
                }
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {

        }

        private void btnFixFile_Click(object sender, RoutedEventArgs e)
        {
            if (ITM == null) return;

            string path = ITM.FilePath;

            // 1. Fix Blocked status
            if (File.Exists($"{path}:Zone.Identifier"))
            {
                DeleteFile($"{path}:Zone.Identifier");
            }

            // 2. Fix Read-Only status
            var attributes = File.GetAttributes(path);
            if (attributes.HasFlag(FileAttributes.ReadOnly))
            {
                File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
            }

            // 3. Re-validate (hides the bar and enables Save)
            ValidateFileAccessibility(path);
            
        }
    }
}
