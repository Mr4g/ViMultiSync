using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSyncMaster.DataModel
{
    public class Rs232Data
    {               
        // General In?put
        public string? PrintoutHeader { get; set; }
        public string? Operator { get; set; }
        public string? Device { get; set; }                 
        public string? ST { get; set; }                    

        public string? TestObject { get; set; } 
        public string? TestFault { get; set; } // Number of faults in test
        public string? TotalAbs { get; set; } // Total number of test samples
        public string? TGoodAbs { get; set; } // Number of passed test objects with retest
        public string? GoodAbs { get; set; } // Number of passed test objects
        public string? RGoodAbs { get; set; } // Number of passed test objects with retest
        public string? FaultAbs { get; set; } // Number of failed test objects
        public string? TestingPassed { get; set; }
        public string? TestingFailed { get; set; }
        public string? Producing { get; set; }
        public string? TGoodRel { get; set; } // Relative number of passed test objects with retest (%)
        public string? GoodRel { get; set; } // Relative number of passed test objects (%)
        public string? RGoodRel { get; set; } // Relative number of passed test objects with retest (%)
        public string? FaultRel { get; set; } // Relative number of failed test objects (%)
    }
}