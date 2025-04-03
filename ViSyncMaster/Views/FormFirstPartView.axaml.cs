using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ViSyncMaster.Services;
using ViSyncMaster.ViewModels;
namespace ViSyncMaster;

public partial class FormFirstPartView : UserControl
{
    public FormFirstPartView(MachineStatusService machineStatusService)
    {
        InitializeComponent();
        DataContext = new FormFirstPartViewModel(machineStatusService);
    }
}