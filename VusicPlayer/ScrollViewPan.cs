using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public class ScrollViewPan : Grid
    {
        public void SetCursor(InputSystemCursor shape)
        {
            this.ProtectedCursor = shape;
        }
    }
}

