using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT.Interop;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicPlayer
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EnlargeImage : Window
    {
        public EnlargeImage()
        {
            InitializeComponent();
            imgLarge.Source = new BitmapImage(new Uri(TempImagePath.Path));
            this.ExtendsContentIntoTitleBar = true;
            txtTitle.Text = "Enlarged View of "+Path.GetFileName(TempImagePath.Path);
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.IsShownInSwitchers = false;
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
            var parentHwnd = WindowNative.GetWindowHandle(App.HomeWindowInstance);
            var dialogHwnd = WindowNative.GetWindowHandle(this);
            if (dialogHwnd != IntPtr.Zero && parentHwnd != IntPtr.Zero)
            {
                // Check if they are already parented to avoid redundant OS calls
                IntPtr currentParent = GetWindowLongPtr(dialogHwnd, GWL_HWNDPARENT);

                if (currentParent != parentHwnd)
                {
                    SetWindowLongPtr(dialogHwnd, GWL_HWNDPARENT, parentHwnd);
                }
            }


            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.SetBorderAndTitleBar(true, false);
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
                presenter.IsResizable = false;

            }
            SetTitleBar(titlebar);
        }
        const int GWL_HWNDPARENT = -8;
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
        private void btnCloseEnlarge_Click(object sender, RoutedEventArgs e)
        {
            HomeWindow.ShowWindow();
            this.Close();
        }

        private void SliderReuse_ValueChanged(double obj)
        {
            float? myNullableFloat = (float)obj;
            ImageScanner.ChangeView(null, null, myNullableFloat);
            txtZoom.Text = ImageScanner.ZoomFactor.ToString() + "%";

        }
        private bool _isPanning = false;
        private Point _lastPoint;

        private void ImageScanner_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var ptr = e.GetCurrentPoint(ImageScanner);
            if (ptr.Properties.IsLeftButtonPressed)
            {
                _isPanning = true;
                _lastPoint = ptr.Position;
                grdMain.SetCursor(InputSystemCursor.Create(InputSystemCursorShape.Hand));
            }
        }
        public void ChangeCursor(InputCursor cursor)
        {
            
        }
        private void ImageScanner_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isPanning) return;

            var ptr = e.GetCurrentPoint(ImageScanner);
            var currentPoint = ptr.Position;

            // Calculate how much the mouse moved
            double deltaX = _lastPoint.X - currentPoint.X;
            double deltaY = _lastPoint.Y - currentPoint.Y;

            // Update ScrollViewer offsets
            ImageScanner.ChangeView(ImageScanner.HorizontalOffset + deltaX,
                                    ImageScanner.VerticalOffset + deltaY, null);

            _lastPoint = currentPoint;
        }

        private void ImageScanner_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _isPanning = false;
                grdMain.SetCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
        }
        private void btnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            ImageScanner.ChangeView(null, null, ImageScanner.ZoomFactor + 0.5f);
        }

        private void btnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ImageScanner.ChangeView(null, null, ImageScanner.ZoomFactor - 0.5f);
        }

        private void ImageScanner_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            float currentZoom = ImageScanner.ZoomFactor;

            // 2. Update the Text (1.5 -> "150%")
            txtZoom.Text = $"{(int)(currentZoom * 100)}%";

            // 3. Update the Slider (Value is double, so cast it)
            // We temporarily remove the event handler to prevent an infinite loop
            sldZoom.ValueChanged -= SliderReuse_ValueChanged;
            sldZoom.Value = currentZoom;
            sldZoom.ValueChanged += SliderReuse_ValueChanged;
        }

        private void txtZoom_Tapped(object sender, TappedRoutedEventArgs e)
        {
            tipZoom.IsOpen = true;
            txtZoomTip.Value = Convert.ToDouble(txtZoom.Text);

        }
        private void ProcessZoomInput()
        {
            // 1. Clean the string (remove % if they typed it)
            string rawInput = txtZoom.Text.Replace("%", "").Trim();

            // 2. Try to parse to double
            if (double.TryParse(rawInput, out double percentValue))
            {
                // 3. Convert percentage to factor (e.g., 200 -> 2.0)
                float zoomFactor = (float)(percentValue / 100.0);

                // 4. Clamp the value so it doesn't crash the ScrollViewer
                // (Ensures it stays between your Min and Max zoom)
                zoomFactor = Math.Clamp(zoomFactor, (float)ImageScanner.MinZoomFactor, (float)ImageScanner.MaxZoomFactor);

                // 5. Apply to ScrollViewer
                ImageScanner.ChangeView(null, null, zoomFactor);

                // 6. Sync the Slider
                sldZoom.Value = zoomFactor;
            }
            else
            {
                // Reset to current zoom if they typed gibberish
                txtZoom.Text = $"{(int)(ImageScanner.ZoomFactor * 100)}";
            }
        }
        private void btnOpenFileLoc_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(TempImagePath.Path))
            {
                Process.Start("explorer.exe", $"/select,\"{TempImagePath.Path}\"");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ProcessZoomInput();
        }
    }
}
