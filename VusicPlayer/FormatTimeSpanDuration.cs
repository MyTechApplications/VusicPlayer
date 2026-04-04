using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public class FormatTimeSpanDuration
    {
        public static string Format(TimeSpan duration)
        {
            if (duration.Hours > 0)
                return duration.ToString(@"hh\:mm\:ss");

            return duration.ToString(@"mm\:ss");
        }
    }
}
