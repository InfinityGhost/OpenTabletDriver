using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using OpenTabletDriver.Attributes;
using OpenTabletDriver.Platform.Keyboard;
using OpenTabletDriver.Tablet;

namespace OpenTabletDriver.Daemon.Library.Binding
{
    [PluginName(PLUGIN_NAME)]
    public class KeyBinding : IStateBinding
    {
        private const string PLUGIN_NAME = "Key Binding";

        private readonly InputDevice _device;
        private readonly IVirtualKeyboard _keyboard;

        public KeyBinding(InputDevice device, IVirtualKeyboard keyboard, ISettingsProvider settingsProvider)
        {
            _device = device;
            _keyboard = keyboard;

            settingsProvider.Inject(this);
        }

        [Setting("Key"), MemberValidated(nameof(GetValidKeys))]
        public BindableKey? Key { set; get; }

        public void Press(IDeviceReport report)
        {
            if (Key.HasValue)
                _keyboard.Press(Key.Value);
        }

        public void Release(IDeviceReport report)
        {
            if (Key.HasValue)
                _keyboard.Release(Key.Value);
        }

        public static IEnumerable<BindableKey> GetValidKeys(IServiceProvider serviceProvider)
        {
            var keysProvider = serviceProvider.GetRequiredService<IKeyMapper>();
            return keysProvider.GetBindableKeys();
        }

        public override string ToString() => $"{PLUGIN_NAME}: {Key}";
    }
}
