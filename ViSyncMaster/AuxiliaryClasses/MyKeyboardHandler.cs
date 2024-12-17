using System;
using Xilium.CefGlue;


namespace ViSyncMaster.AuxiliaryClasses
{
    public class MyKeyboardHandler : CefKeyboardHandler
    {
        // ...

        public virtual bool OnPreKeyEvent(CefBrowser browser, CefKeyEvent keyEvent, IntPtr os_event, out bool isKeyboardShortcut)
        {
            isKeyboardShortcut = false;
            return false;
        }

        public virtual bool OnKeyEvent(CefBrowser browser, CefKeyEvent keyEvent, IntPtr osEvent)
        {
            return false;
        }

    }
}
