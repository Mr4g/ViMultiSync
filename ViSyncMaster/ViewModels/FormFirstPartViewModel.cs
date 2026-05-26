using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViSyncMaster.DataModel;
using ViSyncMaster.Services;

namespace ViSyncMaster.ViewModels
{
    public partial class FormFirstPartViewModel : ObservableObject
    {
        Dictionary<string, bool> _visibilityMap = new Dictionary<string, bool>();

        private readonly MachineStatusService _machineStatusService;
        private readonly FirstPartFieldConfigService _fieldConfigService;
        private readonly Dictionary<string, Action<bool>> _visibilitySetters;
        private readonly Dictionary<string, Func<string?>> _fieldValueGetters;
        private readonly List<string> _allFieldKeys;

        [ObservableProperty]
        private bool _showAllFields;

        [ObservableProperty]
        private FirstPartModel _firstPartModel = new FirstPartModel();
        [ObservableProperty]
        private string _validationMessage;
        [ObservableProperty]
        private int _opacityForm = 10;
        [ObservableProperty]
        public bool _isNumberClampVisible;
        [ObservableProperty]
        public bool _isBreakingForceClampVisible;
        [ObservableProperty]
        public bool _isBreakingForceInjectionVisible;
        [ObservableProperty]
        public bool _isHeightClampVisible;
        [ObservableProperty]
        public bool _isBreakingForceLumbergVisible;
        [ObservableProperty]
        public bool _isBreakingForcePlugVisible;
        [ObservableProperty]
        public bool _isHeightPlugVisible;
        [ObservableProperty]
        public bool _isInjectionHardnessVisible;
        [ObservableProperty]
        public bool _isScrewdriverTorqueVisible;
        [ObservableProperty]
        public bool _isPasteWeightVisible;
        [ObservableProperty]
        public bool _isShellSizeVisible;
        [ObservableProperty]
        public bool _isDepartmentVisible;
        [ObservableProperty]
        public bool _isEqVisible;
        [ObservableProperty]
        public bool _isSignatureVisible;

        partial void OnShowAllFieldsChanged(bool oldValue, bool newValue)
        {
            ApplyFieldVisibilityByProduct();
        }

        public FormFirstPartViewModel(MachineStatusService machineStatusService)
        {
            _machineStatusService = machineStatusService;
            var dbPath = Path.Combine("C:", "ViSM", "ConfigFiles", "FirstPartFieldConfig.json");
            _fieldConfigService = new FirstPartFieldConfigService(dbPath);

            _visibilitySetters = new Dictionary<string, Action<bool>>
            {
                ["NumberClamp"] = v => IsNumberClampVisible = v,
                ["BreakingForceClamp"] = v => IsBreakingForceClampVisible = v,
                ["BreakingForceInjection"] = v => IsBreakingForceInjectionVisible = v,
                ["BreakingForcePlug"] = v => IsBreakingForcePlugVisible = v,
                ["HeightClamp"] = v => IsHeightClampVisible = v,
                ["HeightPlug"] = v => IsHeightPlugVisible = v,
                ["BreakingForceLumberg"] = v => IsBreakingForceLumbergVisible = v,
                ["InjectionHardness"] = v => IsInjectionHardnessVisible = v,
                ["ScrewdriverTorque"] = v => IsScrewdriverTorqueVisible = v,
                ["PasteWeight"] = v => IsPasteWeightVisible = v,
                ["ShellSize"] = v => IsShellSizeVisible = v,
                ["Department"] = v => IsDepartmentVisible = v,
                ["Eq"] = v => IsEqVisible = v,
                ["Signature"] = v => IsSignatureVisible = v
            };

            _fieldValueGetters = new Dictionary<string, Func<string?>>
            {
                ["NumberClamp"] = () => FirstPartModel.NumberClamp,
                ["BreakingForceClamp"] = () => FirstPartModel.BreakingForceClamp,
                ["BreakingForceInjection"] = () => FirstPartModel.BreakingForceInjection,
                ["BreakingForcePlug"] = () => FirstPartModel.BreakingForcePlug,
                ["HeightClamp"] = () => FirstPartModel.HeightClamp,
                ["HeightPlug"] = () => FirstPartModel.HeightPlug,
                ["BreakingForceLumberg"] = () => FirstPartModel.BreakingForceLumberg,
                ["InjectionHardness"] = () => FirstPartModel.InjectionHardness,
                ["ScrewdriverTorque"] = () => FirstPartModel.ScrewdriverTorque,
                ["PasteWeight"] = () => FirstPartModel.PasteWeight,
                ["ShellSize"] = () => FirstPartModel.ShellSize,
                ["Department"] = () => FirstPartModel.Department,
                ["Eq"] = () => FirstPartModel.Eq,
                ["Signature"] = () => FirstPartModel.Signature
            };

            _allFieldKeys = _visibilitySetters.Keys.ToList();

            FirstPartModel.ErrorsChanged += FirstPartModel_ErrorsChanged;
            FirstPartModel.NumberProductChanged += FirstPartModel_NumberProductChanged;
        }

        private void FirstPartModel_NumberProductChanged(object? sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(FirstPartModel.NumberProduct))
            {
                ApplyFieldVisibilityByProduct();
            }
        }

