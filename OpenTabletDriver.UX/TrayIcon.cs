﻿using Eto.Forms;

namespace OpenTabletDriver.UX
{
    public class TrayIcon
    {
        public TrayIcon(MainForm window)
        {
            var showWindow = new ButtonMenuItem
            {
                Text = "Show Window"
            };
            showWindow.Click += (sender, e) =>
            {
                window.Show();
                window.WindowState = WindowState.Normal;
                window.BringToFront();
                window.WindowStyle = WindowStyle.Default;
            };

            var restart = new ButtonMenuItem
            {
                Text = "Restart"
            };
            restart.Click += (sender, e) => Application.Instance.Restart();

            var close = new ButtonMenuItem
            {
                Text = "Close"
            };
            close.Click += (sender, e) => window.Close();

            var indicator = new TrayIndicator
            {
                Title = "OpenTabletDriver",
                Image = App.Logo,
                Menu = new ContextMenu
                {
                    Items =
                    {
                        showWindow,
                        restart,
                        close
                    }
                }
            };
			indicator.Activated += (object sender, System.EventArgs e) =>
            {
                window.Show();
                window.WindowState = WindowState.Normal;
                window.BringToFront();
                window.WindowStyle = WindowStyle.Default;
            };
            indicator.Show();
        }
    }
}