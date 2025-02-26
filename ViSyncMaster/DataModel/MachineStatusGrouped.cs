using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSyncMaster.DataModel
{
    public class MachineStatusGrouped
    {
        public string ProductName { get; set; }
        public string Time { get; set; }  // Nowa właściwość przechowująca czas
        public int PassedCount { get; set; } // Liczba pozytywnych wyników testów
        public int FailedCount { get; set; } // Liczba negatywnych wyników testów
        public string Operators { get; set; } // Lista operatorów
    }
}
