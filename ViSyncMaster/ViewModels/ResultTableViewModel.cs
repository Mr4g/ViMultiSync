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
using ViSyncMaster.ViewModels;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ViSyncMaster.ViewModels
{
    public partial class ResultTableViewModel : ObservableObject
    {

        private readonly MachineStatusService _machineStatusService;
        private ObservableCollection<MachineStatus> _originalResultTestList; // Full data list
        private ProductionEfficiencyCalculator _efficiencyCalculator;

        private DispatcherTimer _hourlyTimer;
        private ProductionEfficiency _productionEfficiency;
        private MainWindowViewModel _mainWindowViewModel;
        private bool _isUpdating = false;
        private MachineStatusGrouped _totalRow;



        // Observable properties for binding
        [ObservableProperty] private int _currentShift;
        [ObservableProperty] private ObservableCollection<ISeries> _seriesExpectedEfficiency = new(); // Pozostaw tylko to z atrybutem ObservableProperty
        [ObservableProperty] private ObservableCollection<ISeries> _pieChartSeriesTotalUnitsPredicted = new();
        [ObservableProperty] private ObservableCollection<ISeries> _pieChartSeriesCurrentEfficiency = new();
        [ObservableProperty] private ObservableCollection<ISeries> _pieChartSeriesExpectedEfficiency = new();
        [ObservableProperty] private ObservableCollection<MachineStatus> _resultTestList;
        [ObservableProperty] private ObservableCollection<MachineStatusGrouped> _groupedResultList = new();
        [ObservableProperty] private ObservableCollection<HourlyPlan> _hourlyPlan = new();
        [ObservableProperty] private int _target = -1;
        [ObservableProperty] private int _totalUnitsProduced;
        [ObservableProperty] private double _expectedEfficiency;
        [ObservableProperty] private double _machineEfficiency;
        [ObservableProperty] private double _humanEfficiency;
        [ObservableProperty] private double _machineEfficiencyTotal;
        [ObservableProperty] private double _humanEfficiencyTotal;
        [ObservableProperty] private double _expectedOutput;

        [ObservableProperty] private int _totalCount;
        [ObservableProperty] private int _passedCount;
        [ObservableProperty] private int _failedCount;
        [ObservableProperty] private int _uniqueOperatorCount;
        [ObservableProperty] private int _uniqueProductCount;

        public IEnumerable<ISeries> Series { get; set; }
        public IEnumerable<ISeries> SeriesEfficiency { get; set; }

        public IEnumerable<VisualElement> VisualElements { get; set; }
        public NeedleVisual Needle { get; set; }
        public ObservableValue TotalPartsProducedChart { get; set; }
        public ObservableValue ExpectedPartsChart { get; set; }
        public ObservableValue TargetPartsChart { get; set; }
        public SolidColorPaint LegendTextPaint { get; set; } // Kolor tekstu legendy

        public ResultTableViewModel(MachineStatusService machineStatusService, MainWindowViewModel mainWindowViewModel, ObservableCollection<MachineStatus> resultTests = null)
        {
            _machineStatusService = machineStatusService;
            _mainWindowViewModel = mainWindowViewModel;
            _productionEfficiency = new ProductionEfficiency();
            _originalResultTestList = resultTests ?? throw new ArgumentNullException(nameof(resultTests));
            ResultTestList = new ObservableCollection<MachineStatus>(_originalResultTestList);
            AutoSelectCurrentShiftAsync();
            InicializeChart();
            // Utwórz i dodaj wiersz TOTAL
            _totalRow = new MachineStatusGrouped
            {
                ProductName = "TOTAL",
                ShiftCounterPass = 0,
                ShiftCounterFail = 0,
                Operators = string.Empty
            };
            GroupedResultList.Add(_totalRow);
            RefreshGroupedResultList();
            UpdateGroupedResultListWithTotal();
            // Initializing efficiency calculator
            _currentShift = 1;
            _efficiencyCalculator = new ProductionEfficiencyCalculator(ShiftPlan.CreateDefaultShift1());
            _mainWindowViewModel.ResultTableUpdate += async (s, e) =>
            {
                if (_isUpdating) return;
                _isUpdating = true;
                try
                {
                    // All updates in single UI-thread batch
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        // 1) Apply shift filter
                        await AutoSelectCurrentShiftAsync();
                        // 2) Rebuild grouped list (inside Filter methods)
                        RefreshGroupedResultList();
                        // 3) Update total row
                        UpdateGroupedResultListWithTotal();
                        // 4) Calculate efficiency
                        await CalculateAndDisplayEfficiencyAsync();
                        // 5) Update charts
                        TotalPartsProducedChart.Value = TotalUnitsProduced;
                        ExpectedPartsChart.Value = ExpectedOutput;
                        TargetPartsChart.Value = Target;
                        Needle.Value = Math.Clamp(HumanEfficiency, 0, 200);
                        SeriesExpectedEfficiency[0].Values = new double[] { ExpectedEfficiency };
                    });
                }
                finally
                {
                    _isUpdating = false;
                }
            };
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
                    Name = "Target do zrealizowania",
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
        private async Task AutoSelectCurrentShiftAsync()
        {
            var now = DateTime.Now.TimeOfDay;
            if (now >= TimeSpan.Parse("05:40") && now < TimeSpan.Parse("13:40"))
                await FilterShift1Async();
            else if (now >= TimeSpan.Parse("13:40") && now < TimeSpan.Parse("21:40"))
                await FilterShift2Async();
            else if (now >= TimeSpan.Parse("21:40") || now < TimeSpan.Parse("05:40"))
                await FilterShift3Async();
        }

        // I zmiana
        [RelayCommand]
        public async Task FilterShift1Async()
        {
            CurrentShift = 1;
            await FilterByTimeRangeAsync(DateTime.Today, TimeSpan.Parse("05:40"), TimeSpan.Parse("13:40"));
        }
        // II zmiana
        [RelayCommand]
        public async Task FilterShift2Async()
        {
            CurrentShift = 2;
            await FilterByTimeRangeAsync(DateTime.Today, TimeSpan.Parse("13:40"), TimeSpan.Parse("21:40"));
        }
        // III zmiana
        [RelayCommand]
        public async Task FilterShift3Async()
        {
            CurrentShift = 3;
            await FilterByTimeRangeAsync(DateTime.Today, TimeSpan.Parse("21:40"), TimeSpan.Parse("05:40"));
        }
        // Zmiana III z wczoraj
        [RelayCommand]
        public async Task FilterYesterdayShift3Async()
        {
            CurrentShift = 3;
            await FilterByTimeRangeAsync(DateTime.Today.AddDays(-1), TimeSpan.Parse("21:40"), TimeSpan.Parse("05:40"));
        }

        [RelayCommand]
        public async Task FilterWholeWeekAsync()
        {
            var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1); // Monday
            var endOfWeek = startOfWeek.AddDays(6);                                  // Sunday

            // 1) Filtruj oryginalne dane
            var filtered = _originalResultTestList
                .Where(x => x.StartTime.HasValue
                         && x.StartTime.Value.Date >= startOfWeek
                         && x.StartTime.Value.Date <= endOfWeek)
                .ToList();

            // 2) Diff-update ResultTestList
            ResultTestList.SyncWith(filtered, x => x.Id);

            // 3) Na UI-thread odśwież grupowanie i TOTAL “in-place”
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                RefreshGroupedResultList();
                UpdateGroupedResultListWithTotal();
            });
            // 4) (opcjonalnie) wysyłka liczników pass/fail
            //await GroupByPassedFailedAndTotalCounterAsync(ResultTestList);
        }

        [RelayCommand]
        private void SaveTarget()
        {
            // Save the target and calculate efficiency
            Console.WriteLine($"Saved target: {Target}");
            UpdateChartData();
        }

        partial void OnTargetChanged(int value)
        {
            UpdateChartData();
        }
        partial void OnCurrentShiftChanged(int value)
        {
            var plan = value switch
            {
                1 => ShiftPlan.CreateDefaultShift1(),
                2 => ShiftPlan.CreateDefaultShift2(),
                3 => ShiftPlan.CreateDefaultShift3(),
                _ => ShiftPlan.CreateDefaultShift1()
            };
            _efficiencyCalculator = new ProductionEfficiencyCalculator(plan);
        }

        private bool IsInShift(TimeSpan time, TimeSpan start, TimeSpan end)
        {
            return start <= end
                ? time >= start && time < end
                : time >= start || time < end;
        }

        private async Task FilterByTimeRangeAsync(DateTime date, TimeSpan start, TimeSpan end)
        {
            var filtered = _originalResultTestList
                .Where(x => x.StartTime.HasValue
                         && x.StartTime.Value.Date == date
                         && IsInShift(x.StartTime.Value.TimeOfDay, start, end))
                .ToList();

            ResultTestList.SyncWith(filtered, x => x.Id);
            await Dispatcher.UIThread.InvokeAsync(RefreshGroupedResultList);
            await GroupByPassedFailedAndTotalCounterAsync(ResultTestList);
        }

        private void UpdateGroupedResultListWithTotal()
        {
            if (GroupedResultList == null) return;

            // 1) Policz sumy z obecnych wierszy produktów (pomijamy TOTAL, jeśli już istnieje)
            var totalPassed = GroupedResultList
                .Where(x => x.ProductName != "TOTAL")
                .Sum(x => x.ShiftCounterPass);
            var totalFailed = GroupedResultList
                .Where(x => x.ProductName != "TOTAL")
                .Sum(x => x.ShiftCounterFail);

            // 2) Znajdź istniejący _totalRow
            if (_totalRow == null)
            {
                // jeśli nie zainicjowano go wcześniej, postaraj się go znaleźć w kolekcji
                _totalRow = GroupedResultList.FirstOrDefault(x => x.ProductName == "TOTAL");
            }

            if (_totalRow != null)
            {
                // 3a) In-place aktualizacja pól TOTAL
                _totalRow.ShiftCounterPass = totalPassed;
                _totalRow.ShiftCounterFail = totalFailed;
                // jeżeli masz dodatkowe właściwości, np. sumaryczny licznik:
                // _totalRow.ShiftCounter = totalPassed + totalFailed;
            }
            else
            {
                // 3b) Jeśli _totalRow nie istnieje w kolekcji, dodaj nowy na koniec
                _totalRow = new MachineStatusGrouped
                {
                    ProductName = "TOTAL",
                    ShiftCounterPass = totalPassed,
                    ShiftCounterFail = totalFailed,
                    // ShiftCounter     = totalPassed + totalFailed,
                    Operators = string.Empty
                };
                GroupedResultList.Add(_totalRow);
            }
        }

        public async Task CalculateAndDisplayEfficiencyAsync()
        {
            var snapshot = ResultTestList.ToList();
            var data = snapshot
                .Where(x => x.StartTime.HasValue)
                .Select(x => (x.StartTime.Value, x.Name == "S7.TestingPassed" ? 1 : 0))
                .ToList();

            _efficiencyCalculator.CalculateEfficiency(
                Target,
                data,
                DateTime.Now,
                out int produced,
                out double expectedOut,
                out double machineEff,
                out double humanEff,
                out double machineEffTotal,
                out double humanEffTotal);

            TotalUnitsProduced = produced;
            ExpectedOutput = Math.Round(expectedOut, 0);
            MachineEfficiency = Math.Round(machineEff, 1);
            HumanEfficiency = Math.Round(humanEff, 1);
            MachineEfficiencyTotal = Math.Round(machineEffTotal, 1);
            HumanEfficiencyTotal = Math.Round(humanEffTotal, 1);
            ExpectedEfficiency = Target > 0 ? Math.Round(expectedOut / Target * 100, 1) : 0;
        }

        private async Task UpdateChartData()
        {
            await AutoSelectCurrentShiftAsync(); 
            await CalculateAndDisplayEfficiencyAsync();

            TotalPartsProducedChart.Value = TotalUnitsProduced;
            ExpectedPartsChart.Value = ExpectedOutput;
            TargetPartsChart.Value = Target;
            Needle.Value = Math.Clamp(HumanEfficiency, 0, 200);
            SeriesExpectedEfficiency[0].Values = new double[] { ExpectedEfficiency };
            UpdateHourlyPlanData();
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
            _hourlyTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
            _hourlyTimer.Tick += async (sender, e) =>
            {
                var plan = _currentShift switch
                {
                    1 => ShiftPlan.CreateDefaultShift1(),
                    2 => ShiftPlan.CreateDefaultShift2(),
                    3 => ShiftPlan.CreateDefaultShift3(),
                    _ => ShiftPlan.CreateDefaultShift1()
                };

                var now = DateTime.Now;
                var crossMidnight = plan.ShiftEnd < plan.ShiftStart;
                var shiftStartDate = now.Date;
                if (crossMidnight && now.TimeOfDay < plan.ShiftStart)
                    shiftStartDate = shiftStartDate.AddDays(-1);

                var shutDown = shiftStartDate.Add(plan.ShutDown);
                if (crossMidnight && plan.ShutDown < plan.ShiftStart)
                    shutDown = shutDown.AddDays(1);

                if (now >= shutDown)
                {
                    _hourlyTimer.Stop();
                    return;
                }

                await UpdateChartData();
                await SendDataAsync();
            };
            _hourlyTimer.Start();
        }
        private async Task SendDataAsync()
        {
            _productionEfficiency.Efficiency = HumanEfficiency;
            _productionEfficiency.EfficiencyRequired = ExpectedEfficiency;
            _productionEfficiency.Target = Target;
            _productionEfficiency.Plan = (int)Math.Round(ExpectedOutput);
            _productionEfficiency.PassedPiecesPerShift = (int)TotalUnitsProduced;

            await _machineStatusService.RaportProdcuctionEfficiency(_productionEfficiency);
        }

        private async Task<ObservableCollection<MachineCounters>> GroupByPassedFailedAndTotalCounterAsync(ObservableCollection<MachineStatus> resultList)
        {
            var shiftPass = resultList.Count(x => x.Name == "S7.TestingPassed" && x.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) == true);
            var totalPass = _originalResultTestList.Count(x => x.Name == "S7.TestingPassed" && x.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) == true);

            var shiftFail = resultList.Count(x => x.Name == "S7.TestingFailed" && x.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) == true);
            var totalFail = _originalResultTestList.Count(x => x.Name == "S7.TestingFailed" && x.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) == true);

            var uniqueOperatorCount = _originalResultTestList.Select(x => x.OperatorId).Distinct().Count();
            var uniqueProductCount = _originalResultTestList.Select(x => x.ProductName).Distinct().Count();

            var grouped = new MachineCounters
            {
                ShiftCounterFail = shiftFail,
                ShiftCounterPass = shiftPass,
                ShiftCounter = shiftPass + shiftFail,
                TotalCounterFail = totalFail,
                TotalCounterPass = totalPass,
                TotalCounter = totalPass + totalFail,
                UniqueOperatorCount = uniqueOperatorCount,
                UniqueProductCount = uniqueProductCount,
                Target = Target,
                Plan = (int)Math.Round(ExpectedOutput)
            };
            Debug.WriteLine($"ShiftCounterPass: {grouped.ShiftCounterPass}, " +
                $"ShiftCounterFail: {grouped.ShiftCounterFail}, " +
                $"TotalCounterPass: {grouped.TotalCounterPass}, " +
                $"TotalCounterFail: {grouped.TotalCounterFail}");
            await SendShiftCounterMqtt(grouped);
            return new ObservableCollection<MachineCounters> { grouped };
        }

        private async Task SendShiftCounterMqtt(MachineCounters machineCounters)
        {
            await _machineStatusService.SendShiftCounterMqtt(machineCounters);
        }
        private void RefreshGroupedResultList()
        {
            // 1) Pobierz „nowe” dane z ResultTestList
            var newData = ResultTestList
                .GroupBy(x => x.ProductName)
                .Select(g => new
                {
                    Name = g.Key,
                    Pass = g.Count(x => x.Name == "S7.TestingPassed" && x.Value == "true"),
                    Fail = g.Count(x => x.Name == "S7.TestingFailed" && x.Value == "true"),
                    Operators = string.Join(", ", g.Select(x => x.OperatorId).Distinct())
                })
                .ToList();

            // 2) Usuń nieistniejące ProductName
            foreach (var exist in GroupedResultList.ToList().Where(x => x != _totalRow))
                if (!newData.Any(n => n.Name == exist.ProductName))
                    GroupedResultList.Remove(exist);

            // 3) Dodaj lub zaktualizuj te, które zostały
            foreach (var n in newData)
            {
                var exist = GroupedResultList.FirstOrDefault(x => x.ProductName == n.Name);
                if (exist != null)
                {
                    exist.ShiftCounterPass = n.Pass;
                    exist.ShiftCounterFail = n.Fail;
                    exist.Operators = n.Operators;
                }
                else
                {
                    GroupedResultList.Add(new MachineStatusGrouped
                    {
                        ProductName = n.Name,
                        ShiftCounterPass = n.Pass,
                        ShiftCounterFail = n.Fail,
                        Operators = n.Operators
                    });
                }
            }
            // 4) In-place aktualizacja totalRow
            var totalPass = GroupedResultList.Where(x => x != _totalRow).Sum(x => x.ShiftCounterPass);
            var totalFail = GroupedResultList.Where(x => x != _totalRow).Sum(x => x.ShiftCounterFail);
            _totalRow.ShiftCounterPass = totalPass;
            _totalRow.ShiftCounterFail = totalFail;
            if (GroupedResultList.Remove(_totalRow))
                GroupedResultList.Add(_totalRow);
            // jeśli masz dodatkowe pola
        }
         private void UpdateHourlyPlanData()
        {
            HourlyPlan.Clear();

            if (Target <= 0) return;

            var data = ResultTestList
                .Where(x => x.StartTime.HasValue)
                .Select(x => (x.StartTime!.Value, x.Name == "S7.TestingPassed" ? 1 : 0))
                .ToList();

            if (data.Count == 0) return;

            var firstPiece = data.Min(x => x.Item1);

            var plan = _currentShift switch
            {
                1 => ShiftPlan.CreateDefaultShift1(),
                2 => ShiftPlan.CreateDefaultShift2(),
                3 => ShiftPlan.CreateDefaultShift3(),
                _ => ShiftPlan.CreateDefaultShift1()
            };

            var shiftStartDate = firstPiece.Date;
            if (plan.ShiftEnd < plan.ShiftStart && firstPiece.TimeOfDay < plan.ShiftStart)
                shiftStartDate = shiftStartDate.AddDays(-1);

            var end = shiftStartDate.Add(plan.ShiftEnd);
            if (plan.ShiftEnd < plan.ShiftStart)
                end = end.AddDays(1);

            var t = firstPiece;

            while (t <= end)
            {
                _efficiencyCalculator.CalculateEfficiency(Target, data, t,
                    out _, out double expected, out _, out _, out _, out _);
                int produced = data.Where(d => d.Item1 <= t).Sum(d => d.Item2);
                HourlyPlan.Add(new HourlyPlan
                {
                    Time = t.ToString("HH:mm"),
                    ExpectedUnits = (int)Math.Round(expected),
                    ProducedUnits = produced
                });
                t = t.AddHours(1);
            }

            if (HourlyPlan.Count == 0 || HourlyPlan.Last().Time != end.ToString("HH:mm"))
            {
                _efficiencyCalculator.CalculateEfficiency(Target, data, end,
                    out _, out double expected, out _, out _, out _, out _);
                int produced = data.Where(d => d.Item1 <= end).Sum(d => d.Item2);
                HourlyPlan.Add(new HourlyPlan
                {
                    Time = end.ToString("HH:mm"),
                    ExpectedUnits = (int)Math.Round(expected),
                    ProducedUnits = produced
                });
            }
        }
    }
}
