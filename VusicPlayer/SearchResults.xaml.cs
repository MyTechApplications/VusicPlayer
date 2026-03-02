using LibVLCSharp.Shared;
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicPlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SearchResults : Page
    {
        public SearchResults()
        {
            InitializeComponent();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Results.Clear();

            // CASE A: We received a List of songs (Partial match/Enter pressed)
            if (e.Parameter is List<SongModel> songList)
            {
                foreach (var song in songList)
                {
                    Results.Add(song);
                }
            }
            // CASE B: We received a single SongModel (Suggestion clicked)
            else if (e.Parameter is SongModel singleSong)
            {
                Results.Add(singleSong);
            }

            // UPDATE UI VISIBILITY
            if (Results.Count > 0)
            {
                lstSearchResults.Visibility = Visibility.Visible;
                txtEmptySearchResults.Visibility = Visibility.Collapsed;

                // Re-bind the ItemsSource
                lstSearchResults.ItemsSource = Results;
            }
            else
            {
                lstSearchResults.Visibility = Visibility.Collapsed;
                txtEmptySearchResults.Visibility = Visibility.Visible;
            }
       
        }
          
        
        public ObservableCollection<SongModel> Results { get; set; } = new();
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            // 2. The 'DataContext' of the menu item IS the SongModel for that row
            var selectedsong = button?.DataContext as SongModel;
            if (selectedsong.FilePath != PlaybackState.CurrentlyPlayingPath)
            {
                return; // Do absolutely nothing
            }
            else
            {
                if (selectedsong.Glyph == "\uE769")
                {
                    //if playing, then pause 
                    if (App.MainWindowInstance is HomeWindow homeWindow)
                    {
                        homeWindow.PlayPausePublic("playing");
                    }
                    selectedsong.Glyph = "\uE768";
                }
                else if (selectedsong.Glyph == "\uE768")
                {
                    if (App.MainWindowInstance is HomeWindow homeWindow)
                    {
                        homeWindow.PlayPausePublic("paused");
                    }
                    //if paused, then play
                    selectedsong.Glyph = "\uE769";
                }
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
                    homeWindow.LoadFileFromPath(paths);

                }
            }
        }
        TimeSpan ts;
        SongModel selectedSong = new();
        private void txtTitle_Click(object sender, RoutedEventArgs e)
        {
            var selectedsong = (sender as FrameworkElement).DataContext as SongModel;
            if (selectedsong != null)
            {
                selectedSong = selectedsong;
                PlaySelection();
            }
            UpdatePlaylistPlayState();
        }
        private async void UpdatePlaylistPlayState()
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

        private void txtAlbumHyp_Click(object sender, RoutedEventArgs e)
        {
            GoToAlbum(sender);
        }

        // --- Context Menu Actions (MenuFlyout) ---
        private void GoToAlbum(object sender)
        {

            if (sender is FrameworkElement clickedElement)
            {
                // 2. Extract the DataContext (your SongModel)
                if (clickedElement.DataContext is SongModel clickedItem)
                {
                    // 3. Navigate to the Album page
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


        private void mnftSongDetails_Click(object sender, RoutedEventArgs e)
        {
            var song = GetSongFromMenu(sender);
            var menuItem = sender as MenuFlyoutItem;
            var selectedsong = menuItem?.DataContext as SongModel;
            // Since multiple items use this click, check the Name
            switch (menuItem.Name)
            {
                case "mnftSongDetails":
                    if (App.MainWindowInstance is HomeWindow wind)
                    {
                        wind.ShowSongDetails(selectedsong.FilePath);
                    }
                    break;
                case "mnftAddtoPlaylist":
                    // Add to playlist logic
                    break;
                case "mnftAddtoQueue":
                    // Add to queue logic
                    break;
                case "mnftGoToArtist":

                        this.Frame.Navigate(typeof(ArtistInfo), selectedsong);
                    
                    break;
                case "mnftAddToFavourites":
                    // Add to favorites logic
                    break;
                case "mnftMoveup":
                    // Logic to move item up in the list
                    break;
                case "mnftMovedown":
                    // Logic to move item down in the list
                    break;
                    // ... and so on
            }
        }

        private void mnftEditInfo_Click(object sender, RoutedEventArgs e)
        {
            var song = GetSongFromMenu(sender);
            // Open tag editor
        }

        private void mnftGoToAlbum_Click(object sender, RoutedEventArgs e)
        {
            GoToAlbum(sender);
        }

        // --- Helper Helper ---

        private SongModel GetSongFromMenu(object sender)
        {
            // In WinUI 3, the DataContext of the MenuFlyoutItem is inherited 
            // from the Grid the Flyout is attached to.
            return (sender as MenuFlyoutItem)?.DataContext as SongModel;
        }
    }
}
