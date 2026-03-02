using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VusicTools
{
    public class InteractiveGrid : Grid
    {
        public void SetCursor(Microsoft.UI.Input.InputCursor cursor)
        {
            this.ProtectedCursor = cursor;
        }
    }
}
