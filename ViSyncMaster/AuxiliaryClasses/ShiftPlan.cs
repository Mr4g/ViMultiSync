using System;
using System.Collections.Generic;

namespace ViSyncMaster.AuxiliaryClasses
{
    public record ShiftBreak(TimeSpan Start, TimeSpan End);

    public class ShiftPlan
    {
        public TimeSpan ShiftStart { get; init; }
        public TimeSpan ShiftEnd { get; init; }
        public TimeSpan PlanStart { get; init; }
        public TimeSpan ShutDown { get; init; }
        public List<ShiftBreak> Breaks { get; init; } = new();

        public static ShiftPlan CreateDefaultShift1() => new()
        {
            ShiftStart = TimeSpan.Parse("05:40"),
            ShiftEnd = TimeSpan.Parse("13:40"),
            PlanStart = TimeSpan.Parse("05:50"),
            ShutDown = TimeSpan.Parse("13:35"),
            Breaks = new List<ShiftBreak>
            {
                new ShiftBreak(TimeSpan.Parse("09:40"), TimeSpan.Parse("10:00")),
                new ShiftBreak(TimeSpan.Parse("12:00"), TimeSpan.Parse("12:10"))
            }
        };

        public static ShiftPlan CreateDefaultShift2() => new()
        {
            ShiftStart = TimeSpan.Parse("13:40"),
            ShiftEnd = TimeSpan.Parse("21:40"),
            PlanStart = TimeSpan.Parse("13:50"),
            ShutDown = TimeSpan.Parse("21:35"),
            Breaks = new List<ShiftBreak>
            {
                new ShiftBreak(TimeSpan.Parse("17:40"), TimeSpan.Parse("18:00")),
                new ShiftBreak(TimeSpan.Parse("20:00"), TimeSpan.Parse("20:10"))
            }
        };

        public static ShiftPlan CreateDefaultShift3() => new()
        {
            ShiftStart = TimeSpan.Parse("21:40"),
            ShiftEnd = TimeSpan.Parse("05:40"),
            PlanStart = TimeSpan.Parse("21:50"),
            ShutDown = TimeSpan.Parse("05:35"),
            Breaks = new List<ShiftBreak>
            {
                new ShiftBreak(TimeSpan.Parse("01:40"), TimeSpan.Parse("02:00")),
                new ShiftBreak(TimeSpan.Parse("04:00"), TimeSpan.Parse("04:10"))
            }
        };
    }
}