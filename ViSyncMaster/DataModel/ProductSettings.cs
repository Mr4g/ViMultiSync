using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViSyncMaster.DataModel
{
    public class ProductSettings
    {
        public string NumberProduct { get; set; }
        public int NumberClamp { get; set; }
        public int BreakingForceClamp { get; set; }
        public int BreakingForceInjection { get; set; }
        public int BreakingForceLumberg { get; set; }
        public int BreakingForcePlug { get; set; }
        public int HeightClamp { get; set; }
        public int InjectionHardness { get; set; }
        public int ScrewdriverTorque { get; set; }
        public int PasteWeight { get; set; }
        public int ShellSize { get; set; }
        public int Department { get; set; }
        public int Eq { get; set; }
        public int Signature { get; set; }
    }
}
