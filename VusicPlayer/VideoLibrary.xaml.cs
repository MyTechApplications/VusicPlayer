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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
    public sealed partial class VideoLibrary : Page
    {
        public ObservableCollection<VideoProgress> VideoRecents { get; set; } = new();
        public ObservableCollection<VideoProgress> VideoFavourites { get; set; } = new();

        public VideoLibrary()
        {
            InitializeComponent();
            LoadItems();
        }
        public async Task<BitmapImage> GetFileThumbnailAsync(string path)
        {
            // Define your fallback asset
            Uri fallbackUri = new Uri("ms-appx:///Assets/default.png");

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
                Logger.Log($"Thumbnail extraction failed: {ex.Message}", "HomePage", Logger.LogLevelType.Error);
            }

            // If everything fails, return the app icon
            return new BitmapImage(fallbackUri);
        }

        private async void LoadItems()
        {
            //Load Continue Watching
            //Load Recents
            VideoRecents.Clear();
            var settings = await SettingsHelper.LoadSettingsAsync();
            var currentPlaying = settings.SavedItems;
            foreach (var item in currentPlaying)
            {
                if (item.FilePath != null)
                {
                    var thumbnail = await GetFileThumbnailAsync(item.FilePath);



                    item.Thumbnail = thumbnail;
                    VideoRecents.Add(item);
                }
            }
            UpdateUIState(grdViewRecents, VideoRecents, cntctrlEmptyRecents, "You have no recents. Watch videos to show them here.", "/Assets/recentsicon.png");
            UpdateUIState(grdViewFav, VideoFavourites, cntctrlEmptyFav, "You have no favourites. Heart videos to show them here.", "/Assets/favicon.png");
            //Load Favourites

        }
        private void UpdateUIState(GridView grd, ObservableCollection<VideoProgress>listitems, ContentControl cntctrl, string message, string imagePath)
        {
            bool hasItems = listitems.Count > 0;
            grd.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed;
            cntctrl.Visibility = Visibility.Visible;

            // Reach into the loaded template to find the specific controls
            var panel = cntctrl.Content as StackPanel;
            if (panel != null)
            {
                var textBlock = panel.FindName("txtEmptyStuff") as TextBlock;
                var image = panel.FindName("imgEmptyStuff") as Image;

                if (textBlock != null) textBlock.Text = message;
                if (image != null) image.Source = new BitmapImage(new Uri($"ms-appx://{imagePath}"));
            }
        }
        private void grdHeroContent_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            AnimateScale(1.05);
        }
        private void AnimateScale(double Scale)
        {
            var sb = new Storyboard();
            var animX = new DoubleAnimation { To = Scale, Duration = TimeSpan.FromMilliseconds(200) };
            var animY = new DoubleAnimation { To = Scale, Duration = TimeSpan.FromMilliseconds(200) };

            Storyboard.SetTarget(animX, GridScale);
            Storyboard.SetTargetProperty(animX, "ScaleX");
            Storyboard.SetTarget(animY, GridScale);
            Storyboard.SetTargetProperty(animY, "ScaleY");

            sb.Children.Add(animX);
            sb.Children.Add(animY);
            sb.Begin();
        }
        private void grdHeroContent_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            AnimateScale(1.0);
        }

        private void btnOpenVid_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
