using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Extensions;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ViSyncMaster.AuxiliaryClasses;
using ViSyncMaster.DataModel;
using LiveChartsCore.VisualElements;
using LiveChartsCore.SkiaSharpView.VisualElements;
using Avalonia.Threading;
using ViSyncMaster.Services;
using System.Threading.Tasks;

namespace ViSyncMaster.ViewModels
{
    public partial class ResultTableViewModel : ObservableObject
    {

        private readonly MachineStatusService _machineStatusService;
        private ObservableCollection<MachineStatus> _originalResultTestList; // Full data list
        private readonly ProductionEfficiencyCalculator _efficiencyCalculator;
        private DispatcherTimer _hourlyTimer;
        private ProductionEfficiency _productionEfficiency;


        // Observable properties for binding
        [ObservableProperty] private ObservableCollection<ISeries> _seriesExpectedEfficiency = new(); // Pozostaw tylko to z atrybutem ObservableProperty
        [ObservableProperty] private ObservableCollection<ISeries> _pieChartSeriesTotalUnitsPredicted = new();
        [ObservableProperty] private ObservableCollection<ISeries> _pieChartSeriesCurrentEfficiency = new();
        [ObservableProperty] private ObservableCollection<ISeries> _pieChartSeriesExpectedEfficiency = new();
        [ObservableProperty] private ObservableCollection<MachineStatus> _resultTestList;
        [ObservableProperty] private ObservableCollection<MachineStatusGrouped> _groupedResultList;
        [ObservableProperty] private double _target = 200;
        [ObservableProperty] private double _totalUnitsProduced;
        [ObservableProperty] private double _expectedEfficiency;
        [ObservableProperty] private double _currentEfficiency;
        [ObservableProperty] private double _expectedOutput;

        public IEnumerable<ISeries> Series { get; set; }
        public IEnumerable<ISeries> SeriesEfficiency { get; set; }

        public IEnumerable<VisualElement> VisualElements { get; set; }
        public NeedleVisual Needle { get; set; }
        public ObservableValue TotalPartsProducedChart { get; set; }
        public ObservableValue ExpectedPartsChart { get; set; }
        public ObservableValue TargetPartsChart { get; set; }

        public SolidColorPaint LegendTextPaint { get; set; } // Kolor tekstu legendy

