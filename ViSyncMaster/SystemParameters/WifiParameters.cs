using System;
using System.Diagnostics;
using System.Text;
using ViSyncMaster.AuxiliaryClasses;

namespace ViSyncMaster.SystemParameters
{
    public class WifiParameters
    {
        public string? Ssid { get; private set; }

        /// <summary>
        /// Pobiera nazwę aktualnie połączonej sieci Wi-Fi za pomocą polecenia netsh.
        /// </summary>
        /// <returns>SSID aktualnej sieci Wi-Fi lub "Not connected", jeśli brak połączenia.</returns>
        public string FetchWifiName()
        {
            try
            {
                // Uruchomienie procesu netsh do pobrania informacji o Wi-Fi
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = "wlan show interfaces",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // Wyszukiwanie SSID w wyjściu
                string ssidMarker = "SSID                   : ";
                int ssidStartIndex = output.IndexOf(ssidMarker);
                if (ssidStartIndex >= 0)
                {
                    ssidStartIndex += ssidMarker.Length;
                    int ssidEndIndex = output.IndexOf("\r\n", ssidStartIndex);
                    Ssid = output.Substring(ssidStartIndex, ssidEndIndex - ssidStartIndex).Trim();
                    return Ssid;
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.ShowErrorNetwork("Problem z wyświetleniem nazwy SSID za pomocą polecenia netsh.");
            }

            Ssid = "Not connected";
            return Ssid;
        }
    }
}
