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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicTools
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ProjectPage : Page
    {
        public ProjectPage()
        {
            InitializeComponent();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
           if(e.Parameter is ProjectDetails project)
            {
              txtProjectTitle.Text = project.ProjectName;
            }
        }
        private async void btnExport_Click(object sender, RoutedEventArgs e)
        {
            //Show Export Dialog
            await ExportDialog.ShowAsync();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }
        private void MediaGrid_DragItemsStarting(GridView sender, DragItemsStartingEventArgs args)
        {
            
        }
        public ObservableCollection<MediaItem> MediaItems { get; set; }
    = new ObservableCollection<MediaItem>();

        private async void btnImportMedia_Click(object sender, RoutedEventArgs e)
        {
            //Import Video and Audio Files
            var picker = new Windows.Storage.Pickers.FileOpenPicker();

            picker.FileTypeFilter.Add(".mp4");
            picker.FileTypeFilter.Add(".mp3");
            picker.FileTypeFilter.Add(".wav");
            picker.FileTypeFilter.Add(".mkv");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var files = await picker.PickMultipleFilesAsync();

            if (files != null)
            {
                foreach (var file in files)
                {
                    bool alreadyExists = MediaItems
                        .Any(m => m.FilePath.Equals(file.Path, StringComparison.OrdinalIgnoreCase));

                    if (!alreadyExists)
                    {
                        MediaItems.Add(new MediaItem
                        {
                            FilePath = file.Path,
                            Name = file.Name,
                            Duration = TimeSpan.FromSeconds(10), // temp dummy
                            IsVideo = file.FileType != ".mp3" && file.FileType != ".wav"
                        });
                    }
                }
            }
            MediaGrid.ItemsSource = MediaItems;
        }
        private void MediaGrid_DragOver(object sender, DragEventArgs e)
        {
            // Tell Windows we only want files here
            e.AcceptedOperation = e.DataView.Contains(StandardDataFormats.StorageItems)
                ? DataPackageOperation.Copy
                : DataPackageOperation.None;
        }

        private async void MediaGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                foreach (var item in items)
                {
                    if (item is Windows.Storage.StorageFile file)
                    {
                        //Add to your MediaCollection (the source for the GridView)
                        MediaItems.Add(new MediaItem{
                            Name = file.Name, FilePath = file.Path
                        });
                        // MediaCollection.Add(new MediaItem { Name = file.Name, Path = file.Path });
                    }
                }
            }
        }
        private void MediaGrid_DragItemsStarting_1(object sender, DragItemsStartingEventArgs e)
        {
            var media = e.Items[0] as MediaItem;

            e.Data.Properties.Add("MediaItem", media);
        }
    }
}
