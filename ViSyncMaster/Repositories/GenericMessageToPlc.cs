using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using S7.Net;

namespace ViSyncMaster.Repositories
{
    public class GenericMessageToPlc
    {
        public async Task WriteDataToPlc(List<object> data)
        {
            var ip = "192.168.1.1";
            try
            {
                using (Plc plc = new Plc(CpuType.S71500, ip, 0, 0))
                {
                    await plc.OpenAsync();

                    if (plc.IsConnected)
                    {
                        int dbNumber = 2048;
                        int startByte = 0;

                        for (int i = 0; i < data.Count; i++)
                        {
                            string value = data[i].ToString();
                            string valueTrimmed = value.Trim();
                            int length = valueTrimmed.Length;
                            byte[] dataBytes = S7.Net.Types.String.ToByteArray(valueTrimmed, length);

                            List<byte> plcData = new List<byte>();
                            plcData.Add((byte)length);
                            plcData.Add((byte)length);
                            plcData.AddRange(dataBytes);

                            int offset = 256 * i;

                            plc.WriteBytes(S7.Net.DataType.DataBlock, dbNumber, startByte + offset, plcData.ToArray());
                        }
                    }
                    else
                    {
                        // Handle connection error
                        throw new Exception($"Brak połączenia z PLC.. {ip}");
                    }
                }
            }
            catch (Exception e)
            {
                // Handle other exceptions
                throw new Exception($"Wystąpił błąd podczas komunikacji z PLC: {e.Message}");
            }
        }
    }
}

