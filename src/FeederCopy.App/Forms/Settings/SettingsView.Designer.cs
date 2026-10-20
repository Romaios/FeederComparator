namespace FeederCopy.App.Forms.Settings
{
    partial class SettingsView
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            tlpRoot = new TableLayoutPanel();
            lblTitle = new Label();
            lblIntro = new Label();
            tlpFeederCard = new TableLayoutPanel();
            lblFeederTitle = new Label();
            lblBusId = new Label();
            txtBusId = new TextBox();
            lblConsumerId = new Label();
            txtConsumerId = new TextBox();
            tlpAsBilledCard = new TableLayoutPanel();
            lblAsBilledTitle = new Label();
            lblAccount = new Label();
            txtAccount = new TextBox();
            lblKwh = new Label();
            txtKwh = new TextBox();
            lblMonth = new Label();
            txtMonth = new TextBox();
            tlpExportCard = new TableLayoutPanel();
            lblExportTitle = new Label();
            lblExportStyle = new Label();
            cboExportStyle = new ComboBox();
            flpButtons = new FlowLayoutPanel();
            btnSave = new Button();
            btnRevert = new Button();
            btnDefaults = new Button();
            lblSaved = new Label();
            lblValidation = new Label();
            tlpRoot.SuspendLayout();
            tlpFeederCard.SuspendLayout();
            tlpAsBilledCard.SuspendLayout();
            tlpExportCard.SuspendLayout();
            flpButtons.SuspendLayout();
            SuspendLayout();
            //
            // tlpRoot
            //
            tlpRoot.AutoScroll = true;
            tlpRoot.ColumnCount = 1;
            tlpRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpRoot.Controls.Add(lblTitle, 0, 0);
            tlpRoot.Controls.Add(lblIntro, 0, 1);
            tlpRoot.Controls.Add(tlpFeederCard, 0, 2);
            tlpRoot.Controls.Add(tlpAsBilledCard, 0, 3);
            tlpRoot.Controls.Add(tlpExportCard, 0, 4);
            tlpRoot.Controls.Add(flpButtons, 0, 5);
            tlpRoot.Controls.Add(lblValidation, 0, 6);
            tlpRoot.Dock = DockStyle.Fill;
            tlpRoot.Location = new Point(0, 0);
            tlpRoot.Name = "tlpRoot";
            tlpRoot.Padding = new Padding(20, 16, 20, 16);
            tlpRoot.RowCount = 8;
            tlpRoot.RowStyles.Add(new RowStyle());
            tlpRoot.RowStyles.Add(new RowStyle());
            tlpRoot.RowStyles.Add(new RowStyle());
            tlpRoot.RowStyles.Add(new RowStyle());
            tlpRoot.RowStyles.Add(new RowStyle());
            tlpRoot.RowStyles.Add(new RowStyle());
            tlpRoot.RowStyles.Add(new RowStyle());
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpRoot.Size = new Size(900, 640);
            tlpRoot.TabIndex = 0;
            //
            // lblTitle
            //
            lblTitle.AutoSize = true;
            lblTitle.Margin = new Padding(3, 0, 3, 4);
            lblTitle.Name = "lblTitle";
            lblTitle.Text = "Settings";
            //
            // lblIntro
            //
            lblIntro.AutoSize = true;
            lblIntro.Margin = new Padding(3, 0, 3, 12);
            lblIntro.Name = "lblIntro";
            lblIntro.Text = "Column names the program looks for in the files you import. Names are matched without regard to upper or lower case.";
            //
            // tlpFeederCard
            //
            tlpFeederCard.AutoSize = true;
            tlpFeederCard.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tlpFeederCard.ColumnCount = 2;
            tlpFeederCard.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            tlpFeederCard.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320F));
            tlpFeederCard.Controls.Add(lblFeederTitle, 0, 0);
            tlpFeederCard.Controls.Add(lblBusId, 0, 1);
            tlpFeederCard.Controls.Add(txtBusId, 1, 1);
            tlpFeederCard.Controls.Add(lblConsumerId, 0, 2);
            tlpFeederCard.Controls.Add(txtConsumerId, 1, 2);
            tlpFeederCard.Margin = new Padding(3, 0, 3, 12);
            tlpFeederCard.Name = "tlpFeederCard";
            tlpFeederCard.Padding = new Padding(14, 12, 14, 12);
            tlpFeederCard.RowCount = 3;
            tlpFeederCard.RowStyles.Add(new RowStyle());
            tlpFeederCard.RowStyles.Add(new RowStyle());
            tlpFeederCard.RowStyles.Add(new RowStyle());
            tlpFeederCard.TabIndex = 0;
            //
            // lblFeederTitle
            //
            lblFeederTitle.AutoSize = true;
            tlpFeederCard.SetColumnSpan(lblFeederTitle, 2);
            lblFeederTitle.Margin = new Padding(3, 0, 3, 8);
            lblFeederTitle.Name = "lblFeederTitle";
            lblFeederTitle.Text = "Feeder list columns";
            //
            // lblBusId
            //
            lblBusId.Anchor = AnchorStyles.Left;
            lblBusId.AutoSize = true;
            lblBusId.Name = "lblBusId";
            lblBusId.Text = "Bus ID column";
            //
            // txtBusId
            //
            txtBusId.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtBusId.Margin = new Padding(3, 4, 3, 4);
            txtBusId.Name = "txtBusId";
            txtBusId.TabIndex = 0;
            //
            // lblConsumerId
            //
            lblConsumerId.Anchor = AnchorStyles.Left;
            lblConsumerId.AutoSize = true;
            lblConsumerId.Name = "lblConsumerId";
            lblConsumerId.Text = "Consumer ID column";
            //
            // txtConsumerId
            //
            txtConsumerId.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtConsumerId.Margin = new Padding(3, 4, 3, 4);
            txtConsumerId.Name = "txtConsumerId";
            txtConsumerId.TabIndex = 1;
            //
            // tlpAsBilledCard
            //
            tlpAsBilledCard.AutoSize = true;
            tlpAsBilledCard.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tlpAsBilledCard.ColumnCount = 2;
            tlpAsBilledCard.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            tlpAsBilledCard.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320F));
            tlpAsBilledCard.Controls.Add(lblAsBilledTitle, 0, 0);
            tlpAsBilledCard.Controls.Add(lblAccount, 0, 1);
            tlpAsBilledCard.Controls.Add(txtAccount, 1, 1);
            tlpAsBilledCard.Controls.Add(lblKwh, 0, 2);
            tlpAsBilledCard.Controls.Add(txtKwh, 1, 2);
            tlpAsBilledCard.Controls.Add(lblMonth, 0, 3);
            tlpAsBilledCard.Controls.Add(txtMonth, 1, 3);
            tlpAsBilledCard.Margin = new Padding(3, 0, 3, 12);
            tlpAsBilledCard.Name = "tlpAsBilledCard";
            tlpAsBilledCard.Padding = new Padding(14, 12, 14, 12);
            tlpAsBilledCard.RowCount = 4;
            tlpAsBilledCard.RowStyles.Add(new RowStyle());
            tlpAsBilledCard.RowStyles.Add(new RowStyle());
            tlpAsBilledCard.RowStyles.Add(new RowStyle());
            tlpAsBilledCard.RowStyles.Add(new RowStyle());
            tlpAsBilledCard.TabIndex = 1;
            //
            // lblAsBilledTitle
            //
            lblAsBilledTitle.AutoSize = true;
            tlpAsBilledCard.SetColumnSpan(lblAsBilledTitle, 2);
            lblAsBilledTitle.Margin = new Padding(3, 0, 3, 8);
            lblAsBilledTitle.Name = "lblAsBilledTitle";
            lblAsBilledTitle.Text = "As-Billed columns";
            //
            // lblAccount
            //
            lblAccount.Anchor = AnchorStyles.Left;
            lblAccount.AutoSize = true;
            lblAccount.Name = "lblAccount";
            lblAccount.Text = "Account number column";
            //
            // txtAccount
            //
            txtAccount.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtAccount.Margin = new Padding(3, 4, 3, 4);
            txtAccount.Name = "txtAccount";
            txtAccount.TabIndex = 0;
            //
            // lblKwh
            //
            lblKwh.Anchor = AnchorStyles.Left;
            lblKwh.AutoSize = true;
            lblKwh.Name = "lblKwh";
            lblKwh.Text = "kWh used column";
            //
            // txtKwh
            //
            txtKwh.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtKwh.Margin = new Padding(3, 4, 3, 4);
            txtKwh.Name = "txtKwh";
            txtKwh.TabIndex = 1;
            //
            // lblMonth
            //
            lblMonth.Anchor = AnchorStyles.Left;
            lblMonth.AutoSize = true;
            lblMonth.Name = "lblMonth";
            lblMonth.Text = "Bill month column";
            //
            // txtMonth
            //
            txtMonth.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtMonth.Margin = new Padding(3, 4, 3, 4);
            txtMonth.Name = "txtMonth";
            txtMonth.TabIndex = 2;
            //
            // tlpExportCard
            //
            tlpExportCard.AutoSize = true;
            tlpExportCard.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tlpExportCard.ColumnCount = 2;
            tlpExportCard.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            tlpExportCard.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320F));
            tlpExportCard.Controls.Add(lblExportTitle, 0, 0);
            tlpExportCard.Controls.Add(lblExportStyle, 0, 1);
            tlpExportCard.Controls.Add(cboExportStyle, 1, 1);
            tlpExportCard.Margin = new Padding(3, 0, 3, 12);
            tlpExportCard.Name = "tlpExportCard";
            tlpExportCard.Padding = new Padding(14, 12, 14, 12);
            tlpExportCard.RowCount = 2;
            tlpExportCard.RowStyles.Add(new RowStyle());
            tlpExportCard.RowStyles.Add(new RowStyle());
            tlpExportCard.TabIndex = 2;
            //
            // lblExportTitle
            //
            lblExportTitle.AutoSize = true;
            tlpExportCard.SetColumnSpan(lblExportTitle, 2);
            lblExportTitle.Margin = new Padding(3, 0, 3, 8);
            lblExportTitle.Name = "lblExportTitle";
            lblExportTitle.Text = "Export";
            //
            // lblExportStyle
            //
            lblExportStyle.Anchor = AnchorStyles.Left;
            lblExportStyle.AutoSize = true;
            lblExportStyle.Name = "lblExportStyle";
            lblExportStyle.Text = "Default export style";
            //
            // cboExportStyle
            //
            cboExportStyle.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cboExportStyle.DropDownStyle = ComboBoxStyle.DropDownList;
            cboExportStyle.Items.AddRange(new object[] { "One workbook, sheet per feeder", "Separate file per feeder" });
            cboExportStyle.Margin = new Padding(3, 4, 3, 4);
            cboExportStyle.Name = "cboExportStyle";
            cboExportStyle.TabIndex = 0;
            //
            // flpButtons
            //
            flpButtons.AutoSize = true;
            flpButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flpButtons.Controls.Add(btnSave);
            flpButtons.Controls.Add(btnRevert);
            flpButtons.Controls.Add(btnDefaults);
            flpButtons.Controls.Add(lblSaved);
            flpButtons.Margin = new Padding(0, 0, 0, 8);
            flpButtons.Name = "flpButtons";
            flpButtons.TabIndex = 3;
            flpButtons.WrapContents = false;
            //
            // btnSave
            //
            btnSave.AutoSize = true;
            btnSave.MinimumSize = new Size(110, 34);
            btnSave.Name = "btnSave";
            btnSave.TabIndex = 0;
            btnSave.Text = "Save";
            btnSave.Click += btnSave_Click;
            //
            // btnRevert
            //
            btnRevert.AutoSize = true;
            btnRevert.MinimumSize = new Size(110, 34);
            btnRevert.Name = "btnRevert";
            btnRevert.TabIndex = 1;
            btnRevert.Text = "Undo unsaved changes";
            btnRevert.Click += btnRevert_Click;
            //
            // btnDefaults
            //
            btnDefaults.AutoSize = true;
            btnDefaults.MinimumSize = new Size(110, 34);
            btnDefaults.Name = "btnDefaults";
            btnDefaults.TabIndex = 2;
            btnDefaults.Text = "Fill in defaults";
            btnDefaults.Click += btnDefaults_Click;
            //
            // lblSaved
            //
            lblSaved.Anchor = AnchorStyles.Left;
            lblSaved.AutoSize = true;
            lblSaved.Margin = new Padding(10, 0, 3, 0);
            lblSaved.Name = "lblSaved";
            //
            // lblValidation
            //
            lblValidation.AutoSize = true;
            lblValidation.MaximumSize = new Size(700, 0);
            lblValidation.Name = "lblValidation";
            //
            // SettingsView
            //
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(tlpRoot);
            Name = "SettingsView";
            Size = new Size(900, 640);
            tlpRoot.ResumeLayout(false);
            tlpRoot.PerformLayout();
            tlpFeederCard.ResumeLayout(false);
            tlpFeederCard.PerformLayout();
            tlpAsBilledCard.ResumeLayout(false);
            tlpAsBilledCard.PerformLayout();
            tlpExportCard.ResumeLayout(false);
            tlpExportCard.PerformLayout();
            flpButtons.ResumeLayout(false);
            flpButtons.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel tlpRoot;
        private Label lblTitle;
        private Label lblIntro;
        private TableLayoutPanel tlpFeederCard;
        private Label lblFeederTitle;
        private Label lblBusId;
        private TextBox txtBusId;
        private Label lblConsumerId;
        private TextBox txtConsumerId;
        private TableLayoutPanel tlpAsBilledCard;
        private Label lblAsBilledTitle;
        private Label lblAccount;
        private TextBox txtAccount;
        private Label lblKwh;
        private TextBox txtKwh;
        private Label lblMonth;
        private TextBox txtMonth;
        private TableLayoutPanel tlpExportCard;
        private Label lblExportTitle;
        private Label lblExportStyle;
        private ComboBox cboExportStyle;
        private FlowLayoutPanel flpButtons;
        private Button btnSave;
        private Button btnRevert;
        private Button btnDefaults;
        private Label lblSaved;
        private Label lblValidation;
    }
}
