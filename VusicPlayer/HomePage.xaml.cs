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
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicPlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        public HomePage()
        {
            InitializeComponent();
            GridContinuePlaying.ItemsSource = MyItems;
            CallValue();

            MyItems.CollectionChanged += MyItems_CollectionChanged;
        }
        private async void CallValue()
        {
            MyItems.Clear();
            var settings = await SettingsHelper.LoadSettingsAsync();
            var currentPlaying = settings.SavedItems;
            foreach (var item in currentPlaying)
            {
                if (item.FilePath != null)
                {
                    item.Thumbnail = await GetFileThumbnailAsync(item.FilePath);
                    MyItems.Add(item);
                }
            }
            UpdateUIState();
        }

        private async void MyItems_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove ||
         e.Action == NotifyCollectionChangedAction.Add ||
         e.Action == NotifyCollectionChangedAction.Move)
            {
                var currentSettings = await SettingsHelper.LoadSettingsAsync();
                currentSettings.SavedItems = MyItems;
                await SettingsHelper.SaveSettingsAsync(currentSettings);
            }
        }

        private bool _isLoading = false;
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

    
        }

        private void UpdateUIState()
        {
            bool hasItems = MyItems.Count > 0;
            txtRecentHeading.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed;
            GridContinuePlaying.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed;
            txtEmptyRecents.Visibility = hasItems ? Visibility.Collapsed : Visibility.Visible;
        }
        // Helper to keep the code clean
        public async Task<BitmapImage> GetFileThumbnailAsync(string path)
        {
            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(path);

                // GetScaledImageAsThumbnailAsync allows for higher resolution than the disk cache
                // Use a larger requested size (e.g., 320 or 640) for better quality
                using var thumbnail = await file.GetScaledImageAsThumbnailAsync(
                    ThumbnailMode.VideosView,
                    320,
                    ThumbnailOptions.UseCurrentScale);

                if (thumbnail != null)
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(thumbnail);
                    return bitmapImage;
                }
            }
            catch { /* Handle errors */ }

            return new BitmapImage(new Uri("ms-appx:///Assets/Placeholder.png"));
        }
        public ObservableCollection<VideoProgress> MyItems { get; set; } = new();
        private List<VideoItem> loadedVideos = new List<VideoItem>();

        private async Task<List<VideoItem>> LoadVideosAsync(string folderPath)
        {
            var folder = await StorageFolder.GetFolderFromPathAsync(folderPath);
            var files = await folder.GetFilesAsync();

            var videoItems = new List<VideoItem>();

            foreach (var file in files)
            {
                if (!file.ContentType.StartsWith("video")) continue;

                var thumb = await file.GetScaledImageAsThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.VideosView, 320,
                    ThumbnailOptions.UseCurrentScale);
                BitmapImage? bitmap = null;
                if (thumb != null)
                {
                    bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(thumb);
                }

                var props = await file.GetBasicPropertiesAsync();

                videoItems.Add(new VideoItem
                {
                    FilePath = file.Path,
                    Thumbnail = bitmap,
                    Size = props.Size,
                    DateModified = file.DateCreated
                });
            }

            return videoItems;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();

            // Required for WinUI 3 Desktop apps
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.m_window);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            folderPicker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            folderPicker.FileTypeFilter.Add("*"); // Must have at least one filter

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                txtFolderName.Text = folder.Name;
                var videos = await LoadVideosAsync(folder.Path);
                loadedVideos = await LoadVideosAsync(folder.Path);

                VideoGrid.ItemsSource = loadedVideos;
            }
        }
        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (VideoGrid.Items != null)
                VideoGrid.ItemsSource = loadedVideos.OrderBy(v => v.FileName).ToList();
        }

        private void MenuFlyoutItem_Click_1(object sender, RoutedEventArgs e)
        {
            if (VideoGrid.Items != null)
                VideoGrid.ItemsSource = loadedVideos.OrderByDescending(v => v.DateModified).ToList();
        }

        private void MenuFlyoutItem_Click_2(object sender, RoutedEventArgs e)
        {
            if (VideoGrid.Items != null)
                VideoGrid.ItemsSource = loadedVideos.OrderByDescending(v => v.Size).ToList();
        }

        private async void VideoGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            var clickedVideo = (VideoItem)e.ClickedItem;

            if (clickedVideo.FilePath != null)
            {
                var settings = await SettingsHelper.LoadSettingsAsync();

                // 2. Try to find if THIS specific file exists in the saved progress
                var savedProgress = settings.SavedItems
                    .FirstOrDefault(i => i.FilePath == clickedVideo.FilePath);

                double startPosition = 0;
                bool isNewVideo = true;

                // 3. If found, override the 0 position with the saved progress
                if (savedProgress != null)
                {
                    startPosition = savedProgress.CurrentDuration;
                    isNewVideo = false; // It's not a fresh start anymore
                    System.Diagnostics.Debug.WriteLine($"Found saved progress for {clickedVideo.FileName}: Resuming at {startPosition}");
                }

                // 4. Open the player with the determined position
                UpdateList();
                var playerWindow = new MainWindow(loadedVideos, clickedVideo.FilePath, startPosition, isNewVideo);
               
                playerWindow.Activate();
                App.SetCurrentMainWindow(playerWindow);

            }
        }
        private void UpdateList()
        {
            if (VideoGrid.ItemsSource is IEnumerable items)
            {
                loadedVideos = items.Cast<VideoItem>().ToList();
            }
        }
        private void btnPlayAll_Click(object sender, RoutedEventArgs e)
        {

        }

        private void VideoGrid_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            UpdateList();
        }

        private void GridContinuePlaying_ItemClick(object sender, ItemClickEventArgs e)
        {
            var clickedVideo = (VideoProgress)e.ClickedItem;

            if (clickedVideo.FilePath != null)
            {
                UpdateList();
                var playerWindow = new MainWindow(loadedVideos, clickedVideo.FilePath, clickedVideo.CurrentDuration, false);
              
                playerWindow.Activate();
                App.SetCurrentMainWindow(playerWindow);
            }

        }

        private void GridContinuePlaying_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {

        }

        private async void MenuFlyoutItem_Click_3(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is VideoProgress itemToRemove)
            {
                // 1. Remove from UI
                MyItems.Remove(itemToRemove);

                // 2. Save the ACTUAL current state to disk
                // Load the full settings first to make sure we don't overwrite other data
                var settings = await SettingsHelper.LoadSettingsAsync();

                // Find the specific item in the settings list and remove it
                var target = settings.SavedItems.FirstOrDefault(i => i.FilePath == itemToRemove.FilePath);
                if (target != null)
                {
                    settings.SavedItems.Remove(target);
                    await SettingsHelper.SaveSettingsAsync(settings);
                }

                // 3. Toggle empty state if needed
                if (MyItems.Count == 0)
                {
                    txtRecentHeading.Visibility = Visibility.Collapsed;
                    GridContinuePlaying.Visibility = Visibility.Collapsed;
                    txtEmptyRecents.Visibility = Visibility.Visible;
                }
            }
        }
        }
}
