using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSyncMaster.DataModel
{
    public partial class HourlyPlan : ObservableObject
    {
        [ObservableProperty] private string _period;
        [ObservableProperty] private int _expectedUnits;
        [ObservableProperty] private int _producedUnits;
        [ObservableProperty] private int _downtimeMinutes;
        [ObservableProperty] private bool _isBreak;
        [ObservableProperty] private bool _isBreakActive;
        [ObservableProperty] private int _lostUnitsDueToDowntime;
        [ObservableProperty] private double _efficiency;

        public string ExpectedDisplay => IsBreak ? "PRZERWA" : ExpectedUnits.ToString();
        public string ProducedDisplay => $"{ProducedUnits: 0}";
        public string DowntimeDisplay => IsBreak ? "PRZERWA" : DowntimeMinutes.ToString();
        public string LostUnitsDisplay => IsBreak ? "PRZERWA" : LostUnitsDueToDowntime.ToString();
        public string EfficiencyDisplay =>IsBreak ? "PRZERWA" : $"{Efficiency:0.0} %";
        // Clamp progress to <0..1> to avoid UI glitches for edge-cases (target=0, produced>expected, negative values).
        public double ProgressRatio
        {
            get
            {
                if (IsBreak)
                    return 0;

                var expected = Math.Max(ExpectedUnits, 0);
                var produced = Math.Max(ProducedUnits, 0);
                if (expected <= 0)
                    return 0;

                return Math.Clamp((double)produced / expected, 0d, 1d);
            }
        }
        public double ProgressPercent => ProgressRatio * 100d;



        partial void OnIsBreakChanged(bool value)
        {
            OnPropertyChanged(nameof(ExpectedDisplay));
            OnPropertyChanged(nameof(ProducedDisplay));
            OnPropertyChanged(nameof(LostUnitsDisplay));
            OnPropertyChanged(nameof(DowntimeDisplay));
            OnPropertyChanged(nameof(EfficiencyDisplay));
            OnPropertyChanged(nameof(ProgressRatio));
            OnPropertyChanged(nameof(ProgressPercent));
        }

        partial void OnExpectedUnitsChanged(int value)
        {
            OnPropertyChanged(nameof(ExpectedDisplay));
            OnPropertyChanged(nameof(ProgressRatio));
            OnPropertyChanged(nameof(ProgressPercent));
        }

        partial void OnProducedUnitsChanged(int value)
        {
            OnPropertyChanged(nameof(ProducedDisplay));
            OnPropertyChanged(nameof(ProgressRatio));
            OnPropertyChanged(nameof(ProgressPercent));
        }

        partial void OnDowntimeMinutesChanged(int value) => OnPropertyChanged(nameof(DowntimeDisplay));

        partial void OnLostUnitsDueToDowntimeChanged(int value) { OnPropertyChanged(nameof(DowntimeDisplay)); OnPropertyChanged(nameof(LostUnitsDisplay)); }
        partial void OnEfficiencyChanged(double value) => OnPropertyChanged(nameof(EfficiencyDisplay));

    }
}
