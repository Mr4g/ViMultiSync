using System;
using System.ComponentModel;
using ViSyncMaster.Entitys;

namespace ViSyncMaster.DataModel
{
    public class MachineStatus : IEntity, INotifyPropertyChanged
    {
        public MachineStatus() { }

        public long Id { get; set; }

        /// <summary>
        /// Nazwa statusu maszyny.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Aktualny status maszyny.
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Powód zakończenia statusu (jeśli dotyczy).
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Czas rozpoczęcia statusu.
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Czas zgłoszenia wezwania serwisu.
        /// </summary>
        public DateTime? CallForService { get; set; }

        /// <summary>
        /// Czy wezwanie serwisu jest aktywne.
        /// </summary>
        public bool CallForServiceRunning => CallForService.HasValue;

        /// <summary>
        /// Czas przyjazdu serwisu.
        /// </summary>
        public DateTime? ServiceArrival { get; set; }

        /// <summary>
        /// Czy serwis jest obecny.
        /// </summary>
        public bool ServiceArrivalRunning => ServiceArrival.HasValue;

        /// <summary>
        /// Czas zakończenia statusu (jeśli dotyczy).
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Czy status jest aktywny (brak EndTime oznacza aktywność).
        /// </summary>
        public bool IsActive => !EndTime.HasValue;

        /// <summary>
        /// Łączny czas trwania statusu.
        /// </summary>
        public TimeSpan? DurationStatus => StartTime.HasValue
            ? (EndTime ?? DateTime.Now) - StartTime
            : null;

        /// <summary>
        /// Łączny czas trwania serwisu.
        /// </summary>
        public TimeSpan? DurationService => ServiceArrival.HasValue
            ? (EndTime ?? DateTime.Now) - ServiceArrival
            : null;

        /// <summary>
        /// Łączny czas oczekiwania na serwis
        /// </summary>
        public TimeSpan? DurationWaitingForService => CallForService.HasValue
            ? (ServiceArrival ?? DateTime.Now) - CallForService
            : null;


        /// <summary>
        /// Aktualny krok statusu:
        /// 0 - Brak StartTime
        /// 1 - StartTime + CallForService
        /// 2 - StartTime + CallForService + ServiceArrival
        /// 3 - StartTime + CallForService + ServiceArrival + Reason
        /// </summary>
        public int StepOfStatus
        {
            get
            {
                if (!StartTime.HasValue) return 0;
                if (CallForService.HasValue && !ServiceArrival.HasValue) return 1;
                if (ServiceArrival.HasValue && string.IsNullOrEmpty(Reason)) return 2;
                if (!string.IsNullOrEmpty(Reason)) return 3;
                return 0;
            }
        }

        /// <summary>
        /// Kolor statusu (może być używany w interfejsie użytkownika).
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// Wartość wyliczana na podstawie stanu zakończenia.
        /// </summary>
        public virtual string Value { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
                protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Formatowanie dla czytelnego wyświetlania obiektu.
        /// </summary>
        public override string ToString()
        {
            return $"[ID: {Id}, Name: {Name}, Step: {StepOfStatus}, IsActive: {IsActive}, DurationStatus: {DurationStatus}, DurationStatus: {DurationService} ]";
        }


    }
}
