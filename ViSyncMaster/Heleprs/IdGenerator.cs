using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSyncMaster.Heleprs
{
    public static class IdGenerator
    {
        private static long _lastId = 0;
        private static readonly object _lock = new();

        public static long GetNextId()
        {
            lock (_lock)
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                _lastId = now > _lastId ? now : _lastId + 1;
                return _lastId;
            }
        }
    }
}
