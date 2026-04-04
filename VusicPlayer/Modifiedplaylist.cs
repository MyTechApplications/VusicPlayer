using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public class Modifiedplaylist
    {
        public static string? Name { get; set; }
        public static string? Genre { get; set; }
        public static HashSet<string> SongsPaths { get; set; } = new();
        public static string? Thumbnail { get; set; }
    }
}
