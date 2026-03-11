using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public class QueueList
    {
        public List<string>? MediaPaths { get; set; }
        public string? CurrentMedia { get; set; }
        public bool? LoopMode { get; set; }
        public bool? ShuffleMode { get; set; }
    }
}
