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
            MyItems.CollectionChanged += MyItems_CollectionChanged;
            RecentMusicItems.CollectionChanged += RecentMusicItems_CollectionChanged;
        }
        private async void RecentMusicItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isLoadingData) return;
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
            if (_isLoadingData) return;
            if (e.Action == NotifyCollectionChangedAction.Remove ||
        e.Action == NotifyCollectionChangedAction.Add ||
        e.Action == NotifyCollectionChangedAction.Move)
            {
                var currentSettings = await SettingsHelper.LoadSettingsAsync();
                currentSettings.SavedPlaylists = MyItems;
                await SettingsHelper.SaveSettingsAsync(currentSettings);
            }
        }
        private bool _isLoadingData = false;
        private async Task CallValues()
        {
            _isLoadingData = true;
            try
            {
                MyItems.Clear();
                RecentMusicItems.Clear();
                var settings = await SettingsHelper.LoadSettingsAsync();
                var playlistitemss = settings.SavedPlaylists;
                var recentmusicitems = settings.RecentMusic;
                foreach (var item in playlistitemss)
                {
                    Uri thumbpath = item.Thumbnail ?? new Uri("ms-appx:///Assets/playlistdefaultdark.png");
                    item.plthumb = new BitmapImage(thumbpath);


                    MyItems.Add(item);
                }
                foreach (var item in recentmusicitems)
                {
                    item.Thumbnail = await FileThumbnailObtain.GetFileThumbnailAsync(item.SongPath);
                    RecentMusicItems.Add(item);
                }
                grdViewRecentMusic.ItemsSource = RecentMusicItems;
                GrdViewPlaylists.ItemsSource = MyItems;

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
            finally
            {
                _isLoadingData = false;
            }
        }
        public ObservableCollection<PlaylistProperties> MyItems { get; set; } = new();
        public ObservableCollection<RecentMusic> RecentMusicItems { get; set; } = new();


        private bool _isCreatingPlaylist = false;
        private async void btnNewPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (App.HomeWindowInstance == null) return;
            AllSongs.CollectionChanged += AllSongs_CollectionChanged;
            OceanContentDialog.ClearSubscribers();

            // 2. Now add back only the current listener
            OceanContentDialog.PrimaryRequested += Dlg_PrimaryRequested;
            //           await PlaylistDialog.LoadPlaylistCreationDialog(true, new PlaylistProperties { PlaylistName = "New Playlist" }, this.Frame);
            OceanContentDialog.Show("Create New Playlist", "Create", "", "Cancel", OceanContentDialogDefault.Primary, contentsNewPlaylist, this.XamlRoot, 600, 760, OceanContentDialogType.Elevated, App.HomeWindowInstance, "addicon", "", "");
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
                if (string.IsNullOrEmpty(baseName)) baseName = "New Playlist";

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
                currentSettings.SavedPlaylists.Add(newPlaylist);
                await SettingsHelper.SaveSettingsAsync(currentSettings);
                MyItems.Add(newPlaylist);

                OceanContentDialog.HideDlg();
                HomeWindow.ShowWindow();


                UIUpdate();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                // Allow the button to be used again only after everything is done
                _isSavingPlaylist = false;
            }
        }
        private async void btnOpenMusic_Click(object sender, RoutedEventArgs e)
        {
            if (App.HomeWindowInstance == null) return;
            var files = await PickFiles.PickAudioFileAsync(App.HomeWindowInstance, "Choose Audio");
            if (files != null)
            {


                if (files.Path != null)
                {
                    if (File.Exists(files.Path))
                    {
                        StorageFile file = await StorageFile.GetFileFromPathAsync(files.Path);
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
                        PlayerService.CreatePlayer();
                        QueueHandler.PlayMedia(temp, false, false);
                    }
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
            await CallValues();
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
                GrdViewPlaylists.SelectionMode = ListViewSelectionMode.Single;
                btnDeletePlaylists.Visibility = Visibility.Collapsed;
            }
        }

        private async void dlgConfirmDeletePlaylist_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            MyItems.Remove(sngtemp);
            UIUpdate();
            var currentSettings = await SettingsHelper.LoadSettingsAsync();
            currentSettings.SavedPlaylists = MyItems;
            await SettingsHelper.SaveSettingsAsync(currentSettings);
        }


        private void chckmultiple_Unchecked(object sender, RoutedEventArgs e)
        {
            bool isChecked = chckmultiple.IsChecked ?? false;

            GrdViewPlaylists.SelectionMode = isChecked ? ListViewSelectionMode.Multiple : ListViewSelectionMode.Single;
            btnDeletePlaylists.Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;
        }


        private void UIUpdate()
        {
            if (MyItems.Count == 0)
            {
                btnDeletePlaylists.Visibility = Visibility.Collapsed;
                chckmultiple.IsChecked = false;
                chckSelectAllPlaylists.IsChecked = false;
                txtEmptyPlaylists.Visibility = Visibility.Visible;
                GrdViewPlaylists.Visibility = Visibility.Collapsed;
                chckmultiple.Visibility = Visibility.Collapsed;
            }
            else
            {
                chckSelectAllPlaylists.IsChecked = false;
                chckmultiple.IsChecked = false;
                txtEmptyPlaylists.Visibility = Visibility.Collapsed;
                GrdViewPlaylists.Visibility = Visibility.Visible;
                chckmultiple.Visibility = Visibility.Visible;
            }
            if (grdViewRecentMusic.Items.Count == 0)
            {
                txtEmptyRecents.Visibility = Visibility.Visible;
                btnDeleteRecents.Visibility = Visibility.Collapsed;
                grdViewRecentMusic.Visibility = Visibility.Collapsed;
                chckmultiplerecent.IsChecked = false;
                chckmultiplerecent.Visibility = Visibility.Collapsed;
            }
        }
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = GrdViewPlaylists.SelectedItems.Cast<PlaylistProperties>().ToList();

            foreach (var item in selectedItems)
            {
                MyItems.Remove(item);
            }
            UIUpdate();
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


        private void chckmultiplerecent_Checked(object sender, RoutedEventArgs e)
        {
            bool isMultiple = chckmultiplerecent.IsChecked ?? false;

            grdViewRecentMusic.SelectionMode = isMultiple ? ListViewSelectionMode.Multiple : ListViewSelectionMode.Single;
            btnDeleteRecents.Visibility = isMultiple ? Visibility.Visible : Visibility.Collapsed;
        }

        private void chckmultiplerecent_Unchecked(object sender, RoutedEventArgs e)
        {
            bool isMultiple = chckmultiplerecent.IsChecked ?? false;

            grdViewRecentMusic.SelectionMode = isMultiple ? ListViewSelectionMode.Multiple : ListViewSelectionMode.Single;
            btnDeleteRecents.Visibility = isMultiple ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var selectedItems = grdViewRecentMusic.SelectedItems.Cast<RecentMusic>().ToList();

            foreach (var item in selectedItems)
            {
                RecentMusicItems.Remove(item);
            }
            UIUpdate();
            var currentSettings = await SettingsHelper.LoadSettingsAsync();
            currentSettings.RecentMusic = RecentMusicItems;
            await SettingsHelper.SaveSettingsAsync(currentSettings);
            if (btnRemoveSelectedRecents.Flyout is Flyout f)
            {
                f.Hide();
            }
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
                        //UPDATE LOGIC HERE
                    }
                    else
                    {

                        txtMissingpath.Text = clickedRecent.SongPath;

                        OceanContentDialog.Show("Missing File", "", "", "OK", OceanContentDialogDefault.Close, AccessDeniedGrid, this.XamlRoot, 500, 460, OceanContentDialogType.Elevated, App.HomeWindowInstance, "", "", "");
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
            if (App.OceanDialogInstance == null)
            {
                ifbMessagee.IsOpen = true;
                ifbMessagee.Title = "Error";
                ifbMessagee.Message = "An unexpected error occured. Check log details in Settings Page.";
                ifbMessagee.Severity = InfoBarSeverity.Error;
                Logger.Log("Error code 0x0012oc. Refer the github page for more details.", "PlaylistCreation", Logger.LogLevelType.Error);
                return;
            }
            var file = await PickFiles.PickAudioFileAsync(App.OceanDialogInstance, "Choose Audio");

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
                    newfile.Thumbnail = await FileThumbnailObtain.GetFileThumbnailAsync(file.Path);
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
            if (GrdViewPlaylists.Items.Count == 0) return;

            if (chckSelectAllPlaylists.IsChecked ?? false)
                GrdViewPlaylists.SelectAll();
            else
                GrdViewPlaylists.SelectedItems.Clear();
        }

        private void chckSelectAllPlaylists_Unchecked(object sender, RoutedEventArgs e)
        {
            if (GrdViewPlaylists.Items.Count == 0) return;

            if (chckSelectAllPlaylists.IsChecked ?? false)
                GrdViewPlaylists.SelectAll();
            else
                GrdViewPlaylists.SelectedItems.Clear();
        }

        private void chckSelectAllRecents_Checked(object sender, RoutedEventArgs e)
        {
            if (grdViewRecentMusic.Items.Count == 0) return;

            if (chckSelectAllRecents.IsChecked ?? false)
                grdViewRecentMusic.SelectAll();
            else
                grdViewRecentMusic.SelectedItems.Clear();
        }

        private void chckSelectAllRecents_Unchecked(object sender, RoutedEventArgs e)
        {
            if (grdViewRecentMusic.Items.Count == 0) return;

            if (chckSelectAllRecents.IsChecked ?? false)
                grdViewRecentMusic.SelectAll();
            else
                grdViewRecentMusic.SelectedItems.Clear();
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
                ifbMessagee.IsOpen = true;
                ifbMessagee.Title = "Error";
                ifbMessagee.Message = "An unexpected error occured. Check log details in Settings Page.";
                ifbMessagee.Severity = InfoBarSeverity.Error;
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

        private void MenuFlyoutItem_Click_3(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is RecentMusic song)
            {
                if (App.MainWindowInstance is HomeWindow homeWindow)
                {
                    if (!string.IsNullOrEmpty(song.SongPath))
                    {
                 //       homeWindow.ShowSongDetails(song.SongPath);
                    }
                }
            }
        }
    }
        #endregion
}