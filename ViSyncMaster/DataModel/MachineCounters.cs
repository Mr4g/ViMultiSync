using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSyncMaster.DataModel
{
    public class MachineCounters
    {
        public int UniqueOperatorCount { get; set; } // Liczba unikalnych operatorów
        public int UniqueProductCount { get; set; } // Liczba unikalnych produktów
        public int Target { get; set; } // Cel
        public int Plan { get; set; } // Plan
        public int ShiftCounterPass { get; set; } // Liczba pozytywnych wyników testów w zmianie
        public int ShiftCounterFail { get; set; } // Liczba negatywnych wyników testów w zmianie
        public int ShiftCounter { get; set; } // Liczba wszystkich wyników testów w zmianie
        public long TotalCounterPass { get; set; } // Liczba pozytywnych wyników testów w całym okresie
        public long TotalCounterFail { get; set; } // Liczba negatywnych wyników testów w całym okresie
        public long TotalCounter { get; set; } // Liczba wszystkich wyników testów w całym okresie
    }
}
