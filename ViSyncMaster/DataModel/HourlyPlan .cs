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
    }
}
