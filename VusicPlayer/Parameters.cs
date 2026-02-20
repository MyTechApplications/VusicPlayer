using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public class VideoPlayerPageParams
    {
        public List<VideoItem>? Videos { get; set; }
        public string? SelectedVideoPath { get; set; }
        public double CurrentRunningDuration { get; set; }
        public bool newinst { get; set; }
    }
}
