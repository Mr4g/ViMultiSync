using SkiaSharp;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Collections.Generic;
using ViSyncMaster.DataModel;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Linq;
using LiveChartsCore.SkiaSharpView.Painting;
using System.Collections.ObjectModel;
using System;
using LiveChartsCore.Kernel.Sketches;
using Newtonsoft.Json.Linq;
using Avalonia.Controls;
using Xilium.CefGlue;
using DynamicData;

namespace ViSyncMaster.ViewModels
{
    public partial class MachineStatusTableViewModel : ObservableObject
    {
        // Lista statusów maszyn
        [ObservableProperty]
        private ObservableCollection<MachineStatus> machineStatusList;

        // Seria danych dla wykresu
        private ISeries[] _chartSeries;
        public ISeries[] ChartSeries
        {
            get => _chartSeries;
            private set => SetProperty(ref _chartSeries, value);
        }

        // Oś X (etykiety)
        private ICartesianAxis[] _xAxes;
        public ICartesianAxis[] XAxes
        {
            get => _xAxes;
            private set => SetProperty(ref _xAxes, value);
        }

        private ICartesianAxis[] _yAxes;

        public ICartesianAxis[] YAxes
        {
            get => _yAxes;
            private set => SetProperty(ref _yAxes, value);
        }

        public MachineStatusTableViewModel(ObservableCollection<MachineStatus> machineStatuses = null)
        {
            MachineStatusList = machineStatuses ?? throw new ArgumentNullException(nameof(machineStatuses));

            // Nasłuchiwanie zmian w liście (dynamiczna aktualizacja wykresu)
            MachineStatusList.CollectionChanged += (s, e) => UpdateChart();

            
            // Inicjalizacja wykresu
            UpdateChart();
        }

        private void UpdateChart()
        {
            // Przygotowanie danych dla trzech czasów
            var durationStatus = MachineStatusList.Select(m => Math.Round(m.DurationStatus?.TotalMinutes ?? 0, 2)).ToList();
            var durationWaitingForService = MachineStatusList.Select(m => Math.Round(m.DurationWaitingForService?.TotalMinutes ?? 0, 2)).ToList();
            var durationService = MachineStatusList.Select(m => Math.Round(m.DurationService?.TotalMinutes ?? 0, 2)).ToList();

            var labels = MachineStatusList.Select(m => m.Status).ToList();

            if (ChartSeries == null || ChartSeries.Length == 0)
            {
                // Tworzenie serii wykresu
                ChartSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Name = "Czas trwania statusu",
                    Values = durationStatus, // Wartości wykresu dla DurationStatus
                    Fill = new SolidColorPaint(SKColor.Parse("#FF0000")), // Kolor czerwony dla DurationStatus
                    MaxBarWidth = 80,
                    // Etykiety na słupkach
                    DataLabelsPaint = new SolidColorPaint { Color = SKColors.WhiteSmoke },
                    DataLabelsSize = 20,
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,
                    DataLabelsFormatter = point => FormatDuration(point.Model)

                },
                new ColumnSeries<double>
                {
                    Name = "Czas oczekiwania na serwis",
                    Values = durationWaitingForService, // Wartości wykresu dla DurationWaitingForService
                    Fill = new SolidColorPaint(SKColor.Parse("#FFFF00")), // Kolor żółty dla DurationWaitingForService
                    MaxBarWidth = 80,
                    DataLabelsPaint = new SolidColorPaint { Color = SKColors.WhiteSmoke },
                    DataLabelsSize = 20,
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,
                    DataLabelsFormatter = point => FormatDuration(point.Model)

                },
                new ColumnSeries<double>
                {
                    Name = "Czas naprawy",
                    Values = durationService, // Wartości wykresu dla DurationService
                    Fill = new SolidColorPaint(SKColor.Parse("#00FF00")), // Kolor zielony dla DurationService
                    MaxBarWidth = 80,
                    DataLabelsPaint = new SolidColorPaint { Color = SKColors.WhiteSmoke },
                    DataLabelsSize = 20,
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,
                    DataLabelsFormatter = point => FormatDuration(point.Model)

                }
            };

            // Konfiguracja osi X (dostosowanie do trzech sekcji)
            XAxes = new ICartesianAxis[]
            {
                new Axis
                {
                    Name = "Statusy",
                    Labels = labels, // Etykiety dla każdego statusu
                    LabelsPaint = new SolidColorPaint { Color = SKColors.WhiteSmoke },
                    SeparatorsPaint = new SolidColorPaint { Color = SKColors.LightGray }, // Kolor separatorów
                    SeparatorsAtCenter = false, // Separatory na początku i końcu
                    ShowSeparatorLines = false,
                }
            };

            // Konfiguracja osi Y
            YAxes = new ICartesianAxis[]
            {
                new Axis
                {
                    Name = "Czas trwania (minuty)",
                    MinLimit = 0, // Wymuszenie startu od 0
                    LabelsPaint = new SolidColorPaint { Color = SKColors.WhiteSmoke }
                }
            };
        }
            else
            {
                // Aktualizacja wartości dla każdej serii
                for (int i = 0; i < ChartSeries.Length; i++)
                {
                    if (ChartSeries[i] is ColumnSeries<double> columnSeries)
                    {
                        // W zależności od indeksu, przypisz odpowiednią listę wartości
                        if (i == 0)
                        {
                            columnSeries.Values = durationStatus; // Czas trwania statusu
                        }
                        else if (i == 1)
                        {
                            columnSeries.Values = durationWaitingForService; // Czas oczekiwania na serwis
                        }
                        else if (i == 2)
                        {
                            columnSeries.Values = durationService; // Czas naprawy
                        }
                    }
                }

                // Aktualizacja etykiet osi X
                XAxes[0].Labels = labels;
            
            }
        }

        // Funkcja formatowania czasu w formacie hh:mm:ss (opcjonalnie do tooltipów)
        private string FormatDuration(double value)
        {
            var minutes = Math.Round(value, 2); // Zaokrąglenie do 2 miejsc po przecinku
            var timeSpan = TimeSpan.FromMinutes(minutes); // Przekształcenie na TimeSpan
            return timeSpan.ToString(@"hh\:mm\:ss"); // Formatowanie
        }
    }
}
