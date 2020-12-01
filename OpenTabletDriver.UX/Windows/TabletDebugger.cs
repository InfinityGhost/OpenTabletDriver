﻿using System;
using Eto.Drawing;
using Eto.Forms;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Tablet.Touch;
using OpenTabletDriver.Tablet;

namespace OpenTabletDriver.UX.Windows
{
    public class TabletDebugger : Form
    {
        public TabletDebugger()
        {
            Title = "Tablet Debugger";

            rawTabCtrl = new GroupBox
            {
                Text = "Raw Tablet Data",
                Padding = App.GroupBoxPadding
            };
            
            tabReportCtrl = new GroupBox
            {
                Text = "Tablet Report",
                Padding = App.GroupBoxPadding
            };

            rawAuxCtrl = new GroupBox
            {
                Text = "Raw Aux Data",
                Padding = App.GroupBoxPadding
            };
            
            auxReportCtrl = new GroupBox
            {
                Text = "Aux Report",
                Padding = App.GroupBoxPadding
            };

            rawTouchCtrl = new GroupBox
            {
                Text = "Raw Touch Data",
                Padding = App.GroupBoxPadding
            };

            touchReportCtrl = new GroupBox
            {
                Text = "Touch Report",
                Padding = App.GroupBoxPadding
            };

            reportRateCtrl = new GroupBox
            {
                Text = "Report Rate",
                Padding = App.GroupBoxPadding
            };

            rawTabCtrl.Content = rawTabText = new Label
            {
                Font = new Font(FontFamilies.Monospace, textSize)
            };

            tabReportCtrl.Content = tabReportText = new Label
            {
                Font = new Font(FontFamilies.Monospace, textSize)
            };

            rawAuxCtrl.Content = rawAuxText = new Label
            {
                Font = new Font(FontFamilies.Monospace, textSize)
            };

            auxReportCtrl.Content = auxReportText = new Label
            {
                Font = new Font(FontFamilies.Monospace, textSize)
            };

            rawTouchCtrl.Content = rawTouchText = new Label
            {
                Font = new Font(FontFamilies.Monospace, textSize)
            };

            touchReportCtrl.Content = touchReportText = new Label
            {
                Font = new Font(FontFamilies.Monospace, textSize)
            };

            reportRateCtrl.Content = reportRateText = new Label
            {
                Font = new Font(FontFamilies.Monospace, textSize)
            };

            var mainLayout = new TableLayout
            {
                Width = 700,
                Height = 600,
                Spacing = new Size(5, 5),
                Rows =
                {
                    new TableRow
                    {
                        Cells =
                        {
                            new TableCell(rawTabCtrl, true),
                            new TableCell(tabReportCtrl, true)
                        },
                        ScaleHeight = true
                    },
                    new TableRow
                    {
                        Cells =
                        {
                            new TableCell(rawAuxCtrl, true),
                            new TableCell(auxReportCtrl, true)
                        },
                        ScaleHeight = true
                    },
                    new TableRow
                    {
                        Cells =
                        {
                            new TableCell(rawTouchCtrl, true),
                            new TableCell(touchReportCtrl, true)
                        },
                        ScaleHeight = true
                    }
                }
            };

            this.Content = new StackLayout
            {
                Padding = 5,
                Spacing = 5,
                Items = 
                {
                    new StackLayoutItem(mainLayout, HorizontalAlignment.Stretch, true),
                    new StackLayoutItem(reportRateCtrl, HorizontalAlignment.Stretch)
                }
            };

            InitializeAsync();
        }

        private void InitializeAsync()
        {
            App.Driver.Instance.TabletReport += HandleReport;
            App.Driver.Instance.AuxReport += HandleReport;
            App.Driver.Instance.TouchReport += HandleReport;
            App.Driver.Instance.SetTabletDebug(true);
            this.Closing += (sender, e) =>
            {
                App.Driver.Instance.TabletReport -= HandleReport;
                App.Driver.Instance.AuxReport -= HandleReport;
                App.Driver.Instance.TouchReport -= HandleReport;
                App.Driver.Instance.SetTabletDebug(false);
            };
        }

        private GroupBox rawTabCtrl, tabReportCtrl, rawAuxCtrl, auxReportCtrl, rawTouchCtrl, touchReportCtrl, reportRateCtrl;
        private Label rawTabText, tabReportText, rawAuxText, auxReportText, rawTouchText, touchReportText, reportRateText;
        private float textSize = 10;
        private float reportRate;
        private DateTime lastTime = DateTime.UtcNow;

        private void HandleReport(object sender, IDeviceReport report)
        {
            if (report is ITabletReport tabletReport)
            {
                Application.Instance.AsyncInvoke(() => 
                {
                    var now = DateTime.UtcNow;
                    reportRate += (float)(((now - lastTime).TotalMilliseconds - reportRate) / 50);
                    lastTime = now;
                    rawTabText.Text = tabletReport?.StringFormat(true);
                    tabReportText.Text = tabletReport?.StringFormat(false).Replace(", ", Environment.NewLine);
                    reportRateText.Text = $"{(uint)(1000 / reportRate)}hz";
                });
            }
            if (report is IAuxReport auxReport)
            {
                Application.Instance.AsyncInvoke(() => 
                {
                    rawAuxText.Text = auxReport?.StringFormat(true);
                    auxReportText.Text = auxReport?.StringFormat(false).Replace(", ", Environment.NewLine);
                });
            }
            if (report is ITouchReport touchReport)
            {
                Application.Instance.AsyncInvoke(() =>
                {
                    rawTouchText.Text = touchReport?.StringFormat(true);
                    touchReportText.Text = touchReport?.StringFormat(false).Replace(", ", Environment.NewLine);
                });
            }
        }
    }
}