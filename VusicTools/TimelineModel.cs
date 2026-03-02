using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace VusicTools
{
    public enum TrackType { Video, Audio }
    public class TimelineClip : INotifyPropertyChanged
    {
        public string FileName { get; set; } = "Filename";
        public TrackType Type { get; set; }
        public double Duration { get; set; } // In seconds

        private double _startTime;
        public double StartTime
        {
            get => _startTime;
            set { _startTime = value; OnPropertyChanged(); }
        }

        // A helper for the UI: 1 second = 50 pixels
        public double Width => Duration * 50;
        public double X => StartTime * 50;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "mediafile") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

        public class TimelineTrack
    {
        public string Name { get; set; } = "Track Name";
        public bool IsVideoTrack { get; set; }
        public bool IsMuted { get; set; } = false; // New property
        public ObservableCollection<TimelineClip> Clips { get; set; } = new ObservableCollection<TimelineClip>();

    }
}