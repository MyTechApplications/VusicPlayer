using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VusicTools
{
    public class MediaItem
    {
        public string FilePath { get; set; }
        public string Name { get; set; }
        public TimeSpan Duration { get; set; }
        public bool IsVideo { get; set; }
    }
}
