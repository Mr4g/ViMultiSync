using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ViSyncMaster.Keyboard.Layout
{
    public partial class VirtualKeyboardLayoutDE : UserControl
    {
        public VirtualKeyboardLayoutDE()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public string LayoutName => "de-DE";
    }
}
