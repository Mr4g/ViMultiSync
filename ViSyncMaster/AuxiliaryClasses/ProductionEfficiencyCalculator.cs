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
        private readonly Func<DateTime, DateTime, double>? _downtimeProvider;


        public ProductionEfficiencyCalculator(bool isShift1, Func<DateTime, DateTime, double>? downtimeProvider = null)
                : this(isShift1 ? ShiftPlan.CreateDefaultShift1()
                                 : ShiftPlan.CreateDefaultShift2(), downtimeProvider)
        {
        }

        public ProductionEfficiencyCalculator(ShiftPlan plan, Func<DateTime, DateTime, double>? downtimeProvider = null)
        {
            _shiftStart = plan.ShiftStart;
            _shiftEnd = plan.ShiftEnd;
            _planStart = plan.PlanStart;
            _shutDown = plan.ShutDown;
            _breaks = plan.Breaks ?? new List<ShiftBreak>();
            _crossMidnight = _shiftEnd < _shiftStart;
            _downtimeProvider = downtimeProvider;
        }


        // Metoda do obliczenia wydajności na podstawie targetu i liczby sztuk
        public void CalculateEfficiency(
           int target,
           List<(DateTime Time, int PassedUnits)> efficiencyDataList,
           DateTime now,
           out int totalUnitsProduced,
           out double expectedOutput,
           out double machineEfficiency,
           out double humanEfficiency,
           out double machineEfficiencyTotal,
           out double humanEfficiencyTotal)
        {
            totalUnitsProduced = efficiencyDataList.Sum(x => x.PassedUnits);

            // Determine key times
            var shiftStartDate = now.Date.Add(_shiftStart);
            if (now.TimeOfDay < _shiftStart)
                shiftStartDate = shiftStartDate.AddDays(-1);

            var planStart = GetDateTime(shiftStartDate, _planStart);
            var shutDown = GetDateTime(shiftStartDate, _shutDown);
            var current = now > shutDown ? shutDown : now;

            var netShiftMinutes = NetMinutes(planStart, shutDown, shiftStartDate);

            if (netShiftMinutes <= 0 || target <= 0)
            {
                expectedOutput = 0;
                machineEfficiency = 0;
                humanEfficiency = 0;
                machineEfficiencyTotal = 0;
                humanEfficiencyTotal = 0;
                return;
            }

            DateTime? firstPieceTime = efficiencyDataList.Count > 0
                ? efficiencyDataList.Min(x => x.Time)
                : (DateTime?)null;

            if (firstPieceTime.HasValue && firstPieceTime < planStart)
                firstPieceTime = planStart;

            double elapsedFromFirstPiece = 0;
            double minutesFromFirstPieceToEnd = 0;
            if (firstPieceTime.HasValue)
            {
                elapsedFromFirstPiece = NetMinutes(firstPieceTime.Value, current, shiftStartDate);
                minutesFromFirstPieceToEnd = NetMinutes(firstPieceTime.Value, shutDown, shiftStartDate);

                expectedOutput = minutesFromFirstPieceToEnd > 0
                    ? target * (elapsedFromFirstPiece / minutesFromFirstPieceToEnd)
                    : 0;
            }
            else
            {
                expectedOutput = 0;
            }

            machineEfficiency = expectedOutput > 0
                ? totalUnitsProduced / expectedOutput * 100
                : 0;

            // Total efficiency from plan start to shutdown
            machineEfficiencyTotal = target > 0
                ? totalUnitsProduced / (double)target * 100
                : 0;

            // Human efficiency from first produced piece
            if (!firstPieceTime.HasValue)
            {
                humanEfficiency = machineEfficiency; // preserve old behaviour
                humanEfficiencyTotal = machineEfficiencyTotal;
                return;
            }

            // elapsedFromFirstPiece already calculated above
            var expectedFromFirstPiece = minutesFromFirstPieceToEnd > 0
                ? target * (elapsedFromFirstPiece / minutesFromFirstPieceToEnd)
                : 0;
            humanEfficiency = expectedFromFirstPiece > 0
                ? totalUnitsProduced / expectedFromFirstPiece * 100
                : machineEfficiency;

            humanEfficiencyTotal = machineEfficiencyTotal;
        }

        private DateTime GetDateTime(DateTime shiftStartDate, TimeSpan ts)
        {
            var dt = shiftStartDate.Date.Add(ts);
            if (_crossMidnight && ts < _shiftStart)
                dt = dt.AddDays(1);
            return dt;
        }

        private double NetMinutes(DateTime from, DateTime to, DateTime shiftStartDate)
        {
            if (to <= from) return 0;
            double minutes = (to - from).TotalMinutes;
            foreach (var br in _breaks)
            {
                var bs = GetDateTime(shiftStartDate, br.Start);
                var be = GetDateTime(shiftStartDate, br.End);
                if (be <= from || bs >= to) continue;
                var s = bs < from ? from : bs;
                var e = be > to ? to : be;
                if (e > s) minutes -= (e - s).TotalMinutes;
            }
            if (_downtimeProvider != null)
            {
                minutes -= _downtimeProvider(from, to);
            }
            if (minutes < 0) minutes = 0;
            return minutes;
        }
    }
}
 
