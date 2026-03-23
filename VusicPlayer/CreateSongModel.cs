using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace VusicPlayer
{
    public class CreateSongModel
    {
        public static SongModel songmodelcreated = new();
        public static TimeSpan duration;
        public static async void CreateSong(string FilePath)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(FilePath);
            MusicProperties properties = await file.Properties.GetMusicPropertiesAsync();
            duration = properties.Duration;
            string title = !string.IsNullOrWhiteSpace(properties.Title) ? properties.Title : file.DisplayName;
            string album = !string.IsNullOrWhiteSpace(properties.Album) ? properties.Album : "Unknown Album";
            string artist = !string.IsNullOrWhiteSpace(properties.Artist) ? properties.Artist : "Unknown Artist";
            songmodelcreated = new SongModel
            {
                Title = title,
                AlbumName = album,
                Artist = artist,
                SongDuration = properties.Duration,
                FilePath = file.Path
            };
        }
    }
}
