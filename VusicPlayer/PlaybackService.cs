using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public class PlaybackService
    {
        private static readonly Lazy<PlaybackService> _instance = new(() => new PlaybackService());
        public static PlaybackService Instance => _instance.Value;

        public LibVLC LibVLC { get; private set; }
        public MediaPlayer MediaPlayer { get; private set; }

        private PlaybackService()
        {
            LibVLC = new LibVLC();
            MediaPlayer = new MediaPlayer(LibVLC);
      
        }

        public void PlayTrack(string url)
        {
            using var media = new Media(LibVLC, new Uri(url));
            MediaPlayer.Play(media);
        }
    }
}
