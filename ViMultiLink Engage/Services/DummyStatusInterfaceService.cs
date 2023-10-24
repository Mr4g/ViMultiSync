using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMultiSync.DataModel;

namespace ViMultiSync.Services
{
    public class DummyStatusInterfaceService : IStatusInterfaceService
    {
        public Task<List<DowntimePanelItem>> GetDowntimePanelAsync() =>
            Task.FromResult(new List<DowntimePanelItem>(new []
            {
                new DowntimePanelItem { Status = "Downtime", LongText = "MECHANICZNA", ShortText = "MECH" },
                new DowntimePanelItem { Status = "Downtime", LongText = "ELEKTRYCZNA", ShortText = "ELE" },
                new DowntimePanelItem { Status = "Downtime", LongText = "USTAWIACZ", ShortText = "UST" },

                new DowntimePanelItem { Status = "Maintenance", LongText = "SPRZĄTANIE", ShortText = "SPRZ" },
                new DowntimePanelItem { Status = "Maintenance", LongText = "SPRZĄTANIE", ShortText = "SPRZ" },
                new DowntimePanelItem { Status = "Maintenance", LongText = "SPRZĄTANIE", ShortText = "SPRZ" },

                new DowntimePanelItem { Status = "Setting", LongText = "PRZEZBROJENIE", ShortText = "PRZEZ" },
                new DowntimePanelItem { Status = "Setting", LongText = "KOREKTY", ShortText = "KORE" },
                new DowntimePanelItem { Status = "Setting", LongText = "ZMIANA PARAMETRÓW", ShortText = "PARAMETRY" },
            }));
    }
}
