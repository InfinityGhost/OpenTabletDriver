﻿using TabletDriverPlugin.Tablet;

namespace OpenTabletDriver.UX.Debugging
{
    public class DebugAuxReport : DebugDeviceReport, IAuxReport
    {
        public bool[] AuxButtons { set; get; }
    }
}