using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;

namespace VusicPlayer
{
    public class QueueHandler
    {

        public static void PlayMedia(ObservableCollection<SongModel> media, bool IsShuffleEnabled, bool IsLoopEnabled)
        {
            if (media != null)
            {
                videoindex = -1;
                PlaybackState.CurrentlyPlayingPath = "";
                    QueueListHolder.VusicQueue.Clear();

                    foreach (var item in media)
                    {
                        QueueListHolder.VusicQueue.Add(item);
                    }

                    if (IsShuffleEnabled)
                    {
                        ShuffleList();
                    }
                    PlayNext();
                
            }
        }
        public static void ResetVideoIndex()
        {
            videoindex = -1;
        }
        private static Random rng = new Random();
        private static int videoindex = -1;
        public static void ShuffleList()
        {
            videoindex = -1;
            var items = QueueListHolder.VusicQueue.ToList();
            int n = items.Count;


            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                var value = items[k];
                items[k] = items[n];
                items[n] = value;
            }
                     QueueListHolder.VusicQueue.Clear();
                     foreach (var item in items)
                     {
                         QueueListHolder.VusicQueue.Add(item);
                     }

           foreach(var item in QueueListHolder.VusicQueue)
            {
                Debug.WriteLine(item.FilePath + " QUEUEHANDLER SHUFFLED");
            }
        }
        public static event EventHandler<string>? QueueUpdated;
        private static void MarkSongCompleted()
        {
            var song = QueueListHolder.VusicQueue.FirstOrDefault(x => x.FilePath == PlaybackState.CurrentlyPlayingPath);
            if (song != null)
            {
                Debug.WriteLine("Can't hold it back anymore  " + song.FilePath);
                song.IsCompleted = true;
            }

        }
        public static void PlayNext()
        {
                     MarkSongCompleted();
                 PlayVideoAtIndex(videoindex + 1);
       
        }
        public static bool Loop = false;
        private static void PlayVideoAtIndex(int index)
        {
            var queue = QueueListHolder.VusicQueue;
            if (queue == null || queue.Count == 0 || index < 0) return;

            int targetIndex = index;

            // 1. Handle End-of-Queue / Looping
            if (targetIndex >= queue.Count)
            {
                if (Loop)
                {
                    foreach(var items in QueueListHolder.VusicQueue)
                    {
                        items.IsCompleted = false;
                    }
                    targetIndex = 0; // Reset to start
                }
                else
                {
                    return; // Stop playback at the end
                }
            }
            while (targetIndex < queue.Count && queue[targetIndex].IsCompleted)
            {
                targetIndex++;
            }

            // 2. Check if we found a valid video within bounds
            if (targetIndex >= queue.Count)
            {
                return;
            }

            var item = queue[targetIndex];
            if (string.IsNullOrEmpty(item.FilePath)) return;

            // 3. Update state and play
            videoindex = targetIndex;

            PlayerService.PlayFile(item.FilePath);
            QueueUpdated?.Invoke(null, "completed");
        }
    }
}
