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
        [ObservableProperty] private double _taktTimeSeconds;
        [ObservableProperty] private int _plannedQty;
        [ObservableProperty] private int _actualQty;
        [ObservableProperty] private int _expectedQty;
        [ObservableProperty] private DateTime? _startTime;
        [ObservableProperty] private DateTime? _plannedEndTime;
        [ObservableProperty] private int _difference;
        [ObservableProperty] private string _status;
        [ObservableProperty] private bool _isPlanBeyondShift;

        public double ProgressPercent
        {
            get
            {
                if (PlannedQty <= 0) return 0;
                var ratio = (double)Math.Max(ActualQty, 0) / PlannedQty;
                return Math.Clamp(ratio * 100.0, 0, 100);
            }
        }

        /// <summary>
        /// Zwraca tylko numer produktu (tekst przed pierwszą spacją).
        /// </summary>
        public string ProductNumber
            => string.IsNullOrWhiteSpace(ProductName)
               ? string.Empty
               : ProductName.Split(' ')[0];

        public string StartTimeDisplay => StartTime?.ToString("yyyy-MM-dd HH:mm") ?? "-";
        public string PlannedEndTimeDisplay => PlannedEndTime?.ToString("yyyy-MM-dd HH:mm") ?? "-";

        partial void OnPlannedQtyChanged(int value) => OnPropertyChanged(nameof(ProgressPercent));
        partial void OnActualQtyChanged(int value) => OnPropertyChanged(nameof(ProgressPercent));
    }
}
