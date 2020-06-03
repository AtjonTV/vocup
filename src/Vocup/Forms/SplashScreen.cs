using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Vocup.Util;

namespace Vocup.Forms
{
    public partial class SplashScreen : Form
    {
        public SplashScreen()
        {
            InitializeComponent();
        }

        private void SplashScreen_Paint(object sender, PaintEventArgs e)
        {
            using (var brush =
                new LinearGradientBrush(new Point(0, 0), new Point(0, Height), Color.White, Color.LightGray))
            using (var grayPen = new Pen(Brushes.Gray, 2.0f))
            {
                // Draw background
                e.Graphics.FillRectangle(brush, 0, 0, Width, Height);

                // Draw window border
                e.Graphics.DrawRectangle(grayPen, 0, 0, Width, Height);
            }
        }

        private void SplashScreen_Load(object sender, EventArgs e)
        {
            // Display version and copyright

            LbVersion.Text = string.Format(LbVersion.Text, AppInfo.GetVersion(3));
            LbCopyright.Text = AppInfo.CopyrightInfo;
        }
    }
}