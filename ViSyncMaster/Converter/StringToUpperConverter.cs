using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ViSyncMaster.Converter
{
    public class StringToUpperConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value?.ToString()?.ToUpper(culture) ?? string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value?.ToString() ?? string.Empty;
        }
    }
}
