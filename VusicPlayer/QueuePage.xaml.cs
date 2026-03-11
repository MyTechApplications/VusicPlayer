using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicPlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class QueuePage : Page
    {
        public QueuePage()
        {
            InitializeComponent();
        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if(e.Parameter is TransposeMediaDetails MediaPath)
            {
                if (string.IsNullOrEmpty(MediaPath.MediaPath)) return;
                if (!File.Exists(MediaPath.MediaPath)) return;
                txtSongName.Text = Path.GetFileName(MediaPath.MediaPath);
                StorageFile storageFile =
                               await StorageFile.GetFileFromPathAsync(MediaPath.MediaPath);

                MusicProperties props =
                    await storageFile.Properties.GetMusicPropertiesAsync();
                txtAlbum.Content = $"• {props.Album}";
                txtArtist.Content = $"• {props.Artist}";
                TimeSpan duration = props.Duration;
                txtTotalDuration.Text = duration.ToString(@"hh\:mm\:ss");
                txtRunningDuration.Text = MediaPath.CurrentDur;
                btnPlayPause.IsEnabled = true;
                btnPrev.IsEnabled = true;
                btnFav.IsEnabled = true;
                btnShuffle.IsEnabled = true;
                btnVolume.IsEnabled = true;
                sldMain.IsEnabled = true;
                sldVolume.IsEnabled = true;
                btnNext.IsEnabled = true;
            }
        }
        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnPlayPause_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnVolume_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
