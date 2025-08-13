using Avalonia.Controls;
using ViSyncMaster.Views.Controls;

namespace ViSyncMaster.Views
{
    public partial class ScadaHostView : UserControl
    {
        public ScadaHostNative NativeHostControl => NativeHost;

        public ScadaHostView()
        {
            InitializeComponent();
        }
    }
}