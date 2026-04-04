using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public class PlaylistProperties : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public string? PlaylistId { get; set; } 
        public string? PlaylistName { get; set; } = "";
        public string? PlaylistCount { get; set; } = "";
        public string? PlaylistNowPlaying { get; set; } = "";
    
        public Uri? Thumbnail { get; set; }
        public string? PlaylistGenre { get; set; } = "";
        public HashSet<string> SongsPaths { get; set; } = new();
        public DateTime DateCreation { get; set; }
        public void NotifyCountChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PlaylistCount)));
        }

        [JsonIgnore]
        public BitmapImage? plthumb { get; set; }
    }
}
