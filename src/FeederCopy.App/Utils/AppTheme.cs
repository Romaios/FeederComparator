using System.Reflection;

namespace FeederCopy.App.Utils
{
    public static class AppTheme
    {
        public static readonly Color Primary = Color.FromArgb(20, 77, 91);
        public static readonly Color PrimaryDark = Color.FromArgb(11, 43, 57);
        public static readonly Color PrimaryLight = Color.FromArgb(221, 236, 239);
        public static readonly Color Accent = Color.FromArgb(40, 116, 133);
        public static readonly Color AccentLight = Color.FromArgb(229, 244, 246);
        public static readonly Color Background = Color.FromArgb(240, 244, 246);
        public static readonly Color CardBackground = Color.White;
        public static readonly Color SurfaceAlt = Color.FromArgb(235, 242, 244);
        public static readonly Color ControlSurface = Color.FromArgb(250, 253, 253);
        public static readonly Color OutlineButtonBack = Color.FromArgb(247, 251, 252);
        public static readonly Color ButtonBorder = Color.FromArgb(177, 195, 202);
        public static readonly Color Border = Color.FromArgb(207, 218, 222);
        public static readonly Color AltRow = Color.FromArgb(247, 250, 251);
        public static readonly Color TextPrimary = Color.FromArgb(22, 36, 43);
        public static readonly Color TextSecondary = Color.FromArgb(64, 82, 91);
        public static readonly Color TextMuted = Color.FromArgb(104, 123, 132);
        public static readonly Color Success = Color.FromArgb(35, 126, 93);
        public static readonly Color Warning = Color.FromArgb(176, 111, 28);
        public static readonly Color WarningLight = Color.FromArgb(250, 237, 211);
        public static readonly Color Danger = Color.FromArgb(177, 58, 58);
        public static readonly Color DangerHover = Color.FromArgb(194, 70, 70);
        public static readonly Color DangerPressed = Color.FromArgb(138, 40, 40);
        public static readonly Color ProblemFill = Color.FromArgb(255, 242, 204);

        public static readonly Font HeadingFont = new("Bahnschrift SemiBold", 12.5F);
        public static readonly Font TitleFont = new("Bahnschrift SemiBold", 15F);
        public static readonly Font BodyFont = new("Segoe UI", 10F);
        public static readonly Font BodyStrongFont = new("Segoe UI Semibold", 10F);
        public static readonly Font SmallFont = new("Segoe UI", 9F);
        public static readonly Font SmallFontBold = new("Segoe UI Semibold", 9F);
        public static readonly Font ButtonFont = new("Segoe UI Semibold", 9.5F);
        public static readonly Font MonoFont = new("Consolas", 9.5F);

        private const int AccentWidth = 4;

        private static readonly PropertyInfo? DoubleBufferedProperty =
            typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void StyleForm(Form form)
        {
            form.BackColor = Background;
            form.ForeColor = TextPrimary;
            form.Font = BodyFont;
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
            comboBox.FlatStyle = FlatStyle.System;
            comboBox.Font = BodyFont;
            comboBox.ForeColor = TextPrimary;
            comboBox.BackColor = ControlSurface;
            comboBox.MaxDropDownItems = 12;
        }

        public static void StyleTextBox(TextBox textBox)
        {
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.Font = BodyFont;
            textBox.ForeColor = TextPrimary;
            textBox.BackColor = Color.White;
        }

        public static void StyleNumeric(NumericUpDown numeric)
        {
            numeric.Font = BodyFont;
            numeric.ForeColor = TextPrimary;
            numeric.BackColor = Color.White;
            numeric.BorderStyle = BorderStyle.FixedSingle;
        }

        public static void StyleRadio(RadioButton radio)
        {
            radio.Font = BodyFont;
            radio.ForeColor = TextPrimary;
            radio.Cursor = Cursors.Hand;
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
        }

        public static void AlignRight(DataGridViewColumn column) =>
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        public static void StyleMutedCell(DataGridViewCellStyle style)
        {
            style.ForeColor = TextMuted;
            style.SelectionForeColor = TextMuted;
        }

        public static void StyleProblemCell(DataGridViewCellStyle style)
        {
            style.BackColor = ProblemFill;
            style.SelectionBackColor = ProblemFill;
        }

        public static void StyleListBox(ListBox listBox)
        {
            listBox.BorderStyle = BorderStyle.None;
            listBox.BackColor = CardBackground;
            listBox.ForeColor = TextPrimary;
            listBox.Font = BodyFont;
            listBox.IntegralHeight = false;
        }

        public static void StyleSummaryBox(TextBox textBox)
        {
            textBox.BorderStyle = BorderStyle.None;
            textBox.BackColor = CardBackground;
            textBox.ForeColor = TextPrimary;
            textBox.Font = MonoFont;
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
