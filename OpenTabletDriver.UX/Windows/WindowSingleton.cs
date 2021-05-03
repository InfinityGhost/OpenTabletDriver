using Eto.Forms;

namespace OpenTabletDriver.UX.Windows
{
    public class WindowSingleton<T> where T : Window, new()
    {
        private T window;

        public void Show()
        {
            if (window == null)
            {
                window = new T();
                window.Closed += (_, _) => window = null;
            }

            switch (window)
            {
                case DesktopForm desktopForm:
                    desktopForm.Show();
                    break;
                case Form form:
                    form.Show();
                    break;
                case Dialog dialog:
                    dialog.ShowModal();
                    break;
            }

            window.Focus();
        }
    }
}