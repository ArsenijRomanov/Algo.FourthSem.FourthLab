using System;
using System.Globalization;
using Avalonia.Data.Converters;
using SearchAlgorithms.UI.Shared.Helpers;

namespace HamiltonianPath.Avalonia.Converters;

public sealed class ByteSizeConverter : IValueConverter
{
    public static ByteSizeConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is long bytes ? FormatHelper.FormatBytes(bytes) : value?.ToString();

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
