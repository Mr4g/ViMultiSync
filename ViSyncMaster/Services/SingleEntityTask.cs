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
        private readonly Action<T> _action;

        public SingleEntityTask(T entity, Action<T> action)
        {
            _entity = entity;
            _action = action;
        }

        public void Execute()
        {
            _action(_entity);
        }
    }
}
