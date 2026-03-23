using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public class MediaPlaybackController : INotifyPropertyChanged

    {
        public static MediaPlaybackController instance { get; } = new MediaPlaybackController();
        private double _currentPosition;
        public double CurrentPosition
        {
            get => _currentPosition;
            set
            {
                if (_currentPosition == value) return;
                _currentPosition = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RunningDurationString));
            }
        }
        private double _totalDuration;
        private ImageSource _thumbnail = new BitmapImage(new Uri("ms-appx:///Assets/play.png"));
        public ImageSource Thumbnail
        {
            get => _thumbnail;
            set
            {
                _thumbnail = value;
                OnPropertyChanged();
            }
        }
        private ImageSource _thumbnail2 = new BitmapImage(new Uri("ms-appx:///Assets/appicon.png"));
        public ImageSource Thumbnail2
        {
            get => _thumbnail2;
            set
            {
                _thumbnail2 = value;
                OnPropertyChanged();
            }
        }
        public double TotalDuration
        {
            get => _totalDuration;
            set { _totalDuration = value; OnPropertyChanged(); }
        }
        private string _runnningDurationText = "00:00:00";
        public string RunningDurationString
        {
            get
            {
                TimeSpan t = TimeSpan.FromSeconds(CurrentPosition);
                return t.ToString(@"hh\:mm\:ss");
            }
        }
        private string volumetext = "100%";
        public string VolumeString
        {
            get => volumetext;
            set { volumetext = value; OnPropertyChanged(); }
        }
        private string volglyph = "\uE767";
        public string VolumeGlyph
        {
            get => volglyph;
            set { volglyph = value; OnPropertyChanged(); }
        }
        private Brush volforeground = new SolidColorBrush(Colors.White);
        public Brush VolumeForeground
        {
            get => volforeground;
            set { volforeground = value; OnPropertyChanged(); }
        }
        private string songname = "Nothing playing";
        public string SongDisplayName
        {
            get => songname;
            set { songname = value; OnPropertyChanged(); }
        }
        private string albumname = "Unknown Album";
        public string AlbumDisplayName
        {
            get => albumname;
            set { albumname = value; OnPropertyChanged(); }
        }
        private string artistname = "Unknown Artist";
        public string ArtistDisplayName
        {
            get => artistname;
            set { artistname = value; OnPropertyChanged(); }
        }
        private string _totaldurationText = "00:00:00";
        public string TotalDurationString
        {
            get => _totaldurationText;
            set { _totaldurationText = value; OnPropertyChanged(); }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            // Check if we are already on the UI thread
            if (Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() != null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
            else
            {
                // We are on a background thread! "Post" the update to the UI thread.
                // Use the Dispatcher from your Main Window or App.
                App.HomeWindowInstance?.DispatcherQueue.TryEnqueue(() =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                });
            }
        }
    }
}
