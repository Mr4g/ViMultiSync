using System;
using System.Collections.Generic;
using ViSyncMaster.DataModel;

public class Rs232DataProcessor
{
    private Rs232Data _lastData;
    private List<Rs232Data> _bufferedTests = new();

    public event EventHandler<Rs232Data> OnProducingStarted;
    public event EventHandler<Rs232Data> OnProducingStopped;
    public event EventHandler<Rs232Data> OnTestReported;

    public void Process(Rs232Data newData)
    {
        if (newData == null) return;

        bool isProducing = string.Equals(newData.Producing, "true", StringComparison.OrdinalIgnoreCase);
        bool wasProducing = string.Equals(_lastData?.Producing, "true", StringComparison.OrdinalIgnoreCase);
        bool isPassed = string.Equals(newData.TestingPassed, "true", StringComparison.OrdinalIgnoreCase);
        bool isFailed = string.Equals(newData.TestingFailed, "true", StringComparison.OrdinalIgnoreCase);

        // Jeśli Producing się zaczęło – wysyłamy natychmiast
        if (!wasProducing && isProducing)
        {
            OnProducingStarted?.Invoke(this, new Rs232Data
            {
                Status = "Producing",
                ProductName = newData.ProductName,
                OperatorId = newData.OperatorId,
                Timestamp = DateTime.Now
            });
        }

        // Jeśli przychodzi Passed/Failed – buforujemy
        if (isPassed || isFailed)
        {
            _bufferedTests.Add(new Rs232Data
            {
                Status = isPassed ? "Passed" : "Failed",
                ProductName = newData.ProductName,
                OperatorId = newData.OperatorId,
                Timestamp = DateTime.Now
            });
        }

        // Jeśli Producing się zakończyło – wysyłamy zbuforowane sztuki
        if (wasProducing && !isProducing && _bufferedTests.Count > 0)
        {
            foreach (var test in _bufferedTests)
            {
                OnTestReported?.Invoke(this, test);
            }

            _bufferedTests.Clear();

            // Opcjonalnie informujemy o zakończeniu produkcji
            OnProducingStopped?.Invoke(this, new Rs232Data
            {
                Status = "Waiting",
                ProductName = newData.ProductName,
                OperatorId = newData.OperatorId,
                Timestamp = DateTime.Now
            });
        }

        _lastData = newData;
    }
}
