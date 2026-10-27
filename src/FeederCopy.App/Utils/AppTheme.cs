using System.Reflection;
using System.Runtime.InteropServices;
using FeederCopy.Core.Models.Settings;

namespace FeederCopy.App.Utils
{
    public static class AppTheme
    {
        private sealed class Palette
        {
            public required Color Primary { get; init; }
            public required Color PrimaryDark { get; init; }
            public required Color PrimaryLight { get; init; }
            public required Color Accent { get; init; }
            public required Color AccentLight { get; init; }
            public required Color Highlight { get; init; }
            public required Color Background { get; init; }
            public required Color CardBackground { get; init; }
            public required Color SurfaceAlt { get; init; }
            public required Color ControlSurface { get; init; }
            public required Color InputBackground { get; init; }
            public required Color OutlineButtonBack { get; init; }
            public required Color ButtonBorder { get; init; }
            public required Color Border { get; init; }
            public required Color AltRow { get; init; }
            public required Color TextPrimary { get; init; }
            public required Color TextSecondary { get; init; }
            public required Color TextMuted { get; init; }
            public required Color Success { get; init; }
            public required Color Warning { get; init; }
            public required Color WarningLight { get; init; }
            public required Color Danger { get; init; }
            public required Color DangerHover { get; init; }
            public required Color DangerPressed { get; init; }
            public required Color ProblemFill { get; init; }
            public required Color ProblemText { get; init; }
        }

        private static readonly Palette LightPalette = new()
        {
            Primary = Color.FromArgb(20, 77, 91),
            PrimaryDark = Color.FromArgb(11, 43, 57),
            PrimaryLight = Color.FromArgb(221, 236, 239),
            Accent = Color.FromArgb(40, 116, 133),
            AccentLight = Color.FromArgb(229, 244, 246),
            Highlight = Color.FromArgb(20, 77, 91),
            Background = Color.FromArgb(240, 244, 246),
            CardBackground = Color.White,
            SurfaceAlt = Color.FromArgb(235, 242, 244),
            ControlSurface = Color.FromArgb(250, 253, 253),
            InputBackground = Color.White,
            OutlineButtonBack = Color.FromArgb(247, 251, 252),
            ButtonBorder = Color.FromArgb(177, 195, 202),
            Border = Color.FromArgb(207, 218, 222),
            AltRow = Color.FromArgb(247, 250, 251),
            TextPrimary = Color.FromArgb(22, 36, 43),
            TextSecondary = Color.FromArgb(64, 82, 91),
            TextMuted = Color.FromArgb(104, 123, 132),
            Success = Color.FromArgb(35, 126, 93),
            Warning = Color.FromArgb(176, 111, 28),
            WarningLight = Color.FromArgb(250, 237, 211),
            Danger = Color.FromArgb(177, 58, 58),
            DangerHover = Color.FromArgb(194, 70, 70),
            DangerPressed = Color.FromArgb(138, 40, 40),
            ProblemFill = Color.FromArgb(255, 242, 204),
            ProblemText = Color.FromArgb(22, 36, 43),
        };

        private static readonly Palette DarkPalette = new()
        {
            Primary = Color.FromArgb(32, 120, 138),
            PrimaryDark = Color.FromArgb(18, 78, 92),
            PrimaryLight = Color.FromArgb(30, 70, 80),
            Accent = Color.FromArgb(48, 144, 163),
            AccentLight = Color.FromArgb(36, 60, 68),
            Highlight = Color.FromArgb(110, 200, 216),
            Background = Color.FromArgb(16, 23, 27),
            CardBackground = Color.FromArgb(27, 37, 43),
            SurfaceAlt = Color.FromArgb(36, 49, 56),
            ControlSurface = Color.FromArgb(32, 44, 51),
            InputBackground = Color.FromArgb(22, 31, 36),
            OutlineButtonBack = Color.FromArgb(32, 44, 51),
            ButtonBorder = Color.FromArgb(70, 90, 99),
            Border = Color.FromArgb(52, 68, 76),
            AltRow = Color.FromArgb(31, 42, 48),
            TextPrimary = Color.FromArgb(228, 236, 240),
            TextSecondary = Color.FromArgb(172, 188, 196),
            TextMuted = Color.FromArgb(126, 144, 153),
            Success = Color.FromArgb(78, 196, 146),
            Warning = Color.FromArgb(230, 162, 64),
            WarningLight = Color.FromArgb(66, 52, 26),
            Danger = Color.FromArgb(236, 104, 104),
            DangerHover = Color.FromArgb(214, 88, 88),
            DangerPressed = Color.FromArgb(170, 60, 60),
            ProblemFill = Color.FromArgb(84, 64, 24),
            ProblemText = Color.FromArgb(250, 226, 170),
        };

