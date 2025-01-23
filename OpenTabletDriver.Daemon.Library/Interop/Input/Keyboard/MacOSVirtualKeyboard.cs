using System;
using System.Collections.Generic;
using OpenTabletDriver.Native.MacOS;
using OpenTabletDriver.Native.MacOS.Generic;
using OpenTabletDriver.Native.MacOS.Input;
using OpenTabletDriver.Platform.Keyboard;

namespace OpenTabletDriver.Daemon.Library.Interop.Input.Keyboard
{
    using static MacOS;

    public class MacOSVirtualKeyboard : IVirtualKeyboard
    {
        private readonly IKeyMapper _keysProvider;

        public MacOSVirtualKeyboard(IKeyMapper keysProvider)
        {
            _keysProvider = keysProvider;
        }

        private void KeyEvent(BindableKey key, bool isPress)
        {
            var code = _keysProvider[key];
            var keyEvent = CGEventCreateKeyboardEvent(IntPtr.Zero, (CGKeyCode)code, isPress);
            CGEventPost(CGEventTapLocation.kCGHIDEventTap, keyEvent);
            CFRelease(keyEvent);
        }

        public void Press(BindableKey key)
        {
            KeyEvent(key, true);
        }

        public void Release(BindableKey key)
        {
            KeyEvent(key, false);
        }

        public void Press(IEnumerable<BindableKey> keys)
        {
            foreach (var key in keys)
                KeyEvent(key, true);
        }

        public void Release(IEnumerable<BindableKey> keys)
        {
            foreach (var key in keys)
                KeyEvent(key, false);
        }

        public IEnumerable<BindableKey> SupportedKeys => _keysProvider.GetBindableKeys();
    }
}
