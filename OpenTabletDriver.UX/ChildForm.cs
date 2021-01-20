using Eto.Drawing;
using Eto.Forms;

namespace OpenTabletDriver.UX
{
    using static App;

    public abstract class ChildForm : Form
    {
        protected ChildForm()
        {
            Owner = Application.Instance.MainForm;
            Icon = Logo.WithSize(Logo.Size);
        }

        public new void Show()
        {
            var x = Owner.Location.X + (Owner.ClientSize.Width / 2);
            var y = Owner.Location.Y + (Owner.ClientSize.Height / 2);
            var center = new PointF(x, y);

            Location = new Point((int)(center.X - (ClientSize.Width / 2)), (int)(center.Y - (ClientSize.Height / 2)));
            base.Show();
        }
    }
}
