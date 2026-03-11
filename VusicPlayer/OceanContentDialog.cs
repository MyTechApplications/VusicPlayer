using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace VusicPlayer
{
    //Version 1.1.0.0
    public enum OceanContentDialogDefault
    {
        Primary,
        Secondary,
        Close
    }
    public enum OceanContentDialogType
    {
        Elevated,
      Movable,
      MovableandNonElevated,
      NonElevated
    }


    public class OceanContentDialog()
    {
        public static event Action? PrimaryRequested;
        public static event Action? SecondaryRequested;
        public static event Action? CloseRequested;
        public static event Action? HideRequested;
        static OceanPopup? populs;
        private static OceanDialog? currentDialog;
        public static void HideDlg()
        {
            if (populs == null)
            {
                populs = new();
            }
            if (currentDialog == null) return;
            CloseRequested?.Invoke();
            currentDialog.HideDialog();
            HomeWindow.ShowWindow();
            populs.Hide();
        }
        public static void Show(string Title, string PrimaryButtonText, string SecondaryButtonText, string CloseButtonText, OceanContentDialogDefault DefaultButton, Microsoft.UI.Xaml.Controls.Grid Contents, XamlRoot root, int Width, int Height, OceanContentDialogType DialogType, Window ParentWindow)
        {
            bool secondaryvisible = !string.IsNullOrEmpty(SecondaryButtonText);
          
            currentDialog= OceanDialog.ShowDialog(
    Title,
    secondaryvisible,
    Contents,
 DefaultButton, CloseButtonText, PrimaryButtonText, Width, Height, DialogType, ParentWindow);

            if(populs == null)
            {
                populs = new();
            }
            //     dlg.CloseButtonText = CloseButtonText;
            populs.Hide();
            CenterDialog.CenterDialogRec(currentDialog, ParentWindow);
            currentDialog.Activate();
            currentDialog.PrimaryRequested += () =>
            {
                PrimaryRequested?.Invoke();
            };
            currentDialog.SecondaryRequested += () =>
            {
                SecondaryRequested?.Invoke();
            };
       
            if (DialogType == OceanContentDialogType.Movable || DialogType == OceanContentDialogType.Elevated)
            {
                
                populs.Show(root, "");

                currentDialog.CloseRequested += () =>
                {
                    CloseRequested?.Invoke();
                    currentDialog.HideDialog();
                    HomeWindow.ShowWindow();
                    populs.Hide();
                };
            }
            currentDialog.CloseRequested += () =>
            {
                CloseRequested?.Invoke();
                currentDialog.HideDialog();
                HomeWindow.ShowWindow();
            };
        }

    }
}
