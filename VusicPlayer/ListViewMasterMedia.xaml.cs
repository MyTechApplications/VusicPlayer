using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicPlayer
{
    public sealed partial class ListViewMasterMedia : UserControl
    {
        public ListViewMasterMedia()
        {
            InitializeComponent();
            this.DataContext = this;
        }
        public bool AllowRearranging
        {
            get => (bool)GetValue(AllowRearrangingProperty);
            set => SetValue(AllowRearrangingProperty, value);
        }

        public static readonly DependencyProperty AllowRearrangingProperty =
            DependencyProperty.Register(
                nameof(AllowRearranging),
                typeof(bool),
                typeof(ListViewMasterMedia),
                new PropertyMetadata(true, OnAllowRearrangingChanged));
        private static void OnAllowRearrangingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListViewMasterMedia control)
            {
                bool canRearrange = (bool)e.NewValue;
                // Update the internal ListView's properties
                control.lstViewPlaylist.CanReorderItems = canRearrange;
                control.lstViewPlaylist.AllowDrop = canRearrange;
            }
        }
        public IList<object> SelectedItems
        {
            get => (IList<object>)GetValue(SelectedItemsProperty);
            set => SetValue(SelectedItemsProperty, value);
        }

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register(
                nameof(SelectedItems),
                typeof(IList<object>),
                typeof(ListViewMasterMedia),
                new PropertyMetadata(null));
        public ObservableCollection<SongModel> ItemsSource
        {
            get => (ObservableCollection<SongModel>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }
        public static readonly DependencyProperty ItemsSourceProperty =
    DependencyProperty.Register(
        nameof(ItemsSource),
        typeof(ObservableCollection<SongModel>),
        typeof(ListViewMasterMedia),
        new PropertyMetadata(null));
        private void lstViewPlaylist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            stkMultiOptions.Visibility = lstViewPlaylist.SelectedItems.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void mnftContext_Opened(object sender, object e)
        {

        }
        private void PlaySelection(SongModel selectedSong)
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
        private void mnftPlaySong_Click(object sender, RoutedEventArgs e)
        {
            var menuFlyoutItem = sender as MenuFlyoutItem;
            var selectedsong = menuFlyoutItem?.DataContext as SongModel;
            if (selectedsong != null)
            {
                PlaySelection(selectedsong);
            }
        }

        private void mnftPlaySongNext_Click(object sender, RoutedEventArgs e)
        {
            var song = (sender as MenuFlyoutItem)?.DataContext as SongModel;
            if (song == null ||
                QueueHandler.videoindex == -1 ||
                song.FilePath == PlaybackState.CurrentlyPlayingPath)
                return;

            var queue = QueueListHolder.VusicQueue;


            var existing = queue.FirstOrDefault(x => x.FilePath == song.FilePath);
            if (existing != null)
                queue.Remove(existing);
            int index = Math.Clamp(QueueHandler.videoindex + 1, 0, queue.Count);

            queue.Insert(index, song);
        }

        private void mnftRemoveSong_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is SongModel song)
            {
                this.ItemsSource.Remove(song);
            }
        }

        private void mnftSongDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.DataContext is SongModel songModel)
            {
                if (App.MainWindowInstance is HomeWindow homeWindow)
                {
                    if (!string.IsNullOrEmpty(songModel.FilePath))
                    {
               //         homeWindow.ShowSongDetails(songModel.FilePath);
                    }
                }
            }
        }

        private void mnftAddtoQueue_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.DataContext is SongModel song)
            {
                QueueListHolder.VusicQueue.Add(song);
            }
        }

        private void mnftEditInfo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.DataContext is SongModel songModel)
            {
                if (App.MainWindowInstance is HomeWindow homeWindow)
                {
                    if (!string.IsNullOrEmpty(songModel.FilePath))
                    {
         //               homeWindow.ShowSongDetails(songModel.FilePath);
                    }
                }
            }
        }
        public event Action<SongModel>? ShowArtistDetails;
        public event Action<SongModel>? ShowAlbumDetails;
        private void mnftGoToArtist_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement clickedElement)
            {
                if (clickedElement.DataContext is SongModel clickedItem)
                {
                    ShowArtistDetails?.Invoke(clickedItem);
                }
            }
        }

        private void mnftGoToAlbum_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement clickedElement)
            {
                if (clickedElement.DataContext is SongModel clickedItem)
                {
                    ShowAlbumDetails?.Invoke(clickedItem);
                }
            }
        }

        private async void mnftAddToFavourites_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.DataContext is SongModel songModel)
            {
                songModel.IsFavourite = !songModel.IsFavourite;
                songModel.FavOpacity = songModel.IsFavourite ? 1 : 0;
                songModel.FavString = songModel.IsFavourite ? "Remove from Favourites" : "Add to Favourites";

                var settings = await SettingsHelper.LoadSettingsAsync();
                var favourites = settings.Favourites;
                var alreadyexisting = favourites.FirstOrDefault(f => f.FilePath == songModel.FilePath);
                if (alreadyexisting != null)
                {
                    favourites.Remove(alreadyexisting);
                }
                else
                {
                    if (songModel.FilePath != null)
                    {
                        favourites.Add(new FavouritesModel { FilePath = songModel.FilePath });
                    }
                }
                await SettingsHelper.SaveSettingsAsync(settings);
            }
        }
        private SongModel? GetSelectedSong(object sender)
        {
            return (sender as MenuFlyoutItem)?.DataContext as SongModel;
        }
        private void mnftMoveup_Click(object sender, RoutedEventArgs e)
        {
            var song = GetSelectedSong(sender);
            if (song == null) return;

            int index = ItemsSource.IndexOf(song);
            if (index <= 0) return;

            ItemsSource.Move(index, index - 1);
        }

        private void mnftMovedown_Click(object sender, RoutedEventArgs e)
        {
            var song = GetSelectedSong(sender);
            if (song == null) return;

            int index = ItemsSource.IndexOf(song);
            if (index >= ItemsSource.Count - 1) return;

            ItemsSource.Move(index, index + 1);
        }

        private void mnftMovetotop_Click(object sender, RoutedEventArgs e)
        {
            var song = GetSelectedSong(sender);
            if (song == null) return;

            int index = ItemsSource.IndexOf(song);
            if (index <= 0) return;

            ItemsSource.Move(index, 0);
        }

        private void mnftMovetobottom_Click(object sender, RoutedEventArgs e)
        {
            var song = GetSelectedSong(sender);
            if (song == null) return;

            int index = ItemsSource.IndexOf(song);
            if (index == ItemsSource.Count - 1) return;

            ItemsSource.Move(index, ItemsSource.Count - 1);
        }

        private void btnGlyph_Click(object sender, RoutedEventArgs e)
        {
            if (sender is HyperlinkButton button && button.DataContext is SongModel song)
            {
                if (song.isPlaying )
                {
                    if (song.isPaused)
                    {
                        PlayerService.Play();
                    }
                    else
                    {
                        PlayerService.Pause();
                    }
                }
                
                else
                {
                    song.Glyph = "\uE769";
                    PlaySelection(song);
                }
            }
        }

        private void hypTitle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is HyperlinkButton hyperlink && hyperlink.DataContext is SongModel song)
            {
                PlaySelection(song);
            }
        }

        private void hypArtist_Click(object sender, RoutedEventArgs e)
        {
            if (sender is HyperlinkButton hyperlink && hyperlink.DataContext is SongModel song)
            {
                ShowArtistDetails?.Invoke(song);
            }
        }

        private void hypAlbum_Click(object sender, RoutedEventArgs e)
        {
            if (sender is HyperlinkButton hyperlink && hyperlink.DataContext is SongModel song)
            {
                ShowAlbumDetails?.Invoke(song);
            }
        }

        private async void btnFavourite_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var song = btn?.DataContext as SongModel;
            if (btn == null) return;
            if (song != null)
            {
                song.IsFavourite = !song.IsFavourite;

                var settings = await SettingsHelper.LoadSettingsAsync();
                var favourites = settings.Favourites;
                var alreadyexisting = favourites.FirstOrDefault(f => f.FilePath == song.FilePath);
                if (alreadyexisting != null)
                {
                    favourites.Remove(alreadyexisting);
                    song.FavOpacity = 0;
                    song.FavString = "Add to Favourites";
                }
                else
                {
                    if (song.FilePath != null)
                        favourites.Add(new FavouritesModel { FilePath = song.FilePath });
                    song.FavOpacity = 1;
                    song.FavString = "Remove from Favourites";
                }
                await SettingsHelper.SaveSettingsAsync(settings);


                var fillHeart = btn.FindName("FillHeart") as FontIcon;
                if (fillHeart == null) return;
                if (song.IsFavourite)
                    AnimateHeart.AnimateHeartIcon(fillHeart, 1.0, 1.0);
                else
                    AnimateHeart.AnimateHeartIcon(fillHeart, 0.0, 0.0);
            }
        }

        private void btnRemoveSelectionsConfirm_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = lstViewPlaylist.SelectedItems.Cast<SongModel>().ToList();

            foreach (var item in selectedItems)
            {
                ItemsSource.Remove(item);
            }
            if (btnRemoveSelections.Flyout is Flyout f)
            {
                f.Hide();
            }
        }
        private void ShowEditOptionsForMultiple()
        {
            if (App.HomeWindowInstance == null) return;
            OceanContentDialog.Show("Properties", "Save", "", "Cancel", OceanContentDialogDefault.Primary, grdMassEdit, this.XamlRoot, 800, 800, OceanContentDialogType.Elevated, App.HomeWindowInstance, "saveicon", "", "");
            OceanContentDialog.PrimaryRequested += OceanContentDialog_PrimaryRequested1;

            lstViewEdit.ItemsSource = ItemsSource;
            lstViewEdit.SelectedItems.Clear();
            foreach (var item in lstViewPlaylist.SelectedItems)
            {
                lstViewEdit.SelectedItems.Add(item);
            }
            txtEditArtist.Text = ItemsSource[0].Artist;
            txtEditAlbum.Text = ItemsSource[0].AlbumName;
        }
        private void btnEditAlbumMass_Click(object sender, RoutedEventArgs e)
        {
            tbviAlbum.IsSelected = true;
            ShowEditOptionsForMultiple();
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
                        if (item.FilePath == null) continue;
                        if (FileReady.IsFileReady(item.FilePath))
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
                        if (item.FilePath == null) continue;
                        if (FileReady.IsFileReady(item.FilePath))
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

                }
            }

        }


        private void btnEditArtistMass_Click(object sender, RoutedEventArgs e)
        {
            tbviArtist.IsSelected = true;
            ShowEditOptionsForMultiple();
        }

        private void btnAddtoPlaylistMass_Click(object sender, RoutedEventArgs e)
        {
            tbviAddToPlaylist.IsSelected = true;
            ShowEditOptionsForMultiple();
        }

        private void btnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            lstViewPlaylist.SelectAll();
        }

        private void btnClearSelection_Click(object sender, RoutedEventArgs e)
        {
            lstViewPlaylist.SelectedItems.Clear();
        }

        private void btnRemoveSelectionsFromFavourites_Click(object sender, RoutedEventArgs e)
        {
            //Later
        }

        private void mnftSelectitem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.DataContext is SongModel songModel)
            {
                lstViewEdit.SelectedItems.Add(songModel);
            }
        }

        private void mnftUnselectItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.DataContext is SongModel songModel)
            {
                lstViewEdit.SelectedItems.Remove(songModel);
            }
        }

        private void mnftSetAlbumName_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.DataContext is SongModel songModel)
            {
                txtEditAlbum.Text = songModel.AlbumName;
            }
        }

        private void mnftSetArtistName_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.DataContext is SongModel songModel)
            {
                txtEditArtist.Text = songModel.Artist;
            }
        }

        private void btnFixFile_Click(object sender, RoutedEventArgs e)
        {
            //Later
        }

        private async void tbViewEdit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tbviAddToPlaylist.IsSelected)
            {
                var currentSettings = await SettingsHelper.LoadSettingsAsync();
                var Playlists = currentSettings.SavedPlaylists;
                foreach (var item in Playlists)
                {
                    if (item == null) return;
                    List<string> playlistitems = new();
                    if (item.PlaylistName != null)
                        playlistitems!.Add(item.PlaylistName);
                    lstViewAddToPlaylists.ItemsSource = playlistitems;

                }
            }
        }

        private void btnCreateNewPlaylistUnderEdit_Click(object sender, RoutedEventArgs e)
        {
            //Later
        }

        private void lstViewAddToPlaylists_ItemClick(object sender, ItemClickEventArgs e)
        {
            //Later
        }

        private void btnAddtoselectedPl_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnSortItems_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftSortName_Click(object sender, RoutedEventArgs e)
        {
            var sorted = this.ItemsSource.OrderBy(p => p.Title).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                var oldIndex = this.ItemsSource.IndexOf(sorted[i]);
                var newIndex = i;

                if (oldIndex != newIndex)
                {
                    this.ItemsSource.Move(oldIndex, newIndex);
                }
            }
        }

        private void mnftSortDuration_Click(object sender, RoutedEventArgs e)
        {
            var sorted = this.ItemsSource.OrderBy(p => p.SongDuration).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                var oldIndex = this.ItemsSource.IndexOf(sorted[i]);
                var newIndex = i;

                if (oldIndex != newIndex)
                {
                    this.ItemsSource.Move(oldIndex, newIndex);
                }
            }
        }

 

        private void mnftSortbyArtist_Click(object sender, RoutedEventArgs e)
        {
            var sorted = this.ItemsSource.OrderBy(p => p.Artist).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                var oldIndex = this.ItemsSource.IndexOf(sorted[i]);
                var newIndex = i;

                if (oldIndex != newIndex)
                {
                    this.ItemsSource.Move(oldIndex, newIndex);
                }
            }
        }

        private void mnftSortbyAlbum_Click(object sender, RoutedEventArgs e)
        {
            var sorted = this.ItemsSource.OrderBy(p => p.AlbumName).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                var oldIndex = this.ItemsSource.IndexOf(sorted[i]);
                var newIndex = i;

                if (oldIndex != newIndex)
                {
                    this.ItemsSource.Move(oldIndex, newIndex);
                }
            }
        }

        private void mnftSortbyDateMod_Click(object sender, RoutedEventArgs e)
        {
            var sorted =ItemsSource.OrderByDescending(p => p.DateModified).ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                var oldIndex = ItemsSource.IndexOf(sorted[i]);
                if (oldIndex != i)
                {
                    ItemsSource.Move(oldIndex, i);
                }
            }
        }

        private void mnftSortbyDateCreated_Click(object sender, RoutedEventArgs e)
        {
            var sorted = ItemsSource.OrderByDescending(p => p.DateCreated).ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                var oldIndex = ItemsSource.IndexOf(sorted[i]);
                if (oldIndex != i)
                {
                    ItemsSource.Move(oldIndex, i);
                }
            }
        }
    }
}
