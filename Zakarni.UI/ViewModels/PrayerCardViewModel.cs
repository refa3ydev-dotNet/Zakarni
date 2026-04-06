using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;

namespace Zakarni.UI.ViewModels;

public partial class PrayerCardViewModel : ObservableObject
{
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private string _time = string.Empty;
    public string Time
    {
        get => _time;
        set => SetProperty(ref _time, value);
    }

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    private Brush _background = null!;
    public Brush Background
    {
        get => _background;
        set => SetProperty(ref _background, value);
    }

    private Brush _borderBrush = null!;
    public Brush BorderBrush
    {
        get => _borderBrush;
        set => SetProperty(ref _borderBrush, value);
    }

    private Brush _foreground = null!;
    public Brush Foreground
    {
        get => _foreground;
        set => SetProperty(ref _foreground, value);
    }

    private Microsoft.UI.Xaml.Thickness _borderThickness = new Microsoft.UI.Xaml.Thickness(1);
    public Microsoft.UI.Xaml.Thickness BorderThickness
    {
        get => _borderThickness;
        set => SetProperty(ref _borderThickness, value);
    }
}
