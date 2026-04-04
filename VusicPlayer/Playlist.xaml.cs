using ABI.Microsoft.UI.Xaml;
using FlyleafLib.MediaFramework.MediaPlaylist;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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
using Button = Microsoft.UI.Xaml.Controls.Button;
using File = System.IO.File;
using FileAttributes = System.IO.FileAttributes;
using FrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using Path = System.IO.Path;
using RoutedEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using TextBox = Microsoft.UI.Xaml.Controls.TextBox;
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
            txtDateCreation.Text = selectedPlaylist.DateCreation.ToString("dd MMMM yyyy"); txtItemCount.Text = selectedPlaylist.PlaylistCount; imgPlaylistCover.Source = new BitmapImage(selectedPlaylist.Thumbnail);
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
                 var colorbrush = new SolidColorBrush(Microsoft.UI.Colors.White);
                    var glyph = "\uEC4F";
                    if (PlaybackState.CurrentlyPlayingPath == file.Path)
                    {
                        colorbrush = new SolidColorBrush(Microsoft.UI.Colors.Cyan);
                        if(PlayerService.MasterPlayer!.IsPlaying)
                        glyph = "\uE769";
                        else
                        {
                            glyph = "\uE768";
                        }
                    }
                    var settings = await SettingsHelper.LoadSettingsAsync();
                    var favourites = settings.Favourites; 
                    var favSet = new HashSet<FavouritesModel>(favourites);
                    bool isfav = favSet.Any(f => f.FilePath == file.Path);
                    double opac = isfav ? 1.0 : 0.0;
                    string text = isfav ? "Remove from Favourites" : "Add to Favourites";
                    SongCollection.Add(new SongModel
                    {
                        Title = title,
                        AlbumName = album,
                        Artist = artist,
                        SongDuration = properties.Duration,
                        FilePath = file.Path,
                        FavOpacity = opac,
                        FavString = text,
                        IsFavourite = favSet.Any(f => f.FilePath == file.Path),
                    Glyph = glyph,
                    TitleColor = colorbrush,
                    });
                }
                catch
                {
                    missingFiles.Add(path); // Track missing files
                }
            }
            lstViewUnified.ItemsSource = SongCollection;
            //lstViewPlaylist.ItemsSource = SongCollection;
            //        lstViewMaster.LoadMedia(SongCollection, this.Frame);
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
            lstViewUnified.ShowArtistDetails += (songmodel) =>
            {
                this.Frame?.Navigate(typeof(ArtistInfo), songmodel);
            };
            lstViewUnified.ShowAlbumDetails += (songmodel) =>
            {
                this.Frame?.Navigate(typeof(Album), songmodel);
            };
        }
        private PlaylistProperties? _currentPlaylist;
        public ObservableCollection<SongModel> SongCollection { get; set; } = new(); List<string> missingFiles = new List<string>();
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is PlaylistProperties selectedPlaylist)
            {
                LoadMasterValues(selectedPlaylist);
            }
        }
        private async void RelocateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPlaylist == null || missingFiles.Count == 0) return;

            //for (int i = 0; i < missingFiles.Count; i++)
            //{
            //    string missingPath = missingFiles[i];

            //    var picker = new Windows.Storage.Pickers.FileOpenPicker();
            //    picker.FileTypeFilter.Add("*");
            //    WinRT.Interop.InitializeWithWindow.Initialize(
            //        picker,
            //        WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance)
            //    );

            //    StorageFile newFile = await picker.PickSingleFileAsync();
            //    if (newFile != null)
            //    {
            //        int index = _currentPlaylist.SongsPaths.IndexOf(missingPath);
            //        if (index >= 0) _currentPlaylist.SongsPaths[index] = newFile.Path;
            //        MusicProperties properties = await newFile.Properties.GetMusicPropertiesAsync();

            //        string title = !string.IsNullOrWhiteSpace(properties.Title) ? properties.Title : newFile.DisplayName;
            //        string album = !string.IsNullOrWhiteSpace(properties.Album) ? properties.Album : "Unknown Album";
            //        string artist = !string.IsNullOrWhiteSpace(properties.Artist) ? properties.Artist : "Unknown Artist";
            //        // Reload into SongCollection
            //        SongCollection.Add(new SongModel
            //        {
            //            Title = title,
            //            AlbumName = album,
            //            Artist = artist,
            //            SongDuration = properties.Duration,
            //            FilePath = newFile.Path,
            //        });
            //    }
            //}

            missingFiles.Clear();
            iBMissingFiles.IsOpen = false;
        }
        private CancellationTokenSource? _saveTaskTokenSource;
        private async void SongCollection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isSaving) return;
            _saveTaskTokenSource?.Cancel();
            _saveTaskTokenSource = new CancellationTokenSource();
            var token = _saveTaskTokenSource.Token;
            try
            {
                Debug.WriteLine("Caled");
                // Wait 300ms of "silence" before actually processing/saving
                await Task.Delay(300, token);
                var currentSettings = await SettingsHelper.LoadSettingsAsync();

                if (_currentPlaylist != null)
                {
                    var playlistInMasterList = currentSettings.SavedPlaylists
                        .FirstOrDefault(p => p.PlaylistId == _currentPlaylist.PlaylistId);

                    if (playlistInMasterList != null)
                    {
                        HashSet<string> SongPathsModified = new();
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

                UIUpdate();

                int count = SongCollection.Count;
                txtItemCount.Text = $"{count} {(count == 1 ? "item" : "items")}";
            }
            catch (TaskCanceledException) { return; }
        }
        string? original = "";
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            EditPlaylistDialogShow();
            txtEditPlaylistName.SelectAll();
        }
        private void UIUpdate()
        {
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
                if (ITM != null)
                    if (ITM.FilePath != null)
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

        bool playallrunning = false;
        private async void btnPlayAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in SongCollection)
            {
                item.IsCompleted = false;
            }
            PlayerService.CreatePlayer();
            QueueHandler.PlayMedia(SongCollection, btnShuffle.IsChecked ?? false, btnLoop.IsChecked?? false);
            playallrunning = true;
            UpdatePlaylistPlayState();
            var currentSettings = await SettingsHelper.LoadSettingsAsync();
            var playlists = currentSettings.SavedPlaylists;

            foreach (var playlist in playlists)
            {
                if (playlist.PlaylistName == txtPlaylistName.Text)
                {
                    playlist.PlaylistNowPlaying = "Now Playing...";
                }
                else
                {
                    // Reset all others to an empty string
                    playlist.PlaylistNowPlaying = "";
                }
            }

            await SettingsHelper.SaveSettingsAsync(currentSettings);
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
                if (playallrunning == true)
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
                        }
                    }
                    PlayerService.UpdatePlayQueue(paths);
                }
            }
        }
        private async void btnEditPlaylistInfo_Click(object sender, RoutedEventArgs e)
        {
            AllSongs.CollectionChanged += AllSongs_CollectionChanged;

            EditPlaylistDialogShow();
        }
        private async void EditPlaylistDialogShow()
        {
            if (App.HomeWindowInstance == null) return;
            if (_currentPlaylist != null)
            {
                txtEditPlaylistName.Text = _currentPlaylist.PlaylistName;
                txtEditGenre.Text = _currentPlaylist.PlaylistGenre;
                imgPlaylistCov.Source = imgPlaylistCover.Source;
                AllSongs.Clear();
                foreach (var item in SongCollection)
                {
                    AllSongs.Add(item);
                }
                lstViewPlaylistAddedSongs.ItemsSource = AllSongs;
                //   await PlaylistDialog.LoadPlaylistCreationDialog(false, _currentPlaylist, this.Frame);
                OceanContentDialog.Show("Edit Playlist", "Save", "", "Cancel", OceanContentDialogDefault.Primary, contentsNewPlaylist, this.XamlRoot, 600, 760, OceanContentDialogType.Elevated, App.HomeWindowInstance, "saveicon", "", "");
                OceanContentDialog.PrimaryRequested += OceanContentDialog_PrimaryRequested;
            }
        }
        private bool _isSaving = false;
        private async Task SavePlaylist()
        {

        }
        private async void OceanContentDialog_PrimaryRequested()
        {
            if (_isSaving) return; // Prevent multiple clicks/executions
            _isSaving = true;
            try
            {
                if (txtEditPlaylistName.Text == "")
                {
                    mssgBar.IsOpen = true;
                    mssgBar.Title = "Error";
                    mssgBar.Message = "Playlist name cannot be empty.";
                    mssgBar.Severity = InfoBarSeverity.Error;
                    return;
                }
                if (_currentPlaylist == null) return;
                txtPlaylistName.Text = txtEditPlaylistName.Text;
                txtEditGenre.Text = txtEditGenre.Text;
                SongCollection.Clear();
                imgPlaylistCover.Source = imgPlaylistCov.Source;
                foreach (var item in AllSongs)
                {
                    SongCollection.Add(item);
                }

           //     lstViewMaster.LoadMedia(SongCollection, this.Frame);
                var currentSettings = await SettingsHelper.LoadSettingsAsync();

                var playlistInMasterList = currentSettings.SavedPlaylists
               .FirstOrDefault(p => p.PlaylistId == _currentPlaylist.PlaylistId);
                if (playlistInMasterList != null)
                {
                    Debug.WriteLine("Found Playlist: " + playlistInMasterList.PlaylistId);
                    string playlistname = txtEditPlaylistName.Text ?? "Unknown Playlist";
                    string Genre = txtEditGenre.Text ?? "";
                    playlistInMasterList.PlaylistName =
         string.IsNullOrEmpty(txtEditPlaylistName.Text)
             ? playlistname
             : txtEditPlaylistName.Text;

                    if (playlistcoverpath == "")
                    {
                        playlistcoverpath = playlistInMasterList.Thumbnail!.ToString();
                    }
                    playlistInMasterList.PlaylistGenre = txtEditGenre.Text;
                    Uri defaultPath = new Uri(playlistcoverpath);


                    if (playlistcoverpath != "")
                    {
                        defaultPath = new Uri(playlistcoverpath);
                    }
                    else
                    {
                        Uri darkIcon = new Uri("ms-appx:///Assets/playlistdefaultdark.png");

                        defaultPath = darkIcon;
                    }

                    playlistInMasterList.Thumbnail = defaultPath;

                    playlistInMasterList.SongsPaths?.Clear();
                    foreach (var item in AllSongs)
                    {
                        if (item.FilePath != null)
                        {
                            playlistInMasterList.SongsPaths?.Add(item.FilePath);
                        }
                    }
                    playlistInMasterList.PlaylistCount = $"{AllSongs.Count} {(AllSongs.Count == 1 ? "item" : "items")}";

                    await SettingsHelper.SaveSettingsAsync(currentSettings);
                }
                OceanContentDialog.HideDlg();
                HomeWindow.ShowWindow();
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("The settings save was canceled by the system.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Actual error: {ex.Message}");
            }
            finally
            {
                _isSaving = false;
            }
            //if (_isSaving) return;
            //try
            //{
            //    PlaylistDialog.SavePla();
            //    SongCollection.Clear();
            //    txtPlaylistName.Text = Modifiedplaylist.Name;
            //    imgPlaylistCover.Source = new BitmapImage(new Uri(Modifiedplaylist.Thumbnail ?? "ms-appx:///Assets/playlistdefaultdark.png"));
            //    TimeSpan ts = TimeSpan.Zero;

            //    foreach (string path in Modifiedplaylist.SongsPaths)
            //    {
            //        try
            //        {
            //            bool alreadyExists = SongCollection.Any(s => s.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase));

            //            if (alreadyExists) continue;
            //            StorageFile file = await StorageFile.GetFileFromPathAsync(path);
            //            MusicProperties properties = await file.Properties.GetMusicPropertiesAsync();

            //            string title = !string.IsNullOrWhiteSpace(properties.Title) ? properties.Title : file.DisplayName;
            //            string album = !string.IsNullOrWhiteSpace(properties.Album) ? properties.Album : "Unknown Album";
            //            string artist = !string.IsNullOrWhiteSpace(properties.Artist) ? properties.Artist : "Unknown Artist";

            //            ts += properties.Duration;

            //            SongCollection.Add(new SongModel
            //            {
            //                Title = title,
            //                AlbumName = album,
            //                Artist = artist,
            //                SongDuration = properties.Duration,
            //                FilePath = file.Path,

            //            });
            //        }
            //        catch
            //        {
            //            missingFiles.Add(path); // Track missing files
            //        }
            //    }
            //    int count = SongCollection.Count;
            //    txtItemCount.Text = $"{count} {(count == 1 ? "item" : "items")}";
            //    string formatted = ts.TotalHours >= 1 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"m\:ss");
            //    txtTotalDuration.Text = formatted;
            //    if (SongCollection.Count == 0)
            //    {
            //        panelEmptyplaylists.Visibility = Visibility.Visible;
            //        txtPlaylistContentHeader.Visibility = Visibility.Collapsed;
            //        ListPanel.Visibility = Visibility.Collapsed;
            //    }
            //    lstViewMaster.LoadMedia(SongCollection, this.Frame);
            //    OceanContentDialog.HideDlg();
            //    HomeWindow.ShowWindow();
            //}
            //finally
            //{
            //    _isSaving = false;
            //}

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



        //private async void btnAddSongs_Click(object sender, RoutedEventArgs e)
        //{
        //    //   EditPlaylistDialogShow();
        //    //   var control = PlaylistDialog as PlaylistParameters;
        //    //   if (control != null)
        //    //   {
        //    //       await control.btnAddSongsClick();
        //    ////       var playlistupdated = await PlaylistDialog.SavePlaylist();
        //    // //      LoadMasterValues(playlistupdated);
        //    //       OceanContentDialog.HideDlg();
        //    //       HomeWindow.ShowWindow();
        //    //       return;
        //    //       // CLEAR HISTORY HERE

        //    //   }
        //    //var picker = new FileOpenPicker();
        //    ////Add songs to playlist
        //    //var hwnd = WindowNative.GetWindowHandle(App.MainWindowInstance);
        //    //InitializeWithWindow.Initialize(picker, hwnd);

        //    //picker.FileTypeFilter.Add(".mp3");
        //    //picker.FileTypeFilter.Add(".wav");
        //    //picker.FileTypeFilter.Add(".m4a");
        //    //picker.FileTypeFilter.Add(".ogg");

        //    //var files = await picker.PickMultipleFilesAsync();

        //    //if (files == null) return;

        //    //ObservableCollection<string> existingPaths = new();
        //    //foreach (var file in files)
        //    //{
        //    //    MusicProperties properties = await file.Properties.GetMusicPropertiesAsync();

        //    //    string title = !string.IsNullOrWhiteSpace(properties.Title) ? properties.Title : file.DisplayName;
        //    //    string album = !string.IsNullOrWhiteSpace(properties.Album) ? properties.Album : "Unknown Album";
        //    //    string artist = !string.IsNullOrWhiteSpace(properties.Artist) ? properties.Artist : "Unknown Artist";

        //    //    existingPaths.Add(file.Path);
        //    //    if (!SongCollection.Any(s => s.FilePath == file.Path))
        //    //    {
        //    //        var musicProps = await file.Properties.GetMusicPropertiesAsync();

        //    //        string duration = FormatDuration(musicProps.Duration);

        //    //        SongCollection.Add(new SongModel
        //    //        {
        //    //            Title = title,
        //    //            AlbumName = album,
        //    //            Artist = artist,
        //    //            SongDuration = musicProps.Duration,

        //    //            FilePath = file.Path,
        //    //        });
        //    //    }
        //    //}

        //}


        private void btnEditAddSongs_Click(object sender, RoutedEventArgs e)
        {

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

        private async void UpdatePlaylistPlayState()
        {

        }
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            var selectedsong = button?.DataContext as SongModel;
            if (selectedsong == null) return;
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
            //if (ITM == null) return;

            //string path = ITM.FilePath;

            //// 1. Fix Blocked status
            //if (File.Exists($"{path}:Zone.Identifier"))
            //{
            //    DeleteFile($"{path}:Zone.Identifier");
            //}

            //// 2. Fix Read-Only status
            //var attributes = File.GetAttributes(path);
            //if (attributes.HasFlag(FileAttributes.ReadOnly))
            //{
            //    File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
            //}

            //// 3. Re-validate (hides the bar and enables Save)
            //ValidateFileAccessibility(path);

        }

        private async void btnDeletePlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPlaylist == null) return;
            if (_currentPlaylist.PlaylistName == null) return;
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
        #region EditPlaylistCodeBehind
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
        private void AllSongs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isSaving) return;
            txtNullAddedSongs.Visibility = AllSongs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            txtAddedSongs.Text = "Added Songs: " + $"{AllSongs.Count} {(AllSongs.Count == 1 ? "item" : "items")}";
        }
        private async void btnAddSongs_Click(object sender, RoutedEventArgs e)
        {
            AddSongsClick();
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
        private void btnRemovePlaylistCover_Click(object sender, RoutedEventArgs e)
        {
            ToolTipService.SetToolTip(imgPlaylistCov, "");
            CoverOptions.Visibility = Visibility.Collapsed;
            btnAddPlaylistCover.IsEnabled = true;
            imgPlaylistCov.Source = new BitmapImage(new Uri("ms-appx:///Assets/playlistdefaultdark.png"));
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
        private async void AddSongsClick()
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
            var files = await PickFiles.PickMultipleAudioFilesAsync(App.OceanDialogInstance, "Add items");
            if (files == null) return;

            foreach (var file in files)
            {
                if (!AllSongs.Any(s => s.FilePath == file.Path))
                {
                    var musicProps = await file.Properties.GetMusicPropertiesAsync();

                    string duration = FormatTimeSpanDuration.Format(musicProps.Duration);
                    AllSongs.Add(new SongModel
                    {
                        Title = musicProps.Title,
                        SongDuration = musicProps.Duration,
                        FilePath = file.Path

                    });
                }
            }
            lstViewPlaylistAddedSongs.ItemsSource = AllSongs;
            lstViewPlaylistAddedSongs.StartBringIntoView();
        }
        private async void btnAddSongs2_Click(object sender, RoutedEventArgs e)
        {
            if (App.HomeWindowInstance == null) return;
            var files = await PickFiles.PickMultipleAudioFilesAsync(App.HomeWindowInstance, "Add items");
            if (files == null) return;

            foreach (var file in files)
            {
                if (!AllSongs.Any(s => s.FilePath == file.Path))
                {
                    var musicProps = await file.Properties.GetMusicPropertiesAsync();

                    string duration = FormatTimeSpanDuration.Format(musicProps.Duration);
                    AllSongs.Add(new SongModel
                    {
                        Title = musicProps.Title,
                        SongDuration = musicProps.Duration,
                        FilePath = file.Path

                    });
                }
            }
            lstViewPlaylistAddedSongs.ItemsSource = AllSongs;
            lstViewPlaylistAddedSongs.StartBringIntoView();
        }

        private void imgPlaylistCov_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //SHOW LARGE VIEW OF IMAGE
            Debug.WriteLine("Hhdhd");
            TempImagePath.Path = _currentPlaylist!.Thumbnail!.ToString();
            EnlargeImage enlargeImage = new EnlargeImage();
            enlargeImage.Activate();
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
                SongCollection.Remove(item);
            }
            if (btnRemoveSelections.Flyout is Flyout f)
            {
                f.Hide();
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
            OceanContentDialog.PrimaryRequested += OceanContentDialog_PrimaryRequested1; ;
            lstViewEdit.ItemsSource = SongCollection;
            lstViewEdit.SelectedItems.Clear();
            foreach (var item in lstViewPlaylist.SelectedItems)
            {
                lstViewEdit.SelectedItems.Add(item);
            }
            tbviAlbum.IsSelected = true;

            txtEditAlbum.Text = SongCollection[0].AlbumName;

        }
        public bool IsFileReady(string path)
        {
            try
            {
                // Try to open the file with Exclusive access
                using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    return true;
                }
            }
            catch (IOException)
            {
                return false; // File is locked by another process
            }
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
                        if (IsFileReady(item.FilePath))
                        {
                            item.AlbumName = txtEditAlbum.Text;
                        var file = TagLib.File.Create(item.FilePath);
                        file.Tag.Album = txtEditAlbum.Text;
                        file.Save();
                            OceanContentDialog.HideDlg();
                            HomeWindow.ShowWindow();
                        }
                        else
                        {
                            btnFixFile.Visibility = Visibility.Collapsed;
                            FileStatusInfoBar.IsOpen = true;
                            FileStatusInfoBar.Title = "Error";
                            FileStatusInfoBar.Severity = InfoBarSeverity.Error;
                            FileStatusInfoBar.Message = "The file is in use by another process.";

                        }
                    }
                    catch (Exception ex)
                    {
                        FileStatusInfoBar.Message = "The file is in use by another process. Check log details under App Settings";
                        FileStatusInfoBar.IsOpen = true;
                        FileStatusInfoBar.Title = "Error";
                        FileStatusInfoBar.Severity = InfoBarSeverity.Error;
                        btnFixFile.Visibility = Visibility.Collapsed;
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
                        if (IsFileReady(item.FilePath))
                        {
                            item.Artist = txtEditArtist.Text;
                            var file = TagLib.File.Create(item.FilePath);
                            file.Tag.AlbumArtists = new[] { txtEditArtist.Text };

                            file.Save();
                            OceanContentDialog.HideDlg();
                            HomeWindow.ShowWindow();
                        }
                        else
                        {
                            btnFixFile.Visibility = Visibility.Collapsed;
                            FileStatusInfoBar.IsOpen = true;
                            FileStatusInfoBar.Title = "Error";
                            FileStatusInfoBar.Severity = InfoBarSeverity.Error;
                            FileStatusInfoBar.Message = "The file is in use by another process.";
                    
                        }
                    }
                    catch (Exception ex)
                    {
                        FileStatusInfoBar.Message = "The file is in use by another process. Check log details under App Settings";
                        FileStatusInfoBar.IsOpen = true;
                        FileStatusInfoBar.Title = "Error";
                        FileStatusInfoBar.Severity = InfoBarSeverity.Error;
                        btnFixFile.Visibility = Visibility.Collapsed;
                        Logger.Log(ex.Message, "ListViewMedia.ArtistSetMultiple", Logger.LogLevelType.Error);
                    }
                    //catch (IOException ex) when (IsFileLocked(ex))
                    //{
                    //    DispatcherQueue.TryEnqueue(() =>
                    //    {
                    //        btnFixFile.Visibility = Visibility.Collapsed;
                    //        FileStatusInfoBar.IsOpen = true;
                    //        FileStatusInfoBar.Title = "Error";
                    //        FileStatusInfoBar.Message = "The file is in use by another process. Check log page for more details under App Settings";
                    //        Logger.Log(ex.Message, "ListViewMedia.ArtistSetMultiple", Logger.LogLevelType.Error);
                    //    });


                    //}
                    //catch (COMException ex)
                    //{
                    //    btnFixFile.Visibility = Visibility.Collapsed;
                    //    FileStatusInfoBar.IsOpen = true;
                    //    FileStatusInfoBar.Title = "Error";
                    //    FileStatusInfoBar.Message = "An unexpected error occured while setting Artist property. Check log page for more details under App Settings";
                    //    Logger.Log("COMEx: "+ex.Message, "ListViewMedia.ArtistSetMultiple", Logger.LogLevelType.Error);
                    //}
                    //Check for blocked files
                }
            }
           
        }
        private bool IsFileLocked(IOException exception)
        {
            int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(exception) & ((1 << 16) - 1);
            return errorCode == 32 || errorCode == 33; // 32 = Sharing Violation, 33 = Lock Violation
        }
        private void btnEditArtistMass_Click(object sender, RoutedEventArgs e)
        {
            if (App.HomeWindowInstance == null) return;
            OceanContentDialog.Show("Properties", "Save", "", "Cancel", OceanContentDialogDefault.Primary, MassEditgrd, this.XamlRoot, 800, 800, OceanContentDialogType.Elevated, App.HomeWindowInstance, "saveicon", "", "");
            OceanContentDialog.PrimaryRequested += OceanContentDialog_PrimaryRequested1;

            tbviArtist.IsSelected = true;
            lstViewEdit.ItemsSource = SongCollection;
            txtEditArtist.Text = SongCollection[0].Artist;
            txtEditAlbum.Text = SongCollection[0].AlbumName;
            foreach (var item in lstViewPlaylist.SelectedItems)
            {
                lstViewEdit.SelectedItems.Add(item);
            }
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
        private Button? Favouritebutton;
        string justmodfavpath = "";


        private void CallFavButton()
        {
            if (justmodfavpath == "")
            {
                return;
            }
            else
            {
                if (Favouritebutton == null) return;
                var rootGrid = Favouritebutton.Content as Grid;
                if (rootGrid == null) return;
                if (justmodfavpath != selectedSong.FilePath) return;
                // 2. Find the FillHeart icon by name within this specific Button
                var fillHeartIcon = rootGrid.FindName("FillHeart") as FontIcon;
                if (fillHeartIcon == null) return;

                bool currentlyChecked = fillHeartIcon.Opacity > 0;

                if (!currentlyChecked)
                {
                    ToolTipService.SetToolTip(Favouritebutton, "Remove from Favourites");

                    // Pass the specific icon we found to your animation method
                    AnimateHeart.AnimateHeartIcon(fillHeartIcon, 1.0, 1.0);
                }
                else
                {
                    ToolTipService.SetToolTip(Favouritebutton, "Add to Favourites");

                    AnimateHeart.AnimateHeartIcon(fillHeartIcon, 0.0, 0.0);
                }
            }
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
               var alreadyexisting  = favourites.FirstOrDefault(f => f.FilePath == song.FilePath);  
                if (alreadyexisting != null)
                {
                    favourites.Remove(alreadyexisting);
                    ToolTipService.SetToolTip(btn, "Add to favourites");
                    song.FavOpacity = 0;
                    song.FavString = "Add to Favourites";
                }
                else
                {
                    favourites.Add(new FavouritesModel { FilePath=song.FilePath});
                    ToolTipService.SetToolTip(btn, "Remove from favourites");
                    song.FavOpacity = 1;
                    song.FavString = "Remove from Favourites";
                }
                await SettingsHelper.SaveSettingsAsync(settings);
            
                // 3. Trigger animation
                var fillHeart = btn.FindName("FillHeart") as FontIcon;
                if(fillHeart == null) return;
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
        

        // Standard helper to find elements inside DataTemplates
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

        }

        private void lstViewPlaylist_ItemClick(object sender, ItemClickEventArgs e)
        {

        }

        private void btnFavourite_DataContextChanged(FrameworkElement sender, Microsoft.UI.Xaml.DataContextChangedEventArgs args)
        {
            //var song = args.NewValue as SongModel;
            //var btn = sender as Button;
            //var fillHeart = btn?.FindName("FillHeart") as FontIcon;

            //if (song != null && fillHeart != null)
            //{
            //    // Instant update without animation to prevent "flickering" hearts 
            //    // while scrolling fast
            //    fillHeart.Opacity = song.IsFavourite ? 1.0 : 0.0;
            //    var transform = fillHeart.RenderTransform as Microsoft.UI.Xaml.Media.ScaleTransform;
            //    if (transform != null)
            //    {
            //        transform.ScaleX = song.IsFavourite ? 1.0 : 0.0;
            //        transform.ScaleY = song.IsFavourite ? 1.0 : 0.0;
            //    }
            }

        private void btnLoop_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnLoop_Checked(object sender, RoutedEventArgs e)
        {
            QueueHandler.Loop = btnLoop.IsChecked ?? false;

        }

        private void btnLoop_Unchecked(object sender, RoutedEventArgs e)
        {
            QueueHandler.Loop = btnLoop.IsChecked ?? false;

        }
    }
    }
        #endregion




