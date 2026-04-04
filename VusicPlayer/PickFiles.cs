using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace VusicPlayer
{
    public class PickFiles
    {
        public static async Task<StorageFile?> PickAudioFileAsync(Window wind, string commitbuttontext)
        {

            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(wind);
            picker.CommitButtonText = commitbuttontext;
            if (hwnd == IntPtr.Zero)
            {
                hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentActiveWindow);
            }
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            #region AudioFileTypes 
            picker.FileTypeFilter.Add(".mp3");
            picker.FileTypeFilter.Add(".wav");
            picker.FileTypeFilter.Add(".ogg");
            picker.FileTypeFilter.Add(".m4a");
            picker.FileTypeFilter.Add(".aac");
            picker.FileTypeFilter.Add(".wma");
            picker.FileTypeFilter.Add(".flac");
            picker.FileTypeFilter.Add(".ac3");
            picker.FileTypeFilter.Add(".alac");
            picker.FileTypeFilter.Add(".aiff");
            picker.FileTypeFilter.Add(".opus");
            picker.FileTypeFilter.Add(".ape");
            picker.FileTypeFilter.Add(".wv");
            picker.FileTypeFilter.Add(".tta");
            picker.FileTypeFilter.Add(".dsf");
            picker.FileTypeFilter.Add(".dff");
            picker.FileTypeFilter.Add(".mp2");
            picker.FileTypeFilter.Add(".amr");
            picker.FileTypeFilter.Add(".au");
            picker.FileTypeFilter.Add(".snd");
            picker.FileTypeFilter.Add(".mka");
            #endregion

            var files = await picker.PickSingleFileAsync();
            return files;
        }
        public static async Task<IReadOnlyList<StorageFile>> PickMultipleAudioFilesAsync(Window window, string commitbuttontext)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.CommitButtonText = commitbuttontext;
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            picker.ViewMode = PickerViewMode.List;

        
            string[] types = { ".mp3", ".wav", ".ogg", ".m4a", ".aac", ".wma", ".flac", ".ac3",
                       ".alac", ".aiff", ".opus", ".ape", ".wv", ".tta", ".dsf",
                       ".dff", ".mp2", ".amr", ".au", ".snd", ".mka" };

            foreach (var type in types) picker.FileTypeFilter.Add(type);
            return await picker.PickMultipleFilesAsync();
        }
        public static async Task<StorageFile?> PickSingleImageFileAsync(Window wind, string commitbuttontext)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.CommitButtonText = commitbuttontext;
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(wind);
            if (hwnd == IntPtr.Zero)
            {
                hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.CurrentActiveWindow);
            }
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            #region ImageFileTypes 
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".bmp");
            picker.FileTypeFilter.Add(".gif");
            picker.FileTypeFilter.Add(".tiff");
            picker.FileTypeFilter.Add(".webp");
            picker.FileTypeFilter.Add(".heic");
            picker.FileTypeFilter.Add(".svg");
            #endregion
            var files = await picker.PickSingleFileAsync();
            return files;

        }
    }
}
