using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public class AppPersonalization
    {
        public string Theme { get; set; } = "System Default";
        public bool AlwaysEnableSubtitles { get; set; }
        public List<string> MusicFolders { get; set; } = new();
    }
}
