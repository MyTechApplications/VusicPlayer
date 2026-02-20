using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public class PlaylistProperties
    {
        public string PlaylistName { get; set; }
        public string PlaylistCount { get; set; }
        public string PlaylistNowPlaying { get; set; }
    
        public string Thumbnail { get; set; }
        public string PlaylistGenre { get; set; }
        public List<string> SongsPaths { get; set; }
        public DateTime DateCreation { get; set; }
    }
}
