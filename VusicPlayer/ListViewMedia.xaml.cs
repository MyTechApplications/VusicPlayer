using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
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
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Vortice.MediaFoundation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using FileAttributes = System.IO.FileAttributes;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicPlayer
{
    public sealed partial class ListViewMedia : UserControl
    {
        public ListViewMedia()
        {
            InitializeComponent();
        }
        public ObservableCollection<SongModel> SongCollection { get; set; } = new();
        public ObservableCollection<SongModel> SongCollection2 { get; set; } = new();

        public void LoadMedia(ObservableCollection<SongModel> songslist, Frame fr)
        {

            frm = fr;
            App.HomeWindowInstance?.DispatcherQueue.TryEnqueue(() =>
            {
                // 2. Clear the existing collection instead of replacing the reference
                SongCollection.Clear();

                // 3. Populate it
                if (songslist != null)
                {
                    foreach (var song in songslist)
                    {
                        SongCollection.Add(song);
                    }
                }

                // 4. Re-assign the ItemsSource ONLY if it's not already set
                if (lstViewPlaylist.ItemsSource == null)
                {
                    lstViewPlaylist.ItemsSource = SongCollection;
                }
                SongCollection.CollectionChanged += SongCollection_CollectionChanged;
            });
        }

        private void SongCollection_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (PageState.IsQueuePage)
            {
                if (e.Action == NotifyCollectionChangedAction.Move)
                {
                    Debug.WriteLine("Wait's a shitstick");
                    // The item that was moved
                    var movedItem = e.NewItems?[0];

                    int oldIndex = e.OldStartingIndex;
                    int newIndex = e.NewStartingIndex;
                    if (oldIndex >= 0 && oldIndex < QueueListHolder.VusicQueue.Count)
                    {
                        var itemToMove = QueueListHolder.VusicQueue[oldIndex];
                        QueueListHolder.VusicQueue.Move(oldIndex, newIndex);
                    }

                }
            }
        }

        Frame? frm;
        SongModel selectedSong = new();
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
                    QueueService.PlayMedia(paths);

                }
            }
        }
        private void lstViewPlaylist_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {

        }

        private async void mnftContext_Opened(object sender, object e)
        {
            var flyout = sender as MenuFlyout;
            var addToPlaylist = flyout?.Items
        .OfType<MenuFlyoutSubItem>()
        .FirstOrDefault(x => x.Text == "Add to Playlist");

            if (addToPlaylist == null)
                return;

            addToPlaylist.Items.Clear();
            var selectedsong = addToPlaylist?.DataContext as SongModel;
            if (selectedsong == null) return;
            var currentSettings = await SettingsHelper.LoadSettingsAsync();
            var Playlists = currentSettings.SavedPlaylists;
            foreach (var item in Playlists)
            {
                MenuFlyoutItem playlistitem = new MenuFlyoutItem();
                playlistitem.Text = item.PlaylistName;
                addToPlaylist?.Items.Add(playlistitem);
                playlistitem.Click += async (sender, e) =>
                {
                    var path = selectedsong?.FilePath;

                    if (path != null && !item.SongsPaths.Contains(path))
                    {
                        item.SongsPaths.Add(path);
                        int count = item.SongsPaths.Count;
                        item.PlaylistCount = $"{count} {(count == 1 ? "item" : "items")}";
                        await SettingsHelper.SaveSettingsAsync(currentSettings);
                    }
                };

            }
            var mnftAddtoFav = flyout?.Items
    .OfType<MenuFlyoutItem>()
    .FirstOrDefault(x => x.Tag.ToString() == "Favo");

            if (mnftAddtoFav == null) return;
            var heartIcon = mnftAddtoFav.Icon as FontIcon;
            if (heartIcon == null) return;
            if (selectedsong.IsFavourite == true)
            {
                mnftAddtoFav.Text = "Remove from Favourites";
                heartIcon.Glyph = "\uEB52";
                heartIcon.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                mnftAddtoFav.Text = "Add to Favourites";
                heartIcon.Foreground = new SolidColorBrush(Colors.Transparent);
                heartIcon.Glyph = "\uEB51";
            }

            MenuFlyoutItem playlistitem2 = new MenuFlyoutItem();
            playlistitem2.Text = "New Playlist";
            FontIcon fnticon = new FontIcon();
            fnticon.Glyph = "\uE710";
            playlistitem2.Icon = fnticon;
            playlistitem2.Click += async (sender, e) =>
            {
                if (App.HomeWindowInstance == null) return;


                tempsong = selectedsong;
                HashSet<string> songpathstoadd = new();
                if (selectedsong.FilePath != null)
                {
                    songpathstoadd.Add(selectedsong.FilePath);
                }
                if (frm != null)
                {
                await    PlaylistDialogNew.LoadPlaylistCreationDialog(false, new PlaylistProperties { SongsPaths = songpathstoadd }, frm);
                }
                OceanContentDialog.Show("Create Playlist", "Create", "", "Cancel", OceanContentDialogDefault.Primary, contentsNewPlaylist, this.XamlRoot, 600, 760, OceanContentDialogType.Elevated, App.HomeWindowInstance, "addicon", "", "");
                OceanContentDialog.PrimaryRequested += OceanContentDialog_PrimaryRequested1;
            };
            addToPlaylist?.Items.Add(playlistitem2);
            var mnftAddtoQueue = flyout?.Items
    .OfType<MenuFlyoutItem>()
    .FirstOrDefault(x => x.Text == "Add to Play Queue");
            if (mnftAddtoQueue == null) return;
            if (PageState.IsQueuePage == true)
            {

                mnftAddtoQueue.Visibility = Visibility.Collapsed;
            }
            else
            {
                mnftAddtoQueue.Visibility = Visibility.Visible;
            }
            var mnftPlayNext = flyout?.Items
  .OfType<MenuFlyoutItem>()
  .FirstOrDefault(x => x.Text == "Play Next");
            if (mnftPlayNext == null) return;
            if (QueueListHolder.VusicQueue.Count == 0)
            {
                mnftPlayNext.Visibility = Visibility.Collapsed;
            }
            else
            {
                mnftPlayNext.Visibility = Visibility.Visible;
            }
            var mnftHeader = flyout?.Items
.OfType<MenuFlyoutItem>()
.FirstOrDefault(x => x.Name == "txtHeaderContext");
            if (mnftHeader == null) return;
            mnftHeader.Text = selectedsong.Title;
        }
        SongModel tempsong;
        private async void OceanContentDialog_PrimaryRequested1()
        {
            OceanContentDialog.HideDlg();
            HomeWindow.ShowWindow();
           // PlaylistDialogNew.SavePlaylist();
        }

        private void mnftPlaySong_Click(object sender, RoutedEventArgs e)
        {
            var menuFlyoutItem = sender as MenuFlyoutItem;
            var selectedsong = menuFlyoutItem?.DataContext as SongModel;
            if (selectedsong != null)
            {
                selectedSong = selectedsong;
                PlaySelection();
            }
        }

        private void removesongfromplaylistcreation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is SongModel song)
            {
                SongCollection.Remove(song);
            }

        }

        private void mnftSongDetails_Click(object sender, RoutedEventArgs e)
        {
            var menuFlyoutItem = sender as MenuFlyoutItem;
            var selectedsong = menuFlyoutItem?.DataContext as SongModel;
            if (App.MainWindowInstance is HomeWindow wind)
            {
         //       wind.ShowSongDetails(selectedsong.FilePath);
            }
        }

        private void mnftAddtoPlaylist_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void mnftAddtoQueue_Click(object sender, RoutedEventArgs e)
        {
            var menuFlyoutItem = sender as MenuFlyoutItem;
            var selectedsong = menuFlyoutItem?.DataContext as SongModel;
            if (selectedsong != null)
            {
                QueueListHolder.VusicQueue.Add(selectedsong);
            }
        }

        private void mnftEditInfo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftGoToArtist_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement clickedElement)
            {
                // 2. Extract the DataContext (your SongModel)
                if (clickedElement.DataContext is SongModel clickedItem)
                {
                    // 3. Navigate to the Album page
                    frm?.Navigate(typeof(ArtistInfo), clickedItem);
                }
            }
        }

        private void mnftGoToAlbum_Click(object sender, RoutedEventArgs e)
        {
            GoToAlbum(sender);
        }
        string justmodfavpath = "";
        private async void mnftAddToFavourites_Click(object sender, RoutedEventArgs e)
        {
            var menuFlyoutItem = sender as MenuFlyoutItem;

            var selectedsong = menuFlyoutItem?.DataContext as SongModel;
            if (selectedsong == null) return;
            if (selectedsong.FilePath == null) return;
            var currentSettings = await SettingsHelper.LoadSettingsAsync();
            var list = currentSettings.Favourites;
            var favos = currentSettings.Favourites.FirstOrDefault(p => p.FilePath == selectedSong.FilePath);
            if (favos != null)
            {
                selectedsong.IsFavourite = false;
                list.Remove(favos);
            }
            else
            {
                selectedsong.IsFavourite = true;
                list.Add(new FavouritesModel { FilePath = selectedsong.FilePath ?? "" });
            }

            await SettingsHelper.SaveSettingsAsync(currentSettings);

            justmodfavpath = selectedsong.FilePath;
            CallFavButton();
            if (menuFlyoutItem == null) return;
            var heartIcon = menuFlyoutItem.Icon as FontIcon;
            if (heartIcon == null) return;
            if (selectedsong.IsFavourite == true)
            {
                menuFlyoutItem.Text = "Remove from Favourites";
                heartIcon.Glyph = "\uEB52";
                heartIcon.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                menuFlyoutItem.Text = "Add to Favourites";
                heartIcon.Foreground = new SolidColorBrush(Colors.Transparent);
                heartIcon.Glyph = "\uEB51";
            }
        }
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
                    AnimateHeart(fillHeartIcon, 1.0, 1.0);
                }
                else
                {
                    ToolTipService.SetToolTip(Favouritebutton, "Add to Favourites");

                    AnimateHeart(fillHeartIcon, 0.0, 0.0);
                }
            }
        }
        private void mnftMoveup_Click(object sender, RoutedEventArgs e)
        {
            var song = GetSelectedSong(sender);
            if (song == null) return;

            int index = SongCollection.IndexOf(song);
            if (index <= 0) return;

            SongCollection.Move(index, index - 1);
        }

        private void mnftMovedown_Click(object sender, RoutedEventArgs e)
        {
            var song = GetSelectedSong(sender);
            if (song == null) return;

            int index = SongCollection.IndexOf(song);
            if (index >= SongCollection.Count - 1) return;

            SongCollection.Move(index, index + 1);
        }
        private void mnftMovetotop_Click(object sender, RoutedEventArgs e)
        {
            var song = GetSelectedSong(sender);
            if (song == null) return;

            int index = SongCollection.IndexOf(song);
            if (index <= 0) return;

            SongCollection.Move(index, 0);
        }
        private SongModel? GetSelectedSong(object sender)
        {
            return (sender as MenuFlyoutItem)?.DataContext as SongModel;
        }
        private void mnftMovetobottom_Click(object sender, RoutedEventArgs e)
        {
            var song = GetSelectedSong(sender);
            if (song == null) return;

            int index = SongCollection.IndexOf(song);
            if (index == SongCollection.Count - 1) return;

            SongCollection.Move(index, SongCollection.Count - 1);
        }

        private void mnftTools_Click(object sender, RoutedEventArgs e)
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
                    frm?.Navigate(typeof(Album), clickedItem);
                }
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {

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

        private void txtArtistHyp_Click(object sender, RoutedEventArgs e)
        {
            var clickedArtist = sender as HyperlinkButton;

            var clickedItem = clickedArtist?.DataContext as SongModel;
            if (clickedArtist != null)
            {

                frm?.Navigate(typeof(ArtistInfo), clickedItem);
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
            OceanContentDialog.PrimaryRequested += OceanContentDialog_PrimaryRequested;
            lstViewEdit.ItemsSource = SongCollection;
            lstViewEdit.SelectedItems.Clear();
            foreach (var item in lstViewPlaylist.SelectedItems)
            {
                lstViewEdit.SelectedItems.Add(item);
            }
            tbviAlbum.IsSelected = true;

            txtEditAlbum.Text = SongCollection[0].AlbumName;

        }

        private async void OceanContentDialog_PrimaryRequested()
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
                        item.AlbumName = txtEditAlbum.Text;
                        var file = TagLib.File.Create(item.FilePath);
                        file.Tag.Album = txtEditAlbum.Text;
                        file.Save();

                    }
                    catch (COMException ex)
                    {
                        btnFixFile.Visibility = Visibility.Collapsed;
                        FileStatusInfoBar.IsOpen = true;
                        FileStatusInfoBar.Title = "Error";
                        FileStatusInfoBar.Message = "An unexpected error occured while setting Album property. Check log page for more details under App Settings";
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
                        item.Artist = txtEditArtist.Text;
                        var file = TagLib.File.Create(item.FilePath);
                        file.Tag.AlbumArtists = new[] { txtEditArtist.Text };

                        file.Save();
                    }
                    catch (IOException ex) when (IsFileLocked(ex))
                    {
                        btnFixFile.Visibility = Visibility.Collapsed;
                        FileStatusInfoBar.IsOpen = true;
                        FileStatusInfoBar.Title = "Error";
                        FileStatusInfoBar.Message = "The file is in use by another process. Check log page for more details under App Settings";
                        Logger.Log(ex.Message, "ListViewMedia.ArtistSetMultiple", Logger.LogLevelType.Error);

                    }
                    catch (COMException ex)
                    {
                        btnFixFile.Visibility = Visibility.Collapsed;
                        FileStatusInfoBar.IsOpen = true;
                        FileStatusInfoBar.Title = "Error";
                        FileStatusInfoBar.Message = "An unexpected error occured while setting Artist property. Check log page for more details under App Settings";
                        Logger.Log(ex.Message, "ListViewMedia.ArtistSetMultiple", Logger.LogLevelType.Error);
                    }
                    //Check for blocked files
                }
            }
            OceanContentDialog.HideDlg();
            HomeWindow.ShowWindow();
        }
        private bool IsFileLocked(IOException exception)
        {
            int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(exception) & ((1 << 16) - 1);
            return errorCode == 32 || errorCode == 33; // 32 = Sharing Violation, 33 = Lock Violation
        }
        private void btnEditArtistMass_Click(object sender, RoutedEventArgs e)
        {
            if (App.HomeWindowInstance == null) return;
            OceanContentDialog.Show("Properties", "Save", "", "Cancel", OceanContentDialogDefault.Primary, MassEditgrd, this.XamlRoot, 600, 600, OceanContentDialogType.Elevated, App.HomeWindowInstance, "saveicon", "", "");
            OceanContentDialog.PrimaryRequested += OceanContentDialog_PrimaryRequested;

            tbviArtist.IsSelected = true;
            lstViewEdit.ItemsSource = SongCollection;
            txtEditArtist.Text = SongCollection[0].Artist;

            foreach (var item in lstViewPlaylist.SelectedItems)
            {
                lstViewEdit.SelectedItems.Add(item);
            }
            tbviAlbum.IsSelected = true;
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
                    List<string> playlistitems = new();
                    playlistitems!.Add(item.PlaylistName);
                    lstViewAddToPlaylists.ItemsSource = playlistitems;

                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void lstViewAddToPlaylists_ItemClick(object sender, ItemClickEventArgs e)
        {

        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {

        }

        private void mnftSetAlbumName_Click(object sender, RoutedEventArgs e)
        {
            var menuFlyoutItem = sender as MenuFlyoutItem;
            var selectedsong = menuFlyoutItem?.DataContext as SongModel;
            if (selectedsong != null)
            {
                txtEditAlbum.Text = selectedsong.AlbumName;
            }

        }

        private void mnftSetArtistName_Click(object sender, RoutedEventArgs e)
        {
            var menuFlyoutItem = sender as MenuFlyoutItem;
            var selectedsong = menuFlyoutItem?.DataContext as SongModel;
            if (selectedsong != null)
            {
                txtEditArtist.Text = selectedsong.Artist;
            }
        }

        private void mnftSelectitem_Click(object sender, RoutedEventArgs e)
        {
            var menuFlyoutItem = sender as MenuFlyoutItem;
            var selectedsong = menuFlyoutItem?.DataContext as SongModel;
            if (selectedsong != null)
            {
                lstViewEdit.SelectedItems.Add(selectedsong);
            }
        }
        string? tempfilepath = "";
        private void btnFixFile_Click(object sender, RoutedEventArgs e)
        {



        }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]

        private static extern bool DeleteFile(string lpFileName);

        private void ValidateFileAccessibility(string filePath)
        {

            string path = filePath;

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

        }

        private void mnftUnselectItem_Click(object sender, RoutedEventArgs e)
        {
            var menuFlyoutItem = sender as MenuFlyoutItem;
            var selectedsong = menuFlyoutItem?.DataContext as SongModel;
            if (selectedsong != null)
            {
                lstViewEdit.SelectedItems.Remove(selectedsong);
            }
        }

        private void btnClearSelection_Click(object sender, RoutedEventArgs e)
        {
            lstViewPlaylist.SelectedItems.Clear();
        }

        private void btnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            lstViewPlaylist.SelectAll();
        }
        bool isChecked = false;
        private Button? Favouritebutton;
        private void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;

            if (btn == null) return;
            Favouritebutton = btn;
            var selectedsong = btn?.DataContext as SongModel;
            if (selectedsong != null)
            {
                selectedSong = selectedsong;
            }
            CallFavButton();
        }
        private void AnimateHeart(FontIcon target, double targetOpacity, double targetScale)
        {
            var storyboard = new Microsoft.UI.Xaml.Media.Animation.Storyboard();

            // Scale X Animation
            var scaleXAnim = new DoubleAnimation { To = targetScale, Duration = TimeSpan.FromMilliseconds(200) };
            Storyboard.SetTarget(scaleXAnim, target.RenderTransform);
            Storyboard.SetTargetProperty(scaleXAnim, "ScaleX");

            // Scale Y Animation
            var scaleYAnim = new DoubleAnimation { To = targetScale, Duration = TimeSpan.FromMilliseconds(200) };
            Storyboard.SetTarget(scaleYAnim, target.RenderTransform);
            Storyboard.SetTargetProperty(scaleYAnim, "ScaleY");

            // Opacity Animation
            var opacityAnim = new DoubleAnimation { To = targetOpacity, Duration = TimeSpan.FromMilliseconds(150) };
            Storyboard.SetTarget(opacityAnim, target);
            Storyboard.SetTargetProperty(opacityAnim, "Opacity");

            storyboard.Children.Add(scaleXAnim);
            storyboard.Children.Add(scaleYAnim);
            storyboard.Children.Add(opacityAnim);

            storyboard.Begin();
        }

        private void btnRemoveSelectionsFromFavourites_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftPlaySongNext_Click(object sender, RoutedEventArgs e)
        {
            var menuFlyoutItem = sender as MenuFlyoutItem;
            var selectedsong = menuFlyoutItem?.DataContext as SongModel;
            if (selectedsong == null) return;
            int currentindex = QueueHandler.videoindex;
            if (currentindex == -1) return;
            if (selectedsong.FilePath == PlaybackState.CurrentlyPlayingPath) return;
            var existingSong = QueueListHolder.VusicQueue.FirstOrDefault(x => x.FilePath == selectedsong.FilePath);

            if (existingSong != null)
            {
                QueueListHolder.VusicQueue.Remove(existingSong);
            }


            int insertAt = QueueHandler.videoindex + 1;

            insertAt = Math.Max(0, Math.Min(insertAt, QueueListHolder.VusicQueue.Count));

            QueueListHolder.VusicQueue.Insert(insertAt, selectedsong);

        }
    }
}
