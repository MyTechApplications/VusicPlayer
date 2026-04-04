using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public class BoolToTextConverter : IValueConverter
    {
        // Default values, but we can override these in XAML if we want
        public string TrueText { get; set; } = "Remove from Favourites";
        public string FalseText { get; set; } = "Add to Favourites";

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
            {
                return b ? TrueText : FalseText;
            }
            return FalseText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
