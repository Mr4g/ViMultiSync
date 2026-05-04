using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ViSyncMaster.Converter
{
    public class StringPairEqualsConverter : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 2) return false;
            var left = values[0]?.ToString();
            var right = values[1]?.ToString();
            return !string.IsNullOrWhiteSpace(left) && string.Equals(left, right, StringComparison.Ordinal);
        }
    }
}
