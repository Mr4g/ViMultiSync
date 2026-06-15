using Avalonia.Controls;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Models;
using MsBox.Avalonia;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;
using ViSyncMaster.DataModel;
using MsBox.Avalonia.Enums;
using System.Linq;
using Serilog;

namespace ViSyncMaster.AuxiliaryClasses
{
    public class SerialPortListener
    {
        private SerialPort serialPort;
        private StringBuilder dataBuffer;
        private readonly object bufferLock = new object();

        public SerialPortListener()
        {
            dataBuffer = new StringBuilder();
            serialPort = new SerialPort
            {
                BaudRate = 9600,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None
            };

            serialPort.DataReceived += SerialPort_DataReceived;
        }

        public void StartListening(string comPortNumber)
        {
            serialPort.PortName = "COM" + comPortNumber;

            try
            {
                serialPort.Open();
                Log.Information("Otwarto port {PortName} z parametrami: BaudRate={BaudRate}, Parity={Parity}, DataBits={DataBits}, StopBits={StopBits}",
                    serialPort.PortName, serialPort.BaudRate, serialPort.Parity, serialPort.DataBits, serialPort.StopBits);
            }
            catch (UnauthorizedAccessException)
            {
                Log.Error("RS232 - Port {PortName} jest już używany lub brak uprawnień.", serialPort.PortName);
                ShowError("BŁĄD WYSYŁANIA DANYCH", "RS232 - Port jest już używany lub brak uprawnień.");
            }
            catch (ArgumentException)
            {
                Log.Error("RS232 - Numer portu {PortName} jest nieprawidłowy (ArgumentException).", serialPort.PortName);
                ShowError("BŁĄD WYSYŁANIA DANYCH", "RS232 - Numer portu jest nieprawidłowy.");
            }
            catch (IOException)
            {
                Log.Error("RS232 - Numer portu {PortName} jest nieprawidłowy (IOException).", serialPort.PortName);
                ShowError("BŁĄD WYSYŁANIA DANYCH", "RS232 - Numer portu jest nieprawidłowy.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "RS232 - Nie udało się połączyć z portem {PortName}.", serialPort.PortName);
                ShowError("BŁĄD WYSYŁANIA DANYCH", $"RS232 - Nie udało się połączyć: {ex.Message}");
            }
        }

        public void StopListening()
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
                Log.Information("Zamknięto port {PortName}", serialPort.PortName);
            }
        }

