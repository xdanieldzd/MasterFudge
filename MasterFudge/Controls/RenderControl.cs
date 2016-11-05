using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.ComponentModel;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace MasterFudge.Controls
{
    /* Abridged from Cobalt's RenderControl */

    [DesignTimeVisible(true), ToolboxItem(true)]
    public class RenderControl : GLControl, IComponent
    {
        public event EventHandler<EventArgs> Render;

        static bool IsRuntime
        {
            get { return (LicenseManager.UsageMode != LicenseUsageMode.Designtime); }
        }

        static bool IsReady
        {
            get { return (IsRuntime && (GraphicsContext.CurrentContext != null)); }
        }

        static int GetMaxAASamples()
        {
            List<int> maxSamples = new List<int>();
            int retVal = 0;
            try
            {
                int samples = 0;
                do
                {
                    GraphicsMode mode = new GraphicsMode(GraphicsMode.Default.ColorFormat, GraphicsMode.Default.Depth, GraphicsMode.Default.Stencil, samples);
                    if (!maxSamples.Contains(mode.Samples)) maxSamples.Add(samples);
                    samples += 2;
                }
                while (samples <= 32);
            }
            finally
            {
                retVal = maxSamples.Last();
            }
            return retVal;
        }

        public RenderControl() : base(new GraphicsMode(GraphicsMode.Default.ColorFormat, GraphicsMode.Default.Depth, GraphicsMode.Default.Stencil, GetMaxAASamples()))
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

            Application.Idle += ((s, e) =>
            {
                if (IsReady) Invalidate();
            });
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (!IsReady)
            {
                e.Graphics.Clear(BackColor);
                using (Pen pen = new Pen(Color.Red, 3.0f))
                {
                    e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                    e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    e.Graphics.DrawLine(pen, Point.Empty, new Point(ClientRectangle.Right, ClientRectangle.Bottom));
                    e.Graphics.DrawLine(pen, new Point(0, ClientRectangle.Bottom), new Point(ClientRectangle.Right, 0));
                }
                return;
            }

            OnRender(EventArgs.Empty);

            SwapBuffers();
        }

        protected virtual void OnRender(EventArgs e)
        {
            if (!IsReady) return;

            Render?.Invoke(this, e);
        }

        protected override void OnLoad(EventArgs e)
        {
            if (!IsReady) return;

            GL.ClearColor(BackColor);

            base.OnLoad(e);

            OnResize(EventArgs.Empty);
        }

        protected override void OnResize(EventArgs e)
        {
            if (!IsReady) return;

            base.OnResize(e);
        }
    }
}
