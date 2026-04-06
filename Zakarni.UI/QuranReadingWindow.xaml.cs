using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Documents;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zakarni.Core.Models;
using Zakarni.Core.Interfaces;
using Zakarni.Core.Services;
using Zakarni.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Windows.Media.Core;

namespace Zakarni.UI;

public sealed partial class QuranReadingWindow : Window
{
    private readonly QuranService _quranService;
    public QuranViewModel ViewModel { get; }
    
    private readonly List<QuranSurah> _surahs;
    private QuranSurah _currentSurah;
    
    // Minimum window constraints
    private readonly int _minWidth = 1100;
    private readonly int _minHeight = 680;
    private readonly int _maxWidth = 1600;

    private QuranAyah? _currentlyPlayingAyah;

    public QuranReadingWindow(QuranSurah startSurah)
    {
        this.InitializeComponent();
        this.SystemBackdrop = new MicaBackdrop();

        // 1. Set Default Window Size
        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1280, 760));

        // 2. Enforce Minimum Window Size
        this.AppWindow.Changed += AppWindow_Changed;

        // 3. Application Icon
        try
        {
            var iconPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Assets", "Zakarni.ico");
            if (System.IO.File.Exists(iconPath))
            {
                this.AppWindow.SetIcon(iconPath);
            }
        }
        catch { /* Silently fallback */ }

        var settings = App.Services.GetRequiredService<ISettingsService>();
        if (this.Content is FrameworkElement root)
        {
            root.FlowDirection = settings.CurrentLanguage == Language.Arabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }
        
        _quranService = App.Services.GetRequiredService<QuranService>();
        ViewModel = App.Services.GetRequiredService<QuranViewModel>();
        
        _surahs = _quranService.GetSurahs();
        _currentSurah = _surahs.FirstOrDefault(s => s.Number == startSurah.Number) ?? startSurah;

        // Setup MediaPlayer event
        AudioPlayer.MediaPlayer.MediaEnded += MediaPlayer_MediaEnded;

        _ = LoadSurahAsync();
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

    private async Task LoadSurahAsync()
    {
        if (_currentSurah == null) return;

        // Trigger full page loading UI
        ViewModel.IsLoadingSurah = true;

        SurahNameTitle.Text = "سورة " + _currentSurah.Name;
        SurahInfoText.Text = $"Surah {_currentSurah.Number} • {_currentSurah.EnglishName}";

        ErrorText.Visibility = Visibility.Collapsed;

        // Reset ScrollViewer to top
        MainScrollViewer?.ChangeView(null, 0, null);

        // Fetch Tafsir and Audio from API
        await ViewModel.FetchSurahDetailsAsync(_currentSurah);

        if (!string.IsNullOrEmpty(ViewModel.ReadingErrorMessage))
        {
            ErrorText.Text = ViewModel.ReadingErrorMessage;
            ErrorText.Visibility = Visibility.Visible;
        }

        RenderContinuousSurah(_currentSurah);

        // Hide full page loading UI
        ViewModel.IsLoadingSurah = false;
    }

    private void RenderContinuousSurah(QuranSurah surah)
    {
        SurahContentPanel.Children.Clear();

        var primaryBrush = (SolidColorBrush)Application.Current.Resources["TextPrimaryBrush"];
        var accentBrush = (SolidColorBrush)Application.Current.Resources["BrandAccentBrush"];
        var alphaBrush = (SolidColorBrush)Application.Current.Resources["BrandAccentAlphaBrush"];

        // 1. Beautiful Surah Header
        var headerBorder = new Border
        {
            Background = alphaBrush,
            BorderBrush = accentBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(64, 24, 64, 24),
            Margin = new Thickness(0, 0, 0, 32),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = new StackPanel
            {
                Spacing = 16,
                Children = 
                {
                    new TextBlock { Text = "سورة " + surah.Name, FontSize = 40, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = accentBrush, HorizontalAlignment = HorizontalAlignment.Center },
                    new TextBlock { Text = surah.EnglishName, FontSize = 16, Foreground = primaryBrush, Opacity = 0.8, HorizontalAlignment = HorizontalAlignment.Center }
                }
            }
        };
        SurahContentPanel.Children.Add(headerBorder);

        // 2. Basmalah
        if (surah.Number != 9 && surah.Number != 1)
        {
            var basmalah = new TextBlock
            {
                Text = "بِسْمِ اللَّهِ الرَّحْمٰنِ الرَّحِيمِ",
                FontSize = 32,
                Foreground = primaryBrush,
                FontFamily = (FontFamily)Application.Current.Resources["QuranFont"],
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 48)
            };
            SurahContentPanel.Children.Add(basmalah);
        }

        // 3. Group ayahs by Page to prevent RichTextBlock rendering limit on huge surahs like Al-Baqarah
        var pages = surah.Ayahs.GroupBy(a => a.PageNumber).OrderBy(g => g.Key);

        foreach (var page in pages)
        {
            var richText = new RichTextBlock 
            { 
                FontFamily = (FontFamily)Application.Current.Resources["QuranFont"], 
                FontSize = 32, 
                Foreground = primaryBrush, 
                TextAlignment = TextAlignment.Justify, 
                LineHeight = 64, 
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 24) // Space between pages
            };

            var currentPara = new Paragraph();
            richText.Blocks.Add(currentPara);

            foreach (var ayah in page)
            {
                var hyperlink = new Hyperlink { UnderlineStyle = UnderlineStyle.None, Foreground = primaryBrush };
                
                var capturedAyah = ayah;
                hyperlink.Click += (s, e) => OnAyahClicked(capturedAyah);

                hyperlink.Inlines.Add(new Run { Text = ayah.Text });
                currentPara.Inlines.Add(hyperlink);
                
                var container = new InlineUIContainer();
                var badgeBorder = new Border
                {
                    Width = 36,
                    Height = 36,
                    CornerRadius = new CornerRadius(18),
                    Background = alphaBrush,
                    BorderBrush = accentBrush,
                    BorderThickness = new Thickness(1.5),
                    Margin = new Thickness(12, 0, 12, -8),
                    Child = new TextBlock
                    {
                        Text = ayah.NumberInSurah.ToString(),
                        Foreground = accentBrush,
                        FontSize = 14,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                container.Child = badgeBorder;
                
                currentPara.Inlines.Add(container);
                currentPara.Inlines.Add(new Run { Text = " " });
            }

            SurahContentPanel.Children.Add(richText);

            // Add a subtle page number indicator between pages to break up long blocks
            var pageIndicator = new TextBlock 
            { 
                Text = $"Page {page.Key}", 
                FontSize = 14, 
                Foreground = primaryBrush, 
                Opacity = 0.3, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                Margin = new Thickness(0, 0, 0, 48) 
            };
            SurahContentPanel.Children.Add(pageIndicator);
        }
    }

    private void OnAyahClicked(QuranAyah ayah)
    {
        ViewModel.ActiveAyah = ayah;
        ViewModel.IsSidePanelOpen = true;
    }

    private void OnCloseSidePanel(object sender, RoutedEventArgs e)
    {
        ViewModel.IsSidePanelOpen = false;
    }

    private void OnPlayActiveAyah(object sender, RoutedEventArgs e)
    {
        if (ViewModel.ActiveAyah != null && !string.IsNullOrEmpty(ViewModel.ActiveAyah.AudioUrl))
        {
            PlayAudio(ViewModel.ActiveAyah);
        }
    }

    private void PlayAudio(QuranAyah ayah)
    {
        _currentlyPlayingAyah = ayah;
        NowPlayingText.Text = $"Playing: Surah {ayah.SurahNameAr}, Ayah {ayah.NumberInSurah}";
        
        AudioPlayer.MediaPlayer.Source = MediaSource.CreateFromUri(new System.Uri(ayah.AudioUrl));
        AudioPlayer.MediaPlayer.Play();
        
        PlayPauseIcon.Glyph = "\uE769"; // Pause icon
    }

    private void OnPlayPauseToggle(object sender, RoutedEventArgs e)
    {
        var session = AudioPlayer.MediaPlayer.PlaybackSession;
        if (session.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Playing)
        {
            AudioPlayer.MediaPlayer.Pause();
            PlayPauseIcon.Glyph = "\uE768";
        }
        else if (session.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Paused)
        {
            AudioPlayer.MediaPlayer.Play();
            PlayPauseIcon.Glyph = "\uE769";
        }
        else if (_currentlyPlayingAyah == null && ViewModel.ActiveAyah != null)
        {
            PlayAudio(ViewModel.ActiveAyah);
        }
        else if (_currentlyPlayingAyah == null && _currentSurah.Ayahs.Any())
        {
            // Play first ayah of surah
            PlayAudio(_currentSurah.Ayahs.First());
        }
    }

    private void OnStopAudio(object sender, RoutedEventArgs e)
    {
        AudioPlayer.MediaPlayer.Pause();
        AudioPlayer.MediaPlayer.Source = null;
        PlayPauseIcon.Glyph = "\uE768";
        NowPlayingText.Text = "Stopped";
        _currentlyPlayingAyah = null;
    }

    private void MediaPlayer_MediaEnded(Windows.Media.Playback.MediaPlayer sender, object args)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            PlayPauseIcon.Glyph = "\uE768"; // Play icon
            NowPlayingText.Text = "Finished";
        });
    }

    private void OnNextSurah(object sender, RoutedEventArgs e) 
    { 
        int idx = _surahs.IndexOf(_currentSurah);
        if (idx >= 0 && idx < _surahs.Count - 1) 
        { 
            _currentSurah = _surahs[idx + 1]; 
            _ = LoadSurahAsync(); 
        } 
    }
    
    private void OnPrevSurah(object sender, RoutedEventArgs e) 
    { 
        int idx = _surahs.IndexOf(_currentSurah);
        if (idx > 0) 
        { 
            _currentSurah = _surahs[idx - 1]; 
            _ = LoadSurahAsync(); 
        } 
    }
    
    private void OnClose(object sender, RoutedEventArgs e) 
    { 
        AudioPlayer.MediaPlayer.Pause();
        this.Close(); 
    }
}
