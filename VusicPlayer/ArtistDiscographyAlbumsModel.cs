using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public class ArtistDiscographyAlbumsModel
    {
        public string? AlbumName { get; set; }
       
        public string? AlbumCount { get; set; }
        public string? AlbumYear { get; set; }
        [JsonIgnore]
        public BitmapImage? AlbumCoverThumbnail { get; set; }
    }
}
