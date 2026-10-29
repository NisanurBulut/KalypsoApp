using System.Globalization;

namespace Kalypso.Maui.Converters;

/// <summary>Turns the PNG bytes of a QR code into something an Image can show.</summary>
public sealed class BytesToImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is byte[] { Length: > 0 } bytes ? ImageSource.FromStream(() => new MemoryStream(bytes)) : null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
