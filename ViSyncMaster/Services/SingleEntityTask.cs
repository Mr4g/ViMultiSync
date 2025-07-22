using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSyncMaster.Services
{
    public class SingleEntityTask<T> : IQueueTask where T : class
    {
        private readonly T _entity;
        private readonly Func<T, Task> _action;

        public SingleEntityTask(T entity, Func<T, Task> action)
        {
            _entity = entity;
            _action = action;
        }

        public async Task ExecuteAsync()
        {
            await _action(_entity);
        }
    }
}