        private static Palette _palette = LightPalette;

        public static bool IsDark { get; private set; }

        public static void Use(ThemeMode theme)
        {
            IsDark = theme == ThemeMode.Dark;
            _palette = IsDark ? DarkPalette : LightPalette;
        }

        public static Color Primary => _palette.Primary;
        public static Color PrimaryDark => _palette.PrimaryDark;
        public static Color PrimaryLight => _palette.PrimaryLight;
        public static Color Accent => _palette.Accent;
        public static Color AccentLight => _palette.AccentLight;
        public static Color Highlight => _palette.Highlight;
        public static Color Background => _palette.Background;
        public static Color CardBackground => _palette.CardBackground;
        public static Color SurfaceAlt => _palette.SurfaceAlt;
        public static Color ControlSurface => _palette.ControlSurface;
        public static Color OutlineButtonBack => _palette.OutlineButtonBack;
        public static Color ButtonBorder => _palette.ButtonBorder;
        public static Color Border => _palette.Border;
        public static Color AltRow => _palette.AltRow;
        public static Color TextPrimary => _palette.TextPrimary;
        public static Color TextSecondary => _palette.TextSecondary;
        public static Color TextMuted => _palette.TextMuted;
        public static Color Success => _palette.Success;
        public static Color Warning => _palette.Warning;
        public static Color WarningLight => _palette.WarningLight;
        public static Color Danger => _palette.Danger;
        public static Color DangerHover => _palette.DangerHover;
        public static Color DangerPressed => _palette.DangerPressed;
        public static Color ProblemFill => _palette.ProblemFill;

        public static readonly Font HeadingFont = new("Bahnschrift SemiBold", 12.5F);
        public static readonly Font TitleFont = new("Bahnschrift SemiBold", 15F);
        public static readonly Font BodyFont = new("Segoe UI", 10F);
        public static readonly Font BodyStrongFont = new("Segoe UI Semibold", 10F);
        public static readonly Font SmallFont = new("Segoe UI", 9F);
        public static readonly Font SmallFontBold = new("Segoe UI Semibold", 9F);
        public static readonly Font ButtonFont = new("Segoe UI Semibold", 9.5F);
        public static readonly Font MonoFont = new("Consolas", 9.5F);
        public static readonly Font BrandFont = new("Bahnschrift SemiBold", 13F);
        public static readonly Font NavFont = new("Segoe UI Semibold", 10F);
        public static readonly Font IconFont = new("Segoe MDL2 Assets", 13F);

        public const int TopTabExpandedWidth = 150;
        public const int TopTabCollapsedWidth = 52;
        public const int TopTabExpandedHeight = 46;
        public const int TopTabCollapsedHeight = 36;
        public const int HeaderExpandedHeight = 62;
        public const int HeaderCollapsedHeight = 46;
        private const int TopTabIconWidth = 40;
        private const int TopTabCorner = 8;
        private const int TopTabBar = 3;

        private static readonly Color[] LightNavPalette =
        [
            Color.FromArgb(13, 148, 136),
            Color.FromArgb(124, 58, 237),
            Color.FromArgb(37, 99, 235),
            Color.FromArgb(217, 119, 6),
            Color.FromArgb(219, 39, 119),
            Color.FromArgb(234, 88, 12),
        ];

        private static readonly Color[] DarkNavPalette =
        [
            Color.FromArgb(45, 212, 191),
            Color.FromArgb(167, 139, 250),
            Color.FromArgb(96, 165, 250),
            Color.FromArgb(251, 191, 36),
            Color.FromArgb(244, 114, 182),
            Color.FromArgb(251, 146, 60),
        ];

        public static Color NavColor(int index)
        {
            Color[] palette = IsDark ? DarkNavPalette : LightNavPalette;
            return palette[index % palette.Length];
        }

        private const int AccentWidth = 4;

