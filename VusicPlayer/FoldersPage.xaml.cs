using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
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
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;
using FileAttributes = System.IO.FileAttributes;
using Path = System.IO.Path;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicPlayer;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class FoldersPage : Page
{
    string folderloadedpath = "";
    public FoldersPage()
    {
        InitializeComponent();
        loadedVideos.CollectionChanged += LoadedVideos_CollectionChanged;

    }

    private void LoadedVideos_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Remove ||
          e.Action == NotifyCollectionChangedAction.Add ||
          e.Action == NotifyCollectionChangedAction.Move)
        {
            if (loadedVideos.Count != 0)
            {
                txtEmptyFolder.Visibility = Visibility.Collapsed;
                txtItems.Text = $"{loadedVideos.Count} item{(loadedVideos.Count > 1 ? "s" : "")}";
            }
            else
            {
                txtEmptyFolder.Visibility = Visibility.Visible;
                txtItems.Text = $"{loadedVideos.Count} item{(loadedVideos.Count > 1 ? "s" : "")}";
            }
        }
    }
    string currentFilePath = "";
    private async void LoadPreviewDetails(string path)
    {
        try
        {
            // Basic FileInfo
            var fileInfo = new FileInfo(path);

            // File name
            PreviewFileName.Text = fileInfo.Name;

            // File path
            PreviewFilePath.Content = fileInfo.FullName;

            // File size (convert bytes to KB/MB)
            double sizeInMB = fileInfo.Length / (1024.0 * 1024.0);
            PreviewSize.Text = $"Size: {sizeInMB:F2} MB";

            // Created & Modified
            PreviewCreated.Text = $"Created: {fileInfo.CreationTime}";
            PreviewModified.Text = $"Modified: {fileInfo.LastWriteTime}";

            // Attributes
            ChkReadOnly.IsChecked = fileInfo.IsReadOnly;
            ChkHidden.IsChecked = (fileInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;

            // Thumbnail
            BitmapImage bitmap = new BitmapImage();

            if (File.Exists(path))
            {
                try
                {
                    // For images, you can load directly
                    if (fileInfo.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                        fileInfo.Extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        fileInfo.Extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
                    {
                        using var stream = File.OpenRead(path);
                        await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
                    }
                    else
                    {
                        // For other files, try getting StorageFile thumbnail
                        StorageFile file = await StorageFile.GetFileFromPathAsync(path);
                        using var thumb = await file.GetScaledImageAsThumbnailAsync(
                            ThumbnailMode.SingleItem, 150, ThumbnailOptions.UseCurrentScale);

                        if (thumb != null)
                            await bitmap.SetSourceAsync(thumb);
                    }
                }
                catch
                {
                    // fallback
                    var exeFolder = AppContext.BaseDirectory;
                    var defaultPath = Path.Combine(exeFolder, "Assets", "Placeholder.png");
                    using var stream = File.OpenRead(defaultPath);
                    await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
                }
            }

            PreviewThumbnail.Source = bitmap;

            // Duration (for video/audio)
            if (fileInfo.Extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase) ||
                fileInfo.Extension.Equals(".mkv", StringComparison.OrdinalIgnoreCase) ||
                fileInfo.Extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(path);
                    var props = await file.Properties.GetVideoPropertiesAsync();
                    if (props != null)
                        PreviewDuration.Text = $"Duration: {props.Duration.Hours:D2}:{props.Duration.Minutes:D2}:{props.Duration.Seconds:D2}";
                }
                catch
                {
                    PreviewDuration.Text = "Duration: N/A";
                }
            }
            else
            {
                PreviewDuration.Text = "";
            }
        }

        catch (Exception ex)
        {
            // Optionally log or error
            PreviewFileName.Text = "Error loading file preview";
        }
    }
    private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {

        if (sender is MenuFlyoutItem mnft &&
        mnft.DataContext is VideoItem data &&
        data.FilePath is not null && data.IsFolder == false)
        {
            //Show Preview
            LoadPreviewDetails(data.FilePath);
            PreviewGrid.Visibility = Visibility.Visible;

            currentFilePath = data.FilePath;
        }
    }

    private async void VideoGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        var clickedItem = (VideoItem)e.ClickedItem;
        if (clickedItem == null) return;
        if (clickedItem.FilePath == null) return;

        if (clickedItem.IsFolder)
        {
            var newFolder = new FolderModel
            {
                Path = clickedItem.FilePath,
                Name = Path.GetFileName(clickedItem.FilePath)
            };
            this.Frame.Navigate(typeof(FoldersPage), newFolder);
        }
        else
        {
            var settings = await SettingsHelper.LoadSettingsAsync();
            var savedProgress = settings.SavedItems.FirstOrDefault(i => i.FilePath == clickedItem.FilePath);

            double startPosition = savedProgress?.CurrentDuration ?? 0;
            bool isNewVideo = savedProgress == null;

            var playerWindow = new MainWindow(loadedVideos, clickedItem.FilePath, startPosition, isNewVideo);
            playerWindow.Activate();
            App.VideoPlayerWindowInstance = playerWindow;
            HomeWindow.HideWindow();

            App.SetCurrentMainWindow(playerWindow);
        }
    }




    private void MenuFlyoutItem_Click_4(object sender, RoutedEventArgs e)
    {

    }

    private async void btnDelete_Click(object sender, RoutedEventArgs e)
    {
        await dlgConfirmDelete.ShowAsync();
    }

    private void dlgConfirmDelete_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {

    }

    private void MenuFlyoutItem_Click_5(object sender, RoutedEventArgs e)
    {

    }

    private void MenuFlyoutItem_Click_6(object sender, RoutedEventArgs e)
    {
        //Rename File
        if (sender is MenuFlyoutItem { DataContext: VideoItem data } && !data.IsFolder)
        {
         //   ttRenameFile.IsOpen = true;
            var container = VideoGrid.ContainerFromItem(data) as GridViewItem;
            if (container != null)
            {
                ttRenameFile.Target = container;
           //     ttRenameFile.IsOpen = true;
                ttRenameFile.PreferredPlacement = TeachingTipPlacementMode.Bottom;
            }
            txtRenameFile.Text = Path.GetFileNameWithoutExtension(data.FilePath);
            originalfilenamee = data.FileName;
            txtRenameFile.Tag = data.FilePath;
        }
    }
    string originalfilenamee = "";
    private void MenuFlyoutItem_Click_7(object sender, RoutedEventArgs e)
    {

    }

    private void MenuFlyoutItem_Click_8(object sender, RoutedEventArgs e)
    {

    }

    private void MenuFlyoutItem_Click_9(object sender, RoutedEventArgs e)
    {

    }

    private void btnProperties_Click(object sender, RoutedEventArgs e)
    {

    }
    public ObservableCollection<FolderModel> folderloaded = new();
    string currentFolderPath = "";
    private List<FolderNode> GetCrumbsFromPath(string fullPath)
    {
        var crumbs = new List<FolderNode>();
        var parts = fullPath.Split(System.IO.Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

        string currentPath = "";
        foreach (var part in parts)
        {
            currentPath = System.IO.Path.Combine(currentPath, part);
            crumbs.Add(new FolderNode { Label = part, Path = currentPath });
        }
        return crumbs;
    }
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // e.Parameter is the 'clickedPlaylist' we sent earlier
        // Keep track globally for this playlist

        if (e.Parameter is FolderModel loadedFolder)
        {
            loadedVideos.Clear();
            loadedVideos = await LoadVideosAsync(loadedFolder.Path);
            currentFolderPath = loadedFolder.Path;
            brdcbFolderPath.ItemsSource = GetCrumbsFromPath(currentFolderPath);
            txtFolderPath.Text = currentFolderPath;
            hypPath.Tag = currentFolderPath;
            await SettingsHelper.LoadSettingsAsync();
            var folders = await SettingsHelper.LoadSettingsAsync();
            var newfolder = new FolderModel
            {
                Name = loadedFolder.Name,
                Path = loadedFolder.Path
            };
            folderloadedpath = loadedFolder.Path;
            bool alreadyExists = folders.FoldersRecent
               .Any(x => x.Path == loadedFolder.Path);

            if (!alreadyExists)
            {
                // Insert at the start of the list
                folders.FoldersRecent.Insert(0, newfolder);
            }

            await SettingsHelper.SaveSettingsAsync(folders);
            if (loadedVideos.Count != 0)
            {
                txtEmptyFolder.Visibility = Visibility.Collapsed;
                txtItems.Text = $"{loadedVideos.Count} item{(loadedVideos.Count > 1 ? "s" : "")}";
            }
            else
            {
                txtEmptyFolder.Visibility = Visibility.Visible;
                txtItems.Text = $"{loadedVideos.Count} item{(loadedVideos.Count > 1 ? "s" : "")}";
            }
        }
        try
        {
            VideoGrid.ItemsSource = loadedVideos;
        }
        catch (Exception ex)
        {
            Logger.Log(ex.Message, "FoldersPage", Logger.LogLevelType.Error);
        }
    }
    private async Task<ObservableCollection<VideoItem>> LoadVideosAsync(string folderPath)
    {
        var items = new ObservableCollection<VideoItem>();
        if (!Directory.Exists(folderPath)) return items;
        try
        {
            var folder = await StorageFolder.GetFolderFromPathAsync(folderPath);

            // 1. Load Subfolders as Items
            var subFolders = await folder.GetFoldersAsync();
            foreach (var sub in subFolders)
            {
                items.Add(new VideoItem
                {
                    FilePath = sub.Path,
                    IsFolder = true,
                    // You'll need a folder icon in your Assets
                    Thumbnail = new BitmapImage(new Uri("ms-appx:///Assets/foldericon.png")),
                    DateModified = sub.DateCreated
                });
            }

            // 2. Load Video Files (Non-recursive)
            var files = await folder.GetFilesAsync();
            foreach (var file in files)
            {
                if (!file.ContentType.StartsWith("video")) continue;

                BitmapImage bitmap = new BitmapImage();
                try
                {
                    // 1. Get the thumbnail
                    using var thumb = await file.GetScaledImageAsThumbnailAsync(
                        ThumbnailMode.VideosView, 320, ThumbnailOptions.UseCurrentScale);

                    // 2. CHECK if thumb is null or has no size before setting source
                    if (thumb != null && thumb.Size > 0)
                    {
                        await bitmap.SetSourceAsync(thumb);
                    }
                    else
                    {
                        // Fallback if thumbnail generation failed
                        bitmap = await GetDefaultThumbnailAsync();
                    }
                }
                catch (Exception)
                {
                    // Fallback for any IO/WinRT errors
                    bitmap = await GetDefaultThumbnailAsync();
                }

                var props = await file.GetBasicPropertiesAsync();

                // Add to collection on the UI thread to prevent threading crashes
                items.Add(new VideoItem
                {
                    FilePath = file.Path,
                    IsFolder = false,
                    Thumbnail = bitmap,
                    Size = props.Size,
                    DateModified = file.DateCreated
                });
            }

        }
        catch (UnauthorizedAccessException)
        {
            if (App.HomeWindowInstance != null)
            {
                OceanContentDialog.Show("Access Denied", "", "", "OK", OceanContentDialogDefault.Close, AccessDeniedGrid, this.XamlRoot, 400, 260, OceanContentDialogType.Elevated, App.HomeWindowInstance, "", "", "removeicon");
            }
            else
            {
                Logger.Log($"Folder Access Denied: {folderPath}", "FoldersPage", Logger.LogLevelType.Error);
            }
        }
        return items;
    }
    private async Task LoadFolderRecursive(
        StorageFolder folder,
        ObservableCollection<VideoItem> videoItems)
    {
        // 1️⃣ Load files in current folder
        var files = await folder.GetFilesAsync();

        foreach (var file in files)
        {
            if (!file.ContentType.StartsWith("video"))
                continue;

            BitmapImage bitmap;

            try
            {
                var thumb = await file.GetScaledImageAsThumbnailAsync(
                    ThumbnailMode.VideosView,
                    320,
                    ThumbnailOptions.UseCurrentScale);

                if (thumb != null)
                {
                    bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(thumb);
                }
                else
                {
                    bitmap = await GetDefaultThumbnailAsync();
                }
            }
            catch
            {
                bitmap = await GetDefaultThumbnailAsync();
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

        // 2️⃣ Load subfolders recursively
        var subFolders = await folder.GetFoldersAsync();

        foreach (var sub in subFolders)
        {
            await LoadFolderRecursive(sub, videoItems);
        }
    }
    private async Task<BitmapImage> GetDefaultThumbnailAsync()
    {
        var exeFolder = AppContext.BaseDirectory;
        var defaultPath = Path.Combine(exeFolder, "Assets", "default.png");

        var bitmap = new BitmapImage();

        using (var stream = File.OpenRead(defaultPath))
        {
            await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
        }

        return bitmap;
    }

    private ObservableCollection<VideoItem> loadedVideos = new ObservableCollection<VideoItem>();

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        //preview close
        PreviewGrid.Visibility = Visibility.Collapsed;
    }

    private async void btnDelete_Click_1(object sender, RoutedEventArgs e)
    {
        //delete selected files
        var selectedItems = VideoGrid.SelectedItems.Cast<VideoItem>().ToList();

        if (selectedItems.Count == 0)
            return; // nothing selected

        foreach (var item in selectedItems)
        {
            if (File.Exists(item.FilePath))
            {
                // Delete or process the file
                File.Delete(item.FilePath);
                loadedVideos.Remove(item);
            }
            else
            {
                dlgFileNotExist.Content = $"The file path {item.FilePath} does not exist.";
                await dlgFileNotExist.ShowAsync();
            }
        }
    }

    private void chckSelectMultiple_Checked(object sender, RoutedEventArgs e)
    {
        if (chckSelectMultiple.IsChecked == true)
        {
            VideoGrid.SelectionMode = ListViewSelectionMode.Multiple;

        }
    }

    private void chckSelectMultiple_Unchecked(object sender, RoutedEventArgs e)
    {
        if (chckSelectMultiple.IsChecked == false)
        {
            VideoGrid.SelectionMode = ListViewSelectionMode.Single;
        }
    }

    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
        //Rename Folder
        ttRenameFolder.IsOpen = true;
        //     txtRenameFolder.Text = txtFolderName.Text;
    }
    public async Task<StorageFolder> RenameFolderWithNumberAsync(string folderPath, string desiredName)
    {
        StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(folderPath);
        string parentPath = Path.GetDirectoryName(folder.Path)!;

        string newName = desiredName;
        int count = 1;
        while (Directory.Exists(Path.Combine(parentPath, newName)))
        {
            newName = $"{desiredName} ({count})";
            count++;
        }

        await folder.RenameAsync(newName, NameCollisionOption.FailIfExists);
        folderloadedpath = Path.Combine(parentPath, newName);
        await SettingsHelper.LoadSettingsAsync();
        var folders = await SettingsHelper.LoadSettingsAsync();
        var newfolder = new FolderModel
        {
            Name = newName,
            Path = folderloadedpath,
        };
        folders.FoldersRecent.Add(newfolder);
        //  txtFolderName.Text = txtRenameFolder.Text;

        await SettingsHelper.SaveSettingsAsync(folders);
        return folder;
    }
    private async void Button_Click_2(object sender, RoutedEventArgs e)
    {
        //Set Folder Name
        //       originalfoldername = txtFolderName.Text;
        if (txtRenameFolder.Text == "")
        {
            txtRenameFolder.Text = originalfoldername;
        }
        if (Directory.Exists(folderloadedpath))
        {
            await RenameFolderWithNumberAsync(folderloadedpath, txtRenameFolder.Text);
            txtRenameSuccess.Text = $"Folder successfully renamed from {originalfoldername} to {txtRenameFolder.Text}";
            ttFolderRenamedSuccess.IsOpen = true;
            prgFolderRenameComplete.Value = 0;
            int durationMs = 5000; // 5 seconds
            int steps = 100;       // progress steps
            int delay = durationMs / steps;

            for (int i = 1; i <= steps; i++)
            {
                prgFolderRenameComplete.Value = i;
                await Task.Delay(delay);
            }
            //         originalfoldername = txtFolderName.Text;
            ttFolderRenamedSuccess.IsOpen = false;

        }
        else
        {
            await dlgFolderNotExist.ShowAsync();
        }
    }
    string originalfoldername;

    private void dlgFolderNotExist_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Go back to home page
        this.Frame.Navigate(typeof(HomePage));
    }

    private async void btnUndo_Click(object sender, RoutedEventArgs e)
    {//Undo allows user to rename it back
        txtRenameFolder.Text = originalfoldername;
        await RenameFolderWithNumberAsync(folderloadedpath, originalfoldername);
        ttFolderRenamedSuccess.IsOpen = false;
    }

    private void dlgFileNotExist_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {

    }

    private void PreviewFilePath_Click(object sender, RoutedEventArgs e)
    {
        string filePath = PreviewFilePath.Content?.ToString() ?? string.Empty;

        if (File.Exists(filePath))
        {
            Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }
    }

    private void ChkReadOnly_Checked(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(currentFilePath) && File.Exists(currentFilePath))
        {
            var attributes = File.GetAttributes(currentFilePath);
            attributes |= FileAttributes.ReadOnly;  // add readonly
            File.SetAttributes(currentFilePath, attributes);
        }
    }

    private void ChkHidden_Checked(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(currentFilePath) && File.Exists(currentFilePath))
        {
            var attributes = File.GetAttributes(currentFilePath);
            attributes |= FileAttributes.Hidden;  // add hidden
            File.SetAttributes(currentFilePath, attributes);
        }
    }

    private void ChkReadOnly_Unchecked(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(currentFilePath) && File.Exists(currentFilePath))
        {
            var attributes = File.GetAttributes(currentFilePath);
            attributes &= ~FileAttributes.ReadOnly; // remove readonly
            File.SetAttributes(currentFilePath, attributes);
        }

    }

    private void ChkHidden_Unchecked(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(currentFilePath) && File.Exists(currentFilePath))
        {
            var attributes = File.GetAttributes(currentFilePath);
            attributes &= ~FileAttributes.Hidden; // remove hidden
            File.SetAttributes(currentFilePath, attributes);
        }
    }

   
    private void BtnDelete_Click_2(object sender, RoutedEventArgs e)
    {
    }

    private void BtnRename_Click_1(object sender, RoutedEventArgs e)
    {

    }

    private void BtnProperties_Click_1(object sender, RoutedEventArgs e)
    {
        try
        {
            if (App.MainWindowInstance is HomeWindow wind)
            {
                wind.ShowSongDetails(currentFilePath);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }
    string sortselection = "Name";
    private void RadioMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        sortselection = "Name";
        SortItems();

    }
    private void SortItems()
    {
        if (sortselection == "Name")
        {
            var sorted = loadedVideos.OrderBy(x => x.FileName).ToList();
            loadedVideos.Clear();
            foreach (var item in sorted)
                loadedVideos.Add(item);
        }
        else if (sortselection == "Date")
        {
            var sorted = loadedVideos.OrderBy(x => File.GetCreationTime(x.FilePath)).ToList();
            loadedVideos.Clear();
            foreach (var item in sorted)
                loadedVideos.Add(item);
        }
        else
        {
            var sorted = loadedVideos.OrderBy(x => new FileInfo(x.FilePath).Length).ToList();
            loadedVideos.Clear();
            foreach (var item in sorted)
                loadedVideos.Add(item);
        }
    }
    // Sort by Date (CreationTime)
    private void RadioMenuFlyoutItem_Click_1(object sender, RoutedEventArgs e)
    {
        sortselection = "Date";
        SortItems();
    }

    // Sort by Size
    private void RadioMenuFlyoutItem_Click_2(object sender, RoutedEventArgs e)
    {
        sortselection = "Size";
        SortItems();
    }

    private async void Button_Click_3(object sender, RoutedEventArgs e)
    {
        if (txtRenameFile.Text == "")
        {
            txtRenameFile.Text = originalfilenamee;
        }

        Debug.WriteLine(txtRenameFile.Tag.ToString());
        if (string.IsNullOrEmpty(txtRenameFile.Tag.ToString()) || !File.Exists(txtRenameFile.Tag.ToString()))
        {
            dlgFileNotExist.Title = "File Not Found";
            dlgFileNotExist.Content = $"The file path {txtRenameFile.Tag.ToString()} does not exist.";
            _ = dlgFileNotExist.ShowAsync();

            return;
        }
        else
        {
            try
            {
                string directory = Path.GetDirectoryName(txtRenameFile.Tag.ToString())!;
                string extension = System.IO.Path.GetExtension(txtRenameFile.Tag.ToString());
                string newPath = Path.Combine(directory, txtRenameFile.Text + extension);
                File.Move(txtRenameFile.Tag.ToString(), newPath);
                var item = loadedVideos.FirstOrDefault(v => v.FilePath == txtRenameFile.Tag.ToString());

                if (item != null)
                {
                    int index = loadedVideos.IndexOf(item);

                    // Remove old item
                    loadedVideos.RemoveAt(index);

                    // Update values
                    item.FilePath = newPath;

                    // Insert back at same position
                    loadedVideos.Insert(index, item);
                    SortItems();
                }
            }
            catch (Exception ex)
            {
                dlgFileNotExist.Title = "File In Use";
                dlgFileNotExist.Content = $"The file path {txtRenameFile.Tag.ToString()} is being used by another process.";
                _ = dlgFileNotExist.ShowAsync();
            }
        }
    }

    private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
    {
        string path = hypPath.Tag?.ToString() ?? "";

        if (string.IsNullOrEmpty(path)) return;

        if (File.Exists(path))
        {
            // It's a file: Open parent folder and select the file
            Process.Start("explorer.exe", $"/select,\"{path}\"");
        }
        else if (Directory.Exists(path))
        {
            // It's a folder: Open the folder itself
            Process.Start("explorer.exe", $"\"{path}\"");
        }
        else
        {
            // Path doesn't exist anymore
            dlgFileNotExist.Content = $"The path {path} could not be found.";
            _ = dlgFileNotExist.ShowAsync();
        }
    }

    private void brdcbFolderPath_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        var clickedCrumb = (FolderNode)args.Item;
        if (Directory.Exists(clickedCrumb.Path))
        {
            var newFolder = new FolderModel
            {
                Path = clickedCrumb.Path,
                Name = Path.GetFileName(clickedCrumb.Path)
            };
            this.Frame.Navigate(typeof(FoldersPage), newFolder);
        }
        else
        {
            if (App.HomeWindowInstance == null) return;
            TextBlock text = new();
            Grid grd = new();
            text.Text = $"The folder path {clickedCrumb.Path} doesn't exist.";
            grd.Children.Add(text);
            OceanContentDialog.Show("Folder Does Not Exist", "", "", "OK", OceanContentDialogDefault.Close, grd, this.XamlRoot, 400, 260, OceanContentDialogType.Elevated, App.HomeWindowInstance, "", "", "");

        }
    }

    private void mnftCopyPath_Click(object sender, RoutedEventArgs e)
    {
        var package = new DataPackage();
        package.SetText(txtFolderPath.Text);
        Clipboard.SetContent(package);

    }

    private void BtnRename_Click(object sender, RoutedEventArgs e)
    {

    }

    private void BtnRename_Click_2(object sender, RoutedEventArgs e)
    {

    }

    private void btnRename_Click_3(object sender, RoutedEventArgs e)
    {

    }
}
