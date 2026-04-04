using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicPlayer;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class Album : Page, IUpdateableMusicPage
{
    public Album()
    {
        InitializeComponent();
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
    private async void btnRenameAlbum_Click(object sender, RoutedEventArgs e)
    {
        if(txtRename.Text == "")
        {
            txtRename.Text = txtAlbumName.Text;
            return;
        }
        foreach (SongModel item in lstViewPlaylist.Items)
        {
            try
            {
                if (IsFileReady(item.FilePath))
                {
                    var file = TagLib.File.Create(item.FilePath);
                    file.Tag.Album = txtRename.Text;
                    file.Save();
                    item.AlbumName = txtRename.Text;
                    var settings = await SettingsHelper.LoadSettingsAsync();
                    var albumcurrent = settings.AlbumsList.FirstOrDefault(p => p.Name == txtAlbumName.Text);
                    if (albumcurrent != null)
                    {
                        albumcurrent.Name = txtRename.Text;
                        await SettingsHelper.SaveSettingsAsync(settings);
                    }
                    txtAlbumName.Text = txtRename.Text;
                    currentAlbumname = txtRename.Text;
                }
                else
                {
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
                Logger.Log(ex.Message, "ListViewMedia.AlbumSetMultiple", Logger.LogLevelType.Error);
            }
            /*    if (string.IsNullOrWhiteSpace(txtRename.Text))
                {
                    txtRename.Text = txtAlbumName.Text;
                }

                txtAlbumName.Text = txtRename.Text;

                List<string> failedFiles = new List<string>();
                int successCount = 0;

                foreach (var item in FoundSongs)
                {
                    try
                    {
                        StorageFile file = await StorageFile.GetFileFromPathAsync(item.FilePath);
                        var propertiesToSave = new Dictionary<string, object>
                {
                    { "System.Music.AlbumTitle", txtAlbumName.Text }
                };

                        await file.Properties.SavePropertiesAsync(propertiesToSave);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        // Add the file name or path to our failure list
                        failedFiles.Add(item.Title ?? item.FilePath);
                        Logger.Log($"Skipped {item.FilePath}: {ex.Message}");
                    }
                }

                // 2. Show the result to the user
                if (failedFiles.Count > 0)
                {
                    ShowCompletionNotification(successCount, failedFiles);
                }   SearchFiles();
           */
        }
        flyoutRename.Hide();
     
    }
    private void ShowCompletionNotification(int successCount, List<string> failedFiles)
    {
        UpdateResultTip.Subtitle = $"Updated {successCount} files.";

        if (failedFiles.Count > 0)
        {
            // Create a summary of why it failed (Permissions/File in use)
            string fileList = string.Join(", ", failedFiles.Take(3)); // Show first 3
            if (failedFiles.Count > 3) fileList += "...";

            UpdateResultTip.Content = $"Some files could not be updated because they were read-only or in use by another app: \n{fileList}";
            UpdateResultTip.IsOpen = true;
        }
    }

    string currentAlbumname = "";
    SongModel selectedSongs = new();
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is SongModel selectedSongArtist)
        {
            selectedSongs = selectedSongArtist;
            txtAlbumName.Text = selectedSongArtist.AlbumName;
            currentAlbumname = selectedSongArtist.AlbumName;

            // 1. Load existing thumbnail from settings
            await LoadExistingThumbnailAsync();

            // 2. Search for the songs in this album
            SearchFiles();

            // 3. Update the UI count label dynamically
            FoundSongs.CollectionChanged += (s, args) =>
            {
                int count = FoundSongs.Count;
                txtSongCount.Text = $"• {count} {(count == 1 ? "item" : "items")}";
                if (FoundSongs.Count == 0)
                {
                    txtSongCount.Text = "• No songs found";
                    txtNoSongs.Visibility = Visibility.Visible;
                    txtAlbumHeader.Visibility = Visibility.Collapsed;
                }
                else
                {
                    txtNoSongs.Visibility = Visibility.Collapsed;
                    txtAlbumHeader.Visibility = Visibility.Visible;
                }
            };
            if (NotifierClass.RenameAlbum == true)
            {
                flyoutRename.ShowAt(btnRename);
                NotifierClass.RenameAlbum = false;
            }
        }
        if (FoundSongs.Count == 0)
        {
            txtSongCount.Text = "• No songs found";
            txtNoSongs.Visibility = Visibility.Visible;
            txtAlbumHeader.Visibility = Visibility.Collapsed;
        }
    }
    ObservableCollection<string> AlbumsList { get; set; } = new();

    private void LoadArtists()
    {
        foreach (var song in FoundSongs)
        {
            string artistName = !string.IsNullOrWhiteSpace(song.Artist) ? song.Artist : "Unknown Artist";
            uniqueArtists.Add(artistName);
        }
        foreach (var name in uniqueArtists)
        {
            var artistdisplays = new ArtistShow { ArtistName = name };
        }
    }
    private async Task LoadExistingThumbnailAsync()
    {
        // Define the fallback URI
        Uri fallbackUri = new Uri("ms-appx:///Assets/defaultalbum.png");

        var currentSettings = await SettingsHelper.LoadSettingsAsync();

        // Look for a saved entry matching the current album name
        var existingAlbum = currentSettings.AlbumsList?
            .FirstOrDefault(a => a.Name == currentAlbumname);

        if (existingAlbum != null && !string.IsNullOrEmpty(existingAlbum.Thumbnail))
        {
            try
            {
                // Attempt to load the user's custom thumbnail
                imgAlbumCover.Source = new BitmapImage(new Uri(existingAlbum.Thumbnail));
                tempalbumcoverstring = existingAlbum.Thumbnail;
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to load thumbnail, reverting to default: {ex.Message}", "AlbumPage", Logger.LogLevelType.Error);
                imgAlbumCover.Source = new BitmapImage(fallbackUri);
            }
        }
        else
        {
            // No entry found or no thumbnail string exists; use default
            imgAlbumCover.Source = new BitmapImage(fallbackUri);
        }
    }
    string? tempalbumcoverstring;
    ObservableCollection<string> paths = new();
    HashSet<string> uniqueArtists = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public ObservableCollection<SongModel> FoundSongs { get; set; } = new ObservableCollection<SongModel>();
    private void btnPlayAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in FoundSongs)
        {
            item.IsCompleted = false;
        }
        PlayerService.CreatePlayer();
        QueueHandler.PlayMedia(FoundSongs, btnShuffle.IsChecked ?? false, false);

    }
    TimeSpan ts;
    public void UpdateCurrentListhere(string currentplaying)
    {
        if (currentplaying == null) return;

        this.DispatcherQueue.TryEnqueue(() =>
        {
            // Get the system's standard text color for the current theme
            var normalBrush = (Application.Current.Resources["TextFillColorPrimaryBrush"] as SolidColorBrush);
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

    private async void SearchFiles()
    {
        string targetAlbum = txtAlbumName.Text;
        FoundSongs.Clear();
        uniqueArtists.Clear();

        // 1. Show and Reset Progress Bar
        ttProgress.IsOpen = true;
        prgProgress.IsIndeterminate = true; // Slide back and forth while we gather files
        prgProgress.Value = 0;

        string[] searchPaths = {
        UserDataPaths.GetDefault().Music,
        UserDataPaths.GetDefault().Downloads,
        UserDataPaths.GetDefault().Documents,
        UserDataPaths.GetDefault().Videos
    };

        // We'll store files in a list first to know the total count
        List<StorageFile> allFoundFiles = new List<StorageFile>();

        foreach (var path in searchPaths)
        {
            try
            {
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);
                var queryOptions = new QueryOptions(CommonFileQuery.OrderByMusicProperties, new[] { ".mp3", ".flac", ".m4a" });
                queryOptions.ApplicationSearchFilter = $"System.Music.AlbumTitle:=\"{targetAlbum}\"";

                var query = folder.CreateFileQueryWithOptions(queryOptions);
                var files = await query.GetFilesAsync();
                allFoundFiles.AddRange(files);
            }
            catch { /* Handle access denied */ }
        }

        // 2. Now we know the total, switch to "Filling" mode
        if (allFoundFiles.Count > 0)
        {
            prgProgress.IsIndeterminate = false;
            prgProgress.Maximum = allFoundFiles.Count;

            int processedCount = 0;

            foreach (var file in allFoundFiles)
            {
                var tagFile = TagLib.File.Create(file.Path);
                var tag = tagFile.Tag;

                var props = await file.Properties.GetMusicPropertiesAsync();
                ts += props.Duration;
                string artistName = !string.IsNullOrWhiteSpace(props.AlbumArtist)
                    ? props.AlbumArtist : props.Artist;

                if (!string.IsNullOrEmpty(artistName))
                    uniqueArtists.Add(artistName);

                FoundSongs.Add(new SongModel
                {
                    Title = string.IsNullOrEmpty(props.Title) ? file.Name : props.Title,
                    Artist = props.Artist,
                    AlbumName = props.Album,
                    SongDuration = props.Duration,
                    FilePath = file.Path
                });
                string albumName = string.IsNullOrWhiteSpace(tag.Album)
                     ? "Unknown Album"
                     : tag.Album;
                string artists = (tag.AlbumArtists != null && tag.AlbumArtists.Length > 0)
? string.Join(", ", tag.AlbumArtists)
: "Unknown Artist";
                AlbumsList.Add(albumName);
                // Update Progress
                processedCount++;
                prgProgress.Value = processedCount;
            }
        }
        lstViewPlaylist.ItemsSource = FoundSongs;
        string formatted = ts.TotalHours >= 1
     ? ts.ToString(@"h\:mm\:ss")
     : ts.ToString(@"m\:ss");
        txtTotalDuration.Text = formatted;
        // 3. Finalize UI
        var sortedArtists = uniqueArtists.OrderBy(a => a);
        txtArtistsInvolved.Text = "• " + string.Join(", ", sortedArtists);
        ArtistShows.Clear();
        foreach (var artist in uniqueArtists)
        {

            Uri fallbackUri = new Uri("ms-appx:///Assets/defaultartist.png");

            var currentSettings = await SettingsHelper.LoadSettingsAsync();
            var existingAlbum = currentSettings.ArtistsList?
                .FirstOrDefault(a => a.Name == artist);
            string thumbnail = "ms-appx:///Assets/defaultartist.png";

            if (existingAlbum != null)
            {
                thumbnail = existingAlbum.Thumbnail;
            }


            var ArtistSe = new ArtistShow { ArtistName = artist, ArtistThumbnailImage = new BitmapImage(new Uri(thumbnail)), ArtistThumbnail = thumbnail };
            ArtistShows.Add(ArtistSe);
        }


        grdViewArtists.ItemsSource = ArtistShows;
        if (ArtistShows.Count == 0)
        {
            grdViewArtists.Visibility = Visibility.Collapsed;
            txtArtistsHeader.Visibility = Visibility.Collapsed;
        }
        else
        {
            grdViewArtists.Visibility = Visibility.Visible;
            txtArtistsHeader.Visibility = Visibility.Visible;

        }
        // Optional: Hide progress bar after a short delay
        await Task.Delay(500);
        ttProgress.IsOpen = false;
        UpdateCurrentListhere(PlaybackState.CurrentlyPlayingPath);
    }
    private ObservableCollection<ArtistShow> ArtistShows { get; set; } = new ObservableCollection<ArtistShow>();

    private void btnShuffle_Click(object sender, RoutedEventArgs e)
    {

    }
    ObservableCollection<SongModel> original = new();

    private void Shuffle()
    {

        if (btnShuffle.IsChecked == true)
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
    }
    private async void btnAddSongs_Click(object sender, RoutedEventArgs e)
    {
        if (App.HomeWindowInstance == null)
        {
            FileStatusInfoBar.IsOpen = true;
            FileStatusInfoBar.Title = "Error";
            FileStatusInfoBar.Message = "An unexpected error occured. Check log details in App Settings Page.";
            FileStatusInfoBar.Severity = InfoBarSeverity.Error;
            Logger.Log("Error code 0x0012oc. Refer the github page for more details.", "PlaylistCreation", Logger.LogLevelType.Error);
            return;
        }
        var files = await PickFiles.PickMultipleAudioFilesAsync(App.HomeWindowInstance, "Add items");
        if (files == null) return;

        foreach (var file in files)
        {
            if (!FoundSongs.Any(s => s.FilePath == file.Path))
            {
                var musicProps = await file.Properties.GetMusicPropertiesAsync();
                if (IsFileReady(file.Path))
                {
                    var file2 = TagLib.File.Create(file.Path);
                    file2.Tag.Album = txtAlbumName.Text;
                    file2.Save();
                }
                string duration = FormatTimeSpanDuration.Format(musicProps.Duration);
                FoundSongs.Add(new SongModel
                {
                    Title = musicProps.Title,
                    SongDuration = musicProps.Duration,
                    FilePath = file.Path,
                    AlbumName = txtAlbumName.Text,
                    Artist = musicProps.Artist
                });
            }
        }
    }
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
                QueueService.PlayMedia(paths);
            }
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



    private void mnftContext_Opened(object sender, object e)
    {

    }

    private void lstViewPlaylist_ItemClick(object sender, ItemClickEventArgs e)
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
    SongModel selectedSong = new();
    private void txtAlbumHyp_Click(object sender, RoutedEventArgs e)
    {

        GoToAlbum(sender);
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

    private void removesongfromplaylistcreation_Click(object sender, RoutedEventArgs e)
    {

    }



    private void lstViewPlaylist_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {

    }

    private void mnftEditInfo_Click(object sender, RoutedEventArgs e)
    {

    }

    private void txtTitle_Click(object sender, RoutedEventArgs e)
    {
        var menuFlyoutItem = sender as HyperlinkButton;

        // 2. The 'DataContext' of the menu item IS the SongModel for that row
        var selectedsong = menuFlyoutItem?.DataContext as SongModel;
        if (selectedsong != null)
        {
            selectedSong = selectedsong;
            PlaySelection();
        }
    }

    private async void btnSetAlbumCover_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();

        // Get the handle from the specific instance we know is alive
        IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);

        if (hwnd == IntPtr.Zero)
        {
            // If for some reason the main window is gone, try the current active one
            hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentActiveWindow);
        }
        picker.CommitButtonText = "Choose Album Cover";
        // 2. Initialize the picker with the handle
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".ico");

        var file = await picker.PickSingleFileAsync();

        if (file != null)
        {
            imgAlbumCover.Source = new BitmapImage(new Uri(file.Path));
            var currentSettings = await SettingsHelper.LoadSettingsAsync();
            var albums = currentSettings.AlbumsList;
            var existingAlbum = albums.FirstOrDefault(a => a.Name == currentAlbumname);

            if (existingAlbum != null)
            {
                // 1. Update the existing entry
                existingAlbum.Thumbnail = file.Path;
            }
            else
            {
                // 2. Create a new entry if it doesn't exist
                var newAlbum = new AlbumDetails
                {
                    Name = currentAlbumname,
                    Thumbnail = file.Path
                    // Add other default properties here
                };
                albums.Add(newAlbum);
            }

            // Don't forget to save the changes back to storage!
            await SettingsHelper.SaveSettingsAsync(currentSettings);
        }

    }
    private async void RefreshStuff()
    {
        ArtistShows.Clear();
        ts = TimeSpan.Zero;
        txtAlbumName.Text = selectedSongs.AlbumName;
        currentAlbumname = selectedSongs.AlbumName;

        // 1. Load existing thumbnail from settings
        await LoadExistingThumbnailAsync();

        // 2. Search for the songs in this album
        SearchFiles();

        // 3. Update the UI count label dynamically
        FoundSongs.CollectionChanged += (s, args) =>
        {
            int count = FoundSongs.Count;
            txtSongCount.Text = $"• {count} {(count == 1 ? "item" : "items")}";
            if (FoundSongs.Count == 0)
            {
                txtSongCount.Text = "• No songs found";
                txtNoSongs.Visibility = Visibility.Visible;
                txtAlbumHeader.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtNoSongs.Visibility = Visibility.Collapsed;
                txtAlbumHeader.Visibility = Visibility.Visible;
            }


            if (FoundSongs.Count == 0)
            {
                txtSongCount.Text = "• No songs found";
                txtNoSongs.Visibility = Visibility.Visible;
                txtAlbumHeader.Visibility = Visibility.Collapsed;
            }
        };
    }
    private async void btnRefresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshStuff();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        txtRename.Text = txtAlbumName.Text;
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

    private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
    {
        var hypbtn = sender as HyperlinkButton;
        var selectedartist = hypbtn?.DataContext as ArtistShow;
        if (selectedartist != null)
        {
            var songmodel = new SongModel { Artist = selectedartist.ArtistName };
            this.Frame?.Navigate(typeof(ArtistInfo), songmodel);
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

    private void imgAlbumCover_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(tempalbumcoverstring))
        {
            return;
        }
        TempImagePath.Path = tempalbumcoverstring;
        EnlargeImage enlargeImage = new EnlargeImage();
        enlargeImage.Activate();
    }

    private void ppArtist_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is PersonPicture personPicture && personPicture.DataContext is ArtistShow artistShow)
        {
            TempImagePath.Path = artistShow.ArtistThumbnail;
            EnlargeImage enlargeImage = new EnlargeImage();
            enlargeImage.Activate();
        }

    }

    private void btnShuffle_Checked(object sender, RoutedEventArgs e)
    {
        Shuffle();
    }

    private void btnShuffle_Unchecked(object sender, RoutedEventArgs e)
    {
        Shuffle();
    }
}

