using NUnit.Framework;
using System;
using System.IO;
using ViSyncMaster.AuxiliaryClasses;

namespace ViSyncMaster.nUnitTests
{
    [TestFixture]
    public class ShiftPlanRangeTests
    {
        [OneTimeSetUp]
        public void Setup()
        {
            var json = """
            {
              "departments": [
                {
                  "name": "CHP",
                  "shifts": [
                    { "shiftNumber": 1, "shiftStart": "06:00", "shiftEnd": "14:00", "planStart": "06:10", "shutDown": "13:55", "breaks": [] },
                    { "shiftNumber": 2, "shiftStart": "14:00", "shiftEnd": "22:00", "planStart": "14:10", "shutDown": "21:55", "breaks": [] },
                    { "shiftNumber": 3, "shiftStart": "22:00", "shiftEnd": "06:00", "planStart": "22:10", "shutDown": "05:55", "breaks": [] }
                  ]
                },
                {
                  "name": "ALT_PLAN",
                  "shifts": [
                    { "shiftNumber": 1, "shiftStart": "07:00", "shiftEnd": "15:00", "planStart": "07:10", "shutDown": "14:55", "breaks": [] },
                    { "shiftNumber": 2, "shiftStart": "15:00", "shiftEnd": "23:00", "planStart": "15:10", "shutDown": "22:55", "breaks": [] },
                    { "shiftNumber": 3, "shiftStart": "23:00", "shiftEnd": "07:00", "planStart": "23:10", "shutDown": "06:55", "breaks": [] }
                  ]
                }
              ]
            }
            """;

            var tmp = Path.GetTempFileName();
            File.WriteAllText(tmp, json);
            ShiftPlan.LoadFromJson(tmp);
        }

        [Test]
        public void Shift1_Today_ReturnsConfiguredRange()
        {
            var range = ShiftPlan.GetShiftTimeRange("CHP", 1, new DateTime(2026, 4, 23), new DateTime(2026, 4, 23, 8, 0, 0));
            Assert.That(range.Start, Is.EqualTo(new DateTime(2026, 4, 23, 6, 0, 0)));
            Assert.That(range.End, Is.EqualTo(new DateTime(2026, 4, 23, 14, 0, 0)));
        }

        [Test]
        public void Shift2_BeforeStart_UsesPreviousDay()
        {
            var range = ShiftPlan.GetShiftTimeRange("CHP", 2, new DateTime(2026, 4, 23), new DateTime(2026, 4, 23, 10, 0, 0));
            Assert.That(range.Start, Is.EqualTo(new DateTime(2026, 4, 22, 14, 0, 0)));
            Assert.That(range.End, Is.EqualTo(new DateTime(2026, 4, 22, 22, 0, 0)));
        }

        [Test]
        public void Shift3_CrossMidnight_IsHandledCorrectly()
        {
            var range = ShiftPlan.GetShiftTimeRange("CHP", 3, new DateTime(2026, 4, 23), new DateTime(2026, 4, 23, 23, 0, 0));
            Assert.That(range.Start, Is.EqualTo(new DateTime(2026, 4, 23, 22, 0, 0)));
            Assert.That(range.End, Is.EqualTo(new DateTime(2026, 4, 24, 6, 0, 0)));
        }

        [Test]
        public void Shift3_Yesterday_DoesNotFallbackToOtherShift()
        {
            var range = ShiftPlan.GetYesterdayShiftRange("CHP", 3, new DateTime(2026, 4, 23), new DateTime(2026, 4, 23, 10, 0, 0));
            Assert.That(range.Start, Is.EqualTo(new DateTime(2026, 4, 22, 22, 0, 0)));
            Assert.That(range.End, Is.EqualTo(new DateTime(2026, 4, 23, 6, 0, 0)));
        }

        [Test]
        public void DifferentPlanName_UsesDifferentHours()
        {
            var range = ShiftPlan.GetShiftTimeRange("ALT_PLAN", 1, new DateTime(2026, 4, 23), new DateTime(2026, 4, 23, 8, 0, 0));
            Assert.That(range.Start, Is.EqualTo(new DateTime(2026, 4, 23, 7, 0, 0)));
            Assert.That(range.End, Is.EqualTo(new DateTime(2026, 4, 23, 15, 0, 0)));
        }

        [Test]
        public void WeeklyRanges_ForSelectedShift_ReturnSevenRangesWithoutFallbackMix()
        {
            var ranges = ShiftPlan.GetWeeklyShiftRanges("CHP", 2, new DateTime(2026, 4, 23));
            Assert.That(ranges.Count, Is.EqualTo(7));
            Assert.That(ranges[0].Start, Is.EqualTo(new DateTime(2026, 4, 20, 14, 0, 0))); // Monday
            Assert.That(ranges[0].End, Is.EqualTo(new DateTime(2026, 4, 20, 22, 0, 0)));
            Assert.That(ranges[6].Start, Is.EqualTo(new DateTime(2026, 4, 26, 14, 0, 0))); // Sunday
            Assert.That(ranges[6].End, Is.EqualTo(new DateTime(2026, 4, 26, 22, 0, 0)));
        }
    }
}
