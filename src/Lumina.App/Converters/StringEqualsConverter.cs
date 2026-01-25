namespace Lumina.App.Converters;

/// <summary>
/// 字符串相等性转换器：当输入值与参数相等时返回 true（忽略大小写）。
/// </summary>
public sealed class StringEqualsConverter : IValueConverter
{
    /// <summary>
    /// 可复用的单例实例。
    /// </summary>
    public static readonly StringEqualsConverter Instance = new();

    /// <summary>
    /// 将输入值与参数进行字符串比较，并返回比较结果。
    /// </summary>
    /// <param name="value">绑定源提供的值。</param>
    /// <param name="targetType">目标类型。</param>
    /// <param name="parameter">转换参数。</param>
    /// <param name="culture">区域性信息。</param>
    /// <returns>如果 <paramref name="value"/> 与 <paramref name="parameter"/> 为字符串且忽略大小写相等，则返回 true；否则返回 false。</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string stringValue && parameter is string paramValue)
        {
            return string.Equals(stringValue, paramValue, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    /// <summary>
    /// 反向转换不受支持。
    /// </summary>
    /// <param name="value">绑定目标提供的值。</param>
    /// <param name="targetType">目标类型。</param>
    /// <param name="parameter">转换参数。</param>
    /// <param name="culture">区域性信息。</param>
    /// <returns>不会返回结果。</returns>
    /// <exception cref="NotSupportedException">始终抛出。</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
