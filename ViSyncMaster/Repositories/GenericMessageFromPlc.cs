using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using S7.Net;

namespace ViSyncMaster.Repositories
{
    public class GenericMessageFromPlc
    {
        public async Task<List<double>> ReadPumpDataFromPlc()
        {
            var ip = "192.168.1.1";
            List<double> result = new List<double>();

            try
            {
                using (Plc plc = new Plc(CpuType.S71500, ip, 0, 0))
                {
                    await plc.OpenAsync();

                    if (plc.IsConnected)
                    {
                        int dbNumber = 2051; // Numer DB dla danych pomp
                        int startByte = 0;

                        // Definicja przesunięć w bajtach dla każdej pompy
                        double[] offsets = { 0.0, 4.0, 8.0 };

                        foreach (double offset in offsets)
                        {
                            byte[] dataBytes = plc.ReadBytes(DataType.DataBlock, dbNumber, startByte + (int)offset, 4); // Wartość rzeczywista ma 4 bajty
                            double value = S7.Net.Types.Double.FromByteArray(dataBytes);
                            result.Add(value);
                        }
                    }
                    else
                    {
                        throw new Exception($"Brak połączenia z PLC.. {ip}");
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Wystąpił błąd podczas komunikacji z PLC: {e.Message}");
            }

            return result;
        }
    }
}
