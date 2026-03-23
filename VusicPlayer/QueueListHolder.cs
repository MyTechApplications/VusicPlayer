using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public static class QueueListHolder
    {
        public static ObservableCollection<SongModel> VusicQueue { get; } = new();



    }
}
