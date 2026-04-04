using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace VusicPlayer
{
    public  class FileThumbnailObtain
    {
        public static async Task<BitmapImage> GetFileThumbnailAsync(string path)
        {
            Uri fallbackUri = new Uri("ms-appx:///Assets/appicon.png");

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
                Logger.Log($"Thumbnail extraction failed: {ex.Message}", "MusicLibrary", Logger.LogLevelType.Error);
            }

            // If everything fails, return the app icon
            return new BitmapImage(fallbackUri);
        }

    }
}
