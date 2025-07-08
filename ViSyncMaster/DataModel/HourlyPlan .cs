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
        [ObservableProperty] private string _time;
        [ObservableProperty] private int _expectedUnits;
        [ObservableProperty] private int _producedUnits;
        [ObservableProperty] private bool _isBreak;
        [ObservableProperty] private bool _isBreakActive;

        public string ExpectedDisplay => IsBreak ? "PRZERWA" : ExpectedUnits.ToString();
        public string ProducedDisplay => IsBreak ? "PRZERWA" : ProducedUnits.ToString();

        partial void OnIsBreakChanged(bool value)
        {
            OnPropertyChanged(nameof(ExpectedDisplay));
            OnPropertyChanged(nameof(ProducedDisplay));
        }

        partial void OnExpectedUnitsChanged(int value) => OnPropertyChanged(nameof(ExpectedDisplay));

        partial void OnProducedUnitsChanged(int value) => OnPropertyChanged(nameof(ProducedDisplay));
    }
}