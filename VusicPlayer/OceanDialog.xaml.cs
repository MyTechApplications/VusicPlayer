using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Interop;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using Windows.UI;
using Windows.UI.WindowManagement;
using WinRT;
using WinRT.Interop;
using AppWindow = Microsoft.UI.Windowing.AppWindow;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicPlayer
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OceanDialog : Window
    {
        Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController? acrylicController;
        Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration? configurationSource;
        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (configurationSource != null)
            {
                configurationSource.IsInputActive =
                    args.WindowActivationState != WindowActivationState.Deactivated;
            }

            // Reattach acrylic if needed
            if (acrylicController == null && DesktopAcrylicController.IsSupported())
            {
                TrySetAcrylicBackdrop(true);
            }
        }

        private T FindChild<T>(DependencyObject parent, string childName)
    where T : DependencyObject
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild &&
                    (child as FrameworkElement)?.Name == childName)
                {
                    return typedChild;
                }

                var result = FindChild<T>(child, childName);
                if (result != null)
                    return result;
            }

            return null!;
        }
        private void StartShimmer(Button button)
        {
            button.ApplyTemplate();

            var transform = FindChild<TranslateTransform>(button, "GradientTransform");
            if (transform == null)
                return;

            var animation = new DoubleAnimation
            {
                From = -1,
                To = 1,
                Duration = new Duration(TimeSpan.FromSeconds(2)),
                RepeatBehavior = RepeatBehavior.Forever
            };

            Storyboard.SetTarget(animation, transform);
            Storyboard.SetTargetProperty(animation, "X");

            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }
        public void HideDialog()
        {
            if (_appWindow == null) return;
            _appWindow.Hide();
        }
        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // Make sure any Mica/Acrylic controller is disposed
            if (acrylicController != null)
            {
                acrylicController.Dispose();
                acrylicController = null;
            }
            Activated -= Window_Activated;
            configurationSource = null;
        }

        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            if (configurationSource != null)
            {
                SetConfigurationSourceTheme();
            }
        }
        private void SetConfigurationSourceTheme()

        {if (configurationSource == null) return;
            switch (((FrameworkElement)this.Content).ActualTheme)
            {
                case ElementTheme.Dark: configurationSource.Theme = SystemBackdropTheme.Dark; break;
                case ElementTheme.Light: configurationSource.Theme = SystemBackdropTheme.Light; break;
                case ElementTheme.Default: configurationSource.Theme = SystemBackdropTheme.Default; break;
            }
        }

        bool TrySetAcrylicBackdrop(bool useAcrylicThin)
        {
            if (DesktopAcrylicController.IsSupported())
            {
                DispatcherQueue.EnsureSystemDispatcherQueue();

                // Hooking up the policy object
                configurationSource = new SystemBackdropConfiguration();
                Activated += Window_Activated;

             
                Closed += Window_Closed;
                ((FrameworkElement)Content).ActualThemeChanged += Window_ThemeChanged;

                // Initial configuration state.
                configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                acrylicController = new DesktopAcrylicController();
                acrylicController.Kind = useAcrylicThin ? DesktopAcrylicKind.Thin : DesktopAcrylicKind.Base;

                // Enable the system backdrop.

                acrylicController.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
                acrylicController.SetSystemBackdropConfiguration(configurationSource);

                return true; // Succeeded.
            }

            return false; // Acrylic is not supported on this system.
        }

        private void OceanDialog_CloseRequested()
        {
            HideDialog();
        }

        private string _titleText = "Title";
        private string _primarybuttonText = "";
        private string _closebuttonText = "Close";
        private string _secondarybuttonText = "Close";
        private bool _isSecondaryButtonVisible = false;
        public string TitleText
        {
            get => _titleText;
            set
            {
                _titleText = value;
                if (txtTitle != null)
                    txtTitle.Text = value;
            }
        }
     
        public string CloseButtonText
        {
            get => _closebuttonText;
            set
            {
                _closebuttonText = value;
                //   if (txtClose != null)
                //            txtClose.Text = value;
            }
        }
        public string PrimaryButtonIcon = "";

        [DllImport("user32.dll", SetLastError = true)]
       static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
        
        public event Action? CloseRequested;
        public event Action? PrimaryRequested;
        public event Action? SecondaryRequested;

        const int GWL_HWNDPARENT = -8;
        private static OceanDialog? _instance;
        void SetImage(Image img, string iconName)
        {
            if (!string.IsNullOrEmpty(iconName))
            {
                img.Source = new BitmapImage(
                    new Uri($"ms-appx:///Assets/{iconName}.png")
                );
            }
        }
        public void Setup(string Title, bool secondaryvisible, Grid contents, OceanContentDialogDefault dlg, string CloseButtonTex, string primarybtntex, string secondarybtntext, string pbi, string sbi, string cbi)
        {
            txtTitle.Text = Title;

            if (contents.Parent is Panel parentPanel)
                parentPanel.Children.Remove(contents);

            Contents.Children.Clear();
            Contents.Children.Add(contents);
            SetImage(imgPrimary, pbi);
            SetImage(imgSecondary, sbi);
            SetImage(imgClose, cbi);
            txtClose.Text = CloseButtonTex;
            btnClose.Visibility = string.IsNullOrEmpty(CloseButtonTex)
             ? Visibility.Collapsed
             : Visibility.Visible;
            txtPrimary.Text = primarybtntex;
            txtSecondary.Text = secondarybtntext;
            btnSecondary.Visibility = secondaryvisible
                ? Visibility.Visible
                : Visibility.Collapsed;
            if (primarybtntex == "")
            {
                btnPrimary.Visibility = Visibility.Collapsed;
            }
            else
            {
                btnPrimary.Visibility = Visibility.Visible;
            }
            var style = (Style)rootgrr.Resources["OceanShimmer"];

            btnClose.Style = null;
            btnPrimary.Style = null;
            btnSecondary.Style = null;

            Button? target = null;

            if (dlg == OceanContentDialogDefault.Primary)
                target = btnPrimary;
            else if (dlg == OceanContentDialogDefault.Secondary)
                target = btnSecondary;
            else if (dlg == OceanContentDialogDefault.Close)
                target = btnClose;

            if (target != null)
            {
                target.Style = style;
                StartShimmer(target);
            }
        }
        public static OceanDialog ShowDialog(string Title, bool secondaryvisible, Grid contents, OceanContentDialogDefault dlg, string CloseButtonTex,string primarybuttex,string secondarybuttontex, int Width, int Height, OceanContentDialogType DialogType, Window wind, string Primarybtnicon, string Secondarybtnicon, string Closebtnicon)
        {
            if (_instance == null)
            {
                _instance = new OceanDialog();
            }
            App.OceanDialogInstance = _instance;

            _instance.Setup(Title, secondaryvisible, contents, dlg, CloseButtonTex, primarybuttex, secondarybuttontex, Primarybtnicon, Secondarybtnicon, Closebtnicon);
            _instance.ResizeWind(Width, Height);
            _instance.Activate();
            return _instance;
        }
        private AppWindow? _appWindow;
        private void ResizeWind(int Width, int Height)
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            if (hwnd != IntPtr.Zero) // Guard against closed windows
            {
                var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
                if (appWindow != null) // Guard against uninitialized windows
                {
                    appWindow.Resize(new Windows.Graphics.SizeInt32(Width, Height));
                }
            }
         
            
        }

        public OceanDialog()
        {
            InitializeComponent();
            TrySetAcrylicBackdrop(true);
            DispatcherQueue.EnsureSystemDispatcherQueue();

            this.ExtendsContentIntoTitleBar = true;
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.IsShownInSwitchers = false;
            _appWindow = appWindow;
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
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke();
        }

        private void btnSecondary_Click(object sender, RoutedEventArgs e)
        {
            SecondaryRequested?.Invoke();
        }

        private void btnPrimary_Click(object sender, RoutedEventArgs e)
        {
            PrimaryRequested?.Invoke();
        }

        private void ChangeDialogSize_Click(object sender, RoutedEventArgs e)
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        
            appWindow.Resize(new Windows.Graphics.SizeInt32(400, 260));
        }
        public void ClearInternalEvents()
        {
            PrimaryRequested = null;
            SecondaryRequested = null;
            CloseRequested = null;
        }
        private void DragRegion_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            e.Handled = true;

        }
    }
}
