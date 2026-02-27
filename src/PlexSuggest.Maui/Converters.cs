using System.Globalization;
using PlexSuggest.Core.Plex.Models;

namespace PlexSuggest.Maui;

public static class Converters
{
    public static readonly TagListConverter TagList = new();
}

public class TagListConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable<Tag> tags)
            return parameter?.ToString() ?? "";

        var prefix = parameter?.ToString() ?? "";
        var names = tags.Select(t => t.Name).Take(5);
        var joined = string.Join(", ", names);
        return string.IsNullOrEmpty(joined) ? $"{prefix}N/A" : $"{prefix}{joined}";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class InvertedBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}
