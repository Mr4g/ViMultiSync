using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ViSyncMaster.Keyboard.Layout;
using ViSyncMaster.Views;

namespace ViSyncMaster.Keyboard
{
    public enum VirtualKeyboardState
    {
        Default,
        Shift,
        Capslock,
        AltCtrl
    }
    public partial class VirtualKeyboard : UserControl
    {
        private static List<Type> Layouts { get; } = new List<Type>();
        private static Func<Type> DefaultLayout { get; set; }

        
        private string actualText = string.Empty;

        public static void AddLayout<TLayout>() => Layouts.Add(typeof(TLayout));
        public bool IsPasswordChar { get; private set; }
        public static void SetDefaultLayout(Func<Type> getDefaultLayout) => DefaultLayout = getDefaultLayout;

        public static async Task<string?> ShowDialog(bool isPasswordChar, TextInputOptions options, string currentText, Window? owner = null)
        {
            var keyboard = new VirtualKeyboard(isPasswordChar, currentText);

            var window = new CoporateWindow();
            window.CoporateContent = keyboard;
            window.Title = "Keyboard";
            window.Topmost = true;

            var mw = ((IClassicDesktopStyleApplicationLifetime)App.Current.ApplicationLifetime).MainWindow;


            await window.ShowDialog(owner ?? mw);
            if (window.Tag is string s)
            {
                //if (options.Source is TextBox tb)
                //    tb.Text = s;
                return s;
            }
            return null;
        }


        public TextBox TextBox_ { get; }
        public Button AcceptButton_ { get; }
        public string targetLayout { get; set; }
        public TransitioningContentControl TransitioningContentControl_ { get; }

        public IObservable<VirtualKeyboardState> KeyboardStateStream => _keyboardStateStream;
        private readonly BehaviorSubject<VirtualKeyboardState> _keyboardStateStream;

        private Window _parentWindow;

        public VirtualKeyboard(bool isPasswordChar, string currentText)
        {
            InitializeComponent();
            IsPasswordChar = isPasswordChar;
            VirtualKeyboard.AddLayout<VirtualKeyboardLayoutUS>();
            VirtualKeyboard.AddLayout<VirtualKeyboardLayoutDE>();
            VirtualKeyboard.SetDefaultLayout(() => typeof(VirtualKeyboardLayoutUS));
            TextBox_ = this.Get<TextBox>("TextBox");
            TransitioningContentControl_ = this.Get<Avalonia.Controls.TransitioningContentControl>("TransitioningContentControl");
            //AcceptButton_ = this.Get<Button>("AcceptButton");

            // AcceptButton_.AddHandler(Button.ClickEvent, acceptClicked);
            // Ustawienie pocz�tkowego tekstu
            TextBox_.Text = currentText;
            actualText = currentText;

            Initialized += async (sender, args) =>
            {

                if (targetLayout == null)
                {
                    TransitioningContentControl_.Content = Activator.CreateInstance(DefaultLayout.Invoke());
                }
                else
                {
                    var layout = Layouts.FirstOrDefault(x => x.Name.ToLower().Contains(targetLayout.ToLower()));
                    if (layout != null)
                    {
                        TransitioningContentControl_.Content = Activator.CreateInstance(layout);
                    }
                    else
                    {
                        TransitioningContentControl_.Content = Activator.CreateInstance(DefaultLayout.Invoke());
                    }
                }

                _parentWindow = this.GetVisualAncestors().OfType<Window>().First();
                await Task.Delay(TimeSpan.FromMilliseconds(100));
                Dispatcher.UIThread.Post(() =>
                {
                    TextBox_.Focus();
                    if (!string.IsNullOrEmpty(TextBox_.Text))
                        TextBox_.CaretIndex = TextBox_.Text.Length;
                });
            };

            KeyDown += (sender, args) =>
            {
                TextBox_.Focus();
                if (args.Key == Key.Escape)
                {
                    TextBox_.Text = "";
                }
                else if (args.Key == Key.Enter)
                {
                    _parentWindow.Tag = TextBox_.Text;
                    _parentWindow.Close();
                }
            };
            _keyboardStateStream = new BehaviorSubject<VirtualKeyboardState>(VirtualKeyboardState.Default);
        }

