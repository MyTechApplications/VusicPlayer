using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicPlayer;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class Album : Page,  IUpdateableMusicPage
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

    }

    private void btnRenameAlbum_Click(object sender, RoutedEventArgs e)
    {

    }
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is SongModel selectedSongArtist)
        {
            txtAlbumName.Text = selectedSongArtist.AlbumName;
            SearchFiles();
            FoundSongs.CollectionChanged += (s, e) =>
            {
                int count = FoundSongs.Count;
                txtSongCount.Text = "• " + $"{count} {(count == 1 ? "item" : "items")}";
            };
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
        txtArtistsInvolved.Text = "• "+string.Join(", ", sortedArtists);

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
}
