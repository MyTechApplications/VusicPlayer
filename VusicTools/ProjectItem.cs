using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VusicTools
{
    public class ProjectItem
    {
        public string? ProjectName { get; set; }
        public string? FolderName { get; set; }
        public ImageSource? Thumbnail { get; set; }

        public bool IsNewProject { get; set; }   // Important
    }
}
