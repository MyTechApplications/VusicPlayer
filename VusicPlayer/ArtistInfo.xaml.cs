using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.Web.Http;
using WinRT.Interop;
using HttpClient = System.Net.Http.HttpClient;
using HttpMethod = System.Net.Http.HttpMethod;
using HttpRequestMessage = System.Net.Http.HttpRequestMessage;
using TextBox = Microsoft.UI.Xaml.Controls.TextBox;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicPlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ArtistInfo : Page
    {
        public ArtistInfo()
        {
            InitializeComponent();
        }
        TimeSpan ts;
        public ObservableCollection<SongModel> FoundSongs { get; set; } = new ObservableCollection<SongModel>();
        public ObservableCollection<SongModel> Singles { get; set; } = new ObservableCollection<SongModel>();
        HashSet<string> uniqueArtists = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ObservableCollection<string> AlbumsList { get; set; } = new();
        private async void SearchFiles()
        {
            string targetArtist = txtArtistName.Text;

            FoundSongs.Clear();
            uniqueArtists.Clear();
            Singles.Clear();
            AlbumsList.Clear();
            ts = TimeSpan.Zero;

            // 🔹 Progress UI start
            ttProgress.IsOpen = true;
            prgProgress.IsIndeterminate = true;
            prgProgress.Value = 0;

            string[] searchPaths =
            {
        UserDataPaths.GetDefault().Music,
        UserDataPaths.GetDefault().Downloads,
        UserDataPaths.GetDefault().Documents,
        UserDataPaths.GetDefault().Videos
    };

            List<StorageFile> allFoundFiles = new List<StorageFile>();

            // 🔹 STEP 1: Collect files
            foreach (var path in searchPaths)
            {
                try
                {
                    StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);

                    var queryOptions = new QueryOptions(
                        CommonFileQuery.OrderByMusicProperties,
                        new[] { ".mp3", ".flac", ".m4a" });

                    queryOptions.ApplicationSearchFilter =
         $"System.Music.AlbumArtist:=\"{targetArtist}\"";

                    var query = folder.CreateFileQueryWithOptions(queryOptions);
                    var files = await query.GetFilesAsync();

                    allFoundFiles.AddRange(files);
                }
                catch
                {
                    // Ignore access denied
                }
            }

            // 🔹 STEP 2: Process files
            if (allFoundFiles.Count > 0)
            {
                prgProgress.IsIndeterminate = false;
                prgProgress.Maximum = allFoundFiles.Count;

                int processedCount = 0;

                foreach (var file in allFoundFiles)
                {
                    try
                    {
                        var tagFile = TagLib.File.Create(file.Path);
                        var tag = tagFile.Tag;

                        string artistName = !string.IsNullOrWhiteSpace(tag.FirstAlbumArtist)
                            ? tag.FirstAlbumArtist
                            : tag.FirstPerformer;
                        if (!string.IsNullOrEmpty(artistName))
                            uniqueArtists.Add(artistName);

                        ts += tagFile.Properties.Duration;

                        // 🔥 Extract year ONCE (IMPORTANT)
                        int year = (int)tag.Year;
                        if (year == 0)
                            year = File.GetCreationTime(file.Path).Year;

                        string albumName = string.IsNullOrWhiteSpace(tag.Album)
                            ? "Unknown Album"
                            : tag.Album;
                        string artists = (tag.AlbumArtists != null && tag.AlbumArtists.Length > 0)
       ? string.Join(", ", tag.AlbumArtists)
       : "Unknown Artist";
                        AlbumsList.Add(albumName);

                        var song = new SongModel
                        {
                            Title = string.IsNullOrEmpty(tag.Title) ? file.Name : tag.Title,
                            Artist = artists,
                            AlbumName = albumName,
                            SongDuration = tagFile.Properties.Duration,
                            FilePath = file.Path,
                            Year = year
                        };

                        FoundSongs.Add(song);

                        // ✅ Add to Singles if no album
                        if (albumName == "Unknown Album")
                        {
                            Singles.Add(song);
                        }
                        processedCount++;
                        prgProgress.Value = processedCount;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex.Message, "ArtistPage.Load", Logger.LogLevelType.Error);
                    }
                }
            }
            lstViewSingles.LoadMedia(Singles, this.Frame);
            // 🔹 Load songs into UI ONCE
            lstViewAllSongs.LoadMedia(FoundSongs, this.Frame);

            // 🔹 STEP 3: Group albums
            var groupedAlbums = FoundSongs
                .GroupBy(s => s.AlbumName)
                .ToList();
            var knownAlbums = groupedAlbums
    .Where(g => g.Key != "Unknown Album")
    .ToList();

            var unknownSongs = groupedAlbums
                .Where(g => g.Key == "Unknown Album")
                .SelectMany(g => g)
                .ToList();
            var albumCollection = new ObservableCollection<ArtistDiscographyAlbumsModel>();

            foreach (var album in groupedAlbums)
            {
                var songs = album.ToList();

                int countsongs = songs.Count;

                int mostCommonYear = songs
                    .Select(s => s.Year)
                    .Where(y => y > 0)
                    .GroupBy(y => y)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key)
                    .Select(g => g.Key)
                    .FirstOrDefault();

                string countsong =
                    $"{countsongs} {(countsongs == 1 ? "item" : "items")}";

                string yearstring =
                    mostCommonYear > 0 ? mostCommonYear.ToString() : "";

                BitmapImage img =
                    await LoadExistingThumbnailAsync(album.Key);

                albumCollection.Add(new ArtistDiscographyAlbumsModel
                {
                    AlbumName = album.Key,
                    AlbumCount = countsong,
                    AlbumCoverThumbnail = img,
                    AlbumYear = yearstring
                });
            }
            grdViewAlbums.ItemsSource = albumCollection;

            // 🔹 Total duration
            string formatted = ts.TotalHours >= 1
                ? ts.ToString(@"h\:mm\:ss")
                : ts.ToString(@"m\:ss");

            txtTotalDuration.Text = formatted;

            // 🔹 Song count
            int count = FoundSongs.Count;
            txtSongCount.Text =
                $"• {count} {(count == 1 ? "item" : "items")}";
            int count2 = albumCollection.Count;
            txtAlbumCount.Text =
             $"• {count2} {(count2 == 1 ? "Album" : "Albums")}";
            var sortedArtists = uniqueArtists.OrderBy(a => a);
            if (Singles.Count == 0)
            {
                txtEmptySingles.Visibility = Visibility.Visible;
            }
            else
            {
                txtEmptySingles.Visibility = Visibility.Collapsed;

            }
            if (FoundSongs.Count == 0)
            {
                txtEmptySongs.Visibility = Visibility.Visible;
            }
            else
            {
                txtEmptySongs.Visibility = Visibility.Collapsed;

            }
            if (albumCollection.Count == 0)
            {
                txtEmptyAlbums.Visibility = Visibility.Visible;
            }
            else
            {
                txtEmptyAlbums.Visibility = Visibility.Collapsed;

            }
            LoadMostPlayedSongs();

            await Task.Delay(500);
            ttProgress.IsOpen = false;
        }
        private async Task<BitmapImage> LoadExistingThumbnailAsync(string albumname)
        {
            Uri fallbackUri = new Uri("ms-appx:///Assets/defaultalbum.png");

            var currentSettings = await SettingsHelper.LoadSettingsAsync();

            var existingAlbum = currentSettings.AlbumsList?
                .FirstOrDefault(a => a.Name == albumname);

            if (existingAlbum != null && !string.IsNullOrEmpty(existingAlbum.Thumbnail))
            {
                try
                {
                    return new BitmapImage(new Uri(existingAlbum.Thumbnail));
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to load thumbnail, reverting to default: {ex.Message}",
                        "AlbumPage", Logger.LogLevelType.Error);

                    return new BitmapImage(fallbackUri);
                }
            }

            return new BitmapImage(fallbackUri);
        }
        ObservableCollection<ArtistDiscographyAlbumsModel> albumslist = new();
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is SongModel selectedSongArtist)
            {
                txtArtistName.Text = selectedSongArtist.Artist;
                imgArtist.DisplayName = txtArtistName.Text;
                SearchFiles();
                Uri fallbackUri = new Uri("ms-appx:///Assets/defaultartist.png");
                var currentSettings = await SettingsHelper.LoadSettingsAsync();
                var existingArtist = currentSettings.ArtistsList?
                    .FirstOrDefault(a => a.Name == txtArtistName.Text);
                mostplayedsongs.CollectionChanged += Mostplayedsongs_CollectionChanged;
                if (existingArtist != null && !string.IsNullOrEmpty(existingArtist.Thumbnail))
                {
                    try
                    {

                        imgArtist.ProfilePicture = new BitmapImage(new Uri(existingArtist.Thumbnail));
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Failed to load thumbnail, reverting to default: {ex.Message}", "ArtistPage", Logger.LogLevelType.Error);
                        imgArtist.ProfilePicture = new BitmapImage(fallbackUri);
                    }
                }
                else
                {

                    imgArtist.ProfilePicture = new BitmapImage(fallbackUri);
                }
            }
        }

        private void Mostplayedsongs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
        }

        private bool IsFileLocked(IOException exception)
        {
            int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(exception) & ((1 << 16) - 1);
            return errorCode == 32 || errorCode == 33;
        }

        private void btnRenameArtist_Click(object sender, RoutedEventArgs e)
        {
            foreach (SongModel item in FoundSongs)
            {
                try
                {

                    var file = TagLib.File.Create(item.FilePath);
                    file.Tag.AlbumArtists = new[] { txtRename.Text };

                    file.Save();

                }
                catch (IOException ex) when (IsFileLocked(ex))
                {

                    ifbError.Title = "Error";
                    ifbError.Severity = InfoBarSeverity.Error;
                    ifbError.Visibility = Visibility.Visible;
                    ifbError.IsOpen = true;
                    ifbError.Message = "An unexpected error occured while renaming Artist. Check log page for more details under App Settings";
                    Logger.Log(ex.Message, "ArtistPage.RenameArtist", Logger.LogLevelType.Error);

                }
                catch (COMException ex)
                {
                    ifbError.Title = "Error";
                    ifbError.Severity = InfoBarSeverity.Error;
                    ifbError.Visibility = Visibility.Visible;
                    ifbError.IsOpen = true;
                    ifbError.Message = "An unexpected error occured while renaming Artist. Check log page for more details under App Settings";
                    Logger.Log(ex.Message, "ArtistPage.RenameArtist", Logger.LogLevelType.Error);
                }
                finally
                {
                    txtArtistName.Text = txtRename.Text;
                    SearchFiles();
                }
            }
        }
        ObservableCollection<string> paths = new();
        bool shuffleenabled = false;
        bool playallrunning = false;
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
            PlayerService.PlayQueue(shuffleenabled, paths);
            playallrunning = true;
        }

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
                    foreach (var itm in FoundSongs)
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



        private void txtRename_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {

                textBox.DispatcherQueue.TryEnqueue(() =>
                {
                    textBox.SelectAll();
                });
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //Remove selected albums
        }


        private void btnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            grdViewAlbums.SelectAll();
        }

        private void btnClearSelection_Click(object sender, RoutedEventArgs e)
        {
            grdViewAlbums.SelectedItems.Clear();
        }

        private void grdViewAlbums_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (chckSelectAlbums.IsChecked == false)
            {
                if (FoundSongs.Count > 0)
                {
                    var clickedPlaylist = e.ClickedItem as ArtistDiscographyAlbumsModel;
                    var songtemp = new SongModel { AlbumName = clickedPlaylist.AlbumName };
                    this.Frame?.Navigate(typeof(Album), songtemp);
                }
            }
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            GoToAlbum(sender);

        }
        private void GoToAlbum(object sender)
        {

            if (sender is FrameworkElement clickedElement)
            {
                // 2. Extract the DataContext (your SongModel)
                if (clickedElement.DataContext is SongModel clickedItem)
                {
                    // 3. Navigate to the Album page
                    this.Frame?.Navigate(typeof(Album), clickedItem);
                }
            }
        }
        private void MenuFlyoutItem_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void MenuFlyoutItem_Click_2(object sender, RoutedEventArgs e)
        {
            GoToAlbum(sender);
            NotifierClass.RenameAlbum = true;
        }
        public bool IsInternetAvailable()
        {
            var profile = NetworkInformation.GetInternetConnectionProfile();
            return profile != null &&
                   profile.GetNetworkConnectivityLevel() ==
                   NetworkConnectivityLevel.InternetAccess;
        }
        private void MenuFlyoutItem_Click_3(object sender, RoutedEventArgs e)
        {

        }

        private void grdViewAlbums_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (grdViewAlbums.SelectedItems.Count == 0)
            {
                stkMultiOptionsAlbums.Visibility = Visibility.Collapsed;
            }
            else
            {
                stkMultiOptionsAlbums.Visibility = Visibility.Visible;

            }
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
                var alreadyexist = FoundSongs.FirstOrDefault(s => s.FilePath == file.Path);
                if (alreadyexist == null)
                {
                    var tagFile = TagLib.File.Create(file.Path);
                    string[] albumartists = tagFile.Tag.AlbumArtists;
                    List<string> artistss = albumartists.ToList();
                    var newArtist = txtArtistName.Text.Trim();

                    if (!string.IsNullOrEmpty(newArtist) &&
                        !artistss.Any(a => a.Equals(newArtist, StringComparison.OrdinalIgnoreCase)))
                    {
                        artistss.Add(newArtist);
                        tagFile.Tag.AlbumArtists = artistss.ToArray();
                        tagFile.Save();
                    }
                }
            }
            SearchFiles();
        }
        ObservableCollection<SongModel> mostplayedsongs = new();
        private async void LoadMostPlayedSongs()
        {

            mostplayedsongs.Clear();
            var currentSettings = await SettingsHelper.LoadSettingsAsync();
            var mostPlayed = currentSettings.RecentMusic;

            if (mostPlayed != null)
            {
                // Sort by PlayCount in descending order (highest first)
                // Then use .ToList() or simply iterate over the sorted collection
                var sortedSongs = mostPlayed.OrderByDescending(x => x.PlayCount);

                foreach (var item in sortedSongs)
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(item.SongPath);
                    MusicProperties properties = await file.Properties.GetMusicPropertiesAsync();
                    string title = !string.IsNullOrWhiteSpace(properties.Title) ? properties.Title : file.DisplayName;
                    string album = !string.IsNullOrWhiteSpace(properties.Album) ? properties.Album : "Unknown Album";
                    string artist = !string.IsNullOrWhiteSpace(properties.Artist) ? properties.Artist : "Unknown Artist";
                    var settings = await SettingsHelper.LoadSettingsAsync();
                    var favourites = settings.Favourites;
                    var favSet = new HashSet<FavouritesModel>(favourites);
                    bool isfav = favSet.Any(f => f.FilePath == file.Path);
                    double opac = isfav ? 1.0 : 0.0;
                    var colorbrush = new SolidColorBrush(Microsoft.UI.Colors.White);
                    var glyph = "\uEC4F";
                    if (PlaybackState.CurrentlyPlayingPath == item.SongPath)
                    {
                        colorbrush = new SolidColorBrush(Microsoft.UI.Colors.Cyan);
                        if (PlayerService.MasterPlayer!.IsPlaying)
                            glyph = "\uE769";
                        else
                        {
                            glyph = "\uE768";
                        }
                    }
                    string text = isfav ? "Remove from Favourites" : "Add to Favourites";
                    if (artist == txtArtistName.Text)
                    {
                        var SongModelt = new SongModel
                        {
                            Title = item.SongName,
                            AlbumName = album,
                            Artist = artist,
                            FilePath = item.SongPath,
                            FavOpacity = opac,
                            FavString = text,
                            SongDuration = properties.Duration,
                            IsFavourite = favSet.Any(f => f.FilePath == file.Path),
                            Glyph = glyph,
                            TitleColor = colorbrush,
                            IsMovableItem = Visibility.Collapsed,
                        };
                        mostplayedsongs.Add(SongModelt);
                    }
                }

                lstViewMasterMostPlayed.ItemsSource = mostplayedsongs;

            }
            if(mostplayedsongs.Count == 0)
            {
                lstViewMasterMostPlayed.Visibility = Visibility.Collapsed;
                txtEmptyMostPlayed.Visibility = Visibility.Visible;
            }
            else
            {
                lstViewMasterMostPlayed.Visibility = Visibility.Visible;
                txtEmptyMostPlayed.Visibility = Visibility.Collapsed;
            }
        }
        private async void btnSetArtistProfilePicture_Click(object sender, RoutedEventArgs e)
        {
            //PICK ARTIST PICTURE ON DISK
            var picker = new Windows.Storage.Pickers.FileOpenPicker();

            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);

            if (hwnd == IntPtr.Zero)
            {
                hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentActiveWindow);
            }
            picker.CommitButtonText = "Choose Profile Picture for Artist";
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".ico");

            var file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                imgArtist.ProfilePicture = new BitmapImage(new Uri(file.Path));
                var currentSettings = await SettingsHelper.LoadSettingsAsync();
                var artists = currentSettings.ArtistsList;
                var existingArtist = artists.FirstOrDefault(a => a.Name == txtArtistName.Text);

                if (existingArtist != null)
                {
                    // 1. Update the existing entry
                    existingArtist.Thumbnail = file.Path;
                }
                else
                {
                    // 2. Create a new entry if it doesn't exist
                    var newArtist = new ArtistDetails
                    {
                        Name = txtArtistName.Text,
                        Thumbnail = file.Path
                        // Add other default properties here
                    };
                    artists.Add(newArtist);
                }
                mnftRemoveImage.Visibility = Visibility.Visible;
                // Don't forget to save the changes back to storage!
                await SettingsHelper.SaveSettingsAsync(currentSettings);
            }

        }
        private Point startPoint;
        private double startX, startY;
        private void chckSelectAlbums_Checked(object sender, RoutedEventArgs e)
        {
            if (chckSelectAlbums.IsChecked == true)
            {
                stkMultiOptionsAlbums.Visibility = Visibility.Visible;
                grdViewAlbums.SelectionMode = ListViewSelectionMode.Multiple;

            }
            else
            {
                stkMultiOptionsAlbums.Visibility = Visibility.Collapsed;
                grdViewAlbums.SelectionMode = ListViewSelectionMode.None;
            }
        }

        private void chckSelectAlbums_Unchecked(object sender, RoutedEventArgs e)
        {
            if (chckSelectAlbums.IsChecked == true)
            {
                grdViewAlbums.SelectionMode = ListViewSelectionMode.Multiple;
                stkMultiOptionsAlbums.Visibility = Visibility.Visible;

            }
            else
            {
                stkMultiOptionsAlbums.Visibility = Visibility.Collapsed;
                grdViewAlbums.SelectionMode = ListViewSelectionMode.None;
            }
        }

        private void btnShowMoreResults_Click(object sender, RoutedEventArgs e)
        {
            //LEAVE
        }
        private CancellationTokenSource _loadingCts;
        private async Task AnimateStatusAsync(string baseText)
        {
            _loadingCts = new CancellationTokenSource();
            var token = _loadingCts.Token;

            int dots = 0;

            while (!token.IsCancellationRequested)
            {
                dots = (dots % 3) + 1;
                txtWaitOnline.Text = baseText + new string('.', dots);

                await Task.Delay(400);
            }
        }
        public ObservableCollection<SerperImage> Images { get; } = new();
        private static readonly HttpClient client = new HttpClient(new HttpClientHandler()
        {
            AllowAutoRedirect = true
        });
        public async Task<List<string>> FindImagesOfArtist(string query)
        {

            // SearchAPI uses 'q' for the query and 'engine=google_images'
            string apiKey = "9fjaTAxacfP9nhoiXWaqBytU";
            string encodedQuery = Uri.EscapeDataString(query);
            string url = $"https://www.searchapi.io/api/v1/search?engine=google_images&q={encodedQuery}&api_key={apiKey}";

            try
            {
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode) return new List<string>();

                var responseString = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(responseString);
                var results = new List<string>();
                if (doc.RootElement.TryGetProperty("images", out JsonElement images))
                {
                    foreach (var item in images.EnumerateArray())
                    {
                        string finalUrl = null;
                        // SearchAPI uses "original" for the high-res link
                        if (item.TryGetProperty("original", out JsonElement originalObj) &&
              originalObj.TryGetProperty("link", out JsonElement linkElement))
                        {
                            string originalUrl = linkElement.GetString();

                            if (!string.IsNullOrEmpty(originalUrl) &&
                                !originalUrl.Contains("lookaside") &&
                                !originalUrl.Contains("tiktok.com"))
                            {
                                finalUrl = originalUrl;
                            }
                        }

                        // 3. Fallback to thumbnail if original is missing or from a "gray" domain
                        if (string.IsNullOrEmpty(finalUrl) && item.TryGetProperty("thumbnail", out JsonElement thumbElement))
                        {
                            finalUrl = thumbElement.GetString();
                        }

                        if (!string.IsNullOrEmpty(finalUrl))
                        {
                            results.Add(finalUrl);
                        }

                        if (results.Count >= 5) break;
                    }
                }
                return results;
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, "ArtistPage.FindImageOnline", Logger.LogLevelType.Error);
                return new List<string>();
            }
        }
        private async void btnFindOnline_Click(object sender, RoutedEventArgs e)
        {
            ifbNoInternet.IsOpen = false;
            txtWaitOnline.Visibility = Visibility.Visible;
            string query = txtArtistToFind.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                txtWaitOnline.Text = "The search box is empty.";
                return;
            }

            if (!IsInternetAvailable())
            {
                ifbNoInternet.Title = "You're not connected to the internet";
                ifbNoInternet.Severity = InfoBarSeverity.Error;
                ifbActionButton.Visibility = Visibility.Visible;
                ifbNoInternet.IsOpen = true;
                ifbMessage.Text = "Connect to the internet to search online";
                ifbActionButton.Content = "Connect to internet in Settings";
                ifbActionButton.Click += ActionButton_Click;
                return;
            }
            try
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                client.Timeout = TimeSpan.FromSeconds(30);

                prgWaitOnline.Visibility = Visibility.Visible;

                _ = AnimateStatusAsync("Finding images");
                Images.Clear();
                var urls = await FindImagesOfArtist(query);
                _ = DispatcherQueue.TryEnqueue(() =>
                {
                    ImageGrid.ItemsSource = urls;
                });
                ImageGrid.SelectionMode = ListViewSelectionMode.Single;

                _loadingCts?.Cancel();
            }
            catch (Exception ex)
            {
                _loadingCts?.Cancel();

                ifbNoInternet.Title = "Error";
                ifbActionButton.Visibility = Visibility.Visible;
                ifbMessage.Text = "An unexpected error occured. Check for log details in Log Page.";
                ifbActionButton.Content = "See Log";
                ifbNoInternet.IsOpen = true;
                ifbNoInternet.Severity = InfoBarSeverity.Error;
                ifbActionButton.Click += ActionButton_Click1;

            }
            finally
            {
                prgWaitOnline.Visibility = Visibility.Collapsed;
                txtWaitOnline.Visibility = Visibility.Collapsed;
            }
        }

        private void ActionButton_Click1(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(LogPage));
            OceanContentDialog.HideDlg();
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:network",
                UseShellExecute = true
            });
        }

        private void btnFindArtistProfileOnline_Click(object sender, RoutedEventArgs e)
        {
            if (App.HomeWindowInstance != null)
            {
                OceanContentDialog.Show("Artist Display Picture", "Set", "", "Cancel", OceanContentDialogDefault.Primary, grdFindOnline, this.XamlRoot, 600, 760, OceanContentDialogType.Elevated, App.HomeWindowInstance, "saveicon", "", "");
                OceanContentDialog.PrimaryRequested += OceanContentDialog_PrimaryRequested;
                txtArtistToFind.Text = txtArtistName.Text;
            }
        }

        private async void OceanContentDialog_PrimaryRequested()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "VusicImages");
            if (ImageGrid.SelectedItems.Count == 0)
            {
                ifbNoInternet.Title = "Action Required";
                ifbMessage.Text = "Select an image to proceed.";
                ifbNoInternet.IsOpen = true;
                ifbActionButton.Visibility = Visibility.Collapsed;
                ifbNoInternet.Severity = InfoBarSeverity.Error;
            }
            else
            {
                string imageUrl = ImageGrid.SelectedItem as string;
                Debug.WriteLine("DD" + imageUrl);
                if (string.IsNullOrEmpty(imageUrl))
                    return;

                await DownloadImageAsync(imageUrl);

            }

        }
        public async Task<string> DownloadImageAsync(string imageUrl)
        {
            try
            {
                txtWaitOnline.Visibility = Visibility.Visible;
                txtWaitOnline.Text = "Downloading image...";

                byte[] imageBytes = await client.GetByteArrayAsync(imageUrl);

                // 📁 Save location (inside app folder)
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                    "VusicImages");

                Directory.CreateDirectory(folder);
                string extension = Path.GetExtension(new Uri(imageUrl).AbsolutePath);
                if (string.IsNullOrEmpty(extension)) extension = ".jpg";
                // 🖼 Unique filename
                string safeName = txtArtistToFind.Text.Replace(" ", "_");
                string fileName = $"{safeName}_{DateTime.Now.Ticks}{extension}";
                string filePath = Path.Combine(folder, fileName);
                Debug.WriteLine(filePath);
                await File.WriteAllBytesAsync(filePath, imageBytes);

                Filepat = filePath;
                txtWaitOnline.Text = "Image saved successfully!";
                return filePath;
            }
            catch (Exception ex)
            {
                ifbNoInternet.Title = "Error";
                ifbMessage.Text = "An unexpected error occured. Check for log details in Log Page.";
                ifbActionButton.Content = "See Log";
                ifbNoInternet.IsOpen = true;
                ifbNoInternet.Severity = InfoBarSeverity.Error;
                Logger.Log(ex.Message, "ArtistFindImageOnline", Logger.LogLevelType.Error);
                ifbActionButton.Click += ActionButton_Click1;
                ifbActionButton.Visibility = Visibility.Visible;
                return null;
            }
            finally
            {
                LoadImage(Filepat);
                txtWaitOnline.Visibility = Visibility.Collapsed;
                OceanContentDialog.HideDlg();
            }
        }
        string Filepat;
        public async void LoadImage(string filePath)
        {
            try
            {
                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
                using (var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    var bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(stream);
                    imgArtist.ProfilePicture = bitmap;
                }
                var currentSettings = await SettingsHelper.LoadSettingsAsync();
                var artists = currentSettings.ArtistsList;
                var existingArtist = artists.FirstOrDefault(a => a.Name == txtArtistName.Text);

                if (existingArtist != null)
                {
                    existingArtist.Thumbnail = file.Path;
                }
                else
                {
                    var newArtist = new ArtistDetails
                    {
                        Name = txtArtistName.Text,
                        Thumbnail = file.Path
                        // Add other default properties here
                    };
                    artists.Add(newArtist);
                }

                // Don't forget to save the changes back to storage!
                await SettingsHelper.SaveSettingsAsync(currentSettings);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, "ArtistImageLoad", Logger.LogLevelType.Error);
            }
        }
        private void MenuFlyoutItem_Click_4(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement clickedElement)
            {
                if (clickedElement.DataContext is var clickedItem)
                {
                    ImageGrid.SelectedItem = clickedItem;
                }
            }
        }

        private void MenuFlyoutItem_Click_5(object sender, RoutedEventArgs e)
        {

        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            SearchFiles();
        }

        private void imgArtist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //if (App.HomeWindowInstance == null) return;
            //var currentImage = imgArtist.ProfilePicture as BitmapImage;
            //originalImage = currentImage;
            //imgLargeViewer.Source = imgArtist.ProfilePicture;
            //OceanContentDialog.Show("View Artist Display Picture", "Save", "", "Cancel", OceanContentDialogDefault.Primary, grdImageLarge, this.XamlRoot, 600, 760, OceanContentDialogType.Elevated, App.HomeWindowInstance, "saveicon", "", "");
            //OceanContentDialog.PrimaryRequested += OceanContentDialog_PrimaryRequested1;
        }
        public async Task<WriteableBitmap> CropPersonPictureAsync(double left, double top, double diameter)
        {
            // 1. Capture the imgArtist control directly to avoid the Null URI crash
            RenderTargetBitmap renderTarget = new RenderTargetBitmap();
            await renderTarget.RenderAsync(imgArtist);

            // Get the pixels from the rendered control
            var pixelBuffer = await renderTarget.GetPixelsAsync();
            byte[] pixels = pixelBuffer.ToArray();

            // 2. Create the destination WriteableBitmap (the cropped result)
            // We use 'diameter' for both width and height to keep it square
            int cropSize = (int)diameter;
            WriteableBitmap croppedResult = new WriteableBitmap(cropSize, cropSize);

            // 3. Perform the Crop
            // We map the pixels from the source (renderTarget) to the destination (croppedResult)
            int sourceWidth = renderTarget.PixelWidth;
            int startX = (int)left;
            int startY = (int)top;

            using (Stream stream = croppedResult.PixelBuffer.AsStream())
            {
                for (int y = 0; y < cropSize; y++)
                {
                    for (int x = 0; x < cropSize; x++)
                    {
                        // Calculate the position in the original source array
                        int sourceX = startX + x;
                        int sourceY = startY + y;

                        // Ensure we stay within bounds of the original image
                        if (sourceX >= 0 && sourceX < sourceWidth && sourceY >= 0 && sourceY < renderTarget.PixelHeight)
                        {
                            int sourceIndex = (sourceY * sourceWidth + sourceX) * 4; // 4 bytes per pixel (BGRA8)

                            // Write the 4 bytes (B, G, R, A) to the stream
                            stream.Write(pixels, sourceIndex, 4);
                        }
                    }
                }
            }

            return croppedResult;
        }        // Needed for LockBuffer pixel access
        [System.Runtime.InteropServices.Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
        [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
        interface IMemoryBufferByteAccess
        {
            void GetBuffer(out byte[] buffer, out uint capacity);
        }
        private async void OceanContentDialog_PrimaryRequested1()
        {
            double left = Canvas.GetLeft(cropCircle);
            double top = Canvas.GetTop(cropCircle);
            double diameter = cropCircle.Width;

            var croppedImage = await CropPersonPictureAsync(
           left, top, diameter);

            imgArtist.ProfilePicture = croppedImage;
        }
        private bool isDragging = false;
        private Windows.Foundation.Point lastPosition;
        private BitmapImage originalImage;
        private TaskCompletionSource<BitmapImage> tcs;
        private void cropCircle_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            isDragging = true;
            lastPosition = e.GetCurrentPoint(canvasOverlay).Position;
            cropCircle.CapturePointer(e.Pointer);
        }

        private void cropCircle_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!isDragging) return;

            var position = e.GetCurrentPoint(canvasOverlay).Position;
            double dx = position.X - lastPosition.X;
            double dy = position.Y - lastPosition.Y;

            Canvas.SetLeft(cropCircle, Canvas.GetLeft(cropCircle) + dx);
            Canvas.SetTop(cropCircle, Canvas.GetTop(cropCircle) + dy);

            lastPosition = position;
        }

        private async void mnftRemoveImage_Click(object sender, RoutedEventArgs e)
        {
            imgArtist.ProfilePicture = null;
            var currentSettings = await SettingsHelper.LoadSettingsAsync();
            var artists = currentSettings.ArtistsList;
            var existingArtist = artists.FirstOrDefault(a => a.Name == txtArtistName.Text);

            if (existingArtist != null)
            {
                // 1. Update the existing entry
                existingArtist.Thumbnail = "";
            }
            else
            {
                // 2. Create a new entry if it doesn't exist
                var newArtist = new ArtistDetails
                {
                    Name = txtArtistName.Text,
                    Thumbnail = ""
                    // Add other default properties here
                };
                artists.Add(newArtist);
            }

            // Don't forget to save the changes back to storage!
            await SettingsHelper.SaveSettingsAsync(currentSettings);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            txtRename.Text = txtArtistName.Text;
        }

        private void ifbError_CloseButtonClick(InfoBar sender, object args)
        {
            ifbError.Visibility = Visibility.Collapsed;
        }

        private void mnftChangeAlbumCover_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftFindAlbumCoverOnline_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftRemoveAlbum_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftDeleteAlbum_Click(object sender, RoutedEventArgs e)
        {

        }

        private void cropCircle_PointerReleased(object sender, PointerRoutedEventArgs e)
        {

            isDragging = false;
            cropCircle.ReleasePointerCapture(e.Pointer);
        }
    }
}
