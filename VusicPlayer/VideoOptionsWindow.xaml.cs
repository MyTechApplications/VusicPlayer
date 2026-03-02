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
using WinRT;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicPlayer
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class VideoOptionsWindow : Window
    {
        public event Action? CloseRequested;
        public event Action? PrimaryRequested;
        public event Action? SecondaryRequested;

        const int GWL_HWNDPARENT = -8;
        private static VideoOptionsWindow? _instance;
        private AppWindow? _appWindow;

        Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController? acrylicController;
        Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration? configurationSource;

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);


        public VideoOptionsWindow()
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
            var parentHwnd = WindowNative.GetWindowHandle(App.VideoPlayerWindowInstance);
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
            this.SetTitleBar(DragRegion);
            _subclassProc = new SubclassProc(WindowSubclassCallback);

            // 3. Set the subclass
            SetWindowSubclass(dialogHwnd, _subclassProc, 0, IntPtr.Zero);
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.SetBorderAndTitleBar(true, false);
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
                presenter.IsResizable = false;
            }
        }
        private IntPtr WindowSubclassCallback(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, uint uIdSubclass, IntPtr dwRefData)
        {
            // Check if the message is a double click on the non-client area (title bar)
            if (uMsg == WM_NCLBUTTONDBLCLK && wParam.ToInt32() == HTCAPTION)
            {
                // Return 0 to indicate we've "handled" the message, preventing the maximize action
                return IntPtr.Zero;
            }

            // Pass all other messages to the default handler
            return DefSubclassProc(hWnd, uMsg, wParam, lParam);
        }
        private const int WM_NCLBUTTONDBLCLK = 0x00A3;
        private const int HTCAPTION = 2;

        // Delegate for the subclass procedure
        private delegate IntPtr SubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, uint uIdSubclass, IntPtr dwRefData);

        [DllImport("Comctl32.dll", SetLastError = true)]
        private static extern bool SetWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, uint uIdSubclass, IntPtr dwRefData);

        [DllImport("Comctl32.dll", SetLastError = true)]
        private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        private SubclassProc _subclassProc;
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

        {
            if (configurationSource == null) return;
            switch (((FrameworkElement)this.Content).ActualTheme)
            {
                case ElementTheme.Dark: configurationSource.Theme = SystemBackdropTheme.Dark; break;
                case ElementTheme.Light: configurationSource.Theme = SystemBackdropTheme.Light; break;
                case ElementTheme.Default: configurationSource.Theme = SystemBackdropTheme.Default; break;
            }
        }
        private void OceanDialog_CloseRequested()
        {
            HideDialog();
        }
        public void Setup( Grid contents)
        {
            if (contents.Parent is Panel parentPanel)
                parentPanel.Children.Remove(contents);

            Contents.Children.Clear();
            Contents.Children.Add(contents);
            StartShimmer(btnPrimary);
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

        public static VideoOptionsWindow ShowDialog(Grid contents)
        {
            if (_instance == null)
            {
                _instance = new VideoOptionsWindow();
            }
            App.OceanDialogInstance = _instance;

            _instance.Setup(contents);
            _instance.ResizeWind(600, 600);
            _instance.Activate();
            return _instance;
        }
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
    }
}