        private void FirstPartModel_ErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
        {
            // Powiadomienie, że CanSend uległo zmianie – UI zaktualizuje przycisk.
            OnPropertyChanged(nameof(CanSend));
            SendCommand.NotifyCanExecuteChanged();
            Debug.WriteLine($"ErrorsChanged dla właściwości: {e.PropertyName}, HasErrors: {FirstPartModel.HasErrors}, CanSend is: {CanSend}");
        }

        // Właściwość, która jest zależna od stanu walidacji modelu.
        public bool CanSend => !FirstPartModel.HasErrors;

        [RelayCommand]
        private async Task Send()
        {
            var isValid=FirstPartModel.UpdateValidationRules(_visibilityMap);
            if (isValid)
            {
                ValidationMessage = "Formularz jest niepoprawny...";
                return;
            }
            else
            {
                ValidationMessage = "Formularz został wysłany...";
                OpacityForm = 100;
                await Task.Delay(5000);
                OpacityForm = 10;
                ValidationMessage = "";
                foreach (var property in typeof(FirstPartModel).GetProperties())
                {
                    if (property.CanRead && property.CanWrite)
                    {
                        var value = property.GetValue(FirstPartModel);

                        // Jeśli właściwość jest stringiem i ma wartość null -> ustaw "-"
                        if (property.PropertyType == typeof(string) && value == null)
                        {
                            property.SetValue(FirstPartModel, "-");
                        }
                    }
                }
                PersistFieldConfigurationForProduct();
                await _machineStatusService.SendFirstPartAsync(FirstPartModel);
                BackToDefaultForm();
            }
        }
        [RelayCommand]
        private void ResetModel()
        {
            BackToDefaultForm();
            OnPropertyChanged(nameof(CanSend));
            ValidationMessage = "";
        }

        private void ApplyFieldVisibilityByProduct()
        {
            if (ShowAllFields || string.IsNullOrWhiteSpace(FirstPartModel.NumberProduct))
            {
                SetVisibilityForFields(_allFieldKeys);
                return;
            }

            var visibleFields = _fieldConfigService.GetVisibleFieldsForProduct(FirstPartModel.NumberProduct);
            SetVisibilityForFields(visibleFields ?? _allFieldKeys);
        }

        private void SetVisibilityForFields(IEnumerable<string> visibleFields)
        {
            var set = new HashSet<string>(visibleFields);
            foreach (var key in _allFieldKeys)
            {
                _visibilitySetters[key](set.Contains(key));
            }

            _visibilityMap["IsNumberClampVisible"] = IsNumberClampVisible;
            _visibilityMap["IsBreakingForceClampVisible"] = IsBreakingForceClampVisible;
            _visibilityMap["IsBreakingForceInjectionVisible"] = IsBreakingForceInjectionVisible;
            _visibilityMap["IsBreakingForcePlugVisible"] = IsBreakingForcePlugVisible;
            _visibilityMap["IsHeightClampVisible"] = IsHeightClampVisible;
            _visibilityMap["IsHeightPlugVisible"] = IsHeightPlugVisible;
            _visibilityMap["IsBreakingForceLumbergVisible"] = IsBreakingForceLumbergVisible;
            _visibilityMap["IsInjectionHardnessVisible"] = IsInjectionHardnessVisible;
            _visibilityMap["IsScrewdriverTorqueVisible"] = IsScrewdriverTorqueVisible;
            _visibilityMap["IsPasteWeightVisible"] = IsPasteWeightVisible;
            _visibilityMap["IsShellSizeVisible"] = IsShellSizeVisible;
            _visibilityMap["IsDepartmentVisible"] = IsDepartmentVisible;
            _visibilityMap["IsEqVisible"] = IsEqVisible;
            _visibilityMap["IsSignatureVisible"] = IsSignatureVisible;
        }

        private void PersistFieldConfigurationForProduct()
        {
            if (string.IsNullOrWhiteSpace(FirstPartModel.NumberProduct))
            {
                return;
            }

            var fieldsToPersist = _allFieldKeys
                .Where(key => _fieldValueGetters.TryGetValue(key, out var getter)
                              && !string.IsNullOrWhiteSpace(getter())
                              && getter() != "-")
                .ToList();

            _fieldConfigService.SaveVisibleFieldsForProduct(FirstPartModel.NumberProduct, fieldsToPersist);
        }

        public void BackToDefaultForm()
        {
            FirstPartModel = new FirstPartModel();

            IsNumberClampVisible = false;
            IsBreakingForceClampVisible = false;
            IsBreakingForceInjectionVisible = false;
            IsBreakingForcePlugVisible = false;
            IsHeightClampVisible = false;
            IsHeightPlugVisible = false;
            IsBreakingForceLumbergVisible = false;
            IsInjectionHardnessVisible = false;
            IsScrewdriverTorqueVisible = false;
            IsPasteWeightVisible = false;
            IsShellSizeVisible = false;
            IsDepartmentVisible = false;
            IsEqVisible = false;
            IsSignatureVisible = false;

            FirstPartModel.ErrorsChanged += FirstPartModel_ErrorsChanged;
            FirstPartModel.NumberProductChanged += FirstPartModel_NumberProductChanged;
        }
    }
}
