using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System;
using Zakarni.Core.Models;
using Windows.UI.Text;

namespace Zakarni.UI.Converters;

public class StrikeThroughConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (bool)value ? TextDecorations.Strikethrough : TextDecorations.None;
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class PriorityToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var priority = (TodoPriority)value;
        return priority switch
        {
            TodoPriority.High => new SolidColorBrush(ColorHelper.FromArgb(255, 231, 76, 60)), // Red
            TodoPriority.Medium => new SolidColorBrush(ColorHelper.FromArgb(255, 241, 196, 15)), // Yellow
            _ => new SolidColorBrush(ColorHelper.FromArgb(255, 149, 165, 166)) // Gray
        };
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class DateTimeToRelativeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var date = (DateTime)value;
        var diff = DateTime.Now - date;
        if (diff.TotalMinutes < 1) return "Just now";
        if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalDays < 1) return $"{(int)diff.TotalHours}h ago";
        return date.ToString("MMM dd");
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