        public ResultTableViewModel(MachineStatusService machineStatusService, ObservableCollection<MachineStatus> resultTests = null)
        {
            _machineStatusService = machineStatusService;
            _productionEfficiency = new ProductionEfficiency();
            _originalResultTestList = resultTests ?? throw new ArgumentNullException(nameof(resultTests));
            ResultTestList = new ObservableCollection<MachineStatus>(_originalResultTestList);
            GroupedResultList = GroupByProductName(ResultTestList);
            AutoSelectCurrentShift();
            InicializeChart();           
            // Initializing efficiency calculator
            _efficiencyCalculator = new ProductionEfficiencyCalculator(true); // Assuming first shift
            _originalResultTestList.CollectionChanged += (s, e) => UpdateChartData();
            StartHourlyTimer();
        }
        private void InicializeChart()
        {
            TotalPartsProducedChart = new ObservableValue { Value = TotalUnitsProduced };
            ExpectedPartsChart = new ObservableValue { Value = ExpectedOutput };
            TargetPartsChart = new ObservableValue { Value = Target };

            // Tworzenie serii
            Series = GaugeGenerator.BuildSolidGauge(

               new GaugeItem(TotalPartsProducedChart, series =>
               {
                   series.Name = "Jest";
                   series.DataLabelsPosition = PolarLabelsPosition.Start;
                   series.DataLabelsPaint = new SolidColorPaint(SKColors.WhiteSmoke);
                   series.Fill = new SolidColorPaint(SKColors.Red); // Czerwony kolor
                   series.InnerRadius = 30; // Ustal promień wewnętrzny
                   series.OuterRadiusOffset = 20;
               }),
               new GaugeItem(ExpectedPartsChart, series =>
               {
                   series.Name = "Powinno być";
                   series.DataLabelsPosition = PolarLabelsPosition.Start;
                   series.DataLabelsPaint = new SolidColorPaint(SKColors.WhiteSmoke);
                   series.Fill = new SolidColorPaint(SKColors.LightBlue); // Zielony kolor
                   series.InnerRadius = 40; // Ustal promień wewnętrzny
                   series.OuterRadiusOffset = 15; // Ustal odstęp od zewnętrznej krawędzi
               }),
               new GaugeItem(TargetPartsChart, series =>
               {
                   series.Name = "Cel";
                   series.DataLabelsPosition = PolarLabelsPosition.Start;
                   series.DataLabelsPaint = new SolidColorPaint(SKColors.WhiteSmoke);
                   series.Fill = new SolidColorPaint(SKColors.Green); // Czerwony kolor
                   series.InnerRadius = 50; // Ustal promień wewnętrzny
               }));

            var sectionsOuter = 130;
            var sectionsWidth = 20;

            Needle = new NeedleVisual
            {
                Value = 0,
                Fill = new SolidColorPaint(SKColors.WhiteSmoke)
            };

            SeriesEfficiency = GaugeGenerator.BuildAngularGaugeSections(
                new GaugeItem(80, s => SetStyle(sectionsOuter, sectionsWidth, s, SKColors.Red)),   // Sekcja czerwona (0-80)
                new GaugeItem(40, s => SetStyle(sectionsOuter, sectionsWidth, s, SKColors.Green)), // Sekcja zielona (80-120)
                new GaugeItem(80, s => SetStyle(sectionsOuter, sectionsWidth, s, SKColors.Yellow))    // Sekcja czerwona (120-200)
            );

            VisualElements =
            [
                new AngularTicksVisual
            {
                Labeler = value => value.ToString("N1"),
                LabelsSize = 16,
                LabelsOuterOffset = 15,
                OuterOffset = 65,
                TicksLength = 20,
                LabelsPaint = new SolidColorPaint(SKColors.WhiteSmoke),
                Stroke = new SolidColorPaint(SKColors.WhiteSmoke)
            },
            Needle
            ];

            SeriesExpectedEfficiency = new ObservableCollection<ISeries>
            {
               new PieSeries<double>
               {
                    Name = "Targergt do zrealizowania",
                    Values = new double[] { ExpectedEfficiency },
                    Fill = new SolidColorPaint(SKColor.Parse("#3498db")), // Niebieski
                    MaxRadialColumnWidth = 70,
                    DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue:0.##}%",
                    DataLabelsPaint = new SolidColorPaint { Color = SKColors.WhiteSmoke },
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer,
                    DataLabelsSize = 25
               }};
            LegendTextPaint = new SolidColorPaint(SKColors.White); // Zmiana koloru legendy na biały
        }

        // Command methods
        [RelayCommand] public void FilterShift1() => FilterByTimeRangeAndGroupByProductNameAndDateTime(DateTime.Today, TimeSpan.Parse("05:40"), TimeSpan.Parse("13:40"));
        [RelayCommand] public void FilterShift2() => FilterByTimeRangeAndGroupByProductNameAndDateTime(DateTime.Today, TimeSpan.Parse("13:40"), TimeSpan.Parse("21:40"));
        [RelayCommand] public void FilterYesterdayShift2() => FilterByTimeRangeAndGroupByProductNameAndDateTime(DateTime.Today.AddDays(-1), TimeSpan.Parse("13:40"), TimeSpan.Parse("21:40"));

        [RelayCommand]
        public void FilterWholeWeek()
        {
            var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1); // Monday of this week
            var endOfWeek = startOfWeek.AddDays(6); // Sunday of this week

            var filteredList = _originalResultTestList
                .Where(x => x.StartTime.HasValue && x.StartTime.Value.Date >= startOfWeek && x.StartTime.Value.Date <= endOfWeek)
                .ToList();

            ResultTestList = new ObservableCollection<MachineStatus>(filteredList);
            GroupedResultList = GroupByProductName(ResultTestList);
            UpdateGroupedResultListWithTotal();
        }

        [RelayCommand]
        private void SaveTarget()
        {
            // Save the target and calculate efficiency
            Console.WriteLine($"Saved target: {Target}");
            UpdateChartData();
        }

        private ObservableCollection<MachineStatusGrouped> GroupByProductName(ObservableCollection<MachineStatus> resultList)
        {
            var groupedData = resultList
                .GroupBy(x => x.ProductName)
                .Select(g => new MachineStatusGrouped
                {
                    ProductName = g.Key,
                    PassedCount = g.Count(x => x.Name == "S7.TestingPassed" && x.Value == "true"),
                    FailedCount = g.Count(x => x.Name == "S7.TestingFailed" && x.Value == "true"),
                    Operators = string.Join(", ", g.Select(x => x.OperatorId).Distinct())
                }).ToList();

            return new ObservableCollection<MachineStatusGrouped>(groupedData);
        }

