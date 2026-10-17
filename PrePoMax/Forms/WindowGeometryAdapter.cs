using System.Drawing;
using System.Windows.Forms;

namespace PrePoMax
{
    public static class WindowGeometryExtensions
    {
        public static void SaveFormSize(this GeneralSettings settings, Form form)
        {
            settings.SaveFormSize(new WindowGeometryAdapter(form));
        }

        public static void ApplyFormSize(this GeneralSettings settings, Form form)
        {
            settings.ApplyFormSize(new WindowGeometryAdapter(form));
        }
    }

    internal sealed class WindowGeometryAdapter : IWindowGeometry
    {
        private readonly Form form;
        public WindowGeometryAdapter(Form form)
        {
            this.form = form;
        }

        public WindowState WindowState
        {
            get
            {
                return (WindowState)(int)form.WindowState;
            }

            set
            {
                form.WindowState = (FormWindowState)(int)value;
            }
        }

        public Rectangle RestoreBounds
        {
            get
            {
                return form.RestoreBounds;
            }
        }

        public Rectangle ScreenBounds
        {
            get
            {
                return Screen.FromControl(form).Bounds;
            }
        }

        public Size MinimumSize
        {
            get
            {
                return form.MinimumSize;
            }
        }

        public Size Size
        {
            get
            {
                return form.Size;
            }
        }

        public Point Location
        {
            get
            {
                return form.Location;
            }
        }

        public void SetBounds(int x, int y, int width, int height)
        {
            form.SetBounds(x, y, width, height);
        }
    }
}
