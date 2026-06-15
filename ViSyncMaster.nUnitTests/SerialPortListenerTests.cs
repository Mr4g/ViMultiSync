using ViSyncMaster.AuxiliaryClasses;
using ViSyncMaster.DataModel;

namespace ViSyncMaster.nUnitTests
{
    [TestFixture]
    public class SerialPortListenerTests
    {
        private const string RetestFrame =
            "Retest_start;TEST_OBJECT:GM3T1;TOTAL_ABS:123;DATE:15.06.2026;TIME:10:45:12;" +
            "FAULT:Open circuit;FROM:X1/12;TO_POINT:X2/08;VALUE:9848 Ohm;" +
            "MEAS_TYPE:Continuity;Retest_end";

        [Test]
        public void TryParseRetestData_MapsRdfDiagFieldsAndPreservesRawFrame()
        {
            bool parsed = SerialPortListener.TryParseRetestData(RetestFrame, out var result);

            Assert.That(parsed, Is.True);
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result!.EventType, Is.EqualTo("RetestResult"));
                Assert.That(result!.TestObject, Is.EqualTo("GM3T1"));
                Assert.That(result.TotalAbs, Is.EqualTo("123"));
                Assert.That(result.Date, Is.EqualTo("15.06.2026"));
                Assert.That(result.Time, Is.EqualTo("10:45:12"));
                Assert.That(result.Fault, Is.EqualTo("Open circuit"));
                Assert.That(result.FromPoint, Is.EqualTo("X1/12"));
                Assert.That(result.ToPoint, Is.EqualTo("X2/08"));
                Assert.That(result.Value, Is.EqualTo("9848 Ohm"));
                Assert.That(result.MeasurementType, Is.EqualTo("Continuity"));
                Assert.That(result.RawFrame, Is.EqualTo(RetestFrame));
            });
        }

        [Test]
        public void TryParseRetestData_IncompleteFrame_ReturnsFalseWithoutThrowing()
        {
            bool parsed = SerialPortListener.TryParseRetestData(
                "Retest_start;TEST_OBJECT:GM3T1;FAULT:Open circuit",
                out var result);

            Assert.That(parsed, Is.False);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void ProcessReceivedData_IncompleteRetestWithNewLine_WaitsForRetestEnd()
        {
            using var received = new ManualResetEventSlim();
            var listener = new SerialPortListener();
            listener.RetestResultReceived += (_, _) => received.Set();

            int splitIndex = RetestFrame.IndexOf("FAULT", StringComparison.Ordinal);
            listener.ProcessReceivedData(RetestFrame[..splitIndex] + "\r\n");

            Assert.That(received.IsSet, Is.False);

            listener.ProcessReceivedData(RetestFrame[splitIndex..]);

            Assert.That(received.Wait(TimeSpan.FromSeconds(1)), Is.True);
        }

        [Test]
        public void ProcessReceivedData_EmitsRetestSplitAcrossSerialReads()
        {
            using var received = new ManualResetEventSlim();
            var listener = new SerialPortListener();
            RetestResultData? result = null;
            listener.RetestResultReceived += (_, data) =>
            {
                result = data;
                received.Set();
            };

            listener.ProcessReceivedData(RetestFrame[..40]);
            listener.ProcessReceivedData(RetestFrame[40..] + "\r\n");

            Assert.That(received.Wait(TimeSpan.FromSeconds(1)), Is.True);
            Assert.That(result?.TestObject, Is.EqualTo("GM3T1"));
        }

        [Test]
        public void ProcessReceivedData_StandardStatusFrameStillEmitsFrameReceived()
        {
            const string statusFrame =
                "Frame_start\nDevice:Tester1\nOP:Operator1\nTO:Product1\nST:Running\n" +
                "TOTAL_ABS:42\nFrame_end";
            using var received = new ManualResetEventSlim();
            var listener = new SerialPortListener();
            Rs232Data? result = null;
            listener.FrameReceived += (_, data) =>
            {
                result = data;
                received.Set();
            };

            listener.ProcessReceivedData(statusFrame[..25]);
            listener.ProcessReceivedData(statusFrame[25..]);

            Assert.That(received.Wait(TimeSpan.FromSeconds(1)), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(result?.Device, Is.EqualTo("Tester1"));
                Assert.That(result?.Operator, Is.EqualTo("Operator1"));
                Assert.That(result?.TestObject, Is.EqualTo("Product1"));
                Assert.That(result?.ST, Is.EqualTo("Running"));
                Assert.That(result?.TotalAbs, Is.EqualTo("42"));
            });
        }

        [Test]
        public void ProcessReceivedData_ProcessesTheFrameWhoseStartMarkerComesFirst()
        {
            const string statusFrame = "Frame_start\nDevice:First\nFrame_end";
            var receivedTypes = new List<string>();
            using var received = new CountdownEvent(2);
            var listener = new SerialPortListener();
            listener.FrameReceived += (_, _) =>
            {
                lock (receivedTypes)
                {
                    receivedTypes.Add("status");
                }
                received.Signal();
            };
            listener.RetestResultReceived += (_, _) =>
            {
                lock (receivedTypes)
                {
                    receivedTypes.Add("retest");
                }
                received.Signal();
            };

            listener.ProcessReceivedData(statusFrame + RetestFrame);

            Assert.That(received.Wait(TimeSpan.FromSeconds(1)), Is.True);
            Assert.That(receivedTypes, Is.EquivalentTo(new[] { "status", "retest" }));
        }

        [Test]
        public void TryParseRetestData_DamagedCompleteFrame_ReturnsFalseWithoutThrowing()
        {
            const string damagedFrame =
                "Retest_start;TEST_OBJECT:GM3T1;TOTAL_ABS;Retest_end";

            Assert.DoesNotThrow(() =>
            {
                bool parsed = SerialPortListener.TryParseRetestData(damagedFrame, out var result);
                Assert.That(parsed, Is.False);
                Assert.That(result, Is.Null);
            });
        }
    }
}
