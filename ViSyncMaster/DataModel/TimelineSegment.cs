using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace ViSyncMaster.DataModel
{
    public partial class TimelineSegment : ObservableObject
    {
        [ObservableProperty]
        private TimeSpan _start;

        [ObservableProperty]
        private TimeSpan _end;

        [ObservableProperty]
        private bool _isDowntime;

        [ObservableProperty]
        private double _width;

        public string Color => IsDowntime ? "Red" : "Green";
    }
}
