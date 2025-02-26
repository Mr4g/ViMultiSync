using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSyncMaster.Services
{
    public class QueueManager<T>
    {
        private static QueueManager<T>? _instance;
        private readonly BufferedQueue<T> _bufferedQueue;

        public QueueManager(Action<T> processItemAction)
        {
            _bufferedQueue = new BufferedQueue<T>(processItemAction);
        }

        public static void Initialize(Action<T> processItemAction)
        {
            if (_instance == null)
            {
                _instance = new QueueManager<T>(processItemAction);
            }
        }

        public static QueueManager<T> Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException("QueueManager is not initialized. Call Initialize first.");
                }
                return _instance;
            }
        }

        public async Task Enqueue(T item)
        {
            await _bufferedQueue.Enqueue(item);
        }
    }
}
