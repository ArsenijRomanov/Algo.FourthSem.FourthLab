using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace SlidingPuzzle.Avalonia.Converters;

public sealed class ActiveChipBrushConverter : IValueConverter
{
    public static ActiveChipBrushConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true
            ? new SolidColorBrush(Color.Parse("#3B4D85"))
            : new SolidColorBrush(Color.Parse("#252C39"));

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
