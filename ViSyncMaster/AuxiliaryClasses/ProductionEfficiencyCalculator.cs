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
        private readonly TimeSpan _break1Start;
        private readonly TimeSpan _break1End;
        private readonly TimeSpan _break2Start;
        private readonly TimeSpan _break2End;
        private readonly TimeSpan _startUp;
        private readonly TimeSpan _shutDown;

        public ProductionEfficiencyCalculator(bool isShift1)
        {
            if (isShift1)
            {
                _shiftStart = TimeSpan.Parse("05:40");
                _shiftEnd = TimeSpan.Parse("13:40");
                _break1Start = TimeSpan.Parse("09:40");
                _break1End = TimeSpan.Parse("10:00");
                _break2Start = TimeSpan.Parse("12:00");
                _break2End = TimeSpan.Parse("12:10");
                _startUp = TimeSpan.Parse("05:40");
                _shutDown = TimeSpan.Parse("13:35");
            }
            else
            {
                _shiftStart = TimeSpan.Parse("13:40");
                _shiftEnd = TimeSpan.Parse("05:40");
                _break1Start = TimeSpan.Parse("21:40");
                _break1End = TimeSpan.Parse("22:00");
                _break2Start = TimeSpan.Parse("00:00");
                _break2End = TimeSpan.Parse("00:10");
                _startUp = TimeSpan.Parse("13:40");
                _shutDown = TimeSpan.Parse("05:35");
            }
        }

        // Metoda do obliczenia wydajności na podstawie targetu i liczby sztuk
        public void CalculateEfficiency(
            int target,
            List<(DateTime Time, int PassedUnits)> efficiencyDataList,
            out int totalUnitsProduced,
            out double expectedOutput,
            out double achievedEfficiency,
            out double expectedEfficiency)
        {
            // Określenie, która zmiana jest aktywna
            TimeSpan shiftStart = TimeSpan.Zero;
            TimeSpan shiftEnd = TimeSpan.Zero;

            if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("05:40") && DateTime.Now.TimeOfDay < TimeSpan.Parse("13:40"))
            {
                // Zmiana 1: 05:40 - 13:40
                shiftStart = TimeSpan.Parse("05:40");
                shiftEnd = TimeSpan.Parse("13:40");
            }
            else if (DateTime.Now.TimeOfDay >= TimeSpan.Parse("13:40") && DateTime.Now.TimeOfDay < TimeSpan.Parse("21:40"))
            {
                // Zmiana 2: 13:40 - 21:40
                shiftStart = TimeSpan.Parse("13:40");
                shiftEnd = TimeSpan.Parse("21:40");
            }
            else
            {
                // Poza aktywnymi zmianami
                totalUnitsProduced = 0;
                expectedOutput = 0;
                achievedEfficiency = 0;
                expectedEfficiency = 0;
                return;
            }

            // Czas trwania zmiany
            var totalShiftDuration = shiftEnd - shiftStart;

            // Liczba wyprodukowanych jednostek
            totalUnitsProduced = efficiencyDataList.Sum(x => x.PassedUnits);

            // Czas, który upłynął od rozpoczęcia zmiany
            var currentTime = DateTime.Now.TimeOfDay;
            var elapsedTime = currentTime - shiftStart;

            // Obliczanie oczekiwanej liczby sztuk
            expectedOutput = target * (elapsedTime.TotalHours / totalShiftDuration.TotalHours);

            // Obliczanie targetu na godzinę
            var targetPerHour = target / totalShiftDuration.TotalHours;

            // Osiągnięta wydajność jako procent produkcji względem oczekiwanej produkcji
            achievedEfficiency = (totalUnitsProduced / expectedOutput) * 100;

            // Oczekiwana wydajność w procentach (czyli ile % targetu powinno być zrobione do tej godziny)
            expectedEfficiency = (elapsedTime.TotalHours / totalShiftDuration.TotalHours) * 100;
        }




    }
}
 
