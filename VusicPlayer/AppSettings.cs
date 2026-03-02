using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public class AppSettings
    {
        public ObservableCollection<VideoProgress> SavedItems { get; set; } = new();
        public ObservableCollection<FolderModel> FoldersRecent { get; set; } = new();
        public ObservableCollection<ArtistDetails> ArtistsList { get; set; } = new();
        public ObservableCollection<AlbumDetails> AlbumsList { get; set; } = new();
        public ObservableCollection<PlaylistProperties> SavedPlaylists { get; set; } = new();
        public ObservableCollection<AppPersonalization> UserSettings { get; set; } = new();
        public ObservableCollection<RecentMusic> RecentMusic { get; set; } = new();

    }
}
