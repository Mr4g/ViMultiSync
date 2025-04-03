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
        private List<ProductSettings> _productSettingsList = new();
        string _settingsFilePath = Path.Combine("C:", "ViSM", "ConfigFiles", "ProductSettings.csv");


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

        public FormFirstPartViewModel(MachineStatusService machineStatusService)
        {
            _machineStatusService = machineStatusService;
            try
            {
                _productSettingsList = LoadProductSettings.LoadSettings(_settingsFilePath);
            }
            catch (Exception ex)
            {
                _productSettingsList = new List<ProductSettings>();
            }
            FirstPartModel.ErrorsChanged += FirstPartModel_ErrorsChanged;
            FirstPartModel.NumberProductChanged += FirstPartModel_NumberProductChanged;
        }

        private void FirstPartModel_NumberProductChanged(object? sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(FirstPartModel.NumberProduct))
            {
                var settings = GetProductSettings(FirstPartModel.NumberProduct, _productSettingsList);
                UpdateFieldVisibility(settings);
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

        public ProductSettings? GetProductSettings(string numberProduct, List<ProductSettings> data)
        {
            return data.FirstOrDefault(p => p.NumberProduct == numberProduct);
        }
        public void UpdateFieldVisibility(ProductSettings? settings)
        {
            if (settings == null)
            {
                IsNumberClampVisible = true;
                IsBreakingForceClampVisible = true;
                IsBreakingForceInjectionVisible = true;
                IsBreakingForcePlugVisible = true;
                IsHeightClampVisible = true;
                IsBreakingForceLumbergVisible = true;
                IsInjectionHardnessVisible = true;
                IsScrewdriverTorqueVisible = true;
                IsPasteWeightVisible = true;
                IsShellSizeVisible = true;
                IsDepartmentVisible = true;
                IsEqVisible = true;
                IsSignatureVisible = true;
                return;
            }
            IsNumberClampVisible = settings?.NumberClamp == 1;
            IsBreakingForceClampVisible = settings?.BreakingForceClamp == 1;
            IsBreakingForceInjectionVisible = settings?.BreakingForceInjection == 1;
            IsBreakingForcePlugVisible = settings?.BreakingForcePlug == 1;
            IsHeightClampVisible = settings?.HeightClamp == 1;
            IsBreakingForceLumbergVisible = settings?.BreakingForceLumberg == 1;
            IsInjectionHardnessVisible = settings?.InjectionHardness == 1;
            IsScrewdriverTorqueVisible = settings?.ScrewdriverTorque == 1;
            IsPasteWeightVisible = settings?.PasteWeight == 1;
            IsShellSizeVisible = settings?.ShellSize == 1;
            IsDepartmentVisible = settings?.Department == 1;
            IsEqVisible = settings?.Eq == 1;
            IsSignatureVisible = settings?.Signature == 1;

            // Dodajemy informacje o widoczności pól do słownika
            _visibilityMap["IsNumberClampVisible"] = IsNumberClampVisible;
            _visibilityMap["IsBreakingForceClampVisible"] = IsBreakingForceClampVisible;
            _visibilityMap["IsBreakingForceInjectionVisible"] = IsBreakingForceInjectionVisible;
            _visibilityMap["IsBreakingForcePlugVisible"] = IsBreakingForcePlugVisible;
            _visibilityMap["IsHeightClampVisible"] = IsHeightClampVisible;
            _visibilityMap["IsBreakingForceLumbergVisible"] = IsBreakingForceLumbergVisible;
            _visibilityMap["IsInjectionHardnessVisible"] = IsInjectionHardnessVisible;
            _visibilityMap["IsScrewdriverTorqueVisible"] = IsScrewdriverTorqueVisible;
            _visibilityMap["IsPasteWeightVisible"] = IsPasteWeightVisible;
            _visibilityMap["IsShellSizeVisible"] = IsShellSizeVisible;
            _visibilityMap["IsDepartmentVisible"] = IsDepartmentVisible;
            _visibilityMap["IsEqVisible"] = IsEqVisible;
            _visibilityMap["IsSignatureVisible"] = IsSignatureVisible;
        }

        public void BackToDefaultForm()
        {
            FirstPartModel = new FirstPartModel();

            IsNumberClampVisible = false;
            IsBreakingForceClampVisible = false;
            IsBreakingForceInjectionVisible = false;
            IsBreakingForcePlugVisible = false;
            IsHeightClampVisible = false;
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
