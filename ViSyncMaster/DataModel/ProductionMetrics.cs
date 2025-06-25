using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSyncMaster.DataModel
{
    public class ProductionMetrics
    {
        public double? ProductionTime { get; set; }  // Czas produkcji w sekundach
        public double? PreparationTime { get; set; } // Czas przygotowania w sekundach
        public double? TaktTime
        {
            get
            {
                if (ProductionTime.HasValue && PreparationTime.HasValue)
                {
                    return Math.Round(ProductionTime.Value + PreparationTime.Value, 1); // Oblicz TaktTime jako sumę ProductionTime i PreparationTime
                }
                return null; // Zwróć null, jeśli którykolwiek z czasów jest null
            }
        }
        public int? UnitsProduced { get; set; }      // Liczba sztuk wyprodukowanych w danym cyklu
        public int? PassedUnits { get; set; }        // Liczba sztuk, które przeszły test
        public int? FailedUnits { get; set; }        // Liczba sztuk, które nie przeszły testu
        public string? TGoodAbs { get; set; }           // Liczba wszystkich testów
        public string? RGoodAbs { get; set; }           // Liczba retestów
        public bool? TestWithRetest { get; set;}
        public string? ProductNumber { get; set; }   // Numer produktu
        public string? OperatorId { get; set; }     // Numer operatora
    }
}
