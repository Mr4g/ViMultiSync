using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xilium.CefGlue;

namespace ViMultiSync.AuxiliaryClasses
{
    public class MyKeyboardHandlerAdapter : Xilium.CefGlue.Common.Handlers.KeyboardHandler
    {
        private MyKeyboardHandler _myKeyboardHandler;

        public MyKeyboardHandlerAdapter(MyKeyboardHandler myKeyboardHandler)
        {
            _myKeyboardHandler = myKeyboardHandler;
        }

        protected override bool OnPreKeyEvent(CefBrowser browser, CefKeyEvent keyEvent, IntPtr os_event, out bool isKeyboardShortcut)
        {
            return _myKeyboardHandler.OnPreKeyEvent(browser, keyEvent, os_event, out isKeyboardShortcut);
        }

        protected override bool OnKeyEvent(CefBrowser browser, CefKeyEvent keyEvent, IntPtr osEvent)
        {
            return _myKeyboardHandler.OnKeyEvent(browser, keyEvent, osEvent);
        }
    }
}