        public event EventHandler<Rs232Data>? FrameReceived;
        public event EventHandler<RetestResultData>? RetestResultReceived;

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string data = ((SerialPort)sender).ReadExisting();
            ProcessReceivedData(data);
        }

        public void ProcessReceivedData(string data)
        {
            lock (bufferLock)
            {
                dataBuffer.Append(data);

                while (true)
                {
                    string bufferContent = dataBuffer.ToString();

                    int frameStartIndex = bufferContent.IndexOf("Frame_start", StringComparison.Ordinal);
                    int retestStartIndex = bufferContent.IndexOf("Retest_start", StringComparison.Ordinal);

                    if (frameStartIndex < 0 && retestStartIndex < 0)
                    {
                        break;
                    }

                    int firstStartIndex = GetFirstStartIndex(frameStartIndex, retestStartIndex);
                    if (firstStartIndex > 0)
                    {
                        dataBuffer.Remove(0, firstStartIndex);
                        continue;
                    }

                    if (frameStartIndex == 0)
                    {
                        int frameEndIndex = bufferContent.IndexOf(
                            "Frame_end",
                            "Frame_start".Length,
                            StringComparison.Ordinal);

                        if (frameEndIndex < 0)
                        {
                            break;
                        }

                        int frameEndExclusive = frameEndIndex + "Frame_end".Length;
                        string frame = bufferContent[..frameEndExclusive];
                        dataBuffer.Remove(0, frameEndExclusive);

                        if (!IsFrameValid(frame))
                        {
                            Log.Warning("Odrzucono ramkę z powodu zakłóceń (zbyt wiele znaków '?'):\n{Frame}", frame);
                            continue;
                        }

                        Log.Information("Odebrano ramkę RS232:\n{Frame}", frame);

                        Task.Run(() =>
                        {
                            var testData = ParseData(frame);
                            FrameReceived?.Invoke(this, testData);
                        });

                        continue;
                    }

                    int retestEndIndex = bufferContent.IndexOf(
                        "Retest_end",
                        "Retest_start".Length,
                        StringComparison.Ordinal);

                    if (retestEndIndex < 0)
                    {
                        break;
                    }

                    int retestEndExclusive = retestEndIndex + "Retest_end".Length;
                    string retestFrame = bufferContent[..retestEndExclusive];
                    dataBuffer.Remove(0, retestEndExclusive);

                    if (!TryParseRetestData(retestFrame, out RetestResultData? retestResult))
                    {
                        continue;
                    }

                    retestResult.Source = serialPort.PortName;
                    Log.Information("Odebrano pełną ramkę retestu RDFDiag: {Frame}", retestFrame);
                    RetestResultReceived?.Invoke(this, retestResult);
                }
            }
        }

        public static bool TryParseRetestData(string? data, out RetestResultData? result)
        {
            result = null;

            if (string.IsNullOrWhiteSpace(data))
            {
                return false;
            }

            string[] segments = data.Split(';', StringSplitOptions.TrimEntries);
            bool hasCompleteEnvelope = segments.Length >= 2
                && segments[0].Equals("Retest_start", StringComparison.Ordinal)
                && segments[^1].Equals("Retest_end", StringComparison.Ordinal);

            if (!hasCompleteEnvelope)
            {
                if (data.Contains("Retest_end", StringComparison.Ordinal))
                {
                    Log.Warning("Odrzucono uszkodzoną pełną ramkę retestu RDFDiag: {Frame}", data);
                }

                return false;
            }

            try
            {
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (string segment in segments[1..^1])
                {
                    int separatorIndex = segment.IndexOf(':');
                    if (separatorIndex <= 0 || separatorIndex == segment.Length - 1)
                    {
                        Log.Warning(
                            "Niepoprawne mapowanie segmentu {Segment} w ramce retestu RDFDiag: {Frame}",
                            segment,
                            data);
                        return false;
                    }

                    string key = segment[..separatorIndex].Trim();
                    string value = segment[(separatorIndex + 1)..].Trim();

                    if (key.Length == 0 || value.Length == 0)
                    {
                        Log.Warning(
                            "Niepoprawne mapowanie segmentu {Segment} w ramce retestu RDFDiag: {Frame}",
                            segment,
                            data);
                        return false;
                    }

                    values[key] = value;
                }

                string[] requiredKeys =
                {
                    "TEST_OBJECT",
                    "TOTAL_ABS",
                    "DATE",
                    "TIME",
                    "FAULT",
                    "FROM",
                    "TO_POINT",
                    "VALUE",
                    "MEAS_TYPE"
                };

                string[] missingKeys = requiredKeys.Where(key => !values.ContainsKey(key)).ToArray();
                if (missingKeys.Length > 0)
                {
                    Log.Warning(
                        "Brak wymaganych pól {MissingKeys} w ramce retestu RDFDiag: {Frame}",
                        string.Join(", ", missingKeys),
                        data);
                    return false;
                }

                result = new RetestResultData
                {
                    TestObject = GetValue(values, "TEST_OBJECT"),
                    TotalAbs = GetValue(values, "TOTAL_ABS"),
                    Date = GetValue(values, "DATE"),
                    Time = GetValue(values, "TIME"),
                    Fault = GetValue(values, "FAULT"),
                    FromPoint = GetValue(values, "FROM"),
                    ToPoint = GetValue(values, "TO_POINT"),
                    Value = GetValue(values, "VALUE"),
                    MeasurementType = GetValue(values, "MEAS_TYPE"),
                    RawFrame = data
                };

                return true;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Nie udało się sparsować ramki retestu RDFDiag: {Frame}", data);
                return false;
            }
        }

        private static string? GetValue(IReadOnlyDictionary<string, string> values, string key)
        {
            return values.TryGetValue(key, out string? value) ? value : null;
        }

        private static int GetFirstStartIndex(int frameStartIndex, int retestStartIndex)
        {
            if (frameStartIndex < 0)
            {
                return retestStartIndex;
            }

            if (retestStartIndex < 0)
            {
                return frameStartIndex;
            }

            return Math.Min(frameStartIndex, retestStartIndex);
        }

        public static Rs232Data ParseData(string data)
        {
            Rs232Data testData = new Rs232Data();
            var lines = data.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            var propertyMap = new Dictionary<string, Action<string>>
            {
                { "Device", value => testData.Device = value },
                { "ST", value => testData.ST = value },
                { "OP", value => testData.Operator = value },
                { "TO", value => testData.TestObject = value },
                { "TST_FAULT", value => testData.TestFault = value },
                { "TOTAL_ABS", value => testData.TotalAbs = value },
                { "TGOOD_ABS", value => testData.TGoodAbs = value },
                { "GOOD_ABS", value => testData.GoodAbs = value },
                { "RGOOD_ABS", value => testData.RGoodAbs = value },
                { "FAULT_ABS", value => testData.FaultAbs = value },
                { "TGOOD_REL", value => testData.TGoodRel = value },
                { "GOOD_REL", value => testData.GoodRel = value },
                { "RGOOD_REL", value => testData.RGoodRel = value },
                { "FAULT_REL", value => testData.FaultRel = value },
                { "S7.TestingPassed", value => testData.TestingPassed = value },
                { "S7.TestingFailed", value => testData.TestingFailed = value },
                { "S7.OperatorId", value => testData.OperatorId = value },
                { "S7.ProductName", value => testData.ProductName = value },
                { "S1.Producing", value => testData.Producing = value }
            };

            foreach (var line in lines)
            {
                var parts = line.Split(new[] { ':' }, 2);
                if (parts.Length < 2) continue;

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                if (propertyMap.ContainsKey(key))
                {
                    propertyMap[key](value);
                }
            }

            return testData;
        }

        private bool IsFrameValid(string frame)
        {
            int questionMarkCount = frame.Count(c => c == '?');
            return questionMarkCount <= 5;
        }

        private void ShowError(string title, string message)
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
                {
                    ButtonDefinitions = new List<ButtonDefinition>
                    {
                        new ButtonDefinition { Name = "OK" },
                        new ButtonDefinition { Name = "Cancel" }
                    },
                    ContentTitle = title,
                    ContentMessage = message,
                    Icon = Icon.Error,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    MaxWidth = 500,
                    MaxHeight = 800,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ShowInCenter = true,
                    Topmost = true,
                });
                box.ShowAsync();
            });
        }
    }
}
