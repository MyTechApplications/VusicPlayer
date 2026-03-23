using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Frame = Microsoft.UI.Xaml.Controls.Frame;

namespace VusicPlayer
{
    public class QueueService
    {

        public static QueueList queueList { get; set; } = new();
        private static int currentVideoIndex = 0;
        private static string currentVideoPath = "";
            public static void PlayMedia(ObservableCollection<string> paths)
        { //{
            //    QueueListHolder.VusicQueue.CollectionChanged += VusicQueue_CollectionChanged;
            //    foreach (var path in paths)
            //    {
            //        var newSong = new SongModel { FilePath = path, IsCompleted = false };
            //        QueueListHolder.VusicQueue.Add(newSong);

            //        App.HomeWindowInstance?.DispatcherQueue.TryEnqueue(() =>
            //        {
            //    });
            //    }
            //    var firstpath = QueueListHolder.VusicQueue?.FirstOrDefault();
            //    if (firstpath?.FilePath == null) return;
            //    string first = firstpath.SongPath;
            //    if (App.HomeWindowInstance is HomeWindow wind)
            //    {
            //        wind.LoadFileFromPath(first);
            //    }
            //    if (firstpath?.SongPath != null)
            //    {
            //        currentVideoPath = firstpath.SongPath;
            //    }
            //    if (PlayerService.MasterPlayer == null) return;
         
            }

        private static void VusicQueue_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
            }
        }

        public static void PlayNext()
        {
          //  Debug.WriteLine("DHDHD  "+PlaybackState.CurrentlyPlayingPath);
          //  var song = QueueListHolder.VusicQueue.FirstOrDefault(x => x.SongPath == PlaybackState.CurrentlyPlayingPath);
          //if(song != null)
          //  {
          //      song.IsCompleted = true;
          //  }
      
          //  PlayVideoAtIndex(currentVideoIndex + 1);
         
        }

        public static void PlayPrevious()
        {
            //var currentSong = QueueListHolder.VusicQueue.FirstOrDefault(x => x.SongPath == currentVideoPath);

            //if (currentSong != null)
            //{
            //    // 2. Find the index of the current song
            //    int currentIndex = QueueListHolder.VusicQueue.IndexOf(currentSong);

            //    // 3. Ensure there is actually a song before it (index > 0)
            //    if (currentIndex > 0)
            //    {
            //        var previousSong = QueueListHolder.VusicQueue[currentIndex - 1];
            //        previousSong.IsCompleted = true;
            //    }
            //}
            PlayVideoAtIndex(currentVideoIndex - 1);
        }
        public static Frame frmMain = new();
        public static void ChangeCurrentVideoIndex()
        {
            currentVideoIndex = -1;
        }
        public async static void PlayVideoAtIndex(int index)
        {
            //    if (index < 0 || index >= QueueListHolder.VusicQueue.Count)

            //        return;
            //    if (QueueListHolder.VusicQueue.Count == 0) return;
            //    var item = QueueListHolder.VusicQueue?[index];
            //    currentVideoIndex = index;
            //    if (item.IsCompleted == true) return;
            //    currentVideoPath = item?.SongPath ?? string.Empty;
            //    Debug.WriteLine(currentVideoPath);
            //    if (currentVideoPath == "") return;
            //    if (App.HomeWindowInstance is HomeWindow wind)
            //    {
            //        wind.LoadFileFromPath(currentVideoPath);
            //    }
            //}
        }
    }
}
