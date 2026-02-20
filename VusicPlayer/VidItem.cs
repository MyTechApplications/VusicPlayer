using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public class VideoItem
    {
        public string? FilePath { get; set; }
        public string? FileName => Path.GetFileName(FilePath);
        public BitmapImage? Thumbnail { get; set; }
        public ulong Size { get; set; }       // in bytes
        public DateTimeOffset DateModified { get; set; }
    }
}
