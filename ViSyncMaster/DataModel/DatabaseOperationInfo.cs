using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSyncMaster.DataModel
{
    public class DatabaseOperationInfo
    {
        public string TableName { get; set; }
        public DatabaseOperation Operation { get; set; } // Możemy wykorzystać wcześniej zdefiniowany enum
        public object Entity { get; set; } // Możemy tutaj przekazać rekord, który został dodany/aktualizowany/usunięty

        public DatabaseOperationInfo(string tableName, DatabaseOperation operation, object entity)
        {
            TableName = tableName;
            Operation = operation;
            Entity = entity;
        }
    }
}
