using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace StarCitizenTrader.Converters;

/// Converts Unix timestamp (long) to formatted DateTime string.
public class UnixTimestampConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long ts && ts > 0)
        {
            var dt = DateTimeOffset.FromUnixTimeSeconds(ts).LocalDateTime;
            return dt.ToString("MMM dd, yyyy HH:mm");
        }
        return "—";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// Converts a double price to a formatted currency string.
public class PriceFormatter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double price)
            return price.ToString("N0");
        return "—";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// Converts a percentage change to a color (green=positive, red=negative).
public class PriceChangeColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double change)
        {
            if (change > 0.1) return new SolidColorBrush(Color.FromRgb(76, 175, 80));   // Green
            if (change < -0.1) return new SolidColorBrush(Color.FromRgb(244, 67, 54));  // Red
        }
        return new SolidColorBrush(Color.FromRgb(158, 158, 158));  // Grey
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// Converts boolean to Visibility.
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility v)
            return v == Visibility.Visible;
        return false;
    }
}

/// Inverted boolean to Visibility.
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? Visibility.Collapsed : Visibility.Visible;
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// Converts operation string to background color.
public class OperationColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string op)
        {
            return op.ToLower() switch
            {
                "buy" => new SolidColorBrush(Color.FromArgb(40, 76, 175, 80)),
                "sell" => new SolidColorBrush(Color.FromArgb(40, 244, 67, 54)),
                _ => new SolidColorBrush(Colors.Transparent)
            };
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// Converts nullable double to display string.
public class NullableDoubleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
            return d.ToString("N0");
        return "N/A";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && double.TryParse(s, out var d))
            return d;
        return null!;
    }
}

/// Returns true if value equals the parameter (for radio-button-like binding).
public class EqualityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return parameter?.ToString() ?? "";
        return Binding.DoNothing;
    }
}