        private void acceptClicked(object? sender, RoutedEventArgs e)
        {
            _parentWindow.Tag = TextBox_.Text;
            _parentWindow.Close();
            
        }

        public void ProcessText(string text)
        {
            TextBox_.Focus();

            if (IsPasswordChar)
            {
                // Je�eli PasswordChar jest ustawiony, zamie� ka�dy znak na "*"
                string maskedText = new string('*', text.Length);

                // Dodaj przekszta�cony tekst do TextBox_.Text
                TextBox_.Text += maskedText;
                actualText += text;
            }
            else
            {
                // Je�eli PasswordChar nie jest ustawiony, dodaj tekst bez zmian
                TextBox_.Text += text;
                actualText += text;
            }
            TextBox_.CaretIndex = TextBox_.Text.Length;

            if (_keyboardStateStream.Value == VirtualKeyboardState.Shift)
            {
                _keyboardStateStream.OnNext(VirtualKeyboardState.Default);
            }
        }

        public string GetActualText()
        {
            return actualText;
        }

        public void Accept()
        {
            _parentWindow.Tag = TextBox_.Text;
            _parentWindow.Close();
        }

        public void ProcessKey(Key key)
        {
            if (key == Key.LeftShift || key == Key.RightShift)
            {
                if (_keyboardStateStream.Value == VirtualKeyboardState.Shift)
                {
                    _keyboardStateStream.OnNext(VirtualKeyboardState.Default);
                }
                else
                {
                    _keyboardStateStream.OnNext(VirtualKeyboardState.Shift);
                }
            }
            else if (key == Key.RightAlt)
            {
                if (_keyboardStateStream.Value == VirtualKeyboardState.AltCtrl)
                {
                    _keyboardStateStream.OnNext(VirtualKeyboardState.Default);
                }
                else
                {
                    _keyboardStateStream.OnNext(VirtualKeyboardState.AltCtrl);
                }
            }
            else if (key == Key.CapsLock)
            {
                if (_keyboardStateStream.Value == VirtualKeyboardState.Capslock)
                {
                    _keyboardStateStream.OnNext(VirtualKeyboardState.Default);
                }
                else
                {
                    _keyboardStateStream.OnNext(VirtualKeyboardState.Capslock);
                }
            }
            else
            {
                if (key == Key.Clear)
                {
                    TextBox_.Text = "";
                    TextBox_.Focus();
                }
                else if (key == Key.Enter || key == Key.ImeAccept)
                {
                    string actualText = this.GetActualText();
                    _parentWindow.Tag = actualText;
                    _parentWindow.Close();
                    //_parentWindow.Tag = TextBox_.Text;
                    //_parentWindow.Close();
                }
                else if (key == Key.Help)
                {
                    _keyboardStateStream.OnNext(VirtualKeyboardState.Default);
                    if (TransitioningContentControl_.Content is KeyboardLayout layout)
                    {
                        var index = Layouts.IndexOf(layout.GetType());
                        if (Layouts.Count - 1 > index)
                        {
                            TransitioningContentControl_.Content = Activator.CreateInstance(Layouts[index + 1]);
                        }
                        else
                        {
                            TransitioningContentControl_.Content = Activator.CreateInstance(Layouts[0]);
                        }
                    }
                }
                else if (key == Key.Back)
                {
                    if (TextBox_.Text != null && TextBox_.Text.Length > 0)
                    {
                        TextBox_.Text = TextBox_.Text.Remove(TextBox_.Text.Length - 1, 1);
                        actualText = actualText.Remove(actualText.Length - 1, 1); // Aktualizuj r�wnie� actualText
                    }
                }
                else
                {
                    TextBox_.Focus();

                    //InputManager.Instance.ProcessInput(new RawKeyEventArgs(KeyboardDevice.Instance, (ulong)DateTime.Now.Ticks, (Window)TextBox.GetVisualRoot(), RawKeyEventType.KeyDown, key, RawInputModifiers.None));
                    //InputManager.Instance.ProcessInput(new RawKeyEventArgs(KeyboardDevice.Instance, (ulong)DateTime.Now.Ticks, (Window)TextBox.GetVisualRoot(), RawKeyEventType.KeyUp, key, RawInputModifiers.None));
                }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}