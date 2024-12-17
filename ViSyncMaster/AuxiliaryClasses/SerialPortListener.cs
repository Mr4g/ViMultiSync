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

namespace ViSyncMaster.AuxiliaryClasses
{
    public class SerialPortListener
    {
        private SerialPort serialPort;
        private StringBuilder dataBuffer;

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
                Console.WriteLine("Nasłuchiwanie na porcie " + serialPort.PortName + "...");
                Console.WriteLine($"Prędkość: {serialPort.BaudRate}, Parzystość: {serialPort.Parity}, Bity danych: {serialPort.DataBits}, Bity stopu: {serialPort.StopBits}");
            }
            catch (UnauthorizedAccessException)
            {
                ShowError("BŁĄD WYSYŁANIA DANYCH", "RS232 - Port jest już używany lub brak uprawnień.");
            }
            catch (ArgumentException)
            {
                ShowError("BŁĄD WYSYŁANIA DANYCH", "RS232 - Numeru portu jest nieprawidłowy.");
            }
            catch (IOException)
            {
                ShowError("BŁĄD WYSYŁANIA DANYCH", "RS232 - Numeru portu jest nieprawidłowy.");
            }
            catch (Exception ex)
            {
                ShowError("BŁĄD WYSYŁANIA DANYCH", $"RS232 - Nie udało się połączyć: {ex.Message}");
            }
        }

        public void StopListening()
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
                Console.WriteLine("Port został zamknięty.");
            }
        }

        public event EventHandler<Rs232Data> FrameReceived;

        private async void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string data = sp.ReadExisting();

            // Dodaj otrzymane dane do bufora
            dataBuffer.Append(data);


            // Sprawdź, czy pełna ramka znajduje się w buforze
            string bufferContent = dataBuffer.ToString();
            if (bufferContent.Contains("Frame_start") && bufferContent.Contains("Frame_end"))
            {
                int startIndex = bufferContent.IndexOf("Frame_start");
                int endIndex = bufferContent.IndexOf("Frame_end") + "Frame_end".Length;

                // Wyciągnij pełną ramkę
                string frame = bufferContent.Substring(startIndex, endIndex - startIndex);

                // Wyczyść bufor do końca ramki
                dataBuffer.Remove(0, endIndex);

                if (!IsFrameValid(frame))
                {
                    return; // Pomijamy dalsze przetwarzanie
                }


                // Wyświetl otrzymaną ramkę
                Console.WriteLine("Odebrano pełną ramkę: \n" + frame);

                // Parsowanie ramki na obiekt Rs232Data asynchronicznie
                Rs232Data testData = await Task.Run(() => ParseData(frame));

                // Wyświetl wartości z obiektu Rs232Data
                FrameReceived?.Invoke(this, testData);
            }
        }

        public static Rs232Data ParseData(string data)
        {
            Rs232Data testData = new Rs232Data();
            var lines = data.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            // Mapowanie kluczy na ustawienia właściwości
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
                { "S1.Producing", value => testData.Producing = value }
                // Dodaj kolejne mapowania według potrzeb
            };

            foreach (var line in lines)
            {
                var parts = line.Split(new[] { ':' }, 2);
                if (parts.Length < 2) continue;

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                // Jeśli klucz istnieje w mapie, ustaw właściwość
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
            if (questionMarkCount > 5) // Przykładowy próg
            {
                ShowError("Zakłócenia w transmisji", "Otrzymano ramkę zawierającą zbyt wiele znaków zapytania.");
                return false;
            }
            return true;
        }

        private void ShowError(string title, string message)
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                var box = MessageBoxManager.GetMessageBoxCustom(
                    new MessageBoxCustomParams
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
