using System;
using System.Collections.Generic;
using System.Diagnostics;
using ViSyncMaster.DataModel;

namespace ViSyncMaster.Services
{
    public class Rs232DataProcessor
    {
        private Rs232Data _lastData;
        private List<Rs232Data> _testBuffer = new();
        private DateTime _productionStartTime;
        private DateTime _lastTestEndTime; // Czas zakończenia poprzedniego testu
        private DateTime _lastProducingFalseTime; // Czas ostatniego Producing = "false"
        private int _unitsProduced;
        private int _unitsTest;
        private int _unitsPassed;
        private int _unitsFailed;
        private int _lastRetest;
        private bool _inProduction; // Flaga określająca, czy produkcja jest w toku

        public event EventHandler<Rs232Data> ProducingStarted;
        public event EventHandler<List<Rs232Data>> TestBatchReady;
        public event EventHandler<Rs232Data> ProducingEnded;
        public event EventHandler<ProductionMetrics> ProductionMetricsReady;

        public Rs232DataProcessor()
        {
            _unitsTest = 0;
        }

        public void Process(Rs232Data newData)
        {
            if (newData == null) return;

            // Jeśli Producing jest puste, traktuj jako kontynuację produkcji
            string currentProducing = string.IsNullOrEmpty(newData.Producing) ? "true" : newData.Producing?.Trim().ToLower();
            string operatorName = string.IsNullOrEmpty(newData.Operator) ? "UNKNOWN" : newData.Operator;

            bool wasProducing = string.Equals(_lastData?.Producing, "true", StringComparison.OrdinalIgnoreCase);
            bool isProducingTrue = string.Equals(currentProducing, "true", StringComparison.OrdinalIgnoreCase);
            bool isProducingFalse = string.Equals(currentProducing, "false", StringComparison.OrdinalIgnoreCase);

            // Debugowanie: Stan produkcji i operator
            Debug.WriteLine($"Current Producing: {currentProducing}, Was Producing: {wasProducing}, Operator: {operatorName}");

            // Sprawdzanie rozpoczęcia produkcji
            if (!_inProduction && isProducingTrue)
            {
                _productionStartTime = DateTime.Now;
                _unitsProduced = 0;
                _unitsPassed = 0;
                _unitsFailed = 0;
                _inProduction = true;
                Debug.WriteLine($"Produkcja rozpoczęta: {newData.ProductName}, Operator: {operatorName}");
                ProducingStarted?.Invoke(this, newData);
            }

            // Buforowanie testów w czasie produkcji
            bool isPassed = string.Equals(newData.TestingPassed, "true", StringComparison.OrdinalIgnoreCase);
            bool isFailed = string.Equals(newData.TestingFailed, "true", StringComparison.OrdinalIgnoreCase);

            if (isPassed)
            {
                _testBuffer.Add(newData);
                _unitsPassed++;
                _unitsProduced++;
                Debug.WriteLine($"Test Passed - Produkt: {newData.ProductName}, Operator: {operatorName}");
            }

            if (isFailed)
            {
                _testBuffer.Add(newData);
                _unitsFailed++;
                _unitsProduced++;
                Debug.WriteLine($"Test Failed - Produkt: {newData.ProductName}, Operator: {operatorName}");
            }

            // Sprawdzanie zakończenia produkcji (Producing = "false")
            if (_inProduction && isProducingFalse)
            {
                Debug.WriteLine($"Produkcja zakończona: {newData.ProductName}, Operator: {operatorName}");

                // Przygotowanie metryk produkcji
                var productionMetrics = new ProductionMetrics
                {
                    // Czas produkcji (od Producing = true do Producing = false)
                    ProductionTime = Math.Round((DateTime.Now - _productionStartTime).TotalSeconds, 1),

                    // Czas przygotowania (od Producing = false do Producing = true)
                    PreparationTime = _lastProducingFalseTime != DateTime.MinValue
                        ? Math.Round((DateTime.Now - _lastProducingFalseTime).TotalSeconds, 1)
                        : 0,
                    TestWithRetest = (_unitsTest > 0) && (int.Parse(newData.RGoodAbs) > _lastRetest),
                    UnitsProduced = _unitsProduced,
                    PassedUnits = _unitsPassed,
                    FailedUnits = _unitsFailed,
                    ProductNumber = newData.ProductName,
                    TGoodAbs = newData.TGoodAbs,
                    RGoodAbs = newData.RGoodAbs,
                    OperatorId = newData.Operator
                };

                Debug.WriteLine($"Metryki produkcji - Czas: {productionMetrics.ProductionTime}s, Sztuki: {_unitsProduced}, Passed: {_unitsPassed}, Failed: {_unitsFailed}");
                ProductionMetricsReady?.Invoke(this, productionMetrics);
                TestBatchReady?.Invoke(this, new List<Rs232Data>(_testBuffer));
                _testBuffer.Clear();


                ProducingEnded?.Invoke(this, newData);
                _lastTestEndTime = DateTime.Now; // Ustawienie czasu zakończenia testu
                _lastProducingFalseTime = DateTime.Now; // Ustawienie czasu zakończenia Producing = "false"
                _lastRetest = int.Parse(newData.RGoodAbs);
                _inProduction = false;
                _unitsTest++;
            }
            _lastData = newData; // Zapisz ostatnie dane
        }
    }
}
