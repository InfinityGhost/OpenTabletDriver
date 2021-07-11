using System.Numerics;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Tablet;

namespace OpenTabletDriver.Vendors.Wacom.IntuosV1
{
    public struct IntuosV1ToolReport : IToolReport, IEraserReport, IProximityReport
    {
        public IntuosV1ToolReport(byte[] report)
        {
            Raw = report;

            Serial = (ulong)(((report[3] & 0x0f) << 28) +
                (report[4] << 20) + (report[5] << 12) +
                (report[6] <<  4) + (report[7] >>  4));

            RawToolID = (uint)((report[2]   <<  4) | ( report[3]         >> 4) |
                       ((report[7] & 0x0f) << 16) | ((report[8] & 0xf0) << 8));

            Tool = report[3].IsBitSet(7) ? ToolType.Eraser : ToolType.Pen;

            Eraser = (Tool == ToolType.Eraser);

            NearProximity = report[1].IsBitSet(6);
            HoverDistance = (uint)report[9] >> 2;
        }

        public byte[] Raw { set; get; }
        public ulong Serial { set; get; }
        public uint RawToolID { set; get; }
        public ToolType Tool { set; get; }
        public bool Eraser { set; get; }
        public bool NearProximity { set; get; }
        public uint HoverDistance { set; get; }
    }
}
