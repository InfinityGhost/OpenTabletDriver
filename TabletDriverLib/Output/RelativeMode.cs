using System;
using TabletDriverLib.Interop;
using TabletDriverLib.Interop.Cursor;
using TabletDriverPlugin;
using TabletDriverPlugin.Tablet;

namespace TabletDriverLib.Output
{
    public class RelativeMode : IRelativeMode
    {
        public float XSensitivity { set; get; }
        public float YSensitivity { set; get; }
        public IFilter Filter { set; get; }
        public TabletProperties TabletProperties { set; get; }
        public TimeSpan ResetTime { set; get; } = TimeSpan.FromMilliseconds(100);

        private ICursorHandler CursorHandler { set; get; } = Platform.CursorHandler;
        private ITabletReport _lastReport;
        private DateTime _lastReceived;
        private Point _lastPosition;

        public void Read(IDeviceReport report)
        {
            if (report is ITabletReport tabletReport)
                Position(tabletReport);
        }

        public void Position(ITabletReport report)
        {
            if (report.Lift <= TabletProperties.MinimumRange)
                return;
            
            var difference = DateTime.Now - _lastReceived;
            if (difference > ResetTime && _lastReceived != DateTime.MinValue)
            {
                _lastReport = null;
                _lastPosition = null;
            }

            if (_lastReport != null)
            {
                var pos = new Point(report.Position.X - _lastReport.Position.X, report.Position.Y - _lastReport.Position.Y);
                
                // Normalize (ratio of 1)
                pos.X /= TabletProperties.MaxX;
                pos.Y /= TabletProperties.MaxY;

                // Scale to tablet dimensions (mm)
                pos.X *= TabletProperties.Width;
                pos.Y *= TabletProperties.Height;

                // Sensitivity setting
                pos.X *= XSensitivity;
                pos.Y *= YSensitivity;
                
                // Translate by cursor position
                pos += GetCursorPosition();

                // Filter
                if (Filter != null)
                    pos = Filter.Filter(pos);

                CursorHandler.SetCursorPosition(pos);
                _lastPosition = pos;
            }
            
            _lastReport = report;
            _lastReceived = DateTime.Now;
        }

        private Point GetCursorPosition()
        {
            if (_lastPosition != null)
                return _lastPosition;
            else
                return CursorHandler.GetCursorPosition();
        }
    }
}