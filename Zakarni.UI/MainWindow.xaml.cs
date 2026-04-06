using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace Zakarni.UI
{
    public sealed partial class MainWindow : Window
    {
        public ViewModels.MainViewModel ViewModel { get; }
        public ViewModels.SettingsViewModel SettingsVM { get; }
        public ViewModels.QuranViewModel QuranVM { get; }
        public ViewModels.TodoViewModel TodoVM { get; }
        public ViewModels.AthkarViewModel AthkarVM { get; }

        private readonly int _minWidth = 1100;
        private readonly int _minHeight = 680;
        private readonly int _maxWidth = 1100;

        public MainWindow()
        {
            // IMPORTANT: Initialize ViewModels BEFORE InitializeComponent for safe x:Bind resolution
            try
            {
                if (App.Services == null) throw new System.Exception("App Services not initialized.");
                
                ViewModel = App.Services.GetRequiredService<ViewModels.MainViewModel>();
                SettingsVM = App.Services.GetRequiredService<ViewModels.SettingsViewModel>();
                QuranVM = App.Services.GetRequiredService<ViewModels.QuranViewModel>();
                TodoVM = App.Services.GetRequiredService<ViewModels.TodoViewModel>();
                AthkarVM = App.Services.GetRequiredService<ViewModels.AthkarViewModel>();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DI Error in MainWindow: {ex.Message}");
                // Provide empty/null safe fallbacks if possible or rethrow if fatal
                throw;
            }

            this.InitializeComponent();
            this.Title = "Zakarni";
            
            // Safe backdrop initialization
            try { this.SystemBackdrop = new MicaBackdrop(); } catch { }
            
            // 1. Set Default Window Size
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(_minWidth, _minHeight));

            // 2. Enforce Minimum Window Size
            this.AppWindow.Changed += AppWindow_Changed;

            // 3. Set Application Icon
            try
            {
                var icoPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Assets", "Zakarni.ico");
                if (System.IO.File.Exists(icoPath))
                {
                    this.AppWindow.SetIcon(icoPath);
                }
            }
            catch { }

            // Custom Title Bar
            this.ExtendsContentIntoTitleBar = true;
            
            var scheduler = App.Services.GetRequiredService<Zakarni.Core.Services.ScheduleTimerService>();
            scheduler.ReminderTriggered += OnReminderTriggered;

            this.AppWindow.Closing += AppWindow_Closing;

            // Initialize navigation
            RootNavigationView.SelectedItem = RootNavigationView.MenuItems[0];
        }

        private void AppWindow_Changed(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowChangedEventArgs args)
        {
            if (args.DidSizeChange)
            {
                bool needsResize = false;
                int newWidth = sender.Size.Width;
                int newHeight = sender.Size.Height;

                if (newWidth < _minWidth) { newWidth = _minWidth; needsResize = true; }
                if (newWidth > _maxWidth) { newWidth = _maxWidth; needsResize = true; }
                if (newHeight < _minHeight) { newHeight = _minHeight; needsResize = true; }

                if (needsResize)
                {
                    sender.Resize(new Windows.Graphics.SizeInt32(newWidth, newHeight));
                }
            }
        }

        private void OnReminderTriggered(string title, string message)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                // Using default icon to avoid compilation errors with NotificationIcon/GuidanceIcon
                App.TrayIcon?.ShowNotification(title, message);
            });
        }

        private void OnNavigationItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                NavigateTo("settings");
            }
            else if (args.InvokedItemContainer is Microsoft.UI.Xaml.Controls.NavigationViewItem item)
            {
                var tag = item.Tag?.ToString();
                if (tag != null) NavigateTo(tag);
            }
        }

        private void OnSettingsNavClick(object sender, RoutedEventArgs e)
        {
            RootNavigationView.SelectedItem = RootNavigationView.SettingsItem;
            NavigateTo("settings");
        }

        private void NavigateTo(string tag)
        {
            HomeView.Visibility = tag == "home" ? Visibility.Visible : Visibility.Collapsed;
            QuranView.Visibility = tag == "quran" ? Visibility.Visible : Visibility.Collapsed;
            TodoView.Visibility = tag == "todo" ? Visibility.Visible : Visibility.Collapsed;
            AthkarView.Visibility = tag == "athkar" ? Visibility.Visible : Visibility.Collapsed;
            SettingsView.Visibility = tag == "settings" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void NavigateToHome() => NavigateTo("home");
        private void NavigateToSettings() => NavigateTo("settings");

        public Visibility ConvertBoolToVisibility(bool isVisible) => isVisible ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ConvertInvertedBoolToVisibility(bool isVisible) => !isVisible ? Visibility.Visible : Visibility.Collapsed;

        private void OnViewFullSchedule(object sender, RoutedEventArgs e)
        {
            // Scroll to the Today's Schedule section using BringIntoView
            TodayScheduleSection.StartBringIntoView();
        }

        private void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender,
            Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
        {
            var settings = App.Services.GetRequiredService<Core.Interfaces.ISettingsService>();
            
            // NEVER let the window actually close in WinUI 3 unless the whole app is exiting, 
            // as recreating a window on the same thread causes black screens and crashes.
            args.Cancel = true;
            this.AppWindow.Hide();

            if (settings.MinimizeToTray)
            {
                // Show floating widget as a mini-dashboard
                App.ShowFloatingWidget();
            }
            else
            {
                // If MinimizeToTray is false, we might want to exit the app or still show the widget.
                // Given the app's widget-centric design, showing the widget is the safest fallback.
                App.ShowFloatingWidget();
            }
        }

        public Microsoft.UI.Xaml.Media.Brush GetCardBackground(bool isActive) =>
            isActive ? (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["BrandAccentBrush"] : (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundBrush"];

        public Microsoft.UI.Xaml.Media.Brush GetCardBorder(bool isActive) =>
            isActive ? (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["BrandAccentBrush"] : (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBorderBrush"];

        public Microsoft.UI.Xaml.Media.Brush GetCardForeground(bool isActive) =>
            isActive ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 0, 61, 50)) : (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];

        private void OnDeleteTask(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Core.Models.TodoItem item)
            {
                TodoVM.DeleteTaskCommand.Execute(item);
            }
        }

        private void OnOpenReadingMode(object sender, RoutedEventArgs e)
        {
            if (QuranVM.SelectedSurah != null)
            {
                var readingWindow = new QuranReadingWindow(QuranVM.SelectedSurah);
                readingWindow.Activate();
            }
        }

        private void OnSaveSettings(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            SettingsVM.Save();
            App.ApplyTheme(this);
            if (App.FloatingWidget != null) App.ApplyTheme(App.FloatingWidget);
            
            // Update FlowDirection for RTL support
            var settings = App.Services.GetRequiredService<Core.Interfaces.ISettingsService>();
            if (this.Content is FrameworkElement root)
            {
                root.FlowDirection = settings.CurrentLanguage == Core.Models.Language.Arabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            }

            RootNavigationView.SelectedItem = RootNavigationView.MenuItems[0];
            NavigateToHome();
        }
    }
}
