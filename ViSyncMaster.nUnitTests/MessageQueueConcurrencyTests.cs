using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ViSyncMaster.DataModel;
using ViSyncMaster.Repositories;
using ViSyncMaster.Services;
using ViSyncMaster.ViewModels;

namespace ViSyncMaster.nUnitTests
{
    [TestFixture]
    public class MessageQueueConcurrencyTests
    {
        [Test]
        public async Task ConcurrentSendAllMessages_NoDuplicateDispatch()
        {
            var db = new SQLiteDatabase(":memory:");
            var repoMs = new GenericRepository<MachineStatus>(db, "MachineStatusQueue");
            var repoTr = new GenericRepository<MachineStatus>(db, "TestingResultQueue");
            var repoPe = new GenericRepository<ProductionEfficiency>(db, "ProductionEfficiency");
            var repoFp = new GenericRepository<FirstPartModel>(db, "FirstPartData");

            var senderMock = new Mock<MessageSender>(new MainWindowViewModel()) { CallBase = true };
            int sendCount = 0;
            senderMock.Setup(s => s.SendMessageAsync(It.IsAny<MachineStatus>()))
                      .Callback(() => sendCount++)
                      .ReturnsAsync(true);

            var queue = new MessageQueue(repoMs, repoTr, repoPe, repoFp, senderMock.Object);

            var field = typeof(MessageQueue).GetField("_machineStatusQueue", BindingFlags.NonPublic | BindingFlags.Instance);
            var msQueue = (Queue<MachineStatus>)field.GetValue(queue)!;
            msQueue.Enqueue(new MachineStatus { Id = 1 });

            var tasks = new[]
            {
                queue.SendAllMessages(),
                queue.SendAllMessages(),
                queue.SendAllMessages()
            };

            await Task.WhenAll(tasks);

            Assert.That(sendCount, Is.EqualTo(1));
            Assert.That(msQueue.Count, Is.EqualTo(0));
        }
    }
}