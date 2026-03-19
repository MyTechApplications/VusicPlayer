using FlyleafLib.MediaPlayer;
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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Vortice.Direct3D11;
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
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if(App.HomeWindowInstance is HomeWindow wind)
            {
                wind.ReattachUI();
            }
            base.OnNavigatedFrom(e);
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

                sldMain.Maximum = duration.TotalSeconds;
                txtTotalDuration.Text = duration.ToString(@"hh\:mm\:ss");
                PlayerService.AttachUI(txtRunningDuration, sldMain);
                btnPlayPause.IsEnabled = true;
                btnPrev.IsEnabled = true;
                btnFav.IsEnabled = true;
                btnShuffle.IsEnabled = true;
                btnVolume.IsEnabled = true;
                sldMain!.IsEnabled = true;
                sldVolume.IsEnabled = true;
                btnNext.IsEnabled = true;
                var player = PlayerService.MasterPlayer;
                if (player == null) return;
                if (player.IsPlaying)
                {
                    imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/pause.png"));
                }
                else
                {
                    imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/play.png"));
                }
                LoadQueue();
            }
        }
        public async void UpdateQueue(string CurrentMediaPath)
        {
            ChangeCurrent();
        }
        ObservableCollection<string> MainPaths = new();
        private void LoadQueue()
        {
            var listnew = QueueService.queueList;
            if (listnew.MediaPaths == null) return;
            MainPaths = listnew.MediaPaths;
            ChangeCurrent();
        }
        private void ChangeCurrent()
        {

        }
        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {

        }
        private void PlayPauseEvent()
        {
            var player = PlayerService.MasterPlayer;
            if (player == null) return;
            if (player.IsPlaying)
            {
                player.Pause();
                imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/play.png"));
            }
            else
            {
                player.Play();
                imgPlayPause.Source = new BitmapImage(new Uri("ms-appx:///Assets/pause.png"));
            }
        }
        private void btnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            PlayPauseEvent();
        }

        private void btnVolume_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
