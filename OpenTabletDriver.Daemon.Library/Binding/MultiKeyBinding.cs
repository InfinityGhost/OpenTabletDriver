using System;
using System.Collections.Generic;
using System.Linq;
using OpenTabletDriver.Attributes;
using OpenTabletDriver.Platform.Keyboard;
using OpenTabletDriver.Tablet;

namespace OpenTabletDriver.Daemon.Library.Binding
{
    [PluginName(PLUGIN_NAME)]
    public class MultiKeyBinding : IStateBinding
    {
        private readonly IVirtualKeyboard _keyboard;
        private readonly IEnumerable<BindableKey> _usableKeys;

        public MultiKeyBinding(IVirtualKeyboard keyboard, ISettingsProvider settingsProvider, IKeyMapper keyMapper)
        {
            _keyboard = keyboard;
            _usableKeys = keyMapper.GetBindableKeys();

            settingsProvider.Inject(this);
        }

        private const string PLUGIN_NAME = "Multi-Key Binding";

        private IList<BindableKey> _keys = Array.Empty<BindableKey>();
        private string _keysString = string.Empty;

        [Setting("Keys")]
        public string Keys
        {
            set
            {
                _keysString = value;
                _keys = ParseKeys(Keys);
            }
            get => _keysString;
        }

        public void Press(IDeviceReport report)
        {
            if (_keys.Count > 0)
                _keyboard.Press(_keys);
        }

        public void Release(IDeviceReport report)
        {
            if (_keys.Count > 0)
                _keyboard.Release(_keys);
        }

        private List<BindableKey> ParseKeys(string str)
        {
            var newKeys = str.Split('+', StringSplitOptions.TrimEntries);
            var newKeysAsBindable = new List<BindableKey>();

            // parse string representation into BindableKey enum
            foreach (var key in newKeys)
            {
                if (Enum.TryParse(key, true, out BindableKey parsedKey))
                    newKeysAsBindable.Add(parsedKey);
            }

            return newKeysAsBindable.All(k => _usableKeys.Contains(k)) ? newKeysAsBindable :
                throw new NotSupportedException($"The keybinding combination ({str}) is not supported.");
        }

        public override string ToString() => $"{PLUGIN_NAME}: {Keys}";
    }
}