        partial void OnTargetChanged(double value)
        {
            UpdateChartData();
        }

        public void FilterByTimeRangeAndGroupByProductNameAndDateTime(DateTime date, TimeSpan startTime, TimeSpan endTime)
        {
            var filteredList = _originalResultTestList
                .Where(x => x.StartTime.HasValue && x.StartTime.Value.Date == date && x.StartTime.Value.TimeOfDay >= startTime && x.StartTime.Value.TimeOfDay <= endTime)
                .ToList();

            ResultTestList = new ObservableCollection<MachineStatus>(filteredList);
            GroupedResultList = GroupByProductName(ResultTestList);
            UpdateGroupedResultListWithTotal();
        }

        private void UpdateGroupedResultListWithTotal()
        {
            if (GroupedResultList == null || !GroupedResultList.Any()) return;

            var totalPassed = GroupedResultList.Sum(x => x.PassedCount);
            var totalFailed = GroupedResultList.Sum(x => x.FailedCount);

            var totalRow = new MachineStatusGrouped
            {
                ProductName = "TOTAL",
                PassedCount = totalPassed,
                FailedCount = totalFailed,
                Operators = string.Empty
            };

            var updatedList = new ObservableCollection<MachineStatusGrouped>(GroupedResultList) { totalRow };
            GroupedResultList = updatedList;
        }

        public void CalculateAndDisplayEfficiency()
        {
            var efficiencyDataList = ResultTestList
                .Where(x => x.StartTime.HasValue)
                .Select(x => (x.StartTime.Value, x.Name == "S7.TestingPassed" ? 1 : 0))
                .ToList();

            int totalUnitsProduced;
            double achievedEfficiency, expectedEfficiency, expectedOutput;

            _efficiencyCalculator.CalculateEfficiency(
                (int)Target,
                efficiencyDataList,
                out totalUnitsProduced,
                out expectedOutput,
                out achievedEfficiency,
                out expectedEfficiency
            );

            TotalUnitsProduced = Math.Round((double)totalUnitsProduced, 0);
            ExpectedOutput = Math.Round((double)expectedOutput, 0);
            CurrentEfficiency = Math.Round((double)achievedEfficiency, 1);
            ExpectedEfficiency = Math.Round((double)expectedEfficiency, 1);
        }

        private void UpdateChartData()
        {
            var now = DateTime.Now.TimeOfDay;
            if (now >= TimeSpan.Parse("05:40") && now < TimeSpan.Parse("13:40"))
            {
                FilterShift1();
            }
            else if (now >= TimeSpan.Parse("13:40") && now < TimeSpan.Parse("21:40"))
            {
                FilterShift2();
            }

            CalculateAndDisplayEfficiency();

            TotalPartsProducedChart.Value = TotalUnitsProduced;
            ExpectedPartsChart.Value = ExpectedOutput;
            TargetPartsChart.Value = Target;
            Needle.Value = CurrentEfficiency;
            SeriesExpectedEfficiency[0].Values = new double[] { ExpectedEfficiency };
        }

        private void AutoSelectCurrentShift()
        {
            var now = DateTime.Now.TimeOfDay;

            if (now >= TimeSpan.Parse("05:40") && now < TimeSpan.Parse("13:40"))
            {
                FilterShift1();
            }
            else if (now >= TimeSpan.Parse("13:40") && now < TimeSpan.Parse("21:40"))
            {
                FilterShift2();
            }
        }

        private static void SetStyle(double sectionsOuter, double sectionsWidth, PieSeries<ObservableValue> series, SKColor color)
        {
            series.OuterRadiusOffset = sectionsOuter;
            series.MaxRadialColumnWidth = sectionsWidth;
            series.CornerRadius = 0;
            series.Fill = new SolidColorPaint(color); // Ustaw kolor dla sekcji
        }
        private void StartHourlyTimer()
        {
            _hourlyTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
            _hourlyTimer.Tick += (sender, e) =>
            {
                // Funkcja, która jest wywoływana co godzinę
                SendDataAsync();
            };
            _hourlyTimer.Start();
        }
        private async Task SendDataAsync()
        {
            _productionEfficiency.Efficiency = CurrentEfficiency;
            _productionEfficiency.EfficiencyRequired = ExpectedEfficiency;
            _productionEfficiency.Target = Target;
            _productionEfficiency.PassedPiecesPerShift = (int)TotalUnitsProduced;

            await _machineStatusService.RaportProdcuctionEfficiency(_productionEfficiency);
        }
    }
}
