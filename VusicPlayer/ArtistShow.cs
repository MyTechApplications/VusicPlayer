using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public class ArtistShow
    {
        public string ArtistName { get; set; } = string.Empty;
        public string ArtistThumbnail { get; set; } = "ms-appx:///Assets/artistdefault.png";
        public string ArtistSongCount { get; set; } = string.Empty;
        public string ArtistAlbumCount { get; set; } = string.Empty;

        [JsonIgnore]
        public BitmapImage ArtistThumbnailImage { get; set; }
    }
}
