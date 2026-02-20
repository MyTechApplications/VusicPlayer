using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public class SongModel:INotifyPropertyChanged
    {
        private string? _title;
        private string? _artist;
        private string? _albumName;

        public string? Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
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
        private Brush _titleColor = new SolidColorBrush(Colors.White); // Default color
        public Brush TitleColor
        {
            get => _titleColor;
            set { _titleColor = value; OnPropertyChanged(nameof(TitleColor)); }
        }
        private string? _glyph = "\uEC4F";// Default color
        public string Glyph
        {
            get => _glyph;
            set { _glyph = value; OnPropertyChanged(nameof(Glyph)); }
        }
        public TimeSpan? SongDuration { get; set; }
        public string FormattedDuration => $"{(int)SongDuration.Value.TotalMinutes:D2}:{SongDuration.Value.Seconds:D2}";
        public string? FilePath { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        // You can add an Icon property here later!
    }
}
