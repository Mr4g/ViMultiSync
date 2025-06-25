using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSyncMaster.Services
{
    public class BatchEntityTask<T> : IQueueTask where T : class
    {
        private readonly List<T> _entities;
        private readonly Action<List<T>> _action;

        public BatchEntityTask(List<T> entities, Action<List<T>> action)
        {
            _entities = entities;
            _action = action;
        }

        public void Execute()
        {
            _action(_entities);
        }
    }
}
