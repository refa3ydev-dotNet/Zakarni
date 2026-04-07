using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinRT.Interop;
using Zakarni.Core.Interfaces;

namespace Zakarni.UI
{
    public sealed partial class PrayerFloatingWidgetWindow : Window
    {
        public ViewModels.MainViewModel ViewModel { get; }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const uint WM_NCLBUTTONDOWN = 0x00A1;
        private const uint HTCAPTION = 2;

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_BORDER_COLOR = 34;

        private const int DWMWCP_DEFAULT = 0;
        private const int DWMWCP_DONOTROUND = 1;
        private const int DWMWCP_ROUND = 2;
        private const int DWMWCP_ROUNDSMALL = 3;

        public PrayerFloatingWidgetWindow()
        {
            InitializeComponent();

            ViewModel = App.Services.GetRequiredService<ViewModels.MainViewModel>();
            var settings = App.Services.GetRequiredService<ISettingsService>();

            SystemBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
            ExtendsContentIntoTitleBar = true;

            ConfigureWindowChrome(settings.KeepFloatingWidgetAlwaysOnTop);
            ResizeWindow();
            SetWindowIcon();
            PositionAtTopCenter();

            Closed += (s, e) => ViewModel.Deactivate();
        }

        private void ConfigureWindowChrome(bool alwaysOnTop)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.SetBorderAndTitleBar(false, false);
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
                presenter.IsAlwaysOnTop = alwaysOnTop;
            }

            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);

            int darkMode = 1;
            DwmSetWindowAttribute(
                hwnd,
                DWMWA_USE_IMMERSIVE_DARK_MODE,
                ref darkMode,
                Marshal.SizeOf<int>());

            int cornerPreference = DWMWCP_ROUND;
            DwmSetWindowAttribute(
                hwnd,
                DWMWA_WINDOW_CORNER_PREFERENCE,
                ref cornerPreference,
                Marshal.SizeOf<int>());

            int borderColor = 0x000000;
            DwmSetWindowAttribute(
                hwnd,
                DWMWA_BORDER_COLOR,
                ref borderColor,
                Marshal.SizeOf<int>());
        }

        private void ResizeWindow()
        {
            AppWindow.Resize(new SizeInt32(450, 72));
        }

        private void SetWindowIcon()
        {
            try
            {
                var iconPath = System.IO.Path.Combine(
                    AppContext.BaseDirectory,
                    "Assets",
                    "Zakarni.ico");

                if (System.IO.File.Exists(iconPath))
                {
                    AppWindow.SetIcon(iconPath);
                }
            }
            catch
            {
            }
        }

        private void PositionAtTopCenter()
        {
            try
            {
                var display = DisplayArea.Primary;
                var workArea = display.WorkArea;

                const int widgetWidth = 460;
                const int margin = 16;

                int x = workArea.X + (workArea.Width - widgetWidth) / 2;
                int y = workArea.Y + margin;

                AppWindow.Move(new PointInt32(x, y));
            }
            catch
            {
            }
        }

        private static int MakeColorRef(byte r, byte g, byte b)
        {
            return r | (g << 8) | (b << 16);
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var properties = e.GetCurrentPoint(Content).Properties;

            if (properties.IsLeftButtonPressed)
            {
                var hwnd = WindowNative.GetWindowHandle(this);
                ReleaseCapture();
                SendMessage(hwnd, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
            }
        }

        private void OnOpenMain(object sender, RoutedEventArgs e)
        {
            var dispatcher = App.UIDispatcher ?? DispatcherQueue;
            dispatcher?.TryEnqueue(() => App.ShowMainWindow());
        }
    }
}