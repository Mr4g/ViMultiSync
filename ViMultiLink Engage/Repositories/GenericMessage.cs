using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMultiSync.DataModel;
using ViMultiSync.Entitys;

namespace ViMultiSync.Repositories
{
    public class GenericMessage<T> : IEntity
    {
        public int Id { get; set; }
        public T MessageData { get; set; }

        public string Value {get; set; }

        public string Name { get; set; }

        public string NameDevice { get; set; }
        public string Status { get; set; }

        public string Reason { get; set; }

        public string Location { get; set; }

        public string Source { get; set; }

        public GenericMessage(T messageData)
        {
            MessageData = messageData;
        }
    }
}
