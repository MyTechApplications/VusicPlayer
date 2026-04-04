using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public class SubtitleItem : INotifyPropertyChanged
    {
        private string _start = "00:00:00,000";
        private string _end = "00:00:00,000";

        public string Start
        {
            get => _start;
            set { _start = value; OnPropertyChanged(); }
        }

        public string End
        {
            get => _end;
            set { _end = value; OnPropertyChanged(); }
        }

        public int Index { get; set; }
        public string? Text { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
