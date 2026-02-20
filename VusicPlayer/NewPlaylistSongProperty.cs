using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public class NewPlaylistSongProperty
    {
        public string? SongPath { get; set;  }
        public string? SongDuration { get; set; }
        public string? SongName => System.IO.Path.GetFileNameWithoutExtension(SongPath);
    }
}
