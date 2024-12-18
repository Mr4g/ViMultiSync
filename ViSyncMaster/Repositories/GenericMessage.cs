using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViSyncMaster.DataModel;
using ViSyncMaster.Entitys;

namespace ViSyncMaster.Repositories
{
    public class GenericMessage<T> : IEntity
    {
        public long Id { get; set; }
        public T MessageData { get; set; }

        public string Value {get; set; }

        public string Name { get; set; }

        public string NameDevice { get; set; }
        public string Status { get; set; }

        public string Reason { get; set; }
        public string TimeOfAllStatus { get; set; }
        public string TimeOfAllRepairs { get; set; }

        public string Location { get; set; }

        public string Source { get; set; }

        public GenericMessage(T messageData)
        {
            MessageData = messageData;
        }
    }
}
