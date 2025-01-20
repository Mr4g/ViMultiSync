using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ViSyncMaster.Views
{
    public class TimeSpanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan timeSpan)
            {
                // Zaokrąglamy do 2 miejsc po przecinku na minutach
                var roundedMinutes = Math.Round(timeSpan.TotalMinutes, 2);
                var roundedTimeSpan = TimeSpan.FromMinutes(roundedMinutes);
                return roundedTimeSpan.ToString(@"hh\:mm\:ss"); // Formatowanie do hh:mm:ss
            }
            return "00:00:00"; // Wartość domyślna, gdy wartość jest null lub nie jest TimeSpan
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
