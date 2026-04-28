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
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace ViSyncMaster.ViewModels
{
    public partial class ResultTableViewModel : ObservableValidator
    {

        private readonly MachineStatusService _machineStatusService;
        private ObservableCollection<MachineStatus> _originalResultTestList; // Full data list
        private ProductionEfficiencyCalculator _efficiencyCalculator;
        private AppConfigData _appConfig = MainWindowViewModel.appConfig;
        private DispatcherTimer _hourlyTimer;
        private ProductionEfficiency _productionEfficiency;
        private MainWindowViewModel _mainWindowViewModel;
        private bool _isUpdating = false;
        private bool _pendingUpdate = false;
        private MachineStatusGrouped _totalRow;
        private bool _isManualShiftSelection;
        private ShiftSelectionMode _currentSelectionMode = ShiftSelectionMode.SystemCurrent;
        private long _lastSeenProductionMarkerTicks;
        private readonly ProductTaktTimeCsvService _taktCsvService;
        private readonly Dictionary<string, int> _plannedQtyByProduct = new(StringComparer.OrdinalIgnoreCase);

        private enum ShiftSelectionMode
        {
            SystemCurrent,
            Shift1,
            Shift2,
            Shift3,
            Shift3Yesterday,
            Week
        }

        private sealed class ProductionPoint
        {
            public DateTime Time { get; init; }
            public int Passed { get; init; }
            public string ProductName { get; init; } = string.Empty;
            public string ProductNumber { get; init; } = string.Empty;
        }



        // Observable properties for binding
        [ObservableProperty] private int _currentShift;
        [ObservableProperty] private ObservableCollection<ISeries> _seriesExpectedEfficiency = new(); // Pozostaw tylko to z atrybutem ObservableProperty
        [ObservableProperty] private ObservableCollection<ISeries> _pieChartSeriesTotalUnitsPredicted = new();
        [ObservableProperty] private ObservableCollection<ISeries> _pieChartSeriesCurrentEfficiency = new();
        [ObservableProperty] private ObservableCollection<ISeries> _pieChartSeriesExpectedEfficiency = new();
        [ObservableProperty] private ObservableCollection<MachineStatus> _resultTestList;
        [ObservableProperty] private ObservableCollection<MachineStatusGrouped> _groupedResultList = new();
        [ObservableProperty] private ObservableCollection<HourlyPlan> _hourlyPlan = new();
        [ObservableProperty] private ObservableCollection<TimelineSegment> _timelineSegments = new();
        [ObservableProperty] private double _currentTimeMarker;
        [ObservableProperty] private double _timelineWidth = 600;
        [ObservableProperty]
        [RegularExpression(
           @"^(-1|0|[1-9][0-9]{0,3})$",
           ErrorMessage = "Target musi być -1 lub liczbą z zakresu 0–9999."
         )]
        private int _target = -1;
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
        [ObservableProperty] private string _selectedShiftRangeInfo = "Brak wybranego zakresu";
        [ObservableProperty] private MachineStatusGrouped _selectedResultRow;
        [ObservableProperty] private string _plannedQtyInput = string.Empty;
        [ObservableProperty] private string _manualTaktTimeInput = string.Empty;
        [ObservableProperty] private string _planEditorMessage = string.Empty;
        [ObservableProperty] private string _currentProductRawName = "-";
        [ObservableProperty] private string _currentProductNumber = "-";
        [ObservableProperty] private string _currentProductTaktInfo = "Brak takt time";
        [ObservableProperty] private string _currentProductTargetInfo = "Brak targetu";
        [ObservableProperty] private bool _isCurrentProductTaktMissing = true;
        [ObservableProperty] private bool _isMissingTaktDialogVisible;
        [ObservableProperty] private bool _isMissingTargetDialogVisible;

        private string _dismissedMissingTaktForProduct = string.Empty;
        private string _dismissedMissingTargetForProduct = string.Empty;

        public IEnumerable<ISeries> Series { get; set; }
        public IEnumerable<ISeries> SeriesEfficiency { get; set; }

        public IEnumerable<VisualElement> VisualElements { get; set; }
        public NeedleVisual Needle { get; set; }
        public ObservableValue TotalPartsProducedChart { get; set; }
        public ObservableValue ExpectedPartsChart { get; set; }
        public ObservableValue TargetPartsChart { get; set; }
        public SolidColorPaint LegendTextPaint { get; set; } // Kolor tekstu legendy

        public ResultTableViewModel(
                    MachineStatusService machineStatusService,
                    MainWindowViewModel mainWindowViewModel,
                    ObservableCollection<MachineStatus> resultTests = null)
        {
            // Wczytaj plany zmian
            ShiftPlan.LoadFromJson();

            _machineStatusService = machineStatusService;
            _mainWindowViewModel = mainWindowViewModel;
            _productionEfficiency = new ProductionEfficiency();
            _originalResultTestList = resultTests ?? throw new ArgumentNullException(nameof(resultTests));
            ResultTestList = new ObservableCollection<MachineStatus>(_originalResultTestList);
            _taktCsvService = new ProductTaktTimeCsvService(Path.Combine("C:", "ViSM", "ConfigFiles", "ProductTaktTimes.csv"));
            _lastSeenProductionMarkerTicks = GetLatestProductionMarkerTicks();

            AutoSelectCurrentShiftAsync();
            InicializeChart();

            // Dodaj wiersz TOTAL
            _totalRow = new MachineStatusGrouped { ProductName = "TOTAL" };
            GroupedResultList.Add(_totalRow);
            RefreshGroupedResultList();
            UpdateGroupedResultListWithTotal();

            // Inicjalizacja kalkulatora wydajności
            var plan = ShiftPlan.GetCurrent(GetShiftPlanKey());
            _efficiencyCalculator = new ProductionEfficiencyCalculator(plan, GetDowntimeMinutes);

            _mainWindowViewModel.ResultTableUpdate += async (s, e) => await QueueUpdateAsync();
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
            var plan = ShiftPlan.GetCurrent(GetShiftPlanKey(), DateTime.Now);
            var mode = plan.ShiftNumber switch
            {
                1 => ShiftSelectionMode.Shift1,
                2 => ShiftSelectionMode.Shift2,
                _ => ShiftSelectionMode.Shift3
            };
            await ApplySelectionModeAsync(mode, isManualSelection: false);
        }

        // I zmiana
        [RelayCommand]
        public async Task FilterShift1Async()
        {
            await ApplySelectionModeAsync(ShiftSelectionMode.Shift1, isManualSelection: true);
        }
        // II zmiana
        [RelayCommand]
        public async Task FilterShift2Async()
        {
            await ApplySelectionModeAsync(ShiftSelectionMode.Shift2, isManualSelection: true);
        }
        // III zmiana
        [RelayCommand]
        public async Task FilterShift3Async()
        {
            await ApplySelectionModeAsync(ShiftSelectionMode.Shift3, isManualSelection: true);
        }
        // Zmiana III z wczoraj
        [RelayCommand]
        public async Task FilterYesterdayShift3Async()
        {
            await ApplySelectionModeAsync(ShiftSelectionMode.Shift3Yesterday, isManualSelection: true);
        }

        [RelayCommand]
        public async Task FilterWholeWeekAsync()
        {
            _isManualShiftSelection = true;
            _currentSelectionMode = ShiftSelectionMode.Week;

            var planKey = GetShiftPlanKey();
            var now = DateTime.Now;

            // Jeśli wybrano zmianę, tydzień liczymy dla tej samej zmiany w każdym dniu tygodnia.
            // Dzięki temu nie ma mieszania logiki "dzisiaj" z logiką tygodniową.
            List<(DateTime Start, DateTime End)> ranges;
            if (CurrentShift > 0)
            {
                ranges = ShiftPlan.GetWeeklyShiftRanges(planKey, CurrentShift, DateTime.Today)
                    .Select(r => (Start: r.Start, End: r.End > now ? now : r.End))
                    .Where(r => r.End > r.Start)
                    .ToList();
            }
            else
            {
                int daysFromMonday = ((int)DateTime.Today.DayOfWeek + 6) % 7;
                var weekStart = DateTime.Today.AddDays(-daysFromMonday);
                var weekEnd = weekStart.AddDays(7);
                ranges = new List<(DateTime Start, DateTime End)>
                {
                    (Start: weekStart, End: weekEnd > now ? now : weekEnd)
                };
            }

            var filtered = _originalResultTestList
                .Where(x => x.StartTime.HasValue
                         && ranges.Any(r => x.StartTime.Value >= r.Start && x.StartTime.Value < r.End))
                .ToList();

            // 2) Diff-update ResultTestList
            ResultTestList.SyncWith(filtered, x => x.Id);

            // 3) Na UI-thread odśwież grupowanie i TOTAL “in-place”
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                RefreshGroupedResultList();
                UpdateGroupedResultListWithTotal();
            });

            if (ranges.Any())
            {
                var start = ranges.Min(r => r.Start);
                var end = ranges.Max(r => r.End);
                var prefix = CurrentShift > 0 ? $"Tydzień | {CurrentShift}ZM" : "Tydzień";
                SelectedShiftRangeInfo = $"{prefix} | {start:yyyy-MM-dd HH:mm} – {end:yyyy-MM-dd HH:mm}";
            }
            // 4) (opcjonalnie) wysyłka liczników pass/fail
            //await GroupByPassedFailedAndTotalCounterAsync(ResultTestList);
        }

        private async Task ApplySelectionModeAsync(ShiftSelectionMode mode, bool isManualSelection)
        {
            _currentSelectionMode = mode;
            _isManualShiftSelection = isManualSelection;

            switch (mode)
            {
                case ShiftSelectionMode.Shift1:
                    await FilterShiftByNumberAsync(1, DateTime.Today);
                    break;
                case ShiftSelectionMode.Shift2:
                    await FilterShiftByNumberAsync(2, DateTime.Today);
                    break;
                case ShiftSelectionMode.Shift3:
                    await FilterShiftByNumberAsync(3, DateTime.Today);
                    break;
                case ShiftSelectionMode.Shift3Yesterday:
                    {
                        var planKey = GetShiftPlanKey();
                        CurrentShift = 3;
                        var range = ShiftPlan.GetYesterdayShiftRange(planKey, 3, DateTime.Today, DateTime.Now);
                        await FilterByShiftPlanRangeAsync(range.Start, range.End);
                        SelectedShiftRangeInfo = BuildShiftRangeInfo(3, range.Start, range.End);
                        break;
                    }
                case ShiftSelectionMode.Week:
                    await FilterWholeWeekAsync();
                    break;
                case ShiftSelectionMode.SystemCurrent:
                default:
                    var currentPlan = ShiftPlan.GetCurrent(GetShiftPlanKey(), DateTime.Now);
                    var currentShift = currentPlan.ShiftNumber switch
                    {
                        1 => 1,
                        2 => 2,
                        _ => 3
                    };
                    await FilterShiftByNumberAsync(currentShift, DateTime.Today);
                    break;
            }
        }

        [RelayCommand]
        private void SaveTarget()
        {
            // Save the target and calculate efficiency
            Console.WriteLine($"Saved target: {Target}");
            UpdateChartData();
        }

        [RelayCommand]
        private void SaveSelectedProductPlan()
        {
            if (string.IsNullOrWhiteSpace(CurrentProductNumber) || CurrentProductNumber == "-")
            {
                PlanEditorMessage = "Brak aktywnego produktu do zapisu planu.";
                return;
            }
            if (!int.TryParse(PlannedQtyInput, NumberStyles.Integer, CultureInfo.InvariantCulture, out var planned) || planned <= 0)
            {
                PlanEditorMessage = "Planned Qty musi być liczbą dodatnią.";
                return;
            }

            _plannedQtyByProduct[CurrentProductNumber] = planned;
            PlanEditorMessage = $"Zapisano plan qty={planned} dla produktu {CurrentProductNumber}.";
            CurrentProductTargetInfo = planned.ToString(CultureInfo.InvariantCulture);
            IsMissingTargetDialogVisible = false;
            _dismissedMissingTargetForProduct = string.Empty;
            RefreshGroupedResultList();
            _ = UpdateHourlyPlanDataAsync();
        }

        [RelayCommand]
        private void SaveMissingTaktTime()
        {
            if (string.IsNullOrWhiteSpace(CurrentProductNumber) || CurrentProductNumber == "-")
            {
                PlanEditorMessage = "Brak aktywnego produktu do zapisu takt time.";
                return;
            }
            if (!double.TryParse(ManualTaktTimeInput, NumberStyles.Float, CultureInfo.InvariantCulture, out var takt) || takt <= 0)
            {
                PlanEditorMessage = "Takt Time musi być dodatnią liczbą.";
                return;
            }

            _taktCsvService.UpsertTaktSeconds(CurrentProductNumber, takt);
            PlanEditorMessage = $"Dodano/zaaktualizowano takt time={takt:0.###}s dla produktu {CurrentProductNumber}.";
            _isCurrentProductTaktMissing = false;
            CurrentProductTaktInfo = $"{takt:0.###} s";
            IsMissingTaktDialogVisible = false;
            _dismissedMissingTaktForProduct = string.Empty;
            _ = UpdateHourlyPlanDataAsync();
        }

        [RelayCommand]
        private void CancelMissingTaktDialog()
        {
            IsMissingTaktDialogVisible = false;
            _dismissedMissingTaktForProduct = CurrentProductNumber;
            PlanEditorMessage = $"Brak takt time dla produktu {CurrentProductNumber}.";
        }

        [RelayCommand]
        private void CancelMissingTargetDialog()
        {
            IsMissingTargetDialogVisible = false;
            _dismissedMissingTargetForProduct = CurrentProductNumber;
            CurrentProductTargetInfo = "Brak targetu";
            PlanEditorMessage = $"Brak targetu dla produktu {CurrentProductNumber}.";
        }

        partial void OnTargetChanged(int value)
        {
            UpdateChartData();
        }
        partial void OnCurrentShiftChanged(int value)
        {
            // Pobierz aktualny plan dla działu
            var plan = ShiftPlan.GetCurrent(GetShiftPlanKey());
            _efficiencyCalculator = new ProductionEfficiencyCalculator(plan, GetDowntimeMinutes);
        }

        private async Task FilterShiftByNumberAsync(int shiftNumber, DateTime referenceDate, bool forcePreviousDay = false)
        {
            var planKey = GetShiftPlanKey();
            CurrentShift = shiftNumber;
            var range = ShiftPlan.GetShiftTimeRange(planKey, shiftNumber, referenceDate, DateTime.Now, forcePreviousDay);
            await FilterByShiftPlanRangeAsync(range.Start, range.End);
            SelectedShiftRangeInfo = BuildShiftRangeInfo(shiftNumber, range.Start, range.End);
        }

        private async Task FilterByShiftPlanRangeAsync(DateTime start, DateTime end)
        {
            // Filtr po pełnym DateTime gwarantuje poprawność dla zmian nocnych (przez północ).
            var now = DateTime.Now;
            var effectiveEnd = end > now ? now : end;
            if (effectiveEnd < start)
                effectiveEnd = start;

            var filtered = _originalResultTestList
                .Where(x => x.StartTime.HasValue
                         && x.StartTime.Value >= start
                         && x.StartTime.Value < effectiveEnd)
                .ToList();

            ResultTestList.SyncWith(filtered, x => x.Id);
            await Dispatcher.UIThread.InvokeAsync(RefreshGroupedResultList);
            await GroupByPassedFailedAndTotalCounterAsync(ResultTestList);
        }

        private string GetShiftPlanKey()
        {
            return !string.IsNullOrWhiteSpace(_appConfig.ShiftPlanName)
                ? _appConfig.ShiftPlanName
                : _appConfig.Line;
        }

        private static string BuildShiftRangeInfo(int shiftNumber, DateTime start, DateTime end)
        {
            string dayLabel = start.Date == end.Date
                ? start.ToString("yyyy-MM-dd")
                : $"{start:yyyy-MM-dd} / {end:yyyy-MM-dd}";
            return $"{shiftNumber}ZM | {dayLabel} | {start:HH:mm}–{end:HH:mm}";
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
            await RefreshSelectionOnUpdateAsync();
            await CalculateAndDisplayEfficiencyAsync();

            TotalPartsProducedChart.Value = TotalUnitsProduced;
            ExpectedPartsChart.Value = ExpectedOutput;
            TargetPartsChart.Value = Target;
            //Needle.Value = Math.Clamp(HumanEfficiency, 0, 200);
            SeriesExpectedEfficiency[0].Values = new double[] { ExpectedEfficiency };
            await UpdateHourlyPlanDataAsync();
        }

        private async Task QueueUpdateAsync()
        {
            if (_isUpdating)
            {
                _pendingUpdate = true;
                return;
            }

            _isUpdating = true;
            try
            {
                do
                {
                    _pendingUpdate = false;
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        await RefreshSelectionOnUpdateAsync();
                        RefreshGroupedResultList();
                        UpdateGroupedResultListWithTotal();
                        await CalculateAndDisplayEfficiencyAsync();
                        TotalPartsProducedChart.Value = TotalUnitsProduced;
                        ExpectedPartsChart.Value = ExpectedOutput;
                        TargetPartsChart.Value = Target;
                        SeriesExpectedEfficiency[0].Values = new double[] { ExpectedEfficiency };
                        await UpdateHourlyPlanDataAsync();
                    });
                }
                while (_pendingUpdate);
            }
            finally
            {
                _isUpdating = false;
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
            _hourlyTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(2) };
            _hourlyTimer.Tick += async (sender, e) =>
            {
                var plan = ShiftPlan.GetCurrent(GetShiftPlanKey());
                var now = DateTime.Now;
                bool cross = plan.ShiftEnd < plan.ShiftStart;
                var startDate = now.Date;
                if (cross && now.TimeOfDay < plan.ShiftStart) startDate = startDate.AddDays(-1);
                var shutDown = startDate.Add(plan.ShutDown);
                if (cross && plan.ShutDown < plan.ShiftStart) shutDown = shutDown.AddDays(1);
                if (now >= shutDown) { _hourlyTimer.Stop(); return; }
                await UpdateChartData();
                await SendDataAsync();
            };
            _hourlyTimer.Start();
        }
        private async Task SendDataAsync()
        {
            var currentIntervalEfficiency = Math.Clamp(Needle.Value, 0, 200);
            _productionEfficiency.Efficiency = currentIntervalEfficiency;
            _productionEfficiency.EfficiencyRequired = ExpectedEfficiency;
            _productionEfficiency.Target = Target;
            _productionEfficiency.Plan = (int)Math.Round(ExpectedOutput);
            _productionEfficiency.PassedPiecesPerShift = (int)TotalUnitsProduced;

            await _machineStatusService.RaportProdcuctionEfficiency(_productionEfficiency);
        }
        private double GetDowntimeMinutes(DateTime from, DateTime to)
        {
            try
            {
                return _machineStatusService.GetDowntimeMinutes(from, to);
            }
            catch
            {
                return 0;
            }
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

        private async Task RefreshSelectionOnUpdateAsync()
        {
            // Timer/refresh nie może resetować ręcznego wyboru.
            // Powrót do bieżącej zmiany tylko gdy wykryto nową aktywność produkcyjną.
            var hasNewActivity = TryDetectNewProductionActivity();
            if (_isManualShiftSelection && !hasNewActivity)
            {
                await ApplySelectionModeAsync(_currentSelectionMode, isManualSelection: true);
                return;
            }

            if (_isManualShiftSelection && hasNewActivity)
                _isManualShiftSelection = false;

            await AutoSelectCurrentShiftAsync();
        }

        private bool TryDetectNewProductionActivity()
        {
            var currentMarker = GetLatestProductionMarkerTicks();
            if (currentMarker <= _lastSeenProductionMarkerTicks)
                return false;

            _lastSeenProductionMarkerTicks = currentMarker;
            return true;
        }

        private long GetLatestProductionMarkerTicks()
        {
            return _originalResultTestList
                .Where(x => x.StartTime.HasValue
                         && (x.Name == "S7.TestingPassed" || x.Name == "S7.TestingFailed"))
                .Select(x => x.StartTime!.Value.Ticks)
                .DefaultIfEmpty(0)
                .Max();
        }

        private async Task SendShiftCounterMqtt(MachineCounters machineCounters)
        {
            await _machineStatusService.SendShiftCounterMqtt(machineCounters);
        }
        private void RefreshGroupedResultList()
        {
            TryGetCurrentShiftRange(out var currentShiftStart, out var currentShiftEnd);

            // 1) Pobierz „nowe” dane z ResultTestList
            var newData = ResultTestList
                .GroupBy(x => x.ProductName)
                .Select(g => new
                {
                    Name = g.Key,
                    Pass = g.Count(x => x.Name == "S7.TestingPassed" && x.Value == "true"),
                    Fail = g.Count(x => x.Name == "S7.TestingFailed" && x.Value == "true"),
                    Operators = string.Join(", ", g.Select(x => x.OperatorId).Distinct()),
                    StartTime = g.Where(x => x.StartTime.HasValue).Select(x => x.StartTime).Min()
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
                    FillDynamicPlanFields(exist, n.StartTime, currentShiftEnd);
                }
                else
                {
                    var row = new MachineStatusGrouped
                    {
                        ProductName = n.Name,
                        ShiftCounterPass = n.Pass,
                        ShiftCounterFail = n.Fail,
                        Operators = n.Operators
                    };
                    FillDynamicPlanFields(row, n.StartTime, currentShiftEnd);
                    GroupedResultList.Add(row);
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

        private void FillDynamicPlanFields(MachineStatusGrouped row, DateTime? productStartTime, DateTime? currentShiftEnd)
        {
            var productNumber = NormalizeProductNumber(row.ProductName);
            var actualQty = row.ShiftCounterPass + row.ShiftCounterFail;
            if (!string.IsNullOrWhiteSpace(productNumber) && !_plannedQtyByProduct.ContainsKey(productNumber) && Target > 0)
                _plannedQtyByProduct[productNumber] = Target; // fallback na istniejący target

            var hasProductNumber = !string.IsNullOrWhiteSpace(productNumber);
            double taktSeconds = 0;
            var hasTakt = hasProductNumber && _taktCsvService.TryGetTaktSeconds(productNumber, out taktSeconds);
            var plannedQty = hasProductNumber && _plannedQtyByProduct.TryGetValue(productNumber, out var pq) ? pq : 0;

            row.TaktTimeSeconds = hasTakt ? taktSeconds : 0;
            row.PlannedQty = plannedQty;
            row.ActualQty = actualQty;
            row.StartTime = productStartTime;

            if (productStartTime.HasValue && hasTakt && taktSeconds > 0)
            {
                var elapsedSeconds = Math.Max(0, (DateTime.Now - productStartTime.Value).TotalSeconds);
                var expected = (int)Math.Floor(elapsedSeconds / taktSeconds);
                row.ExpectedQty = plannedQty > 0 ? Math.Min(expected, plannedQty) : expected;
            }
            else
            {
                row.ExpectedQty = 0;
            }

            row.PlannedEndTime = (productStartTime.HasValue && plannedQty > 0 && hasTakt && taktSeconds > 0)
                ? productStartTime.Value.AddSeconds(plannedQty * taktSeconds)
                : null;
            row.Difference = row.ActualQty - row.ExpectedQty;
            row.IsPlanBeyondShift = row.PlannedEndTime.HasValue && currentShiftEnd.HasValue && row.PlannedEndTime.Value > currentShiftEnd.Value;

            if (!hasProductNumber)
                row.Status = "Brak numeru produktu (7 cyfr)";
            else if (!hasTakt || taktSeconds <= 0)
                row.Status = "Brak takt time (uzupełnij)";
            else if (plannedQty <= 0)
                row.Status = "Wpisz Planned Qty";
            else if (row.Difference > 1)
                row.Status = "Ahead";
            else if (row.Difference < -1)
                row.Status = "Behind";
            else
                row.Status = "On track";
        }

        private bool TryGetCurrentShiftRange(out DateTime? start, out DateTime? end)
        {
            start = null;
            end = null;
            var planKey = GetShiftPlanKey();

            if (_currentSelectionMode == ShiftSelectionMode.Week)
                return false;

            int shiftNumber = CurrentShift > 0 ? CurrentShift : 0;
            if (shiftNumber == 0)
            {
                var current = ShiftPlan.GetCurrent(planKey, DateTime.Now);
                shiftNumber = current.ShiftNumber;
            }

            var range = _currentSelectionMode == ShiftSelectionMode.Shift3Yesterday
                ? ShiftPlan.GetYesterdayShiftRange(planKey, 3, DateTime.Today, DateTime.Now)
                : ShiftPlan.GetShiftTimeRange(planKey, shiftNumber, DateTime.Today, DateTime.Now, forcePreviousDay: false);

            start = range.Start;
            end = range.End;
            return true;
        }
        private async Task UpdateHourlyPlanDataAsync()
        {
            HourlyPlan.Clear();
            var planMessages = new List<HourlyPlanMessage>();

            var data = ResultTestList
                .Where(x => x.StartTime.HasValue)
                .Select(x => new ProductionPoint
                {
                    Time = x.StartTime!.Value,
                    Passed = x.Name == "S7.TestingPassed" ? 1 : 0,
                    ProductName = x.ProductName,
                    ProductNumber = NormalizeProductNumber(x.ProductName)
                })
                .OrderBy(x => x.Time)
                .ToList();

            var (rangeStart, rangeEnd, plan) = ResolveSelectedRangeAndPlan();
            if (rangeEnd <= rangeStart)
                return;

            var currentProduct = data
                .Where(d => d.Time >= rangeStart && d.Time <= DateTime.Now)
                .OrderByDescending(d => d.Time)
                .Select(d => d.ProductName)
                .FirstOrDefault();

            CurrentProductRawName = string.IsNullOrWhiteSpace(currentProduct) ? "-" : currentProduct;
            var normalizedProductNumber = NormalizeProductNumber(currentProduct);
            CurrentProductNumber = string.IsNullOrWhiteSpace(normalizedProductNumber) ? "-" : normalizedProductNumber;
            double taktSeconds = 0;
            var hasTakt = !string.IsNullOrWhiteSpace(normalizedProductNumber) &&
                          _taktCsvService.TryGetTaktSeconds(normalizedProductNumber, out taktSeconds) &&
                          taktSeconds > 0;

            if (string.IsNullOrWhiteSpace(currentProduct))
            {
                IsCurrentProductTaktMissing = false;
                CurrentProductTaktInfo = "Brak danych produktu";
                IsMissingTaktDialogVisible = false;
                CurrentProductTargetInfo = "Brak targetu";
                IsMissingTargetDialogVisible = false;
            }
            else if (string.IsNullOrWhiteSpace(normalizedProductNumber))
            {
                IsCurrentProductTaktMissing = false;
                CurrentProductTaktInfo = "Nie znaleziono 7-cyfrowego numeru produktu";
                IsMissingTaktDialogVisible = false;
                CurrentProductTargetInfo = "Brak targetu";
                IsMissingTargetDialogVisible = false;
            }
            else if (hasTakt)
            {
                IsCurrentProductTaktMissing = false;
                CurrentProductTaktInfo = $"{taktSeconds:0.###} s";
                IsMissingTaktDialogVisible = false;
                _dismissedMissingTaktForProduct = string.Empty;

                var hasTarget = _plannedQtyByProduct.TryGetValue(normalizedProductNumber, out var currentTarget) && currentTarget > 0;
                if (hasTarget)
                {
                    CurrentProductTargetInfo = currentTarget.ToString(CultureInfo.InvariantCulture);
                    IsMissingTargetDialogVisible = false;
                    _dismissedMissingTargetForProduct = string.Empty;
                }
                else
                {
                    CurrentProductTargetInfo = "Brak targetu";
                    IsMissingTargetDialogVisible = false;
                    if (_dismissedMissingTargetForProduct != normalizedProductNumber)
                        IsMissingTargetDialogVisible = true;
                }
            }
            else
            {
                IsCurrentProductTaktMissing = true;
                CurrentProductTaktInfo = "Brak takt time";
                CurrentProductTargetInfo = "Brak targetu";
                IsMissingTargetDialogVisible = false;
                if (_dismissedMissingTaktForProduct != normalizedProductNumber)
                    IsMissingTaktDialogVisible = true;
            }

            var breakRanges = plan.Breaks
                .Select(b => (
                    Start: ToShiftDateTime(b.Start, rangeStart, plan.ShiftStart, plan.ShiftEnd <= plan.ShiftStart),
                    End: ToShiftDateTime(b.End, rangeStart, plan.ShiftStart, plan.ShiftEnd <= plan.ShiftStart)))
                .Where(b => b.End > rangeStart && b.Start < rangeEnd)
                .OrderBy(b => b.Start)
                .ToList();

            var intervals = new List<(DateTime Start, DateTime End, bool IsBreak)>();
            var cursor = rangeStart;
            while (cursor < rangeEnd)
            {
                var nextHour = new DateTime(cursor.Year, cursor.Month, cursor.Day, cursor.Hour, 0, 0).AddHours(1);
                var nextBr = breakRanges.FirstOrDefault(b => b.Start >= cursor);
                var nextBrStart = nextBr != default ? nextBr.Start : DateTime.MaxValue;
                var boundary = new[] { nextHour, nextBrStart, rangeEnd }.Min();

                if (boundary > cursor)
                    intervals.Add((cursor, boundary, false));

                if (nextBr != default && boundary == nextBrStart)
                {
                    var brEnd = nextBr.End < rangeEnd ? nextBr.End : rangeEnd;
                    intervals.Add((nextBr.Start, brEnd, true));
                    cursor = brEnd;
                    breakRanges.Remove(nextBr);
                }
                else
                {
                    cursor = boundary;
                }
            }

            int assignedSum = 0;
            int totalProducedNonBreak = 0;
            var assignedByProduct = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var now = DateTime.Now;
            var isRangeInProgress = now >= rangeStart && now < rangeEnd;

            foreach (var (startInterval, endInterval, isBreak) in intervals)
            {
                var isFutureInterval = isRangeInProgress && startInterval >= now;
                var effectiveIntervalEnd = endInterval;
                if (isRangeInProgress && startInterval < now && endInterval > now)
                    effectiveIntervalEnd = now;
                if (isFutureInterval)
                    effectiveIntervalEnd = startInterval;

                int produced = isFutureInterval
                    ? 0
                    : data.Where(d => d.Time >= startInterval && d.Time < effectiveIntervalEnd).Sum(d => d.Passed);

                int downtimeMinutes = 0;
                int lostUnits = 0;
                int intervalPlan = 0;
                string intervalProductNumber = string.Empty;

                if (!isBreak)
                {
                    var durationSeconds = Math.Max(0, (effectiveIntervalEnd - startInterval).TotalSeconds);
                    if (!isFutureInterval && durationSeconds > 0)
                        downtimeMinutes = (int)Math.Round(await _machineStatusService.GetDowntimeMinutesAsync(startInterval, effectiveIntervalEnd));
                    var downtimeSeconds = Math.Max(0, downtimeMinutes * 60);
                    var productiveSeconds = Math.Max(0, durationSeconds - downtimeSeconds);

                    intervalProductNumber = ResolveIntervalProductNumber(data, startInterval, effectiveIntervalEnd);
                    double intervalTaktSeconds = 0;
                    var hasIntervalTakt = !string.IsNullOrWhiteSpace(intervalProductNumber) &&
                                          _taktCsvService.TryGetTaktSeconds(intervalProductNumber, out intervalTaktSeconds) &&
                                          intervalTaktSeconds > 0;

                    if (hasIntervalTakt)
                    {
                        var rawIntervalPlan = (int)Math.Floor(productiveSeconds / intervalTaktSeconds);
                        lostUnits = (int)Math.Floor(downtimeSeconds / intervalTaktSeconds);

                        if (_plannedQtyByProduct.TryGetValue(intervalProductNumber, out var targetQty) && targetQty > 0)
                        {
                            var alreadyAssigned = assignedByProduct.TryGetValue(intervalProductNumber, out var assignedForProduct)
                                ? assignedForProduct
                                : 0;
                            var remaining = Math.Max(0, targetQty - alreadyAssigned);
                            intervalPlan = Math.Min(rawIntervalPlan, remaining);
                            assignedByProduct[intervalProductNumber] = alreadyAssigned + intervalPlan;
                        }
                        else
                        {
                            intervalPlan = 0;
                        }
                    }

                    assignedSum += intervalPlan;
                    totalProducedNonBreak += produced;
                }

                var efficiency = intervalPlan > 0
                    ? (double)produced / intervalPlan * 100
                    : 0;

                var hp = new HourlyPlan
                {
                    Period = $"{startInterval:HH:mm}-{endInterval:HH:mm}",
                    ExpectedUnits = intervalPlan,
                    ProducedUnits = produced,
                    DowntimeMinutes = downtimeMinutes,
                    LostUnitsDueToDowntime = lostUnits,
                    IsBreak = isBreak,
                    IsBreakActive = isBreak && DateTime.Now >= startInterval && DateTime.Now < endInterval,
                    Efficiency = efficiency
                };
                HourlyPlan.Add(hp);

                planMessages.Add(new HourlyPlanMessage
                {
                    Period = hp.Period,
                    ExpectedUnits = hp.ExpectedUnits,
                    Total = assignedSum,
                    ProducedUnits = hp.ProducedUnits,
                    DowntimeMinutes = hp.DowntimeMinutes,
                    LostUnitsDueToDowntime = hp.LostUnitsDueToDowntime,
                    IsBreak = hp.IsBreak,
                    IsBreakActive = hp.IsBreakActive,
                    Efficiency = hp.Efficiency
                });
            }

            HourlyPlan.Add(new HourlyPlan
            {
                Period = "TOTAL",
                ExpectedUnits = assignedSum,
                ProducedUnits = totalProducedNonBreak,
                DowntimeMinutes = HourlyPlan.Sum(p => p.DowntimeMinutes),
                LostUnitsDueToDowntime = HourlyPlan.Sum(p => p.LostUnitsDueToDowntime),
                IsBreak = false,
                IsBreakActive = false,
                Efficiency = assignedSum > 0
                    ? (double)totalProducedNonBreak / assignedSum * 100
                    : 0
            });

            var relevant = intervals
                .Zip(HourlyPlan.Take(intervals.Count), (iv, hp) => new { iv, hp })
                .Where(x => !x.iv.IsBreak && x.iv.Start <= now)
                .Select(x => x.hp);

            var sumExp = relevant.Sum(hp => hp.ExpectedUnits);
            var sumProd = relevant.Sum(hp => hp.ProducedUnits);
            var currEff = sumExp > 0
                ? Math.Round(sumProd / (double)sumExp * 100, 1)
                : 0;
            Needle.Value = Math.Clamp(currEff, 0, 200);

            var currentMessage = planMessages.FirstOrDefault(pm =>
            {
                var parts = pm.Period.Split('-');
                var start = TimeSpan.Parse(parts[0]);
                var end = TimeSpan.Parse(parts[1]);
                if (end <= start) end = end.Add(TimeSpan.FromDays(1));
                var nowTs = now.TimeOfDay;
                return nowTs >= start && nowTs < end;
            });

            if (currentMessage != null)
                await _machineStatusService.ReportHourlyPlanAsync(
                        new List<HourlyPlanMessage> { currentMessage }
                 );
        }

        private static string NormalizeProductNumber(string rawProductName)
        {
            if (string.IsNullOrWhiteSpace(rawProductName))
                return string.Empty;

            var matches = Regex.Matches(rawProductName, @"(?<!\d)\d{7}(?!\d)");
            if (matches.Count == 0)
                return string.Empty;

            // Dla nazw zawierających kilka numerów preferujemy ostatni 7-cyfrowy numer.
            return matches[^1].Value;
        }

        private static string ResolveIntervalProductNumber(IEnumerable<ProductionPoint> data, DateTime intervalStart, DateTime intervalEnd)
        {
            var intervalProduct = data
                .Where(d => d.Time >= intervalStart && d.Time < intervalEnd && !string.IsNullOrWhiteSpace(d.ProductNumber))
                .GroupBy(d => d.ProductNumber)
                .OrderByDescending(g => g.Count())
                .ThenByDescending(g => g.Max(x => x.Time))
                .Select(g => g.Key)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(intervalProduct))
                return intervalProduct;

            return data
                .Where(d => d.Time < intervalEnd && !string.IsNullOrWhiteSpace(d.ProductNumber))
                .OrderByDescending(d => d.Time)
                .Select(d => d.ProductNumber)
                .FirstOrDefault() ?? string.Empty;
        }

        private (DateTime Start, DateTime End, ShiftPlan Plan) ResolveSelectedRangeAndPlan()
        {
            var planKey = GetShiftPlanKey();
            var now = DateTime.Now;
            int shiftNumber = CurrentShift > 0 ? CurrentShift : ShiftPlan.GetCurrent(planKey, now).ShiftNumber;

            DateTime start;
            DateTime end;

            if (_currentSelectionMode == ShiftSelectionMode.Shift3Yesterday)
            {
                var yesterdayRange = ShiftPlan.GetYesterdayShiftRange(planKey, 3, DateTime.Today, now);
                start = yesterdayRange.Start;
                end = yesterdayRange.End;
                shiftNumber = 3;
            }
            else
            {
                var range = ShiftPlan.GetShiftTimeRange(planKey, shiftNumber, DateTime.Today, now, false);
                start = range.Start;
                end = range.End;
            }

            if (_currentSelectionMode == ShiftSelectionMode.Week && ResultTestList.Any(x => x.StartTime.HasValue))
            {
                start = ResultTestList.Where(x => x.StartTime.HasValue).Min(x => x.StartTime!.Value);
                end = ResultTestList.Where(x => x.StartTime.HasValue).Max(x => x.StartTime!.Value).AddMinutes(1);
            }

            return (start, end, ShiftPlan.GetByNumber(planKey, shiftNumber));
        }

        private static DateTime ToShiftDateTime(TimeSpan time, DateTime shiftStartDateTime, TimeSpan shiftStart, bool crossMidnight)
        {
            var shiftBaseDate = shiftStartDateTime.Date;
            if (crossMidnight && shiftStartDateTime.TimeOfDay < shiftStart)
                shiftBaseDate = shiftBaseDate.AddDays(-1);

            var dt = shiftBaseDate.Add(time);
            if (crossMidnight && time < shiftStart)
                dt = dt.AddDays(1);
            return dt;
        }
    }
}
