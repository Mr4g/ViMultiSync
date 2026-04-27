using NUnit.Framework;
using System;
using System.IO;
using ViSyncMaster.Services;

namespace ViSyncMaster.nUnitTests
{
    [TestFixture]
    public class ProductTaktTimeCsvServiceTests
    {
        [Test]
        public void LoadAndUpsert_Works_ForCommaSeparatedCsv()
        {
            var path = Path.GetTempFileName();
            File.WriteAllText(path, "productName,median_takt_s\n9000577,10.232\n");

            var service = new ProductTaktTimeCsvService(path);
            Assert.That(service.TryGetTaktSeconds("9000577", out var takt), Is.True);
            Assert.That(takt, Is.EqualTo(10.232).Within(0.0001));

            service.UpsertTaktSeconds("NEW100", 12.5);
            var reloaded = new ProductTaktTimeCsvService(path);
            Assert.That(reloaded.TryGetTaktSeconds("NEW100", out var newTakt), Is.True);
            Assert.That(newTakt, Is.EqualTo(12.5).Within(0.0001));
        }
    }
}
