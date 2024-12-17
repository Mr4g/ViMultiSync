using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViSyncMaster.Entitys;

namespace ViSyncMaster.DataModel
{
    public class TestingPassedMessage : IEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Value { get; set; }
        public string? NameDevice { get; set; }
        public string? Status { get; set; }
        public string? Location { get; set; }
        public string? Source { get; set; }
        public string? Reason { get; set; }
        public string? TimeOfAllStatus { get; set; }
        public string? TimeOfAllRepairs { get; set; }

        public TestingPassedMessage()
        {
            this.Name = "S7.TestingPassed";
            this.Value = "false";
        }
    }


}