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
    private async void btnRenameAlbum_Click(object sender, RoutedEventArgs e)
    {
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
    ObservableCollection<string> paths = new();
    HashSet<string> uniqueArtists = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public ObservableCollection<SongModel> FoundSongs { get; set; } = new ObservableCollection<SongModel>();
    private void btnPlayAll_Click(object sender, RoutedEventArgs e)
    {
        if (paths.Count != 0)
        {
            paths.Clear();
        }
        paths = new();
        foreach (var itm in FoundSongs)
        {
            if (itm.FilePath != null)
            {
                paths.Add(itm.FilePath);

            }
        }
        if (App.MainWindowInstance is HomeWindow homeWindow)
        {
            homeWindow.LoadFileFromPath(paths);
        }

    }
    TimeSpan ts;
    public void UpdateCurrentListhere(string currentplaying)
    {
        if (currentplaying == null) return;

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

        // Optional: Hide progress bar after a short delay
        await Task.Delay(500);
        ttProgress.IsOpen = false;
        UpdateCurrentListhere(PlaybackState.CurrentlyPlayingPath);
    }

    private void btnShuffle_Click(object sender, RoutedEventArgs e)
    {

    }

    private void btnAddSongs_Click(object sender, RoutedEventArgs e)
    {

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
                homeWindow.LoadFileFromPath(paths);
            }
        }
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

    private void mnftSongDetails_Click(object sender, RoutedEventArgs e)
    {

    }

    private void lstViewPlaylist_ItemClick(object sender, ItemClickEventArgs e)
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
}

