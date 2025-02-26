using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSyncMaster.DataModel
{
    public interface IActiveEntity
    {
        bool IsActive { get; set; }
    }
}
