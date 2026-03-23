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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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
            });
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



            MenuFlyoutItem playlistitem2 = new MenuFlyoutItem();
            playlistitem2.Text = "New Playlist";
            FontIcon fnticon = new FontIcon();
            fnticon.Glyph = "\uE710";
            playlistitem2.Icon = fnticon;
            playlistitem2.Click += async (sender, e) =>
            {
                if (App.HomeWindowInstance == null) return;
            
            
                tempsong = selectedsong;
                List<string> songpathstoadd = new();
                if (selectedsong.FilePath != null)
                {
                    songpathstoadd.Add(selectedsong.FilePath);
                }
                if (frm != null)
                {
                    PlaylistDialogNew.LoadPlaylistCreationDialog(false, new PlaylistProperties { SongsPaths = songpathstoadd }, frm);
                }
                OceanContentDialog.Show("Create Playlist", "Create", "", "Cancel", OceanContentDialogDefault.Primary, contentsNewPlaylist, this.XamlRoot, 600, 760, OceanContentDialogType.Elevated, App.HomeWindowInstance, "addicon", "", "");
                OceanContentDialog.PrimaryRequested += OceanContentDialog_PrimaryRequested1;
            };
            addToPlaylist?.Items.Add(playlistitem2);


        }
        SongModel tempsong;
        private async void OceanContentDialog_PrimaryRequested1()
        {
            OceanContentDialog.HideDlg();
            HomeWindow.ShowWindow();
            PlaylistDialogNew.SavePlaylist();
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
                wind.ShowSongDetails(selectedsong.FilePath);
            }
        }

        private void mnftAddtoPlaylist_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void mnftAddtoQueue_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftEditInfo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftGoToArtist_Click(object sender, RoutedEventArgs e)
        {
            var clickedArtist = sender as HyperlinkButton;

            var clickedItem = clickedArtist?.DataContext as SongModel;
            if (clickedArtist != null)
            {

                frm?.Navigate(typeof(ArtistInfo), clickedItem);
            }
        }

        private void mnftGoToAlbum_Click(object sender, RoutedEventArgs e)
        {
            GoToAlbum(sender);
        }

        private void mnftAddToFavourites_Click(object sender, RoutedEventArgs e)
        {

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
          if(btnRemoveSelections.Flyout is Flyout f)
            {
                f.Hide();
            }
        
        }
        public IList<object> SelectedItems => lstViewPlaylist.SelectedItems;
        private void lstViewPlaylist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(lstViewPlaylist.SelectedItems.Count > 0)
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
            else if(tbviArtist.IsSelected == true)
            {
                foreach (SongModel item in lstViewPlaylist.SelectedItems)
                {
                    try
                    {
                        item.Artist = txtEditArtist.Text;
                        var file = TagLib.File.Create(item.FilePath);
                        file.Tag.AlbumArtists = new[] { txtEditArtist.Text};

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
                    List<string> playlistitems = new ();
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
        private void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            // 1. Get the Grid inside the Button's Content
            var rootGrid = btn.Content as Grid;
            if (rootGrid == null) return;

            // 2. Find the FillHeart icon by name within this specific Button
            var fillHeartIcon = rootGrid.FindName("FillHeart") as FontIcon;
            if (fillHeartIcon == null) return;
            // Note: Since 'isChecked' is likely a local variable, 
            // it will reset every click. Usually, you'd check the current state:
            bool currentlyChecked = fillHeartIcon.Opacity > 0;

            if (!currentlyChecked)
            {
                ToolTipService.SetToolTip(btn, "Remove from Favourites");

                // Pass the specific icon we found to your animation method
                AnimateHeart(fillHeartIcon, 1.0, 1.0);
            }
            else
            {
                ToolTipService.SetToolTip(btn, "Add to Favourites");

                AnimateHeart(fillHeartIcon, 0.0, 0.0);
            }
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
    }
}
