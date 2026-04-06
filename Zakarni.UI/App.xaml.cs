using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Zakarni.Core.Interfaces;
using Zakarni.Core.Services;
using Zakarni.Core.Models;
using Zakarni.UI.ViewModels;
using System;

namespace Zakarni.UI
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;
        public static MainWindow? MainWindow { get; private set; }
        public static Window? FloatingWidget { get; private set; }

        public static H.NotifyIcon.TaskbarIcon? TrayIcon { get; private set; }

        public static Microsoft.UI.Dispatching.DispatcherQueue? UIDispatcher { get; private set; }

        private static System.Threading.Mutex? _mutex;

        public App()
        {
            // Single Instance Check
            _mutex = new System.Threading.Mutex(true, "Zakarni_SingleInstance_Mutex_992288", out bool isFirstInstance);
            if (!isFirstInstance)
            {
                System.Environment.Exit(0);
                return;
            }

            this.InitializeComponent();

            var sc = new ServiceCollection();

            // Core services
            sc.AddSingleton<ISettingsService, SettingsService>();
            sc.AddSingleton<ILocationService, LocationService>();
            sc.AddSingleton<IPrayerTimeService, PrayerTimeService>();
            sc.AddSingleton<IAudioService, AudioService>();
            sc.AddSingleton<ScheduleTimerService>();
            sc.AddSingleton<QuranService>();
            sc.AddSingleton<AthkarService>();

            // ViewModels
            sc.AddTransient<MainViewModel>();
            sc.AddTransient<SettingsViewModel>();
            sc.AddTransient<QuranViewModel>();
            sc.AddTransient<TodoViewModel>();
            sc.AddTransient<AthkarViewModel>();

            Services = sc.BuildServiceProvider();

            // Load persisted settings
            var settings = Services.GetRequiredService<ISettingsService>();
            settings.Load();

            SetLanguage(settings.CurrentLanguage);

            // Start background scheduler
            var timer = Services.GetRequiredService<ScheduleTimerService>();
            _ = timer.StartAsync();
        }

        private void SetLanguage(Language language)
        {
            var culture = language == Language.Arabic ? "ar-SA" : "en-US";
            
            var resourceManager = new Microsoft.Windows.ApplicationModel.Resources.ResourceManager();
            var resourceContext = resourceManager.CreateResourceContext();
            resourceContext.QualifierValues["Language"] = culture;
            
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(culture);
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(culture);
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            UIDispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            InitializeTrayIcon();
            
            // On startup, we show the Floating Widget
            ShowFloatingWidget();
        }

        private void InitializeTrayIcon()
        {
            TrayIcon = new H.NotifyIcon.TaskbarIcon
            {
                ToolTipText = "Zakarni"
            };

            try
            {
                var iconPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Assets", "Zakarni.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    TrayIcon.IconSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new System.Uri(iconPath));
                }
            }
            catch { }

            // Context Menu
            var menu = new MenuFlyout();

            var toggleMainItem = new MenuFlyoutItem { Text = "Show / Hide Dashboard" };
            toggleMainItem.Click += (s, e) => ToggleMainWindow();
            
            var toggleWidgetItem = new MenuFlyoutItem { Text = "Show / Hide Floating Widget" };
            toggleWidgetItem.Click += (s, e) => ToggleFloatingWidget();
            
            var exitItem = new MenuFlyoutItem { Text = "Exit Zakarni" };
            exitItem.Click += (s, e) => ExitApplication();

            menu.Items.Add(toggleMainItem);
            menu.Items.Add(toggleWidgetItem);
            menu.Items.Add(new MenuFlyoutSeparator());
            menu.Items.Add(exitItem);

            TrayIcon.ContextFlyout = menu;
            TrayIcon.LeftClickCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => ToggleMainWindow());
            TrayIcon.ForceCreate(); // Initialize tray
        }

        public static void ApplyTheme(Window window)
        {
            if (window.Content is FrameworkElement rootElement)
            {
                var settings = Services.GetRequiredService<ISettingsService>();
                rootElement.RequestedTheme = settings.ThemeMode switch
                {
                    Zakarni.Core.Models.ThemeMode.Light => ElementTheme.Light,
                    Zakarni.Core.Models.ThemeMode.Dark => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };
            }
        }

        public static void ShowFloatingWidget()
        {
            // If MainWindow is visible, hide it (strictly one at a time)
            if (MainWindow != null && MainWindow.Visible)
            {
                MainWindow.AppWindow.Hide();
            }

            if (FloatingWidget == null)
            {
                FloatingWidget = new PrayerFloatingWidgetWindow();
                ApplyTheme(FloatingWidget);
                FloatingWidget.Closed += (s, e) => { FloatingWidget = null; };
            }
            FloatingWidget.AppWindow.Show();
            FloatingWidget.Activate();
        }

        public static void HideFloatingWidget()
        {
            if (FloatingWidget != null)
            {
                FloatingWidget.AppWindow.Hide();
            }
        }
        
        public static void ToggleFloatingWidget()
        {
            var dispatcher = UIDispatcher ?? TrayIcon?.DispatcherQueue;
            if (dispatcher == null) return;
            
            dispatcher.TryEnqueue(() =>
            {
                if (FloatingWidget == null || !FloatingWidget.Visible) ShowFloatingWidget();
                else HideFloatingWidget();
            });
        }

        public static void ShowMainWindow()
        {
            // Always prioritize the primary UI dispatcher
            var dispatcher = UIDispatcher ?? MainWindow?.DispatcherQueue ?? FloatingWidget?.DispatcherQueue ?? TrayIcon?.DispatcherQueue;
            
            if (dispatcher != null && !dispatcher.HasThreadAccess)
            {
                dispatcher.TryEnqueue(() => ShowMainWindow());
                return;
            }

            // Close the widget before showing the main window
            HideFloatingWidget();

            if (MainWindow == null)
            {
                try
                {
                    MainWindow = new MainWindow();
                    
                    var settings = Services.GetRequiredService<ISettingsService>();
                    if (MainWindow.Content is FrameworkElement root)
                    {
                        root.FlowDirection = settings.CurrentLanguage == Language.Arabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                    }

                    ApplyTheme(MainWindow);
                    
                    MainWindow.Closed += (s, e) => 
                    { 
                        MainWindow = null; 
                        ShowFloatingWidget(); 
                    };
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error creating MainWindow: {ex.Message}");
                    return;
                }
            }
            
            MainWindow.AppWindow.Show();
            MainWindow.Activate();
        }

        public static void ToggleMainWindow()
        {
            var dispatcher = UIDispatcher ?? MainWindow?.DispatcherQueue ?? FloatingWidget?.DispatcherQueue ?? TrayIcon?.DispatcherQueue;
            if (dispatcher == null) return;

            dispatcher.TryEnqueue(() =>
            {
                if (MainWindow == null || !MainWindow.Visible)
                {
                    ShowMainWindow();
                }
                else
                {
                    MainWindow.AppWindow.Hide();
                    ShowFloatingWidget();
                }
            });
        }

        public static void ExitApplication()
        {
            TrayIcon?.Dispose();
            Microsoft.UI.Xaml.Application.Current.Exit();
        }
    }
}
