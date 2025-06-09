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

        public event EventHandler<Rs232Data> FrameReceived;

        private async void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string data = ((SerialPort)sender).ReadExisting();

            lock (bufferLock)
            {
                dataBuffer.Append(data);

                while (true)
                {
                    string bufferContent = dataBuffer.ToString();

                    int startIndex = bufferContent.IndexOf("Frame_start");
                    int endIndex = bufferContent.IndexOf("Frame_end");

                    if (startIndex == -1 || endIndex == -1 || endIndex < startIndex)
                        break;

                    endIndex += "Frame_end".Length;
                    string frame = bufferContent.Substring(startIndex, endIndex - startIndex);
                    dataBuffer.Remove(0, endIndex);

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
                }
            }
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
