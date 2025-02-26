using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media.TextFormatting;
using ViSyncMaster.Entitys;

namespace ViSyncMaster.DataModel
{

    /// <summary>
    /// Information about a chanel configuration
    /// </summary>
    public class CallForServicePanelItem : MachineStatus
    {
        public void SetStatusBasedOnRole(string role)
        {
            if (string.IsNullOrEmpty(role))
            {
                throw new ArgumentException("Role cannot be null or empty", nameof(role));
            }

            switch (role.ToLower())
            {
                case "mechaniczna":
                case "elektryczna":
                case "płyta":        
                    this.Status = "WEZWIJ SERWIS";
                    break;

                case "kptj":
                    this.Status = "WEZWIJ KPTJ";
                    break;

                case "lider":
                    this.Status = "WEZWIJ LIDER";
                    break;

                default:
                    this.Status = $"WEZWIJ {role}";
                    break;
            }
        }
    }
}
