using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public class FolderModel 
    {
        public BitmapImage Thumbnail { get; set; }
        public string Name { get; set; }    
        public string Path { get; set; }    
    }
}