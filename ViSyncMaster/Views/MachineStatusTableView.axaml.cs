using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Collections.ObjectModel;
using ViSyncMaster.DataModel;
using ViSyncMaster.ViewModels;

namespace ViSyncMaster.Views;

public partial class MachineStatusTableView : UserControl
{
    public MachineStatusTableView()
    {
        InitializeComponent();
    }

    public void SetDataContext(ObservableCollection<MachineStatus> machineStatuses)
    { 
        var viewModel = new MachineStatusTableViewModel(machineStatuses);
        DataContext = viewModel;  // Ustawienie DataContext w widoku
    }
}