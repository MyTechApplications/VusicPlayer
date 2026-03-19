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
using System.Drawing.Text;
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
            try
            {
                InitializeComponent();

                string root = AppContext.BaseDirectory;
                string filePath = Path.Combine(root, "freeupdate.txt");
                //        Logger.Log(filePath, "source", Logger.LogLevelType.Information);
                if (File.Exists(filePath))
                {
                    //ttUpdated.Visibility = Visibility.Visible;
                    //            Logger.Log(filePath + "23", "source", Logger.LogLevelType.Information);

                }
                GridContinuePlaying.ItemsSource = MyItems;
                CallValue();
                CallFolderValue();
                MyItems.CollectionChanged += MyItems_CollectionChanged;
                folders2.CollectionChanged += Folders2_CollectionChanged;
                if (FolderGrid.Items.Count == 0)
                {
                    txtEmptyFolders.Visibility = Visibility.Visible;
                    txtFoldersHeader.Visibility = Visibility.Collapsed;
                }
                else
                {
                    txtEmptyFolders.Visibility = Visibility.Collapsed;
                    txtFoldersHeader.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, "HomePage", Logger.LogLevelType.Error);
            }
        }

        private async void Folders2_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove ||
            e.Action == NotifyCollectionChangedAction.Add ||
            e.Action == NotifyCollectionChangedAction.Move)
            {
                var currentSettings = await SettingsHelper.LoadSettingsAsync();
                currentSettings.FoldersRecent = folders2;
                await SettingsHelper.SaveSettingsAsync(currentSettings);
                if (FolderGrid.Items.Count == 0)
                {
                    txtEmptyFolders.Visibility = Visibility.Visible;
                    txtFoldersHeader.Visibility = Visibility.Collapsed;
                }
                else
                {
                    txtEmptyFolders.Visibility = Visibility.Collapsed;
                    txtFoldersHeader.Visibility = Visibility.Visible;
                }
            }
        }

        ObservableCollection<FolderModel> folders2 = new();
        private async void CallValue()
        {
            MyItems.Clear();
            var settings = await SettingsHelper.LoadSettingsAsync();
            var currentPlaying = settings.SavedItems;
            foreach (var item in currentPlaying)
            {
                if (item.FilePath != null)
                {
                    var thumbnail = await GetFileThumbnailAsync(item.FilePath);



                    item.Thumbnail = thumbnail;
                    MyItems.Add(item);
                }
            }
            UpdateUIState();
        }
        private async void CallFolderValue()
        {
            folders2.Clear();
            var settings = await SettingsHelper.LoadSettingsAsync();
            var currentPlaying = settings.FoldersRecent;
            foreach (var item in currentPlaying)
            {
                if (item.Path != null)
                {
                    var thumbnail = await GetFolderThumbnailAsync(item.Path);

                    // If it's null or empty, use default
                    if (thumbnail == null)
                    {
                        // Build path to default image in your project folder
                        var exeFolder = AppContext.BaseDirectory; // folder where your app .exe is
                        var defaultPath = Path.Combine(exeFolder, "Assets", "folder.png");

                        // Load the default image
                        var bitmap = new BitmapImage();
                        using (var stream = File.OpenRead(defaultPath))
                        {
                            await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
                        }

                        thumbnail = bitmap;
                    }

                    item.Thumbnail = thumbnail;
                    folders2.Add(item);
                }
            }
            FolderGrid.ItemsSource = folders2;
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
        public async Task<BitmapImage> GetFolderThumbnailAsync(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);

                    // Get a thumbnail for the folder
                    using var thumbnail = await folder.GetThumbnailAsync(
                        ThumbnailMode.ListView, // or ThumbnailMode.DocumentsView
                        320,
                        ThumbnailOptions.UseCurrentScale);

                    if (thumbnail != null) // some extra safety
                    {
                        BitmapImage bitmapImage = new BitmapImage();
                        await bitmapImage.SetSourceAsync(thumbnail);
                        return bitmapImage;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Unexpected Error in loading folder thumbnail: " + ex.Message, "HomePage", Logger.LogLevelType.Error);
            }

            // Fallback for unpackaged app
            var exeFolder = AppContext.BaseDirectory;
            var defaultPath = Path.Combine(exeFolder, "Assets", "foldericon.png");

            BitmapImage defaultBitmap = new BitmapImage();
            using (var stream = File.OpenRead(defaultPath))
            {
                await defaultBitmap.SetSourceAsync(stream.AsRandomAccessStream());
            }

            return defaultBitmap;
        }

        public ObservableCollection<VideoProgress> MyItems { get; set; } = new();
        private ObservableCollection<VideoItem> loadedVideos = new ObservableCollection<VideoItem>();

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
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            folderPicker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            folderPicker.FileTypeFilter.Add("*"); // Must have at least one filter

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                txtFolderName.Text = folder.Name;
                //        var videos = await LoadVideosAsync(folder.Path);
                //    loadedVideos = await LoadVideosAsync(folder.Path);
                var FolderProp = new FolderModel { Name = txtFolderName.Text, Path = folder.Path, };
                if (FolderProp != null)
                {
                    this.Frame.Navigate(typeof(FoldersPage), FolderProp);

                }

                //           FolderGrid.ItemsSource = loadedVideos;
            }
        }
        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (FolderGrid.Items != null)
                FolderGrid.ItemsSource = loadedVideos.OrderBy(v => v.FileName).ToList();
        }

        private void MenuFlyoutItem_Click_1(object sender, RoutedEventArgs e)
        {
            if (FolderGrid.Items != null)
                FolderGrid.ItemsSource = loadedVideos.OrderByDescending(v => v.DateModified).ToList();
        }

        private void MenuFlyoutItem_Click_2(object sender, RoutedEventArgs e)
        {
            if (FolderGrid.Items != null)
                FolderGrid.ItemsSource = loadedVideos.OrderByDescending(v => v.Size).ToList();
        }

        private async void FolderGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            var folderr = (FolderModel)e.ClickedItem;
            if (Directory.Exists(folderr.Path))
            {
                this.Frame.Navigate(typeof(FoldersPage), folderr);
            }
            else
            {
                cldg.Title = "Missing Folder";
                cldgContent.Text = $"This folder is missing:  {folderr.Path + Environment.NewLine}";

                cldg.DefaultButton = ContentDialogButton.Primary;
                ToolTipService.SetToolTip(cldgContent, folderr.Path);
                cldg.CloseButtonText = "OK";
                await cldg.ShowAsync();
                folders2.Remove(folderr);

            }
        }

        private void btnPlayAll_Click(object sender, RoutedEventArgs e)
        {

        }

        private void FolderGrid_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
        }

        private async void GridContinuePlaying_ItemClick(object sender, ItemClickEventArgs e)
        {
            var clickedVideo = (VideoProgress)e.ClickedItem;
            prg = clickedVideo;
            if (File.Exists(clickedVideo.FilePath))
            {
                if (clickedVideo.FilePath != null)
                {
                    var playerWindow = new MainWindow(loadedVideos, clickedVideo.FilePath, clickedVideo.CurrentDuration, false);

                    playerWindow.Activate();
                    App.VideoPlayerWindowInstance = playerWindow;
                    HomeWindow.HideWindow();

                }
            }
            else
            {
                cldg.Title = "Missing File";
                cldgContent.Text = $"This file is missing:  {clickedVideo.FilePath + Environment.NewLine} You can relocate its path or remove it from continue watching";
                cldg.PrimaryButtonText = "Relocate";
                cldg.DefaultButton = ContentDialogButton.Primary;
                ToolTipService.SetToolTip(cldgContent, clickedVideo.FilePath);
                cldg.CloseButtonText = "Remove from continue watching";
                cldg.PrimaryButtonClick += Cldg_PrimaryButtonClick;
                cldg.CloseButtonClick += Cldg_CloseButtonClick;
                await cldg.ShowAsync();
            }
        }

        private async void Cldg_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            MyItems.Remove(prg);

            // 2. Save the ACTUAL current state to disk
            // Load the full settings first to make sure we don't overwrite other data
            var settings = await SettingsHelper.LoadSettingsAsync();

            // Find the specific item in the settings list and remove it
            var target = settings.SavedItems.FirstOrDefault(i => i.FilePath == prg.FilePath);
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

        private void Cldg_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void GridContinuePlaying_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {

        }
        VideoProgress prg = new();
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

        private async void MenuFlyoutItem_Click_4(object sender, RoutedEventArgs e)
        {
            //Remove folder item
            if (sender is FrameworkElement element && element.DataContext is FolderModel itemToRemove)
            {
                // 1. Remove from UI
                folders2.Remove(itemToRemove);

                // 2. Save the ACTUAL current state to disk
                // Load the full settings first to make sure we don't overwrite other data
                var settings = await SettingsHelper.LoadSettingsAsync();

                // Find the specific item in the settings list and remove it
                var target = settings.FoldersRecent.FirstOrDefault(i => i.Path == itemToRemove.Path);
                if (target != null)
                {
                    settings.FoldersRecent.Remove(target);
                    await SettingsHelper.SaveSettingsAsync(settings);
                }

                // 3. Toggle empty state if needed
                if (folders2.Count == 0)
                {
                    txtEmptyFolders.Visibility = Visibility.Visible;
                    txtFoldersHeader.Visibility = Visibility.Collapsed;
                }
            }
        }

        private async void MenuFlyoutItem_Click_5(object sender, RoutedEventArgs e)
        {
            //Open folder
            var menuflyoutitme = sender as MenuFlyoutItem;
            if (menuflyoutitme == null) return;
            var data = menuflyoutitme.DataContext as FolderModel;
            if (data == null) return;
            if (Directory.Exists(data.Path))
            {
                this.Frame.Navigate(typeof(FoldersPage), data);
            }
            else
            {
                cldg.Title = "Missing Folder";
                cldgContent.Text = $"This folder is missing:  {data.Path + Environment.NewLine}";
                cldg.DefaultButton = ContentDialogButton.Primary;
                ToolTipService.SetToolTip(cldgContent, data.Path);
                cldg.CloseButtonText = "OK";
                await cldg.ShowAsync();
                folders2.Remove(data);
            }
        }

        private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            string root = AppContext.BaseDirectory;
            string filePath = Path.Combine(root, "freeupdate.txt");
            if (App.HomeWindowInstance == null) return;
            OceanContentDialog.Show($"What's New in Version {Appversionstrings.AppVersion}", "", "", "OK", OceanContentDialogDefault.Primary, grdNewUpdates, this.XamlRoot, 600, 600, OceanContentDialogType.Elevated, App.HomeWindowInstance, "", "", "");
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, "HomeWindow", Logger.LogLevelType.Error);
            }

        }

        private void MenuFlyoutItem_Click_6(object sender, RoutedEventArgs e)
        {

        }

        private void MenuFlyoutItem_Click_7(object sender, RoutedEventArgs e)
        {

        }
        private static readonly string[] AudioExtensions = { ".mp3", ".wav", ".ogg", ".m4a", ".aac", ".wma", ".flac", ".ac3", ".alac", ".aiff", ".opus", ".ape", ".wv", ".tta", ".dsf", ".dff", ".mp2", ".amr", ".au", ".snd", ".mka" };
        private static readonly string[] VideoExtensions = { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".ts", ".m2ts", ".mts", ".3gp", ".3g2", ".f4v", ".mpg", ".mpeg", ".vob", ".asf", ".rm", ".rmvb", ".ogv" };
        public async Task OpenFilePicker()
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance ?? App.CurrentActiveWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            // Add everything to the picker automatically
            AudioExtensions.ToList().ForEach(picker.FileTypeFilter.Add);
            VideoExtensions.ToList().ForEach(picker.FileTypeFilter.Add);

            var file = await picker.PickSingleFileAsync();
            if (file == null) return;

            string ext = Path.GetExtension(file.Path).ToLower();

            if (VideoExtensions.Contains(ext))
            {
                var playerWindow = new MainWindow(new ObservableCollection<VideoItem> { new VideoItem { FilePath = file.Path } }, file.Path, 0, true);
                playerWindow.Activate(); 
                App.SetCurrentMainWindow(playerWindow);

                App.VideoPlayerWindowInstance = playerWindow;

                HomeWindow.HideWindow();



                return;
            }
            else if (AudioExtensions.Contains(ext))
            {
                HomeWindow.ShowWindow().LoadFileFromPath(new ObservableCollection<string> { file.Path });
            }
        }
        private async void btnOpenMedia_Click(object sender, RoutedEventArgs e)
        {
            await OpenFilePicker();
        }

        private void GridContinuePlaying_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(GridContinuePlaying.SelectedItems.Count != 0)
            {
                btnRemoveFromContinueWatchingSelected.Visibility = Visibility.Visible;
            }
            else
            {
                btnRemoveFromContinueWatchingSelected.Visibility = Visibility.Collapsed;

            }
        }

        private void chckSelectAllContinuePlaying_Checked(object sender, RoutedEventArgs e)
        {
            if(chckSelectAllContinuePlaying.IsChecked == true)
            {
                GridContinuePlaying.SelectAll();
            }

            else
            {
                GridContinuePlaying.SelectedItems.Clear();
            }
        }

        private void chckSelectAllContinuePlaying_Unchecked(object sender, RoutedEventArgs e)
        {
            if (chckSelectAllContinuePlaying.IsChecked == true)
            {
                GridContinuePlaying.SelectAll();
            }

            else
            {
                GridContinuePlaying.SelectedItems.Clear();
            }
        }

        private void chckSelectContinuePlaying_Checked(object sender, RoutedEventArgs e)
        {
            if(chckSelectContinuePlaying.IsChecked == true)
            {
                chckSelectAllContinuePlaying.Visibility = Visibility.Visible;
                GridContinuePlaying.SelectionMode = ListViewSelectionMode.Multiple;
            }
            else
            {
                GridContinuePlaying.SelectionMode = ListViewSelectionMode.Single;
                chckSelectAllContinuePlaying.Visibility = Visibility.Collapsed;

            }
        }

        private void chckSelectContinuePlaying_Unchecked(object sender, RoutedEventArgs e)
        {
            if (chckSelectContinuePlaying.IsChecked == true)
            {
                chckSelectAllContinuePlaying.Visibility = Visibility.Visible;
                GridContinuePlaying.SelectionMode = ListViewSelectionMode.Multiple;
            }
            else
            {
                GridContinuePlaying.SelectionMode = ListViewSelectionMode.Single;
                chckSelectAllContinuePlaying.Visibility = Visibility.Collapsed;

            }
        
        }

        private void btnRemoveFromContinueWatchingSelected_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = GridContinuePlaying.SelectedItems.Cast<VideoProgress>().ToList();

            foreach (var item in selectedItems)
            {
                MyItems.Remove(item);
            }
        }

        private void FolderGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridContinuePlaying.SelectedItems.Count != 0)
            {
                btnRemoveFromRecentFoldersSelection.Visibility = Visibility.Visible;
            }
            else
            {

                btnRemoveFromRecentFoldersSelection.Visibility = Visibility.Collapsed;

            }
        }

        private void chckSelectFolders_Checked(object sender, RoutedEventArgs e)
        {
            if (chckSelectFolders.IsChecked == true)
            {
                chckSelectAllFolders.Visibility = Visibility.Visible;
                FolderGrid.SelectionMode = ListViewSelectionMode.Multiple;
            }
            else
            {
                FolderGrid.SelectionMode = ListViewSelectionMode.Single;
                chckSelectAllFolders.Visibility = Visibility.Collapsed;

            }
        }

        private void chckSelectFolders_Unchecked(object sender, RoutedEventArgs e)
        {
            if (chckSelectFolders.IsChecked == true)
            {
                chckSelectAllFolders.Visibility = Visibility.Visible;
                FolderGrid.SelectionMode = ListViewSelectionMode.Multiple;
            }
            else
            {
                FolderGrid.SelectionMode = ListViewSelectionMode.Single;
                chckSelectAllFolders.Visibility = Visibility.Collapsed;

            }
        }

        private void chckSelectAllFolders_Checked(object sender, RoutedEventArgs e)
        {
            if (chckSelectAllFolders.IsChecked == true)
            {
                FolderGrid.SelectAll();
            }

            else
            {
                FolderGrid.SelectedItems.Clear();
            }
        }

        private void btnRemoveFromRecentFoldersSelection_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = FolderGrid.SelectedItems.Cast<FolderModel>().ToList();

            foreach (var item in selectedItems)
            {
                folders2.Remove(item);
            }
        }

        private void chckSelectAllFolders_Unchecked(object sender, RoutedEventArgs e)
        {
            if (chckSelectAllFolders.IsChecked == true)
            {
                FolderGrid.SelectAll();
            }

            else
            {
                FolderGrid.SelectedItems.Clear();
            }
        }
    }
}
