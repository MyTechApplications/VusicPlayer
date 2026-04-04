using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public static class PlaybackState
    {
        private static string? _currentlyPlayingPath;
        public static string? CurrentlyPlayingPath
        {
            get => _currentlyPlayingPath;
            set
            {
                if (_currentlyPlayingPath != value)
                {
                    _currentlyPlayingPath = value;
                    CurrentlyPlayingChanged?.Invoke(value);
                }
            }
        }

        public static event Action<string?>? CurrentlyPlayingChanged; 
        public static PlaylistProperties? currentPlaylist { get; set; }
        public static bool? IsShuffleEnabled { get; set; }
        public static float CurrentPosition { get; set; }
        public static long CurrentSliderPosition { get; set; }
        public static long TotalDuration { get; set; }
        public static bool CurrentState { get; set; }
    }
}
