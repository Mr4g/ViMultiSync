using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ViSyncMaster.AuxiliaryClasses
{
    public class ProductionEfficiencyCalculator
    {
        private readonly TimeSpan _shiftStart;
        private readonly TimeSpan _shiftEnd;
        private readonly TimeSpan _shutDown;
        private readonly TimeSpan _planStart;
        private readonly bool _crossMidnight;
        private readonly List<ShiftBreak> _breaks;

        public ProductionEfficiencyCalculator(bool isShift1)
                : this(isShift1 ? ShiftPlan.CreateDefaultShift1()
                                 : ShiftPlan.CreateDefaultShift2())
        {
        }

        public ProductionEfficiencyCalculator(ShiftPlan plan)
        {
            _shiftStart = plan.ShiftStart;
            _shiftEnd = plan.ShiftEnd;
            _planStart = plan.PlanStart;
            _shutDown = plan.ShutDown;
            _breaks = plan.Breaks ?? new List<ShiftBreak>();
            _crossMidnight = _shiftEnd < _shiftStart;
        }


        // Metoda do obliczenia wydajności na podstawie targetu i liczby sztuk
        public void CalculateEfficiency(
            int target,
            List<(DateTime Time, int PassedUnits)> efficiencyDataList,
            DateTime now,
            out int totalUnitsProduced,
            out double expectedOutput,
            out double machineEfficiency,
            out double humanEfficiency)
        {
            totalUnitsProduced = efficiencyDataList.Sum(x => x.PassedUnits);

            // Determine key times
            var shiftStartDate = now.Date.Add(_shiftStart);
            if (now.TimeOfDay < _shiftStart)
                shiftStartDate = shiftStartDate.AddDays(-1);

            var planStart = GetDateTime(shiftStartDate, _planStart);
            var shutDown = GetDateTime(shiftStartDate, _shutDown);
            var current = now > shutDown ? shutDown : now;

            var netShiftMinutes = NetMinutes(planStart, shutDown);
            var elapsedPlanMinutes = NetMinutes(planStart, current);

            if (netShiftMinutes <= 0 || target <= 0)
            {
                expectedOutput = 0;
                machineEfficiency = 0;
                humanEfficiency = 0;
                return;
            }

            expectedOutput = target * (elapsedPlanMinutes / netShiftMinutes);

            var expectedFromShiftStart = expectedOutput;

            machineEfficiency = expectedFromShiftStart > 0
                ? totalUnitsProduced / expectedFromShiftStart * 100
                : 0;

            // Human efficiency from first produced piece
            if (efficiencyDataList == null || efficiencyDataList.Count == 0)
            {
                humanEfficiency = machineEfficiency; // preserve old behaviour
                return;
            }

            var firstPieceTime = efficiencyDataList.Min(x => x.Time);
            if (firstPieceTime < planStart)
                firstPieceTime = planStart;

            var elapsedFromFirstPiece = NetMinutes(firstPieceTime, current);
            var expectedFromFirstPiece = target * (elapsedFromFirstPiece / netShiftMinutes);
            humanEfficiency = expectedFromFirstPiece > 0
                ? totalUnitsProduced / expectedFromFirstPiece * 100
                : machineEfficiency;
        }

        private DateTime GetDateTime(DateTime shiftStartDate, TimeSpan ts)
        {
            var dt = shiftStartDate.Date.Add(ts);
            if (_crossMidnight && ts < _shiftStart)
                dt = dt.AddDays(1);
            return dt;
        }

        private double NetMinutes(DateTime from, DateTime to)
        {
            if (to <= from) return 0;
            double minutes = (to - from).TotalMinutes;
            foreach (var br in _breaks)
            {
                var bs = GetDateTime(from, br.Start);
                var be = GetDateTime(from, br.End);
                if (be <= from || bs >= to) continue;
                var s = bs < from ? from : bs;
                var e = be > to ? to : be;
                if (e > s) minutes -= (e - s).TotalMinutes;
            }
            return minutes;
        }
    }
}
 
