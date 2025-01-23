using Microsoft.Extensions.DependencyInjection;
using OpenTabletDriver.Daemon.Interop.Input.Keyboard;
using OpenTabletDriver.Daemon.Library.Interop.Display;
using OpenTabletDriver.Daemon.Library.Interop.Environment;
using OpenTabletDriver.Daemon.Library.Interop.Input.Absolute;
using OpenTabletDriver.Daemon.Library.Interop.Input.Keyboard;
using OpenTabletDriver.Daemon.Library.Interop.Input.Relative;
using OpenTabletDriver.Daemon.Library.Interop.Timer;
using OpenTabletDriver.Daemon.Library.Updater;
using OpenTabletDriver.Platform.Display;
using OpenTabletDriver.Platform.Environment;
using OpenTabletDriver.Platform.Keyboard;
using OpenTabletDriver.Platform.Pointer;

namespace OpenTabletDriver.Daemon.Library.Interop
{
    using static ServiceDescriptor;

    public sealed class DesktopMacOSServiceCollection : DesktopServiceCollection
    {
        public DesktopMacOSServiceCollection() : base()
        {
            this.AddServices(new[] {
                Transient<ITimer, MacOSTimer>(),
                Transient<IAbsolutePointer, MacOSAbsolutePointer>(),
                Transient<IRelativePointer, MacOSRelativePointer>(),
                Transient<IVirtualKeyboard, MacOSVirtualKeyboard>(),
                Transient<IKeyMapper, MacOSKeysProvider>(),
                Transient<IVirtualScreen, MacOSDisplay>(),
                Transient<IEnvironmentHandler, MacOSEnvironmentHandler>(),
                Transient<IUpdater, MacOSUpdater>()
            });
        }
    }
}
