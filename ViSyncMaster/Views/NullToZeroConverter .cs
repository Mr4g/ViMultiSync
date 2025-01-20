using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ViSyncMaster.Views
{
    public class NullToZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan timeSpan) // Poprawiona składnia do sprawdzania typu
            {
                return timeSpan.TotalMinutes; // Zwraca całkowitą liczbę minut, jeśli value jest typu TimeSpan
            }
            return 0; // Jeśli value nie jest TimeSpan, zwraca 0
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Sprawdź, czy wartość nie jest null
            if (value == null)
                return null;

            // Jeśli wartość jest typu string (przykład konwersji z tekstu do TimeSpan)
            if (value is string strValue)
            {
                // Spróbuj skonwertować na TimeSpan
                if (TimeSpan.TryParse(strValue, out var timeSpan))
                {
                    return timeSpan; // Zwróć poprawnie skonwertowany TimeSpan
                }
                else
                {
                    // Jeśli konwersja nie powiodła się, zwróć domyślną wartość lub throw new Exception();
                    return TimeSpan.Zero; // Przykład: zwróć domyślny TimeSpan
                }
            }

            // Dodatkowe sprawdzenie dla innych typów, jeśli wymagane
            return null;
        }
    }
}
