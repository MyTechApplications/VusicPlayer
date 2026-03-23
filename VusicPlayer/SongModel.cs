using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.System;
using Windows.UI;

namespace VusicPlayer
{
    public class SongModel:INotifyPropertyChanged
    {
        private string? _title;
        private int _year;
        private string? _artist;
        private string? _albumName;

        public string? Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }
        public int Year
        {
            get => _year;
            set { _year = value; OnPropertyChanged(); }
        }

        public string? Artist
        {
            get => _artist;
            set { _artist = value; OnPropertyChanged(); }
        }

        public string? AlbumName
        {
            get => _albumName;
            set { _albumName = value; OnPropertyChanged(); }
        }
        private Brush _titleColor = new SolidColorBrush(Microsoft.UI.Colors.White); // Safe for any thread!
        public Visibility VisibilityOfStrikethrough => IsCompleted
            ? Visibility.Visible
            : Visibility.Collapsed; public Brush TitleColor
        {
            get => _titleColor;
            set
            {
                _titleColor = value;
                OnPropertyChanged(nameof(TitleColor));
            }
        }
        private string? _glyph = "\uEC4F";// Default color
        public string Glyph
        {
            get => _glyph;
            set { _glyph = value; OnPropertyChanged(nameof(Glyph)); }
        }
        private bool _isCompleted;
        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (_isCompleted != value)
                {
                    _isCompleted = value;
                    DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() =>
                    {
                        OnPropertyChanged();

                        // Explicitly notify that the dependent property has changed
                        OnPropertyChanged(nameof(VisibilityOfStrikethrough));
                    });
                }
            }
        }
        public TimeSpan? SongDuration { get; set; }
        public string FormattedDuration => SongDuration.HasValue
            ? $"{(int)SongDuration.Value.TotalMinutes:D2}:{SongDuration.Value.Seconds:D2}"
            : "00:00";
        public string? FilePath { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;
        public bool? IsFavourite { get; set; }
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            });
        }
        // You can add an Icon property here later!
    }
}
