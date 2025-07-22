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
        private readonly Func<List<T>, Task> _action;

        public BatchEntityTask(List<T> entities, Func<List<T>, Task> action)
        {
            _entities = entities;
            _action = action;
        }

        public async Task ExecuteAsync()
        {
            await _action(_entities);
        }
    }
}
