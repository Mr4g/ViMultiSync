using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSyncMaster.Services
{
    public class WriteThreadManager
    {
        private static WriteThreadManager? _instance;
        private readonly Task _writeTask;
        private readonly BlockingCollection<Action> _writeQueue = new BlockingCollection<Action>();

        private WriteThreadManager()
        {
            _writeTask = Task.Factory.StartNew(ProcessQueue, TaskCreationOptions.LongRunning);
        }

        public static WriteThreadManager Instance => _instance ??= new WriteThreadManager();

        public void Enqueue(Action writeAction)
        {
            _writeQueue.Add(writeAction);
        }

        private void ProcessQueue()
        {
            foreach (var writeAction in _writeQueue.GetConsumingEnumerable())
            {
                writeAction();
            }
        }
    }
}
