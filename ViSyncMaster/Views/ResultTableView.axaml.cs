using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Collections.ObjectModel;
using ViSyncMaster.DataModel;
using ViSyncMaster.Services;
using ViSyncMaster.ViewModels;


namespace ViSyncMaster;

public partial class ResultTableView : UserControl
{
    public ResultTableView()
    {
        InitializeComponent();
    }

    public void SetDataContext(ObservableCollection<MachineStatus> machineStatuses, MainWindowViewModel mainWindowViewModel, MachineStatusService machineStatusService)
    {
        var viewModel = new ResultTableViewModel(machineStatusService, mainWindowViewModel, machineStatuses);
        DataContext = viewModel;  // Ustawienie DataContext w widoku
    }
}