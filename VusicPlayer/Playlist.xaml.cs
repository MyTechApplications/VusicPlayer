using ABI.Microsoft.UI.Xaml;
using FlyleafLib.MediaFramework.MediaPlaylist;
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
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Intrinsics.Arm;
using System.Windows.Media;
using System.Windows.Shapes;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using WinRT.Interop;
using static System.Net.WebRequestMethods;
using Application = Microsoft.UI.Xaml.Application;
using File = System.IO.File;
using FileAttributes = System.IO.FileAttributes;
using FrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using Path = System.IO.Path;
using RoutedEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using Window = Microsoft.UI.Xaml.Window;

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
        private async void LoadMasterValues(PlaylistProperties selectedPlaylist)
        {
            _currentPlaylist = selectedPlaylist;
            txtPlaylistName.Text = selectedPlaylist.PlaylistName; txtGenreCov.Text = "Genre: " + selectedPlaylist.PlaylistGenre; if (selectedPlaylist.PlaylistGenre == "") { txtGenreCov.Text = ""; }
            txtDateCreation.Text = selectedPlaylist.DateCreation.ToString("dd MMMM yyyy"); txtItemCount.Text = selectedPlaylist.PlaylistCount; if (!string.IsNullOrEmpty(selectedPlaylist.Thumbnail)) { imgPlaylistCover.Source = new BitmapImage(new Uri(selectedPlaylist.Thumbnail)); }
            SongCollection.Clear();
            TimeSpan ts = TimeSpan.Zero;
            missingFiles.Clear();
            UpdatePlaylistState._currentPlaylist = _currentPlaylist;

            foreach (string path in selectedPlaylist.SongsPaths)
            {
                try
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(path);
                    MusicProperties properties = await file.Properties.GetMusicPropertiesAsync();

                    string title = !string.IsNullOrWhiteSpace(properties.Title) ? properties.Title : file.DisplayName;
                    string album = !string.IsNullOrWhiteSpace(properties.Album) ? properties.Album : "Unknown Album";
                    string artist = !string.IsNullOrWhiteSpace(properties.Artist) ? properties.Artist : "Unknown Artist";

                    ts += properties.Duration;

                    SongCollection.Add(new SongModel
                    {
                        Title = title,
                        AlbumName = album,
                        Artist = artist,
                        SongDuration = properties.Duration,
                        FilePath = file.Path,

                    });
                }
                catch
                {
                    missingFiles.Add(path); // Track missing files
                }
            }
            lstViewMaster.LoadMedia(SongCollection, this.Frame);
            //lstViewPlaylist.ItemsSource = SongCollection;
            int count = SongCollection.Count; txtItemCount.Text = $"{count} {(count == 1 ? "item" : "items")}";
            SongCollection.CollectionChanged += SongCollection_CollectionChanged;
            if (PlaybackState.CurrentlyPlayingPath != null)
            {
                UpdateCurrentListhere(PlaybackState.CurrentlyPlayingPath);
            }
            string formatted = ts.TotalHours >= 1 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"m\:ss");
            txtTotalDuration.Text = formatted;
            if (SongCollection.Count == 0)
            {
                panelEmptyplaylists.Visibility = Visibility.Visible;
                txtPlaylistContentHeader.Visibility = Visibility.Collapsed;
                ListPanel.Visibility = Visibility.Collapsed;
            }
            // Show InfoBar if files are missing
            if (missingFiles.Count > 0)
            {
                iBMissingFiles.IsOpen = true;
                string fileNames = string.Join(", ", missingFiles.Select(path => Path.GetFileName(path)));

                infoBarMessage.Text = $"The following file(s) could not be located: {fileNames}. Click 'Relocate' to fix.";
            }
            else
            {
                iBMissingFiles.IsOpen = false;
            }

        }
        private PlaylistProperties? _currentPlaylist;
        public ObservableCollection<SongModel> SongCollection { get; set; } = new(); List<string> missingFiles = new List<string>();
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // e.Parameter is the 'clickedPlaylist' we sent earlier
            // Keep track globally for this playlist

            if (e.Parameter is PlaylistProperties selectedPlaylist)
            {
                LoadMasterValues(selectedPlaylist);
            }
        }
        private async void RelocateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPlaylist == null || missingFiles.Count == 0) return;

            for (int i = 0; i < missingFiles.Count; i++)
            {
                string missingPath = missingFiles[i];

                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                picker.FileTypeFilter.Add("*");
                WinRT.Interop.InitializeWithWindow.Initialize(
                    picker,
                    WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance)
                );

                StorageFile newFile = await picker.PickSingleFileAsync();
                if (newFile != null)
                {
                    int index = _currentPlaylist.SongsPaths.IndexOf(missingPath);
                    if (index >= 0) _currentPlaylist.SongsPaths[index] = newFile.Path;
                    MusicProperties properties = await newFile.Properties.GetMusicPropertiesAsync();

                    string title = !string.IsNullOrWhiteSpace(properties.Title) ? properties.Title : newFile.DisplayName;
                    string album = !string.IsNullOrWhiteSpace(properties.Album) ? properties.Album : "Unknown Album";
                    string artist = !string.IsNullOrWhiteSpace(properties.Artist) ? properties.Artist : "Unknown Artist";
                    // Reload into SongCollection
                    SongCollection.Add(new SongModel
                    {
                        Title = title,
                        AlbumName = album,
                        Artist = artist,
                        SongDuration = properties.Duration,
                        FilePath = newFile.Path,
                    });
                }
            }

            missingFiles.Clear();
            iBMissingFiles.IsOpen = false;
        }
        private async void SongCollection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            var currentSettings = await SettingsHelper.LoadSettingsAsync();

            if (_currentPlaylist != null)
            {
                var playlistInMasterList = currentSettings.SavedPlaylists
                    .FirstOrDefault(p => p.PlaylistName == _currentPlaylist.PlaylistName);

                if (playlistInMasterList != null)
                {
                    List<string> SongPathsModified = new();
                    TimeSpan ts = TimeSpan.Zero;

                    var songsSnapshot = SongCollection.ToList();

                    foreach (var item in songsSnapshot)
                    {
                        if (item.FilePath != null)
                        {
                            SongPathsModified.Add(item.FilePath);

                            StorageFile file = await StorageFile.GetFileFromPathAsync(item.FilePath);
                            MusicProperties properties = await file.Properties.GetMusicPropertiesAsync();
                            ts += properties.Duration;
                        }
                    }

                    string formatted = ts.TotalHours >= 1
                        ? ts.ToString(@"h\:mm\:ss")
                        : ts.ToString(@"m\:ss");

                    txtTotalDuration.Text = formatted;

                    int cplount = songsSnapshot.Count;
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

    
        ObservableCollection<string> paths = new();
       
    
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
                    Logger.Log($"Could not open location: {ex.Message}", "PlaylistPage", Logger.LogLevelType.Error);
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
                    Logger.Log("Property Retrieval Failed: " + ex.Message, "PlaylistPage", Logger.LogLevelType.Error);
                    // Fallback: use the standard artist property if the deep dive fails
                    txtContributingArtist.Text = props.Artist;
                }
                #endregion

                ValidateFileAccessibility(ITM.FilePath);
                await dlgEditProperties.ShowAsync();
            }
            catch (Exception ex)
            {
                Logger.Log("Exception: " + ex.Message, "Playlist Page", Logger.LogLevelType.Error);
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
        bool notsavable = true;
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
        bool playallrunning = false;
        private async void btnPlayAll_Click(object sender, RoutedEventArgs e)
        {
            foreach(var item in SongCollection)
            {
                item.IsCompleted = false;
            }
            PlayerService.CreatePlayer();
            QueueHandler.PlayMedia(SongCollection, btnShuffle.IsChecked ?? false, false);
            playallrunning = true;
            UpdatePlaylistPlayState();
        }
        ObservableCollection<string> shuffled = new();
        public void UpdateCurrentState(string currentstate)
        {
           /* if (currentstate == null) return;

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
            }); */
        }

        public void UpdateCurrentListhere(string currentplaying)
        {
         /*   if (currentplaying == null) return;

            this.DispatcherQueue.TryEnqueue(() =>
            {
                // Get the system's standard text color for the current theme
                var normalBrush = Application.Current.Resources["TextFillColorPrimaryBrush"] as SolidColorBrush;
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
            });*/
        }
        private void lstViewPlaylist_ItemClick(object sender, ItemClickEventArgs e)
        {
         
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
           
            }
            else
            {
                shuffleenabled = false;
                txtShuffled.Visibility = Visibility.Collapsed;
                if (playallrunning == true) {
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
                        }
                    }
                    PlayerService.UpdatePlayQueue(paths);
                }
            }
        }
        private async void btnEditPlaylistInfo_Click(object sender, RoutedEventArgs e)
        {
            if (App.HomeWindowInstance == null) return;
            if (_currentPlaylist != null)
            {
                PlaylistDialog.LoadPlaylistCreationDialog(false, _currentPlaylist, this.Frame);
                OceanContentDialog.Show("Edit Playlist", "Save", "", "Cancel", OceanContentDialogDefault.Primary, contentsNewPlaylist, this.XamlRoot, 600, 760, OceanContentDialogType.Elevated, App.HomeWindowInstance, "saveicon", "", "");
                OceanContentDialog.PrimaryRequested += OceanContentDialog_PrimaryRequested;
            }
        }

        private async void OceanContentDialog_PrimaryRequested()
        {
            PlaylistDialog.SavePlaylist();
            OceanContentDialog.HideDlg();
            HomeWindow.ShowWindow();
           
        }

        private void CenterDialog(Window dialog)
        {
            var parent = App.MainWindowInstance;

            var parentHwnd = WinRT.Interop.WindowNative.GetWindowHandle(parent);
            var dialogHwnd = WinRT.Interop.WindowNative.GetWindowHandle(dialog);

            var parentId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(parentHwnd);
            var dialogId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(dialogHwnd);

            var parentApp = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(parentId);
            var dialogApp = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(dialogId);

            var parentPos = parentApp.Position;
            var parentSize = parentApp.Size;
            var dialogSize = dialogApp.Size;

            dialogApp.Move(new Windows.Graphics.PointInt32(
                parentPos.X + (parentSize.Width - dialogSize.Width) / 2,
                parentPos.Y + (parentSize.Height - dialogSize.Height) / 2
            ));
        }


        private async void btnEditPlaylistCover_Click(object sender, RoutedEventArgs e)
        {

        }



        private async void btnAddSongs_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            //Add songs to playlist
            var hwnd = WindowNative.GetWindowHandle(App.MainWindowInstance);
            InitializeWithWindow.Initialize(picker, hwnd);

            picker.FileTypeFilter.Add(".mp3");
            picker.FileTypeFilter.Add(".wav");
            picker.FileTypeFilter.Add(".m4a");
            picker.FileTypeFilter.Add(".ogg");

            var files = await picker.PickMultipleFilesAsync();

            if (files == null) return;

            ObservableCollection<string> existingPaths = new();
            foreach (var file in files)
            {
                MusicProperties properties = await file.Properties.GetMusicPropertiesAsync();

                string title = !string.IsNullOrWhiteSpace(properties.Title) ? properties.Title : file.DisplayName;
                string album = !string.IsNullOrWhiteSpace(properties.Album) ? properties.Album : "Unknown Album";
                string artist = !string.IsNullOrWhiteSpace(properties.Artist) ? properties.Artist : "Unknown Artist";

                existingPaths.Add(file.Path);
                if (!SongCollection.Any(s => s.FilePath == file.Path))
                {
                    var musicProps = await file.Properties.GetMusicPropertiesAsync();

                    string duration = FormatDuration(musicProps.Duration);

                    SongCollection.Add(new SongModel
                    {
                        Title = title,
                        AlbumName = album,
                        Artist = artist,
                        SongDuration = musicProps.Duration,

                        FilePath = file.Path,
                    });
                }
            }

        }
        private string FormatDuration(TimeSpan duration)
        {
            if (duration.Hours > 0)
                return duration.ToString(@"hh\:mm\:ss");

            return duration.ToString(@"mm\:ss");
        }

        private void btnRemovePlaylistCover_Click(object sender, RoutedEventArgs e)
        {

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
           
        }
        private bool isdarktheme = true;
        private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void txtArtistHyp_Click(object sender, RoutedEventArgs e)
        {
           
        }

        private void txtAlbumHyp_Click(object sender, RoutedEventArgs e)
        {
        }
        private void GoToAlbum(object sender)
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
          
        }

        private async void txtTitle_Click(object sender, RoutedEventArgs e)
        {
         
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
            if (button?.DataContext is SongModel selectedson && selectedson.FilePath != PlaybackState.CurrentlyPlayingPath)
            {
                return;
            }

            else
            {
                if (selectedsong.Glyph == "\uE769")
                {
                    //if playing, then pause 
                    selectedsong.Glyph = "\uE768";
                }
                else if (selectedsong.Glyph == "\uE768")
                {
                 
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

        private async void btnDeletePlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPlaylist == null) return;
            dlgDeleteConfirmPlaylist.Title = $"Are you sure you want to delete this playlist? {_currentPlaylist.PlaylistName}";
            deleteplaylistname = _currentPlaylist.PlaylistName;
            await dlgDeleteConfirmPlaylist.ShowAsync();
        }
        string deleteplaylistname = "";
        private async void dlgDeleteConfirmPlaylist_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var currentSettings = await SettingsHelper.LoadSettingsAsync();
            if (_currentPlaylist != null)
            {
                var playlistInMasterList = currentSettings.SavedPlaylists
               .FirstOrDefault(p => p.PlaylistName == _currentPlaylist.PlaylistName);
                if (playlistInMasterList != null)
                {
                    currentSettings.SavedPlaylists.Remove(playlistInMasterList);
                    await SettingsHelper.SaveSettingsAsync(currentSettings);
                }
            }
            var param = "DeletedPlaylist" + deleteplaylistname;
            this.Frame.Navigate(typeof(MusicLibrary), param);
        }

        private async void MenuFlyout_Opened(object sender, object e)
        {
          

        }

        private async void mnftAddtoPlaylist_Loaded(object sender, RoutedEventArgs e)
        {



        }

        private void mnftGoToArtist_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftAddToFavourites_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftMoveup_Click(object sender, RoutedEventArgs e)
        {
           

        }

        private void mnftMovedown_Click(object sender, RoutedEventArgs e)
        {
           

        }

        private void mnftMovetotop_Click(object sender, RoutedEventArgs e)
        {
        
        }

        private void mnftMovetobottom_Click(object sender, RoutedEventArgs e)
        {
          
        }

  
        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftTools_Click(object sender, RoutedEventArgs e)
        {

        }

        private void txtEditPlaylistName_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
