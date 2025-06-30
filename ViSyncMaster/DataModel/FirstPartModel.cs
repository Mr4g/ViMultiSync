using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViSyncMaster.Entitys;

namespace ViSyncMaster.DataModel
{
    public partial class FirstPartModel : ObservableValidator, IEntity
    {

        public event EventHandler? NumberProductChanged;

        public FirstPartModel()
        {
            this.Name = "S7.FirstPartData";
        }

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Numer produktu jest wymagany.")]
        [RegularExpression(@"^\d{7}$", ErrorMessage = "Podpis musi zawierać dokładnie 7 cyfr.")]
        private string? _numberProduct;
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Numer zacisku jest wymagany.")]
        [RegularExpression(@"^(-|((?!0$)\d+([,/]\d+)*))?$", ErrorMessage = "Wartość nie może być 0 i musi zawierać tylko cyfry z ',' '/' lub '-' jeśli jest pusta.")]
        private string? _numberClamp;
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Wysokość zacisku powinna zawierać cyfry lub '-'.")]
        [RegularExpression(@"^(-|((?!0$)\d+([,/]\d+)*))?$", ErrorMessage = "Wartość nie może być 0 i musi zawierać tylko cyfry z ',' '/' lub '-' jeśli jest pusta.")]
        private string? _heightClamp;
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Siła łamania Lumberga powinna zawierać cyfry lub '-'.")]
        [RegularExpression(@"^(-|((?!0$)\d+([,/]\d+)*))?$", ErrorMessage = "Wartość nie może być 0 i musi zawierać tylko cyfry z ',' '/' lub '-' jeśli jest pusta.")]
        private string? _breakingForceLumberg;
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Siła łamania zacisku powinna zawierać cyfry lub '-'.")]
        [RegularExpression(@"^(-|((?!0$)\d+([,/]\d+)*))?$", ErrorMessage = "Wartość nie może być 0 i musi zawierać tylko cyfry z ',' '/' lub '-' jeśli jest pusta.")]
        private string? _breakingForceClamp;
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Siła łamania wtyczki powinna zawierać cyfry lub '-'.")]
        [RegularExpression(@"^(-|((?!0$)\d+([,/]\d+)*))?$", ErrorMessage = "Wartość nie może być 0 i musi zawierać tylko cyfry z ',' '/' lub '-' jeśli jest pusta.")]
        private string? _breakingForcePlug;
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Siła łamania wtrysku powinna zawierać cyfry  lub '-'.")]
        [RegularExpression(@"^(-|((?!0$)\d+([,/]\d+)*))?$", ErrorMessage = "Wartość nie może być 0 i musi zawierać tylko cyfry z ',' '/' lub '-' jeśli jest pusta.")]
        private string? _breakingForceInjection;
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Twardość wtrysku powinna zawierać cyfry lub '-'.")]
        [RegularExpression(@"^(-|((?!0$)\d+([,/]\d+)*))?$", ErrorMessage = "Wartość nie może być 0 i musi zawierać tylko cyfry z ',' '/' lub '-' jeśli jest pusta.")]
        private string? _injectionHardness;
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [RegularExpression(@"^(-|((?!0$)\d+([,/]\d+)*))?$", ErrorMessage = "Wartość nie może być 0 i musi zawierać tylko cyfry z ',' '/' lub '-' jeśli jest pusta.")]
        [Required(ErrorMessage = "Wartość EQ powinna zawierać cyfry  lub '-'.")]
        private string? _eq;
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Moment obrotowy wkrętaka powinnien zawierać cyfry  lub '-'.")]
        [RegularExpression(@"^(-|((?!0$)\d+([,/]\d+)*))?$", ErrorMessage = "Wartość nie może być 0 i musi zawierać tylko cyfry z ',' '/' lub '-' jeśli jest pusta.")]
        private string? _screwdriverTorque;
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Wymiary łuski muszą zawierać cyfry lub '-'")]
        [RegularExpression(@"^(-|((?!0$)\d+([,/]\d+)*))?$", ErrorMessage = "Wartość nie może być 0 i musi zawierać tylko cyfry z ',' '/' lub '-' jeśli jest pusta.")]
        private string? _shellSize;
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Waga pasty muszą zawierać cyfry lub '-'")]
        [RegularExpression(@"^(-|((?!0$)\d+([,/]\d+)*))?$", ErrorMessage = "Wartość nie może być 0 i musi zawierać tylko cyfry z ',' '/' lub '-' jeśli jest pusta.")]
        private string? _pasteWeight;
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Dział jest wymagany.")]
        [RegularExpression(@"^(GM|SK|KM|CHP|NTC)*$", ErrorMessage = "Podpis musi zawierać tylko GM, NTC, SK, CHP lub KM.")]
        private string? _department = "Wybierz dział...";
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Podpis jest wymagany.")]
        [RegularExpression(
        @"^(-|((?!0$)[A-Za-z0-9]+([,/][A-Za-z0-9]+)*))?$",
        ErrorMessage = "Wartość nie może być 0 i może zawierać tylko litery, cyfry oraz ',' lub '/' jako separator.")]
        private string? _signature;
        public long Id { get; set; }
        public string? Name { get; set; }
        public long? SendTime { get; set; }
        public string SendStatus { get; set; } = "Pending";
        public bool ValidateAllModel()
        {
            ValidateAllProperties();
            var isValid = HasErrors;
            return isValid;
        }

        partial void OnNumberProductChanged(string? oldValue, string? newValue)
        {
            if (!string.IsNullOrEmpty(newValue))
            {
                NumberProductChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool UpdateValidationRules(Dictionary<string, bool> visibilityMap)
        {
            ValidateAllProperties(); // Wywołaj ponowną walidację wszystkich właściwości

            if (visibilityMap.Count != 0)
            {
                if (!visibilityMap["IsNumberClampVisible"])
                    ClearErrors(nameof(NumberClamp));
                if (!visibilityMap["IsBreakingForceClampVisible"])
                    ClearErrors(nameof(BreakingForceClamp));
                if (!visibilityMap["IsBreakingForceInjectionVisible"])
                    ClearErrors(nameof(BreakingForceInjection));
                if (!visibilityMap["IsBreakingForcePlugVisible"])
                    ClearErrors(nameof(BreakingForcePlug));
                if (!visibilityMap["IsHeightClampVisible"])
                    ClearErrors(nameof(HeightClamp));
                if (!visibilityMap["IsBreakingForceLumbergVisible"])
                    ClearErrors(nameof(BreakingForceLumberg));
                if (!visibilityMap["IsInjectionHardnessVisible"])
                    ClearErrors(nameof(InjectionHardness));
                if (!visibilityMap["IsScrewdriverTorqueVisible"])
                    ClearErrors(nameof(ScrewdriverTorque));
                if (!visibilityMap["IsPasteWeightVisible"])
                    ClearErrors(nameof(PasteWeight));
                if (!visibilityMap["IsShellSizeVisible"])
                    ClearErrors(nameof(ShellSize));
                if (!visibilityMap["IsDepartmentVisible"])
                    ClearErrors(nameof(Department));
                if (!visibilityMap["IsEqVisible"])
                    ClearErrors(nameof(Eq));
                if (!visibilityMap["IsSignatureVisible"])
                    ClearErrors(nameof(Signature));
            }
            var isValid = HasErrors;
            return isValid;
        }
    }
}