        private static readonly PropertyInfo? DoubleBufferedProperty =
            typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void StyleForm(Form form)
        {
            form.BackColor = Background;
            form.ForeColor = TextPrimary;
            form.Font = BodyFont;
            form.HandleCreated -= ApplyTitleBar;
            form.HandleCreated += ApplyTitleBar;
            if (form.IsHandleCreated)
                ApplyTitleBar(form, EventArgs.Empty);
        }

        public static void StyleView(Control view)
        {
            view.BackColor = Background;
            view.ForeColor = TextPrimary;
            view.Font = BodyFont;
        }

        public static void StylePrimaryButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Accent;
            button.FlatAppearance.MouseDownBackColor = PrimaryDark;
            button.BackColor = Primary;
            button.ForeColor = Color.White;
            button.Font = ButtonFont;
            button.Cursor = Cursors.Hand;
        }

        public static void StyleOutlineButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = ButtonBorder;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = AccentLight;
            button.FlatAppearance.MouseDownBackColor = PrimaryLight;
            button.BackColor = OutlineButtonBack;
            button.ForeColor = TextPrimary;
            button.Font = ButtonFont;
            button.Cursor = Cursors.Hand;
        }

        public static void StyleDangerButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = DangerHover;
            button.FlatAppearance.MouseDownBackColor = DangerPressed;
            button.BackColor = Danger;
            button.ForeColor = Color.White;
            button.Font = ButtonFont;
            button.Cursor = Cursors.Hand;
        }

        public static void StyleComboBox(ComboBox comboBox)
        {
            comboBox.FlatStyle = IsDark ? FlatStyle.Standard : FlatStyle.System;
            comboBox.Font = BodyFont;
            comboBox.ForeColor = TextPrimary;
            comboBox.BackColor = ControlSurface;
            comboBox.MaxDropDownItems = ComboVisibleItems;
            if (!IsDark)
                return;
            if (comboBox.DropDownStyle == ComboBoxStyle.DropDownList)
            {
                comboBox.DrawMode = DrawMode.OwnerDrawFixed;
                comboBox.DrawItem -= DrawComboItem;
                comboBox.DrawItem += DrawComboItem;
            }
            DarkComboPainter.Attach(comboBox);
            ApplyDarkControlTheme(comboBox, "DarkMode_CFD");
        }

        public static void StyleTextBox(TextBox textBox)
        {
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.Font = BodyFont;
            textBox.ForeColor = TextPrimary;
            textBox.BackColor = _palette.InputBackground;
        }

        public static void StyleNumeric(NumericUpDown numeric)
        {
            numeric.Font = BodyFont;
            numeric.ForeColor = TextPrimary;
            numeric.BackColor = _palette.InputBackground;
            numeric.BorderStyle = BorderStyle.FixedSingle;
        }

        public static void StyleRadio(RadioButton radio)
        {
            radio.Font = BodyFont;
            radio.ForeColor = TextPrimary;
            radio.Cursor = Cursors.Hand;
        }

        public static void StyleCheckBox(CheckBox checkBox)
        {
            checkBox.Font = BodyFont;
            checkBox.ForeColor = TextPrimary;
            checkBox.Cursor = Cursors.Hand;
        }

        public static void StyleFieldLabel(Label label)
        {
            label.Font = SmallFont;
            label.ForeColor = TextSecondary;
        }

        public static void StyleMutedLabel(Label label)
        {
            label.Font = SmallFont;
            label.ForeColor = TextMuted;
        }

        public static void StyleErrorLabel(Label label)
        {
            label.Font = SmallFontBold;
            label.ForeColor = Danger;
        }

        public static void StyleWarningLabel(Label label)
        {
            label.Font = SmallFontBold;
            label.ForeColor = Warning;
        }

        public static void StyleSuccessLabel(Label label)
        {
            label.Font = SmallFontBold;
            label.ForeColor = Success;
        }

        public static void StyleSectionLabel(Label label)
        {
            label.Font = HeadingFont;
            label.ForeColor = TextPrimary;
        }

        public static void StyleTitleLabel(Label label)
        {
            label.Font = TitleFont;
            label.ForeColor = Primary;
        }

        public static void StyleEmptyState(Label label)
        {
            label.Font = BodyFont;
            label.ForeColor = TextMuted;
            label.TextAlign = ContentAlignment.MiddleCenter;
        }

        public static void StyleCard(Panel card, Label title)
        {
            StyleCardPanel(card);
            title.Font = BodyStrongFont;
            title.ForeColor = TextPrimary;
        }

        public static void StyleCardPanel(Panel card)
        {
            card.BackColor = CardBackground;
            card.ForeColor = TextPrimary;
            card.Paint -= DrawCardBorder;
            card.Paint += DrawCardBorder;
            card.Resize -= RedrawOnResize;
            card.Resize += RedrawOnResize;
        }

        public static void StyleToolbar(Control toolbar) => toolbar.BackColor = Background;

        public static void StyleHeaderBar(Control bar)
        {
            bar.BackColor = CardBackground;
            bar.Paint -= DrawBottomBorder;
            bar.Paint += DrawBottomBorder;
            bar.Resize -= RedrawOnResize;
            bar.Resize += RedrawOnResize;
        }

        public static void StyleStatusStrip(StatusStrip strip)
        {
            strip.BackColor = SurfaceAlt;
            strip.Font = SmallFont;
            strip.ForeColor = TextSecondary;
            strip.SizingGrip = false;
            strip.RenderMode = ToolStripRenderMode.ManagerRenderMode;
        }

        public static void StyleWarningBanner(Panel banner, Label message, Button dismiss)
        {
            banner.BackColor = WarningLight;
            message.Font = BodyFont;
            message.ForeColor = TextPrimary;
            message.BackColor = WarningLight;
            StyleOutlineButton(dismiss);
            banner.Paint -= DrawWarningAccent;
            banner.Paint += DrawWarningAccent;
        }

        public static void StyleNavTab(RadioButton tab)
        {
            tab.Appearance = Appearance.Button;
            tab.FlatStyle = FlatStyle.Flat;
            tab.FlatAppearance.BorderSize = 0;
            tab.FlatAppearance.CheckedBackColor = CardBackground;
            tab.FlatAppearance.MouseOverBackColor = SurfaceAlt;
            tab.FlatAppearance.MouseDownBackColor = SurfaceAlt;
            tab.BackColor = CardBackground;
            tab.Font = BodyStrongFont;
            tab.TextAlign = ContentAlignment.MiddleCenter;
            tab.Cursor = Cursors.Hand;
            PaintNavTab(tab, EventArgs.Empty);
            tab.CheckedChanged -= PaintNavTab;
            tab.CheckedChanged += PaintNavTab;
            tab.Paint -= DrawNavTabUnderline;
            tab.Paint += DrawNavTabUnderline;
        }

        public static void StyleBrand(Label title, PictureBox logo)
        {
            title.Font = BrandFont;
            title.ForeColor = IsDark ? Highlight : Primary;
            title.BackColor = CardBackground;
            logo.BackColor = CardBackground;
            logo.Image = AppAssets.Logo;
        }

        public static void StyleTopTab(RadioButton tab, Color accent)
        {
            tab.Appearance = Appearance.Button;
            tab.FlatStyle = FlatStyle.Flat;
            tab.FlatAppearance.BorderSize = 0;
            tab.FlatAppearance.CheckedBackColor = CardBackground;
            tab.FlatAppearance.MouseOverBackColor = CardBackground;
            tab.FlatAppearance.MouseDownBackColor = CardBackground;
            tab.BackColor = CardBackground;
            tab.ForeColor = accent;
            tab.Font = NavFont;
            tab.Cursor = Cursors.Hand;
            tab.Paint -= DrawTopTab;
            tab.Paint += DrawTopTab;
            tab.MouseEnter -= RedrawControl;
            tab.MouseEnter += RedrawControl;
            tab.MouseLeave -= RedrawControl;
            tab.MouseLeave += RedrawControl;
            tab.CheckedChanged -= RedrawControl;
            tab.CheckedChanged += RedrawControl;
        }

        public static void StyleNavGroup(Control bar, Color? accent, params Control[] parts)
        {
            Color back = accent is Color a ? Blend(a, CardBackground, IsDark ? 0.2 : 0.1) : CardBackground;
            bar.BackColor = back;
            bar.Tag = accent;
            foreach (Control part in parts)
                part.BackColor = back;
            bar.Invalidate(true);
        }

        public static void StyleNavGroupLabel(Label label, Color accent)
        {
            label.Font = HeadingFont;
            label.ForeColor = accent;
        }

        public static void StyleHeaderIconButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = SurfaceAlt;
            button.FlatAppearance.MouseDownBackColor = PrimaryLight;
            button.BackColor = CardBackground;
            button.ForeColor = TextSecondary;
            button.Font = IconFont;
            button.Cursor = Cursors.Hand;
        }

        public static void StyleGrid(DataGridView grid)
        {
            grid.BackgroundColor = CardBackground;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = Border;
            grid.DefaultCellStyle.Font = BodyFont;
            grid.DefaultCellStyle.ForeColor = TextPrimary;
            grid.DefaultCellStyle.BackColor = CardBackground;
            grid.DefaultCellStyle.SelectionBackColor = PrimaryLight;
            grid.DefaultCellStyle.SelectionForeColor = TextPrimary;
            grid.AlternatingRowsDefaultCellStyle.BackColor = AltRow;
            grid.ColumnHeadersDefaultCellStyle.Font = BodyStrongFont;
            grid.ColumnHeadersDefaultCellStyle.BackColor = SurfaceAlt;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextSecondary;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = SurfaceAlt;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextSecondary;
            grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 36;
            grid.RowTemplate.Height = 30;
            grid.EnableHeadersVisualStyles = false;
            DoubleBufferedProperty?.SetValue(grid, true);
            ApplyDarkControlTheme(grid, "DarkMode_Explorer");
        }

        public static void AlignRight(DataGridViewColumn column) =>
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        public static void StyleTotalRow(DataGridViewRow row)
        {
            row.DefaultCellStyle.Font = BodyStrongFont;
            row.DefaultCellStyle.BackColor = SurfaceAlt;
            row.DefaultCellStyle.SelectionBackColor = PrimaryLight;
        }

        public static void StyleMutedCell(DataGridViewCellStyle style)
        {
            style.ForeColor = TextMuted;
            style.SelectionForeColor = TextMuted;
        }

        public static void StyleProblemCell(DataGridViewCellStyle style)
        {
            style.BackColor = ProblemFill;
            style.SelectionBackColor = ProblemFill;
            style.ForeColor = _palette.ProblemText;
            style.SelectionForeColor = _palette.ProblemText;
        }

        public static void StyleListBox(ListBox listBox)
        {
            listBox.BorderStyle = BorderStyle.None;
            listBox.BackColor = CardBackground;
            listBox.ForeColor = TextPrimary;
            listBox.Font = BodyFont;
            listBox.IntegralHeight = false;
            ApplyDarkControlTheme(listBox, "DarkMode_Explorer");
        }

        public static void StyleSummaryBox(TextBox textBox)
        {
            textBox.BorderStyle = BorderStyle.None;
            textBox.BackColor = CardBackground;
            textBox.ForeColor = TextPrimary;
            textBox.Font = MonoFont;
        }

        private const int ComboVisibleItems = 12;
        private const int DwmUseImmersiveDarkMode = 20;

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string? pszSubIdList);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hWnd, int attribute, ref int value, int size);

        private static void ApplyTitleBar(object? sender, EventArgs e)
        {
            if (sender is not Form form)
                return;
            int dark = IsDark ? 1 : 0;
            _ = DwmSetWindowAttribute(form.Handle, DwmUseImmersiveDarkMode, ref dark, sizeof(int));
        }

        private static void ApplyDarkControlTheme(Control control, string theme)
        {
            if (!IsDark)
                return;
            void Apply(object? sender, EventArgs e) => _ = SetWindowTheme(control.Handle, theme, null);
            control.HandleCreated += Apply;
            if (control.IsHandleCreated)
                Apply(control, EventArgs.Empty);
        }

        private static void DrawComboItem(object? sender, DrawItemEventArgs e)
        {
            if (sender is not ComboBox combo)
                return;
            bool edit = (e.State & DrawItemState.ComboBoxEdit) != 0;
            bool selected = !edit && (e.State & DrawItemState.Selected) != 0;
            Color back = selected ? Primary : ControlSurface;
            Color fore = selected ? Color.White : combo.Enabled ? TextPrimary : TextMuted;
            using (var brush = new SolidBrush(back))
                e.Graphics.FillRectangle(brush, e.Bounds);
            if (e.Index < 0)
                return;
            var text = new Rectangle(e.Bounds.Left + 4, e.Bounds.Top, e.Bounds.Width - 6, e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, combo.GetItemText(combo.Items[e.Index]), combo.Font, text, fore,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }

        private static void RedrawControl(object? sender, EventArgs e) => (sender as Control)?.Invalidate();

        private static Color Blend(Color top, Color bottom, double amount) => Color.FromArgb(
            (int)Math.Round(top.R * amount + bottom.R * (1 - amount)),
            (int)Math.Round(top.G * amount + bottom.G * (1 - amount)),
            (int)Math.Round(top.B * amount + bottom.B * (1 - amount)));

        private static void DrawTopTab(object? sender, PaintEventArgs e)
        {
            if (sender is not RadioButton tab)
                return;
            Color accent = tab.ForeColor;
            bool active = tab.Checked;
            bool hover = tab.ClientRectangle.Contains(tab.PointToClient(Cursor.Position));
            Color surface = tab.Parent?.BackColor ?? CardBackground;
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var clear = new SolidBrush(surface))
                g.FillRectangle(clear, tab.ClientRectangle);

            int inset = tab.LogicalToDeviceUnits(2);
            var pill = Rectangle.Inflate(tab.ClientRectangle, -inset, -inset);
            if (active || hover)
            {
                Color fill = active ? Blend(accent, surface, IsDark ? 0.22 : 0.12) : surface == CardBackground ? SurfaceAlt : Blend(TextPrimary, surface, 0.06);
                using var path = RoundedRectangle(pill, tab.LogicalToDeviceUnits(TopTabCorner));
                using var brush = new SolidBrush(fill);
                g.FillPath(brush, path);
            }
            if (active)
            {
                int bar = tab.LogicalToDeviceUnits(TopTabBar);
                int margin = tab.LogicalToDeviceUnits(10);
                using var accentBrush = new SolidBrush(accent);
                g.FillRectangle(accentBrush, pill.Left + margin, pill.Bottom - bar, Math.Max(1, pill.Width - margin * 2), bar);
            }

            bool collapsed = tab.Width <= tab.LogicalToDeviceUnits(TopTabCollapsedWidth);
            int iconWidth = tab.LogicalToDeviceUnits(TopTabIconWidth);
            var icon = collapsed ? tab.ClientRectangle : new Rectangle(tab.LogicalToDeviceUnits(6), 0, iconWidth, tab.Height);
            Color iconColor = active || hover ? accent : Blend(accent, surface, 0.75);
            if (tab.Tag is string glyph)
                TextRenderer.DrawText(g, glyph, IconFont, icon, iconColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            if (collapsed)
                return;
            var text = new Rectangle(icon.Right, 0, tab.Width - icon.Right - tab.LogicalToDeviceUnits(6), tab.Height);
            TextRenderer.DrawText(g, tab.Text, NavFont, text, active ? TextPrimary : TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        }

        private static System.Drawing.Drawing2D.GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            int d = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static void PaintNavTab(object? sender, EventArgs e)
        {
            if (sender is RadioButton tab)
                tab.ForeColor = tab.Checked ? Primary : TextSecondary;
        }

        private static void DrawNavTabUnderline(object? sender, PaintEventArgs e)
        {
            if (sender is not RadioButton { Checked: true } tab)
                return;
            using var accent = new SolidBrush(Primary);
            e.Graphics.FillRectangle(accent, 0, tab.Height - 3, tab.Width, 3);
        }

        private static void RedrawOnResize(object? sender, EventArgs e) => (sender as Control)?.Invalidate();

        private static void DrawCardBorder(object? sender, PaintEventArgs e)
        {
            if (sender is not Control card)
                return;
            using var pen = new Pen(Border);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        }

        private static void DrawBottomBorder(object? sender, PaintEventArgs e)
        {
            if (sender is not Control bar)
                return;
            if (bar.Tag is Color accent)
            {
                int thickness = bar.LogicalToDeviceUnits(3);
                using var brush = new SolidBrush(accent);
                e.Graphics.FillRectangle(brush, 0, bar.Height - thickness, bar.Width, thickness);
                return;
            }
            using var pen = new Pen(Border);
            e.Graphics.DrawLine(pen, 0, bar.Height - 1, bar.Width, bar.Height - 1);
        }

        private static void DrawWarningAccent(object? sender, PaintEventArgs e)
        {
            if (sender is not Control banner)
                return;
            using var accent = new SolidBrush(Warning);
            e.Graphics.FillRectangle(accent, 0, 0, AccentWidth, banner.Height);
        }
    }
}
