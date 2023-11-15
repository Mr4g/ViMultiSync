using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace ViMultiSync.Keyboard.Layout
{
    public abstract class KeyboardLayout : UserControl
    {
        string LayoutName { get; }
    }
}
