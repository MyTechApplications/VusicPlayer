using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
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
    public sealed partial class ArtistInfo : Page
    {
        public ArtistInfo()
        {
            InitializeComponent();
        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is SongModel selectedSongArtist)
            {
                txtArtistName.Text = selectedSongArtist.Artist;
                imgArtist.DisplayName = txtArtistName.Text;
            }
        }

        private void btnRenameArtist_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnPlayAll_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnShuffle_Click(object sender, RoutedEventArgs e)
        {

        }

        private void lstViewPlaylist_ItemClick(object sender, ItemClickEventArgs e)
        {

        }

        private void lstViewPlaylist_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {

        }

        private void mnftPlaySong_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mnftSongDetails_Click(object sender, RoutedEventArgs e)
        {

        }

        private void removesongfromplaylistcreation_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnEditArtistInfo_Click(object sender, RoutedEventArgs e)
        {

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
    }
}
