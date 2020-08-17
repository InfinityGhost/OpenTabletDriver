using System;
using NativeLib.Linux;
using NativeLib.Linux.Evdev;
using TabletDriverPlugin;
using TabletDriverPlugin.Platform.Pointer;

namespace TabletDriverLib.Interop.Input.Mouse
{
    public class EvdevVirtualMouse : EvdevVirtualPointer, IVirtualMouse
    {
        public unsafe EvdevVirtualMouse()
        {
            Device = new EvdevDevice("OpenTabletDriver Virtual Mouse");

            Device.EnableTypeCodes(
                EventType.EV_REL,
                EventCode.REL_X,
                EventCode.REL_Y
            );

            Device.EnableTypeCodes(
                EventType.EV_KEY,
                EventCode.BTN_LEFT,
                EventCode.BTN_MIDDLE,
                EventCode.BTN_RIGHT,
                EventCode.BTN_FORWARD,
                EventCode.BTN_BACK);

            var result = Device.Initialize();
            switch (result)
            {
                case ERRNO.NONE:
                    Log.Debug("Evdev", $"Successfully initialized virtual mouse. (code {result})");
                    break;
                default:
                    Log.Write("Evdev", $"Failed to initialize virtual mouse. (error code {result})", LogLevel.Error);
                    break;
            }
        }

        public void Move(float dX, float dY)
        {
            Device.Write(EventType.EV_REL, EventCode.REL_X, (int)dX);
            Device.Write(EventType.EV_REL, EventCode.REL_Y, (int)dY);
            Device.Sync();
        }
    }
}