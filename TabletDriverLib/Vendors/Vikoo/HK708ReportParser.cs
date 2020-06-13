using System;
using TabletDriverLib.Tablet;
using TabletDriverPlugin;
using TabletDriverPlugin.Tablet;

namespace TabletDriverLib.Vendors.Vikoo
{
    public class HK708ReportParser : IDeviceReportParser
    {
        public virtual IDeviceReport Parse(byte[] data)
        {
            if(data[1] == 224)
                return new HK708AuxReport(data);
            else
                return new TabletReport(data);
        }
    }
}