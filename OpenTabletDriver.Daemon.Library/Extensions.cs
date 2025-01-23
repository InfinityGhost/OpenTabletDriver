using System.Collections.Generic;
using System.Linq;
using OpenTabletDriver.Platform.Keyboard;

namespace OpenTabletDriver.Daemon.Library;

public static class Extensions
{
    public static IEnumerable<uint> GetOSKeyByKeyBinding(this IKeyMapper keyMapper)
    {
        return keyMapper.GetBindableKeys().Select(key => (uint)keyMapper[key]);
    }
}
