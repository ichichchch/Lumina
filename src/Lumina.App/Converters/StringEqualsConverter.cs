namespace Lumina.App.Converters;

/// <summary>
/// Converter that returns true if the value equals the parameter.
/// </summary>
public sealed class StringEqualsConverter : IValueConverter
{
    public static readonly StringEqualsConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string stringValue && parameter is string paramValue)
        {
            return string.Equals(stringValue, paramValue, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
