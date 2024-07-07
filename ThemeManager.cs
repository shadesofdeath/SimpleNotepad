using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SimpleNote
{
    public static class ThemeManager
    {
        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attribute, ref int pvAttribute, int cbAttribute);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attribute, ref COLORREF pvAttribute, int cbAttribute);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool SetPropA(IntPtr hWnd, string lpString, IntPtr hData);


        public enum DWMWINDOWATTRIBUTE
        {
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
            DWMWA_CAPTION_COLOR = 35,
            DWMWA_BORDER_COLOR = 34,
            DWMWA_MICA_EFFECT = 1029,
            DWMWA_TEXT_COLOR = 36,
            DWMWA_SCROLLBAR_PREFERENCE = 1017,
        }

        public enum PreferredAppMode
        {
            Default,
            AllowDark,
            ForceDark,
            ForceLight,
            Max
        }

        private const int GWL_STYLE = -16;
        private const int WS_HSCROLL = 0x00100000;
        private const int WS_VSCROLL = 0x00200000;


        [StructLayout(LayoutKind.Sequential)]
        public struct COLORREF
        {
            public uint ColorDWORD;

            public COLORREF(Color color)
            {
                ColorDWORD = (uint)color.R + (((uint)color.G) << 8) + (((uint)color.B) << 16);
            }
        }

        public static void ApplyTheme(Form form, bool isDarkMode, bool isLightGrayMode)
        {
            SetWindowTheme(form.Handle, isDarkMode ? "DarkMode_Explorer" : "", null);

            Color backgroundColor, textColor;
            if (isLightGrayMode)
            {
                backgroundColor = Color.FromArgb(210, 210, 210);
                textColor = Color.FromArgb(60, 60, 60);
            }
            else if (isDarkMode)
            {
                backgroundColor = Color.FromArgb(30, 30, 30);
                textColor = Color.FromArgb(220, 220, 220);
            }
            else
            {
                backgroundColor = Color.FromArgb(250, 250, 250);
                textColor = Color.FromArgb(30, 30, 30);
            }

            form.BackColor = backgroundColor;
            form.ForeColor = textColor;

            int useImmersiveDarkMode = isDarkMode ? 1 : 0;
            DwmSetWindowAttribute(form.Handle, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int));

            int micaAttribute = 0;
            DwmSetWindowAttribute(form.Handle, DWMWINDOWATTRIBUTE.DWMWA_MICA_EFFECT, ref micaAttribute, sizeof(int));

            COLORREF color = new COLORREF(backgroundColor);
            DwmSetWindowAttribute(form.Handle, DWMWINDOWATTRIBUTE.DWMWA_CAPTION_COLOR, ref color, Marshal.SizeOf(typeof(COLORREF)));

            UpdateControls(form, isDarkMode, isLightGrayMode);
        }
        private static void UpdateControls(Control control, bool isDarkMode, bool isLightGrayMode)
        {
            Color backgroundColor, textColor;
            if (isLightGrayMode)
            {
                backgroundColor = Color.FromArgb(220, 220, 220);
                textColor = Color.FromArgb(60, 60, 60);
            }
            else if (isDarkMode)
            {
                backgroundColor = Color.FromArgb(32, 32, 32);
                textColor = Color.FromArgb(220, 220, 220);
            }
            else
            {
                backgroundColor = Color.FromArgb(250, 250, 250);
                textColor = Color.FromArgb(30, 30, 30);
            }

            foreach (Control childControl in control.Controls)
            {
                if (childControl is MenuStrip menuStrip)
                {
                    if (menuStrip is CustomMenuStrip customMenuStrip)
                    {
                        customMenuStrip.SetTheme(isDarkMode, isLightGrayMode);
                    }
                    else
                    {
                        menuStrip.BackColor = backgroundColor;
                        menuStrip.ForeColor = textColor;
                    }
                }
                else if (childControl is StatusStrip statusStrip)
                {
                    if (statusStrip is CustomStatusStrip customStatusStrip)
                    {
                        customStatusStrip.SetTheme(isDarkMode, isLightGrayMode);
                    }
                    else
                    {
                        statusStrip.BackColor = backgroundColor;
                        statusStrip.ForeColor = textColor;
                    }
                }
                else if (childControl is RichTextBox rtb)
                {
                    SetScrollBarColors(rtb, isDarkMode, isLightGrayMode);
                }
                else
                {
                    childControl.BackColor = backgroundColor;
                    childControl.ForeColor = textColor;
                }

                UpdateControls(childControl, isDarkMode, isLightGrayMode);
            }
        }


        public static void SetTextBoxColors(TextBox textBox, bool isDarkMode, bool isLightGrayMode)
        {
            if (Environment.OSVersion.Version.Major >= 10)
            {
                SetWindowTheme(textBox.Handle, isDarkMode ? "DarkMode_Explorer" : "Explorer", null);
                int preference = isDarkMode ? 1 : 0;
                DwmSetWindowAttribute(textBox.Handle, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, ref preference, sizeof(int));
            }

            Color backgroundColor, textColor;
            if (isLightGrayMode)
            {
                backgroundColor = Color.FromArgb(220, 220, 220);
                textColor = Color.FromArgb(50, 50, 50);
            }
            else if (isDarkMode)
            {
                backgroundColor = Color.FromArgb(32, 32, 32);
                textColor = Color.WhiteSmoke;
            }
            else
            {
                backgroundColor = Color.FromArgb(250, 250, 250);
                textColor = Color.FromArgb(0, 0, 0);
            }

            textBox.BackColor = backgroundColor;
            textBox.ForeColor = textColor;

            int style = GetWindowLong(textBox.Handle, GWL_STYLE);
            SetWindowLong(textBox.Handle, GWL_STYLE, style | WS_HSCROLL | WS_VSCROLL);
            SetWindowLong(textBox.Handle, GWL_STYLE, style);
            textBox.Invalidate();
        }
        public static void SetScrollBarColors(Control control, bool isDarkMode, bool isLightGrayMode)
        {
            if (Environment.OSVersion.Version.Major >= 10)
            {
                SetWindowTheme(control.Handle, isDarkMode ? "DarkMode_Explorer" : "Explorer", null);
                int preference = isDarkMode ? 1 : 0;
                DwmSetWindowAttribute(control.Handle, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, ref preference, sizeof(int));

                int scrollbarPreference = isDarkMode ? 2 : 1; // 2 for dark, 1 for light
                DwmSetWindowAttribute(control.Handle, DWMWINDOWATTRIBUTE.DWMWA_SCROLLBAR_PREFERENCE, ref scrollbarPreference, sizeof(int));
            }

            Color backgroundColor, textColor;
            if (isLightGrayMode)
            {
                backgroundColor = Color.FromArgb(220, 220, 220);
                textColor = Color.FromArgb(60, 60, 60);
            }
            else if (isDarkMode)
            {
                backgroundColor = Color.FromArgb(30, 30, 30);
                textColor = Color.FromArgb(220, 220, 220);
            }
            else
            {
                backgroundColor = Color.FromArgb(250, 250, 250);
                textColor = Color.FromArgb(30, 30, 30);
            }

            control.BackColor = backgroundColor;
            control.ForeColor = textColor;
        }
        private static void SetScrollBarColors(RichTextBox rtb, bool isDarkMode, bool isLightGrayMode)
        {
            if (Environment.OSVersion.Version.Major >= 10)
            {
                SetWindowTheme(rtb.Handle, isDarkMode ? "DarkMode_Explorer" : "Explorer", null);
                int preference = isDarkMode ? 1 : 0;
                DwmSetWindowAttribute(rtb.Handle, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, ref preference, sizeof(int));
            }

            rtb.BackColor = isLightGrayMode ? Color.FromArgb(220, 220, 220) :
                            isDarkMode ? Color.FromArgb(32, 32, 32) : Color.FromArgb(250, 250, 250);
            rtb.ForeColor = isLightGrayMode ? Color.FromArgb(50, 50, 50) :
                            isDarkMode ? Color.WhiteSmoke : Color.FromArgb(0, 0, 0); // Örneğin, Light mod için siyah metin rengi
        }
        private static void SetScrollBarColors(RichTextBox rtb, bool dark)
        {
            if (Environment.OSVersion.Version.Major >= 10)
            {
                int darkModeForScrollbars = dark ? 1 : 0;
                SetWindowTheme(rtb.Handle, "DarkMode_Explorer", null);
                DwmSetWindowAttribute(rtb.Handle, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkModeForScrollbars, sizeof(int));
            }
            else
            {
                rtb.BackColor = dark ? Color.FromArgb(32, 32, 32) : Color.FromArgb(250, 250, 250);
            }
        }
    }

    public class CustomMenuStrip : MenuStrip
    {
        private bool isDarkMode = false;
        private bool isLightGrayMode = false;

        public Color BorderColor { get; set; } = Color.Transparent;
        public int BorderWidth { get; set; } = 1;

        public CustomMenuStrip()
        {
            this.Renderer = new CustomMenuRenderer();
            BorderColor = Color.FromArgb(16, 60, 60, 60);
        }

        public void SetTheme(bool dark, bool lightGray)
        {
            isDarkMode = dark;
            isLightGrayMode = lightGray;

            Color backgroundColor, textColor;
            if (lightGray)
            {
                backgroundColor = Color.FromArgb(210, 210, 210);
                textColor = Color.FromArgb(60, 60, 60);
            }
            else if (dark)
            {
                backgroundColor = Color.FromArgb(30, 30, 30);
                textColor = Color.FromArgb(220, 220, 220);
            }
            else
            {
                backgroundColor = Color.FromArgb(250, 250, 250);
                textColor = Color.FromArgb(30, 30, 30);
            }

            this.BackColor = backgroundColor;
            this.ForeColor = textColor;
            this.BorderColor = Color.FromArgb(20, textColor);

            (this.Renderer as CustomMenuRenderer).SetTheme(dark, lightGray);
            this.Refresh();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(this.BackColor);
            base.OnPaint(e);

            using (Pen pen = new Pen(BorderColor, BorderWidth))
            {
                e.Graphics.DrawLine(pen, 0, this.Height - BorderWidth, this.Width, this.Height - BorderWidth);
            }

            // Sol kenar boşluğunu kaldır
            Padding = new Padding(0, Padding.Top, Padding.Right, Padding.Bottom);
        }
    }

    public class CustomMenuRenderer : ToolStripProfessionalRenderer
    {
        private bool isDarkMode = false;
        private bool isLightGrayMode = false;

        public CustomMenuRenderer() : base(new CustomColorTable())
        {
        }

        public void SetTheme(bool dark, bool lightGray)
        {
            isDarkMode = dark;
            isLightGrayMode = lightGray;
            (this.ColorTable as CustomColorTable).SetTheme(dark, lightGray);
        }
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = isLightGrayMode ? Color.FromArgb(30, 30, 30) :
                          isDarkMode ? Color.WhiteSmoke : SystemColors.ControlText;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected && !e.Item.Pressed)
                return;

            Rectangle rc = new Rectangle(Point.Empty, e.Item.Size);
            Color c = isLightGrayMode ? Color.FromArgb(220, 220, 220) :
                      isDarkMode ? Color.FromArgb(60, 60, 60) : Color.FromArgb(220, 220, 220);
            using (SolidBrush brush = new SolidBrush(c))
            {
                e.Graphics.FillRectangle(brush, rc);
            }
        }
        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            Rectangle rect = e.ImageRectangle;
            rect.Inflate(-4, -4); // Daha küçük bir alan kullan

            Color tickColor = isDarkMode ? Color.WhiteSmoke : Color.Black;
            using (Pen pen = new Pen(tickColor, 1.8f)) // Daha ince çizgi
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                // Daha yuvarlak ve sevimli bir tik işareti çiz
                e.Graphics.DrawLines(pen, new Point[]
                {
            new Point(rect.Left, rect.Bottom - rect.Height / 2),
            new Point(rect.Left + rect.Width / 5, rect.Bottom - rect.Height / 4),
            new Point(rect.Right - rect.Width / 4, rect.Top + rect.Height / 4)
                });
            }
        }
    }

    public class CustomColorTable : ProfessionalColorTable
    {
        private bool isDarkMode = false;
        private bool isLightGrayMode = false;
        public void SetTheme(bool dark, bool lightGray)
        {
            isDarkMode = dark;
            isLightGrayMode = lightGray;
        }
        public override Color MenuItemSelected => isLightGrayMode ? Color.FromArgb(220, 220, 220) :
                                                  isDarkMode ? Color.FromArgb(60, 60, 60) : Color.FromArgb(220, 220, 220);
        public override Color MenuItemSelectedGradientBegin => isDarkMode ? Color.FromArgb(60, 60, 60) : Color.FromArgb(220, 220, 220);
        public override Color MenuItemSelectedGradientEnd => isDarkMode ? Color.FromArgb(60, 60, 60) : Color.FromArgb(220, 220, 220);
        public override Color MenuItemPressedGradientBegin => isDarkMode ? Color.FromArgb(60, 60, 60) : Color.FromArgb(220, 220, 220);
        public override Color MenuItemPressedGradientEnd => isDarkMode ? Color.FromArgb(60, 60, 60) : Color.FromArgb(220, 220, 220);
        public override Color MenuStripGradientBegin => isDarkMode ? Color.FromArgb(32, 32, 32) : Color.FromArgb(250, 250, 250);
        public override Color MenuStripGradientEnd => isDarkMode ? Color.FromArgb(32, 32, 32) : Color.FromArgb(250, 250, 250);
        public override Color ToolStripDropDownBackground => isDarkMode ? Color.FromArgb(32, 32, 32) : Color.FromArgb(250, 250, 250);
        public override Color ImageMarginGradientBegin => isDarkMode ? Color.FromArgb(32, 32, 32) : Color.FromArgb(250, 250, 250);
        public override Color ImageMarginGradientMiddle => isDarkMode ? Color.FromArgb(32, 32, 32) : Color.FromArgb(250, 250, 250);
        public override Color ImageMarginGradientEnd => isDarkMode ? Color.FromArgb(32, 32, 32) : Color.FromArgb(250, 250, 250);

        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuBorder => Color.Transparent;
        public override Color MenuItemPressedGradientMiddle => isDarkMode ? Color.FromArgb(60, 60, 60) : Color.FromArgb(220, 220, 220);
        public override Color ToolStripBorder => Color.Transparent;
    }

    public class CustomStatusStrip : StatusStrip
    {
        private bool isDarkMode = false;
        private bool isLightGrayMode = false;
        private int paddingLeftRight = 12; // Sol ve sağdan margin değerini belirler

        public CustomStatusStrip()
        {
            this.Renderer = new ToolStripProfessionalRenderer(new CustomColorTable());
            this.Height = 30; // Status bar'ın yüksekliğini ayarlar
        }

        public void SetTheme(bool dark, bool lightGray)
        {
            isDarkMode = dark;
            isLightGrayMode = lightGray;

            Color backgroundColor, textColor, borderColor;
            if (lightGray)
            {
                backgroundColor = Color.FromArgb(220, 220, 220);
                textColor = Color.FromArgb(30, 30, 30);
                borderColor = Color.FromArgb(200, 200, 200);
            }
            else if (dark)
            {
                backgroundColor = Color.FromArgb(40, 40, 40);
                textColor = Color.FromArgb(220, 220, 220);
                borderColor = Color.FromArgb(64, 64, 64);
            }
            else
            {
                backgroundColor = Color.FromArgb(245, 245, 245);
                textColor = Color.FromArgb(30, 30, 30);
                borderColor = Color.FromArgb(200, 200, 200);
            }

            this.BackColor = backgroundColor;
            this.ForeColor = textColor;

            if (this.Renderer is ToolStripProfessionalRenderer renderer)
            {
                if (renderer.ColorTable is CustomColorTable colorTable)
                {
                    colorTable.SetTheme(dark, lightGray);
                }
            }

            this.Refresh();
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Color borderColor = isLightGrayMode ? Color.FromArgb(200, 200, 200) :
                                isDarkMode ? Color.FromArgb(64, 64, 64) : Color.FromArgb(200, 200, 200);

            using (Pen pen = new Pen(borderColor))
            {
                e.Graphics.DrawLine(pen, 0, 0, this.Width, 0);
            }
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);

            foreach (ToolStripItem item in this.Items)
            {
                if (item is ToolStripStatusLabel)
                {
                    item.Padding = new Padding(paddingLeftRight, item.Padding.Top, paddingLeftRight, item.Padding.Bottom);
                }
            }
        }
    }
}