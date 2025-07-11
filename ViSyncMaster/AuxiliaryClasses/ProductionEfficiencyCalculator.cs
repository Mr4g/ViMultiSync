using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ViSyncMaster.AuxiliaryClasses
{
    /// <summary>
    /// Kalkulator wydajności produkcji na podstawie planu zmiany i danych o produkcji.
    /// </summary>
    public class ProductionEfficiencyCalculator
    {
        private readonly TimeSpan _shiftStart;
        private readonly TimeSpan _shiftEnd;
        private readonly TimeSpan _planStart;
        private readonly TimeSpan _shutDown;
        private readonly bool _crossMidnight;
        private readonly List<ShiftBreak> _breaks;
        private readonly Func<DateTime, DateTime, double>? _downtimeProvider;

        /// <summary>
        /// Tworzy kalkulator na bazie pliku JSON i nazwy działu.
        /// </summary>
        /// <param name="department">Nazwa działu (zgodna z JSON, np. "KM").</param>
        /// <param name="downtimeProvider">Opcjonalny provider czasu przestojów.</param>
        public ProductionEfficiencyCalculator(
            string department,
            Func<DateTime, DateTime, double>? downtimeProvider = null)
            : this(ShiftPlan.GetCurrent(department), downtimeProvider)
        {
        }

        /// <summary>
        /// Tworzy kalkulator na bazie konkretnego planu zmiany.
        /// </summary>
        public ProductionEfficiencyCalculator(
            ShiftPlan plan,
            Func<DateTime, DateTime, double>? downtimeProvider = null)
        {
            _shiftStart = plan.ShiftStart;
            _shiftEnd = plan.ShiftEnd;
            _planStart = plan.PlanStart;
            _shutDown = plan.ShutDown;
            _breaks = plan.Breaks?.ToList() ?? new List<ShiftBreak>();
            _crossMidnight = _shiftEnd < _shiftStart;
            _downtimeProvider = downtimeProvider;
        }

        /// <summary>
        /// Oblicza wskaźniki wydajności.
        /// </summary>
        public void CalculateEfficiency(
           int target,
           List<(DateTime Time, int PassedUnits)> data,
           DateTime now,
           out int totalUnitsProduced,
           out double expectedOutput,
           out double machineEfficiency,
           out double humanEfficiency,
           out double machineEfficiencyTotal,
           out double humanEfficiencyTotal)
        {
            totalUnitsProduced = data.Sum(x => x.PassedUnits);

            // Oblicz czas rozpoczęcia zmiany
            var shiftStartDate = now.Date.Add(_shiftStart);
            if (now.TimeOfDay < _shiftStart) shiftStartDate = shiftStartDate.AddDays(-1);

            var planStart = ToDateTime(shiftStartDate, _planStart);
            var shutDown = ToDateTime(shiftStartDate, _shutDown);
            var currentTime = now > shutDown ? shutDown : now;

            var netShiftMinutes = CalculateNetMinutes(planStart, shutDown, shiftStartDate);
            if (netShiftMinutes <= 0 || target <= 0)
            {
                expectedOutput = machineEfficiency = humanEfficiency = machineEfficiencyTotal = humanEfficiencyTotal = 0;
                return;
            }

            // Czas pierwszego wyprodukowanego elementu
            var firstPiece = data.Any() ? data.Min(x => x.Time) : (DateTime?)null;
            if (firstPiece.HasValue && firstPiece < planStart) firstPiece = planStart;

            double elapsed = firstPiece.HasValue
                ? CalculateNetMinutes(firstPiece.Value, currentTime, shiftStartDate)
                : 0;

            double remaining = firstPiece.HasValue
                ? CalculateNetMinutes(firstPiece.Value, shutDown, shiftStartDate)
                : 0;

            // Output expected
            expectedOutput = remaining > 0
                ? target * (elapsed / remaining)
                : 0;

            // Wydajność maszynowa
            machineEfficiency = expectedOutput > 0
                ? totalUnitsProduced / expectedOutput * 100
                : 0;

            // Wydajność całkowita względem targetu
            machineEfficiencyTotal = target > 0
                ? totalUnitsProduced / (double)target * 100
                : 0;

            // Wydajność ludzka od pierwszego elementu
            if (!firstPiece.HasValue)
            {
                humanEfficiency = machineEfficiency;
                humanEfficiencyTotal = machineEfficiencyTotal;
                return;
            }

            var expectedFromFirst = remaining > 0
                ? target * (elapsed / remaining)
                : 0;
            humanEfficiency = expectedFromFirst > 0
                ? totalUnitsProduced / expectedFromFirst * 100
                : machineEfficiency;
            humanEfficiencyTotal = machineEfficiencyTotal;
        }

        private DateTime ToDateTime(DateTime baseDate, TimeSpan time)
        {
            var dt = baseDate.Date.Add(time);
            if (_crossMidnight && time < _shiftStart) dt = dt.AddDays(1);
            return dt;
        }

        private double CalculateNetMinutes(DateTime from, DateTime to, DateTime shiftStartDate)
        {
            if (to <= from) return 0;
            double minutes = (to - from).TotalMinutes;

            // Odejmij przerwy
            foreach (var br in _breaks)
            {
                var bs = ToDateTime(shiftStartDate, br.Start);
                var be = ToDateTime(shiftStartDate, br.End);
                if (be <= from || bs >= to) continue;
                var s = bs < from ? from : bs;
                var e = be > to ? to : be;
                if (e > s) minutes -= (e - s).TotalMinutes;
            }

            // Odejmij przestoje
            if (_downtimeProvider != null)
            {
                minutes -= _downtimeProvider(from, to);
            }

            return Math.Max(0, minutes);
        }
    }
}
