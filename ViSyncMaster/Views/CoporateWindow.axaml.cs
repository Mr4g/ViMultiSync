using Avalonia.Controls;
using Avalonia;
using Avalonia.Markup.Xaml;

namespace ViSyncMaster.Views
{
    public partial class CoporateWindow : Window
    {
        public static readonly StyledProperty<object> CoporateContentProperty = AvaloniaProperty.Register<CoporateWindow, object>(nameof(CoporateContent));

        public object CoporateContent
        {
            get { return GetValue(CoporateContentProperty); }
            set { SetValue(CoporateContentProperty, value); }
        }

        public CoporateWindow()
        {
            DataContext = this;
            InitializeComponent();
//#if DEBUG
//            this.AttachDevTools();
//#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
