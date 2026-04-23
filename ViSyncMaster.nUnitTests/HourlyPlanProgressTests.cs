using NUnit.Framework;
using ViSyncMaster.DataModel;

namespace ViSyncMaster.nUnitTests
{
    [TestFixture]
    public class HourlyPlanProgressTests
    {
        [TestCase(0, 0, 0)]
        [TestCase(0, 20, 0)]
        [TestCase(100, 0, 0)]
        [TestCase(100, 50, 50)]
        [TestCase(100, 150, 100)]
        [TestCase(-10, 20, 0)]
        [TestCase(100, -5, 0)]
        public void ProgressPercent_IsAlwaysClampedAndSafe(int expectedUnits, int producedUnits, double expectedPercent)
        {
            var item = new HourlyPlan
            {
                IsBreak = false,
                ExpectedUnits = expectedUnits,
                ProducedUnits = producedUnits
            };

            Assert.That(item.ProgressPercent, Is.EqualTo(expectedPercent));
            Assert.That(item.ProgressRatio, Is.InRange(0d, 1d));
        }
    }
}
