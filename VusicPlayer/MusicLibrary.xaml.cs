using ABI.Microsoft.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playlists;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using WinRT.Interop;
using FrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using RoutedEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;
using Window = Microsoft.UI.Xaml.Window;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicPlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MusicLibrary : Page
    {
        public MusicLibrary()
        {
            InitializeComponent();
            GrdViewPlaylists.ItemsSource = MyItems;
            CallValues();
            MyItems.CollectionChanged += MyItems_CollectionChanged;
            RecentMusicItems.CollectionChanged += RecentMusicItems_CollectionChanged;
            loadedSongs.CollectionChanged += LoadedSongs_CollectionChanged;
        }

        private void LoadedSongs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            txtAddedSongs.Text = "Added Songs: " + $"{loadedSongs.Count} {(loadedSongs.Count == 1 ? "item" : "items")}";
        }

        private async void RecentMusicItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove ||
        e.Action == NotifyCollectionChangedAction.Add ||
        e.Action == NotifyCollectionChangedAction.Move)
            {
                var currentSettings = await SettingsHelper.LoadSettingsAsync();
                currentSettings.RecentMusic = RecentMusicItems;
                await SettingsHelper.SaveSettingsAsync(currentSettings);

            }
        }

        private async void MyItems_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove ||
        e.Action == NotifyCollectionChangedAction.Add ||
        e.Action == NotifyCollectionChangedAction.Move)
            {
                var currentSettings = await SettingsHelper.LoadSettingsAsync();
                currentSettings.SavedPlaylists = MyItems;
                await SettingsHelper.SaveSettingsAsync(currentSettings);

            }
        }

        private async void CallValues()
        {
            MyItems.Clear();

            var settings = await SettingsHelper.LoadSettingsAsync();
            var unfinishedItems = settings.SavedPlaylists;
            var recentmusicitems = settings.RecentMusic;
            foreach (var item in unfinishedItems)
            {
                if (item.Thumbnail == null && item.SongsPaths?.Count > 0)
                {
                    item.Thumbnail = new Uri(item.SongsPaths[0]).AbsoluteUri;
                }
                MyItems.Add(item); // This sends a notification
            }
            foreach (var item in recentmusicitems)
            {
                item.Thumbnail = await GetFileThumbnailAsync(item.SongPath);
                RecentMusicItems.Add(item);
            }
            grdViewRecentMusic.ItemsSource = RecentMusicItems;
            if (GrdViewPlaylists.Items.Count == 0)
            {
                btnDeletePlaylists.Visibility = Visibility.Collapsed;
                chckmultiple.IsChecked = false;
                txtEmptyPlaylists.Visibility = Visibility.Visible;
                GrdViewPlaylists.Visibility = Visibility.Collapsed;
                chckmultiple.Visibility = Visibility.Collapsed;
            }
            if (grdViewRecentMusic.Items.Count == 0) // Check the bound collection count instead
            {
                btnDeleteRecents.Visibility = Visibility.Collapsed;
                chckmultiplerecent.IsChecked = false;
                txtEmptyRecents.Visibility = Visibility.Visible;
                grdViewRecentMusic.Visibility = Visibility.Collapsed;
                chckmultiplerecent.Visibility = Visibility.Collapsed;
            }
        }
        public ObservableCollection<PlaylistProperties> MyItems { get; set; } = new();
        public ObservableCollection<RecentMusic> RecentMusicItems { get; set; } = new();
        // Change your model property to this:
        // public ImageSource Thumbnail { get; set; }

        public async Task<BitmapImage> GetFileThumbnailAsync(string path)
        {
            // Define your fallback asset
            Uri fallbackUri = new Uri("ms-appx:///Assets/appicon.png");

            try
            {
                if (string.IsNullOrEmpty(path))
                    return new BitmapImage(fallbackUri);
                if (!File.Exists(path)) return new BitmapImage(fallbackUri); ;
                StorageFile file = await StorageFile.GetFileFromPathAsync(path);

                // Get thumbnail from the file's metadata
                using var thumbnail = await file.GetScaledImageAsThumbnailAsync(
                    ThumbnailMode.MusicView, // Better for audio files
                    320,
                    ThumbnailOptions.UseCurrentScale);

                if (thumbnail != null)
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    // This connects the stream to the UI object
                    await bitmapImage.SetSourceAsync(thumbnail);
                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Thumbnail extraction failed: {ex.Message}", "MusicLibrary", Logger.LogLevelType.Error);
            }

            // If everything fails, return the app icon
            return new BitmapImage(fallbackUri);
        }
        private async void btnNewPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (App.HomeWindowInstance == null) return;
            OceanContentDialog.Show("Create New Playlist", "Create", "", "Cancel", OceanContentDialogDefault.Primary, contentsNewPlaylist, this.XamlRoot, 600, 760, OceanContentDialogType.Elevated, App.HomeWindowInstance);
            OceanContentDialog.PrimaryRequested += Dlg_PrimaryRequested;

            if (lstViewPlaylistAddedSongs.Items.Count == 0)
            {
                txtNullAddedSongs.Visibility = Visibility.Visible;
            }
        }
        OceanDialog dlg2;
        OceanPopup oceanPopup;
        private async void Dlg_PrimaryRequested()
        {
            dlg2.Close();
            HomeWindow.ShowWindow();
            oceanPopup.Hide();
            var currentSettings = await SettingsHelper.LoadSettingsAsync();
            string baseName = txtPlaylistName.Text.Trim();
            if (string.IsNullOrEmpty(baseName)) baseName = "New Playlist";

            string finalName = baseName;
            int counter = 1;

            // 2. Check for duplicates in your SavedPlaylists collection
            // Use LINQ's Any() to check if a playlist with the same name exists
            while (currentSettings.SavedPlaylists.Any(p => p.PlaylistName.Equals(finalName, StringComparison.OrdinalIgnoreCase)))
            {
                finalName = $"{baseName} ({counter++})";
            }
            string baseDirectory = AppContext.BaseDirectory;
            string defaultPath = Path.Combine(baseDirectory, "Assets", "playlistdefaultdark.png");
            if (!isdarkmode)
            {

                defaultPath = Path.Combine(baseDirectory, "Assets", "playlistdefaultlight.png");
            }

            if (imgPlaylistCov.Source is BitmapImage bitmap && bitmap.UriSource != null)
            {
                defaultPath = bitmap.UriSource.ToString();
            }

            // 3. Create the new playlist object
            var newPlaylist = new PlaylistProperties
            {
                PlaylistName = finalName,

                PlaylistCount = $"{loadedSongs.Count} {(loadedSongs.Count == 1 ? "item" : "items")}",
                PlaylistNowPlaying = "",
                PlaylistGenre = txtGenre.Text,
                SongsPaths = loadedSongs.Select(s => s.SongPath).ToList(),
                Thumbnail = defaultPath,
                DateCreation = DateTime.Now.Date,
            };

            // 4. Add to the collection and save
            currentSettings.SavedPlaylists.Add(newPlaylist);
            MyItems.Add(newPlaylist);
            if (GrdViewPlaylists.Items.Count == 0)
            {
                chckmultiple.Visibility = Visibility.Collapsed;
                txtEmptyPlaylists.Visibility = Visibility.Visible;
                GrdViewPlaylists.Visibility = Visibility.Collapsed;
            }
            else
            {
                chckmultiple.Visibility = Visibility.Visible;
                txtEmptyPlaylists.Visibility = Visibility.Collapsed;
                GrdViewPlaylists.Visibility = Visibility.Visible;
            }
            await SettingsHelper.SaveSettingsAsync(currentSettings);
        }

        private async void btnOpenMusic_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();

            // Get the handle from the specific instance we know is alive
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);

            if (hwnd == IntPtr.Zero)
            {
                // If for some reason the main window is gone, try the current active one
                hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentActiveWindow);
            }

            // 2. Initialize the picker with the handle
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.FileTypeFilter.Add(".mp3");
            picker.FileTypeFilter.Add(".wav");

            var files = await picker.PickSingleFileAsync();

            if (files != null)
            {

                ObservableCollection<string> str = new();
                str.Add(files.Path);
                if (App.MainWindowInstance is HomeWindow homeWindow)
                {
                    homeWindow.LoadFileFromPath(str);
                }
            }

        }
        PlaylistProperties sngtemp = new PlaylistProperties();
        private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PlaylistProperties song)
            {
                sngtemp = song;
            }
            dlgConfirmDeletePlaylist.Content = "Are you sure you want to delete the playlist '" + sngtemp.PlaylistName + "'?";
            await dlgConfirmDeletePlaylist.ShowAsync();
        }

        private void txtPlaylistName_GotFocus(object sender, RoutedEventArgs e)
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



        private void btnActualReset_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            txtPlaylistName.Text = "";

            txtGenre.Text = "";
            loadedSongs.Clear();
            imgPlaylistCov.Source = null;
            CoverOptions.Visibility = Visibility.Collapsed;
            btnAddPlaylistCover.IsEnabled = true;
            lstViewPlaylistAddedSongs.ItemsSource = loadedSongs;
        }
        private async void removesongfromplaylistcreation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is NewPlaylistSongProperty song)
            {
                // This single line will now update the UI automatically
                loadedSongs.Remove(song);
            }
            if (lstViewPlaylistAddedSongs.Items.Count == 0)
            {
                txtNullAddedSongs.Visibility = Visibility.Visible;
            }
        }
        private ObservableCollection<NewPlaylistSongProperty> loadedSongs = new ObservableCollection<NewPlaylistSongProperty>();
        BitmapImage? img;
        private async void btnAddSongs_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            btnAddPlaylistCover.IsEnabled = false;
            btnAddSongs.IsEnabled = false;
            var hwnd = WindowNative.GetWindowHandle(App.MainWindowInstance);
            InitializeWithWindow.Initialize(picker, hwnd);

            picker.FileTypeFilter.Add(".mp3");
            picker.FileTypeFilter.Add(".wav");
            picker.FileTypeFilter.Add(".m4a");
            picker.FileTypeFilter.Add(".ogg");

            var files = await picker.PickMultipleFilesAsync();

            if (files == null) return;

            foreach (var file in files)
            {
                if (!loadedSongs.Any(s => s.SongPath == file.Path))
                {
                    var musicProps = await file.Properties.GetMusicPropertiesAsync();

                    string duration = FormatDuration(musicProps.Duration);

                    loadedSongs.Add(new NewPlaylistSongProperty
                    {
                        SongPath = file.Path,
                        SongDuration = duration
                    });
                }
            }
            lstViewPlaylistAddedSongs.ItemsSource = loadedSongs;
            btnAddPlaylistCover.IsEnabled = true;
            btnAddSongs.IsEnabled = true;
        }
        private string FormatDuration(TimeSpan duration)
        {
            if (duration.Hours > 0)
                return duration.ToString(@"hh\:mm\:ss");

            return duration.ToString(@"mm\:ss");
        }
        bool isdarkmode = true;
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is string receivedparam)
            {
                if (receivedparam.Contains("DeletedPlaylist"))
                {
                    ttPlaylistDeleted.Title = "Playlist was Deleted: " + receivedparam.Replace("DeletedPlaylist", "");

                    ttPlaylistDeleted.IsOpen = true;
                    await Task.Delay(5000);
                    ttPlaylistDeleted.IsOpen = false;
                }
            }
        }
        private async void dlgNewPlaylist_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {

        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (chckmultiple.IsChecked == true)
            {
                GrdViewPlaylists.SelectionMode = ListViewSelectionMode.Multiple;
                btnDeletePlaylists.Visibility = Visibility.Visible;
            }
            else
            {
                GrdViewPlaylists.SelectionMode = ListViewSelectionMode.Single; btnDeletePlaylists.Visibility = Visibility.Collapsed;
            }
        }

        private async void dlgConfirmDeletePlaylist_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            MyItems.Remove(sngtemp);
            if (GrdViewPlaylists.Items.Count == 0)
            {
                txtEmptyPlaylists.Visibility = Visibility.Visible;
                GrdViewPlaylists.Visibility = Visibility.Collapsed;
                btnDeletePlaylists.Visibility = Visibility.Collapsed;
                chckmultiple.IsChecked = false;
                chckmultiple.Visibility = Visibility.Collapsed;
            }
            var currentSettings = await SettingsHelper.LoadSettingsAsync();
            currentSettings.SavedPlaylists = MyItems;
            await SettingsHelper.SaveSettingsAsync(currentSettings);
        }

        private void GrdViewPlaylists_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {

        }

        private void chckmultiple_Unchecked(object sender, RoutedEventArgs e)
        {
            if (chckmultiple.IsChecked == true)
            {
                GrdViewPlaylists.SelectionMode = ListViewSelectionMode.Multiple;
                btnDeletePlaylists.Visibility = Visibility.Visible;
            }
            else
            {
                btnDeletePlaylists.Visibility = Visibility.Collapsed;
                GrdViewPlaylists.SelectionMode = ListViewSelectionMode.Single;
            }
        }



        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = GrdViewPlaylists.SelectedItems.Cast<PlaylistProperties>().ToList();

            foreach (var item in selectedItems)
            {
                // 2. Remove from the ObservableCollection
                MyItems.Remove(item);
            }
            if (GrdViewPlaylists.Items.Count == 0)
            {
                txtEmptyPlaylists.Visibility = Visibility.Visible;
                btnDeletePlaylists.Visibility = Visibility.Collapsed;
                GrdViewPlaylists.Visibility = Visibility.Collapsed;
                chckmultiple.IsChecked = false;
                chckmultiple.Visibility = Visibility.Collapsed;
            }
            var currentSettings = await SettingsHelper.LoadSettingsAsync();
            currentSettings.SavedPlaylists = MyItems;
            await SettingsHelper.SaveSettingsAsync(currentSettings);
        }

        private async void GrdViewPlaylists_ItemClick(object sender, ItemClickEventArgs e)
        {
            var clickedPlaylist = e.ClickedItem as PlaylistProperties;
            PlaybackState.currentPlaylist = clickedPlaylist;
            if (clickedPlaylist != null)
            {
                this.Frame.Navigate(typeof(Playlist), clickedPlaylist);
            }
        }

        private async void btnAddPlaylistCover_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            btnAddPlaylistCover.IsEnabled = false;
            btnAddSongs.IsEnabled = false;
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
                btnAddPlaylistCover.IsEnabled = false;
                ToolTipService.SetToolTip(imgPlaylistCov, Path.GetFileName(file.Path));
                imgPlaylistCov.Source = new BitmapImage(new Uri(file.Path));
            }
            btnAddPlaylistCover.IsEnabled = true;
            btnAddSongs.IsEnabled = true;
        }

        private void btnRemovePlaylistCover_Click(object sender, RoutedEventArgs e)
        {
            ToolTipService.SetToolTip(imgPlaylistCov, "");
            CoverOptions.Visibility = Visibility.Collapsed;
            btnAddPlaylistCover.IsEnabled = true;
            imgPlaylistCov.Source = null;
        }


        private void chckmultiplerecent_Checked(object sender, RoutedEventArgs e)
        {
            if (chckmultiple.IsChecked == true)
            {
                grdViewRecentMusic.SelectionMode = ListViewSelectionMode.Multiple;
                btnDeleteRecents.Visibility = Visibility.Visible;
            }
            else
            {
                btnDeleteRecents.Visibility = Visibility.Collapsed;
                grdViewRecentMusic.SelectionMode = ListViewSelectionMode.Single;
            }
        }

        private void chckmultiplerecent_Unchecked(object sender, RoutedEventArgs e)
        {
            if (chckmultiple.IsChecked == true)
            {
                grdViewRecentMusic.SelectionMode = ListViewSelectionMode.Multiple;
                btnDeleteRecents.Visibility = Visibility.Visible;
            }
            else
            {
                btnDeleteRecents.Visibility = Visibility.Collapsed;
                grdViewRecentMusic.SelectionMode = ListViewSelectionMode.Single;
            }
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var selectedItems = grdViewRecentMusic.SelectedItems.Cast<RecentMusic>().ToList();

            foreach (var item in selectedItems)
            {
                // 2. Remove from the ObservableCollection
                RecentMusicItems.Remove(item);
            }
            if (grdViewRecentMusic.Items.Count == 0)
            {
                txtEmptyRecents.Visibility = Visibility.Visible;
                btnDeleteRecents.Visibility = Visibility.Collapsed;
                grdViewRecentMusic.Visibility = Visibility.Collapsed;
                chckmultiplerecent.IsChecked = false;
                chckmultiplerecent.Visibility = Visibility.Collapsed;
            }
            var currentSettings = await SettingsHelper.LoadSettingsAsync();
            currentSettings.RecentMusic = RecentMusicItems;
            await SettingsHelper.SaveSettingsAsync(currentSettings);
        }

        private void RecentMusic_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {

        }

        private void RecentMusic_ItemClick(object sender, ItemClickEventArgs e)
        {
            var clickedRecent = e.ClickedItem as RecentMusic;
            if (clickedRecent == null) return;
            if (App.MainWindowInstance is HomeWindow wind)
            {
                ObservableCollection<string> pt = new();
                pt.Add(clickedRecent.SongPath);
                wind.LoadFileFromPath(pt);
            }
        }
        public async void UpdatePath(string oldpath, string newpath)
        {
            foreach (var item in RecentMusicItems.ToList())
            {
                if (item.SongPath == oldpath)
                {
                    RecentMusicItems.Remove(item);

                    var currentSettings = await SettingsHelper.LoadSettingsAsync();
                    currentSettings.RecentMusic = RecentMusicItems;
                    await SettingsHelper.SaveSettingsAsync(currentSettings);
                }
            }
        }
        private async void MenuFlyoutItem_Click_1(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is RecentMusic song)
            {
                RecentMusicItems.Remove(song);
                if (grdViewRecentMusic.Items.Count == 0)
                {
                    txtEmptyRecents.Visibility = Visibility.Visible;
                    grdViewRecentMusic.Visibility = Visibility.Collapsed;
                    btnDeleteRecents.Visibility = Visibility.Collapsed;
                    chckmultiplerecent.IsChecked = false;
                    chckmultiplerecent.Visibility = Visibility.Collapsed;
                }
                var currentSettings = await SettingsHelper.LoadSettingsAsync();
                currentSettings.RecentMusic = RecentMusicItems;
                await SettingsHelper.SaveSettingsAsync(currentSettings);
            }
        }

        private void MenuFlyoutItem_Click_2(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is RecentMusic song)
            {
                string filePath = song.SongPath;
                if (File.Exists(filePath))
                {
                    // This opens explorer and HIGHLIGHTS the specific file
                    Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                }
            }
        }

        private void txtPlaylistName_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }

}