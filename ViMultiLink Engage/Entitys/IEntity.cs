using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViMultiSync.Entitys
{
    public interface IEntity
    {
        int Id { get; set; } 

        string Value { get; set; }

        public string Name { get; set; }

        public string NameDevice { get; set; }
        public string Status { get; set; }

        public string Location { get; set; }

        public string Source { get; set; }
        string Reason { get; set; }
    }
}
