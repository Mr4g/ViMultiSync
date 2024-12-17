using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ViSyncMaster.Keyboard.Layout
{
    public partial class VirtualKeyboardLayoutUS : KeyboardLayout
    {
        public VirtualKeyboardLayoutUS()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public string LayoutName => "en-US";
    }
}
