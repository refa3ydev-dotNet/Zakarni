using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using Microsoft.Extensions.DependencyInjection;
using Windows.Graphics;
using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Input;
using Zakarni.Core.Interfaces;

namespace Zakarni.UI
{
    public sealed partial class PrayerFloatingWidgetWindow : Window
    {
        public ViewModels.MainViewModel ViewModel { get; }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const uint WM_NCLBUTTONDOWN = 0x00A1;
        private const uint HTCAPTION = 2;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        public PrayerFloatingWidgetWindow()
        {
            this.InitializeComponent();
            this.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();

            ViewModel = App.Services.GetRequiredService<ViewModels.MainViewModel>();
            var settings = App.Services.GetRequiredService<ISettingsService>();

            // Configure as compact overlay-style widget
            var presenter = this.AppWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
                presenter.SetBorderAndTitleBar(false, false);
                presenter.IsAlwaysOnTop = settings.KeepFloatingWidgetAlwaysOnTop;
            }

            // Hide from taskbar
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);

            this.AppWindow.Resize(new SizeInt32(450, 72));

            // Set Application Icon correctly
            try
            {
                var iconPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Assets", "Zakarni.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    this.AppWindow.SetIcon(iconPath);
                }
            }
            catch { /* Silently fallback */ }

            PositionAtTopCenter();

            this.Closed += (s, e) =>
            {
                ViewModel.Deactivate();
            };
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var properties = e.GetCurrentPoint(this.Content).Properties;
            if (properties.IsLeftButtonPressed)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                ReleaseCapture();
                SendMessage(hwnd, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
            }
        }

        private void OnOpenMain(object sender, RoutedEventArgs e)
        {
            // Enqueue the transition to avoid blocking the UI thread and causing black screens/crashes
            // during simultaneous window destruction and creation.
            var dispatcher = App.UIDispatcher ?? this.DispatcherQueue;
            dispatcher?.TryEnqueue(() =>
            {
                App.ShowMainWindow();
            });
        }

        private void PositionAtTopCenter()
        {
            try
            {
                var display = DisplayArea.Primary;
                var workArea = display.WorkArea;
                int widgetWidth = 450;
                int margin = 16;

                int x = workArea.X + (workArea.Width - widgetWidth) / 2;
                int y = workArea.Y + margin;

                this.AppWindow.Move(new PointInt32(x, y));
            }
            catch { /* Fallback */ }
        }
    }
}
