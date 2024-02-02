using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using ViMultiSync.Stores;

namespace ViMultiSync.AuxiliaryClasses
{
    public partial class TimerManager : ObservableObject
    {
        private Dictionary<string, Stopwatch> timers;
        private Dictionary<string, DispatcherTimer> timerEvents;

        public TimerManager()
        {
            timers = new Dictionary<string, Stopwatch>();
            timerEvents = new Dictionary<string, DispatcherTimer>();
        }

        public event EventHandler<string> TimerTick;
        // Używamy właściwości z Community Toolkit
        private string _timerForStatus;
        public string TimerForStatus
        {
            get => _timerForStatus;
            private set => SetProperty(ref _timerForStatus, value);
        }

        public void StartTimer(string buttonId)
        {
            if (!timers.ContainsKey(buttonId))
            {
                timers[buttonId] = Stopwatch.StartNew();
                StartTimerEvent(buttonId);
            }
        }

        public void StopTimer(string buttonId)
        {
            if (timers.TryGetValue(buttonId, out var stopwatch))
            {
                stopwatch.Stop();
                StopTimerEvent(buttonId);
            }
        }

        public void ResetTimer(string buttonId)
        {
            if (timers.ContainsKey(buttonId))
            {
                timers[buttonId].Reset();
                timers.Remove(buttonId);
                StopTimerEvent(buttonId);
            }
        }

        private void StartTimerEvent(string buttonId)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (sender, e) => TimerElapsed(sender, e, buttonId);
            timer.Start();
            timerEvents[buttonId] = timer;
        }

        private void StopTimerEvent(string buttonId)
        {
            if (timerEvents.TryGetValue(buttonId, out var timer))
            {
                timer.Stop();
                timerEvents.Remove(buttonId);
            }
        }

        private void TimerElapsed(object sender, object e, string buttonId)
        {
            // Ustawienie właściwości przy użyciu SetProperty z Community Toolkit
            TimerForStatus = $"{timers[buttonId].Elapsed.TotalSeconds}";

            // Wywołanie zdarzenia TimerTick
            TimerTick?.Invoke(this, buttonId);

            // Zaktualizuj listę timerów
            Dictionary<string, double> allTimers = new Dictionary<string, double>();
            foreach (var kvp in timers)
            {
                allTimers[kvp.Key] = kvp.Value.Elapsed.TotalSeconds;
            }

            // Wysłanie wiadomości o zmianie czasu dla konkretnego timera
            StrongReferenceMessenger.Default.Send(new TimerValueChangedMessage(allTimers, buttonId, TimerForStatus));
        }

    }
}
