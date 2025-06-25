using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSyncMaster.DataModel
{
    public enum DatabaseOperation
    {
        Insert,   // Dodanie nowego rekordu
        Update,   // Aktualizacja istniejącego rekordu
        Delete    // Usunięcie rekordu
    }
}
