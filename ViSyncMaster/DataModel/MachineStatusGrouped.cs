using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSyncMaster.DataModel
{
    public partial class MachineStatusGrouped : ObservableObject
    {
        [ObservableProperty] private string _productName;
        [ObservableProperty] private string _time;  // Nowa właściwość przechowująca czas
        [ObservableProperty] private string _operators; // Lista operatorów
        [ObservableProperty] private int _target; // Cel
        [ObservableProperty] private int _shiftCounterPass; // Liczba pozytywnych wyników testów w zmianie
        [ObservableProperty] private int _shiftCounterFail;// Liczba negatywnych wyników testów w zmianie

        /// <summary>
        /// Zwraca tylko numer produktu (tekst przed pierwszą spacją).
        /// </summary>
        public string ProductNumber
            => string.IsNullOrWhiteSpace(ProductName)
               ? string.Empty
               : ProductName.Split(' ')[0];
    }
}
