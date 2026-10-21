using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;

namespace FeederCopy.App.Utils
{
    internal sealed class DarkComboPainter : NativeWindow
    {
        private const int WmPaint = 0x000F;
        private const int ArrowHalfWidth = 4;
        private static readonly ConditionalWeakTable<ComboBox, DarkComboPainter> Painters = new();
        private readonly ComboBox _combo;

        private DarkComboPainter(ComboBox combo)
        {
            _combo = combo;
            combo.HandleCreated += (_, _) => AssignHandle(combo.Handle);
            combo.HandleDestroyed += (_, _) => ReleaseHandle();
            if (combo.IsHandleCreated)
                AssignHandle(combo.Handle);
        }

        public static void Attach(ComboBox combo) => Painters.GetValue(combo, c => new DarkComboPainter(c));

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (RepaintAfter.Contains(m.Msg))
                PaintFrame();
        }

        private static readonly HashSet<int> RepaintAfter =
        [
            WmPaint,
            0x0007,
            0x0008,
            0x0200,
            0x02A3,
            0x0201,
            0x0202,
            0x0111,
        ];

        private void PaintFrame()
        {
            Rectangle bounds = _combo.ClientRectangle;
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;
            int buttonWidth = SystemInformation.VerticalScrollBarWidth;
            var button = new Rectangle(bounds.Right - buttonWidth - 1, bounds.Top + 1, buttonWidth, bounds.Height - 2);
            using Graphics g = Graphics.FromHwnd(_combo.Handle);
            using (var back = new SolidBrush(AppTheme.ControlSurface))
                g.FillRectangle(back, button);
            using (var border = new Pen(_combo.Focused ? AppTheme.Highlight : AppTheme.Border))
                g.DrawRectangle(border, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            int cx = button.Left + button.Width / 2;
            int cy = button.Top + button.Height / 2;
            using var arrow = new Pen(_combo.Enabled ? AppTheme.TextSecondary : AppTheme.TextMuted, 1.6F);
            g.DrawLines(arrow, new[]
            {
                new Point(cx - ArrowHalfWidth, cy - 2),
                new Point(cx, cy + 2),
                new Point(cx + ArrowHalfWidth, cy - 2),
            });
        }
    }
}
