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
using System.Windows.Controls;
using Vortice.Direct3D11;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playlists;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using WinRT.Interop;
using FrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using Page = Microsoft.UI.Xaml.Controls.Page;
using RoutedEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;
using TextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using TextBox = Microsoft.UI.Xaml.Controls.TextBox;
using TextChangedEventArgs = Microsoft.UI.Xaml.Controls.TextChangedEventArgs;
using ToolTipService = Microsoft.UI.Xaml.Controls.ToolTipService;
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
         //   CallValues();
            MyItems.CollectionChanged += MyItems_CollectionChanged;
            RecentMusicItems.CollectionChanged += RecentMusicItems_CollectionChanged;
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
            RecentMusicItems.Clear();
            var settings = await SettingsHelper.LoadSettingsAsync();
            var playlistitemss = settings.SavedPlaylists;
            var recentmusicitems = settings.RecentMusic;
            foreach (var item in playlistitemss)
            {
                string defaultThumbnail = Path.Combine(AppContext.BaseDirectory, "Assets", "playlistdefaultdark.png");

                if (item.Thumbnail == null && item.SongsPaths?.Count > 0)
                {
                    item.Thumbnail = new Uri(item.SongsPaths[0]).AbsoluteUri;
                }
                else if (item.Thumbnail == null)
                {
                    item.Thumbnail = new Uri(defaultThumbnail).AbsoluteUri;
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
            GrdViewPlaylists.ItemsSource = MyItems;

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
        private bool _isCreatingPlaylist = false;
        private async void btnNewPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (App.HomeWindowInstance == null) return;

            PlaylistDialog.LoadPlaylistCreationDialog(true, new PlaylistProperties { PlaylistName="New Playlist"}, this.Frame);
            OceanContentDialog.Show("Create New Playlist", "Create", "", "Cancel", OceanContentDialogDefault.Primary, contentsNewPlaylist, this.XamlRoot, 600, 760, OceanContentDialogType.Elevated, App.HomeWindowInstance, "addicon", "", "");
            OceanContentDialog.PrimaryRequested += Dlg_PrimaryRequested;

        }
        OceanDialog dlg2;
        OceanPopup oceanPopup;
        private async void Dlg_PrimaryRequested()
        {
            OceanContentDialog.HideDlg();
            HomeWindow.ShowWindow();
            PlaylistDialog.SavePlaylist();
            CallValues();
        }
        public async Task<StorageFile?> PickFileAsync(Window wind)
        {

            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(wind);

            if (hwnd == IntPtr.Zero)
            {
                hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentActiveWindow);
            }
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            #region AudioFileTypes 
            picker.FileTypeFilter.Add(".mp3");
            picker.FileTypeFilter.Add(".wav");
            picker.FileTypeFilter.Add(".ogg");
            picker.FileTypeFilter.Add(".m4a");
            picker.FileTypeFilter.Add(".aac");
            picker.FileTypeFilter.Add(".wma");
            picker.FileTypeFilter.Add(".flac");
            picker.FileTypeFilter.Add(".ac3");
            picker.FileTypeFilter.Add(".alac");
            picker.FileTypeFilter.Add(".aiff");
            picker.FileTypeFilter.Add(".opus");
            picker.FileTypeFilter.Add(".ape");
            picker.FileTypeFilter.Add(".wv");
            picker.FileTypeFilter.Add(".tta");
            picker.FileTypeFilter.Add(".dsf");
            picker.FileTypeFilter.Add(".dff");
            picker.FileTypeFilter.Add(".mp2");
            picker.FileTypeFilter.Add(".amr");
            picker.FileTypeFilter.Add(".au");
            picker.FileTypeFilter.Add(".snd");
            picker.FileTypeFilter.Add(".mka");
            #endregion

            var files = await picker.PickSingleFileAsync();
            return files;
        } 
        private async void btnOpenMusic_Click(object sender, RoutedEventArgs e)
        {
            if (App.HomeWindowInstance == null) return;
            var files = await PickFileAsync(App.HomeWindowInstance);
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

        }



        private void btnActualReset_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {

        }
        private async void removesongfromplaylistcreation_Click(object sender, RoutedEventArgs e)
        {

        }
        BitmapImage? img;
        private async void btnAddSongs_Click(object sender, RoutedEventArgs e)
        {
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
            CallValues();
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
            if (chckmultiple.IsChecked == false)
            {
                var clickedPlaylist = e.ClickedItem as PlaylistProperties;
                PlaybackState.currentPlaylist = clickedPlaylist;
                if (clickedPlaylist != null)
                {
                    this.Frame.Navigate(typeof(Playlist), clickedPlaylist);
                }
            }
        }

        private async void btnAddPlaylistCover_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnRemovePlaylistCover_Click(object sender, RoutedEventArgs e)
        {

        }


        private void chckmultiplerecent_Checked(object sender, RoutedEventArgs e)
        {
            if (chckmultiplerecent.IsChecked == true)
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
            if (chckmultiplerecent.IsChecked == true)
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
        RecentMusic temp = new();
        private void RecentMusic_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (chckmultiplerecent.IsChecked == false)
            {
                var clickedRecent = e.ClickedItem as RecentMusic;
                if (clickedRecent == null)
                {
                
                    return;
                }
                temp = clickedRecent;
                if (App.HomeWindowInstance is HomeWindow wind)
                {
                    if (File.Exists(clickedRecent.SongPath))
                    {
                        ObservableCollection<string> pt = new();
                        pt.Add(clickedRecent.SongPath);
                        wind.LoadFileFromPath(pt);
                    }
                    else
                    {
                      
                        txtMissingpath.Text = clickedRecent.SongPath;
                       
                        OceanContentDialog.Show("Missing File", "", "", "OK", OceanContentDialogDefault.Close, AccessDeniedGrid, this.XamlRoot, 500,460, OceanContentDialogType.Elevated, App.HomeWindowInstance, "", "", "");
                    }
                }
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
                    Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                }
            }
        }

        private void txtPlaylistName_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void MenuFlyoutItem_Click_3(object sender, RoutedEventArgs e)
        {

        }

        private void mnftOpenPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PlaylistProperties playlist)
            {
                var clickedPlaylist = playlist;
                PlaybackState.currentPlaylist = clickedPlaylist;
                if (clickedPlaylist != null)
                {
                    this.Frame.Navigate(typeof(Playlist), clickedPlaylist);
                }
            }
        }

        private void MenuFlyoutItem_Click_4(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem? it = sender as MenuFlyoutItem;
            if (it == null) return;
            if (it.Tag.ToString() == "name")
            {
                var sorted = MyItems.OrderBy(x => x.PlaylistName).ToList();
                MyItems.Clear();
                foreach (var item in sorted)
                    MyItems.Add(item);
            }
            else if (it.Tag.ToString() == "date")
            {
                var sorted = MyItems.OrderBy(x => x.DateCreation).ToList();
                MyItems.Clear();
                foreach (var item in sorted)
                    MyItems.Add(item);
            }
            else if (it.Tag.ToString() == "items")
            {
                var sorted = MyItems.OrderBy(x => x.PlaylistCount).ToList();
                MyItems.Clear();
                foreach (var item in sorted)
                    MyItems.Add(item);
            }
        }

        private void btnRemoveFile_Click(object sender, RoutedEventArgs e)
        {
            RecentMusicItems.Remove(temp);
            OceanContentDialog.HideDlg();
            HomeWindow.ShowWindow();
        }

        private async void btnRelocate_Click(object sender, RoutedEventArgs e)
        {
            if(App.OceanDialogInstance == null)
            {
                ifbMessagee.IsOpen = true;
                ifbMessagee.Title = "Error";
                ifbMessagee.Message = "An unexpected error occured. Check log details in Settings Page.";
                ifbMessagee.Severity = InfoBarSeverity.Error;
                Logger.Log("Error code 0x0012oc. Refer the github page for more details.", "PlaylistCreation", Logger.LogLevelType.Error);
                return;
            }
            var file = await PickFileAsync(App.OceanDialogInstance);

            if (file != null)
            {
                int index = RecentMusicItems.IndexOf(temp);

                if (index != -1)
                {
                    RecentMusicItems.RemoveAt(index);

                    var newfile = new RecentMusic
                    {
                        SongName = Path.GetFileName(file.Path),
                        SongPath = file.Path,
                        FolderName = new DirectoryInfo(
                            Path.GetDirectoryName(file.Path) ?? string.Empty
                        ).Name
                    };
                    newfile.Thumbnail = await GetFileThumbnailAsync(file.Path);
                    RecentMusicItems.Insert(index, newfile);
                    ttPlaylistDeleted.Title = "File Relocated to";
                    ttPlaylistDeleted.Content = $"{file.Path}";
                    OceanContentDialog.HideDlg();
                    HomeWindow.ShowWindow();
                    ttPlaylistDeleted.IsOpen = true;
                    await Task.Delay(5000);
                    ttPlaylistDeleted.IsOpen = false;
                    ttPlaylistDeleted.Content = "";
                }
            }
        }

        private void chckSelectAllPlaylists_Checked(object sender, RoutedEventArgs e)
        {
            if (chckSelectAllPlaylists.IsChecked == true)
            {
                GrdViewPlaylists.SelectAll();
            }

            else
            {
                GrdViewPlaylists.SelectedItems.Clear();
            }
        }

        private void chckSelectAllPlaylists_Unchecked(object sender, RoutedEventArgs e)
        {
            if (chckSelectAllPlaylists.IsChecked == true)
            {
                GrdViewPlaylists.SelectAll();
            }

            else
            {
                GrdViewPlaylists.SelectedItems.Clear();
            }
        }

        private void chckSelectAllRecents_Checked(object sender, RoutedEventArgs e)
        {
            if (chckSelectAllRecents.IsChecked == true)
            {
                grdViewRecentMusic.SelectAll();
            }

            else
            {
                grdViewRecentMusic.SelectedItems.Clear();
            }
        }

        private void chckSelectAllRecents_Unchecked(object sender, RoutedEventArgs e)
        {
            if (chckSelectAllRecents.IsChecked == true)
            {
                grdViewRecentMusic.SelectAll();
            }

            else
            {
                grdViewRecentMusic.SelectedItems.Clear();
            }
        }
    }

}