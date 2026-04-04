using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public class PageState
    {
        public static bool IsQueuePage {  get; set; }
        public static bool IsPlaylistInQueue { get; set; }
        public static string PlaylistID { get; set; } = "";
    }
}
