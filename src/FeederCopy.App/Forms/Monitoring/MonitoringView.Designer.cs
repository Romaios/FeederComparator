namespace FeederCopy.App.Forms.Monitoring
{
    partial class MonitoringView
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
            tlpFilter = new TableLayoutPanel();
            lblTitle = new Label();
            lblYear = new Label();
            cboYear = new ComboBox();
            lblSummary = new Label();
            btnExport = new Button();
            lblMessage = new Label();
            dgvSummary = new DataGridView();
            colMonth = new DataGridViewTextBoxColumn();
            colFeeder = new DataGridViewTextBoxColumn();
            colTotalKwh = new DataGridViewTextBoxColumn();
            colActive = new DataGridViewTextBoxColumn();
            colInactive = new DataGridViewTextBoxColumn();
            tlpRoot.SuspendLayout();
            tlpFilter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSummary).BeginInit();
            SuspendLayout();
            tlpRoot.ColumnCount = 1;
            tlpRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpRoot.Controls.Add(tlpFilter, 0, 0);
            tlpRoot.Controls.Add(lblMessage, 0, 1);
            tlpRoot.Controls.Add(dgvSummary, 0, 2);
            tlpRoot.Dock = DockStyle.Fill;
            tlpRoot.Name = "tlpRoot";
            tlpRoot.Padding = new Padding(12);
            tlpRoot.RowCount = 3;
            tlpRoot.RowStyles.Add(new RowStyle());
            tlpRoot.RowStyles.Add(new RowStyle());
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpRoot.Size = new Size(1100, 680);
            tlpRoot.TabIndex = 0;
            tlpFilter.AutoSize = true;
            tlpFilter.ColumnCount = 4;
            tlpFilter.ColumnStyles.Add(new ColumnStyle());
            tlpFilter.ColumnStyles.Add(new ColumnStyle());
            tlpFilter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpFilter.ColumnStyles.Add(new ColumnStyle());
            tlpFilter.Controls.Add(lblTitle, 0, 0);
            tlpFilter.Controls.Add(lblYear, 0, 1);
            tlpFilter.Controls.Add(cboYear, 1, 1);
            tlpFilter.Controls.Add(lblSummary, 2, 1);
            tlpFilter.Controls.Add(btnExport, 3, 1);
            tlpFilter.Dock = DockStyle.Fill;
            tlpFilter.Margin = new Padding(3, 3, 3, 10);
            tlpFilter.Name = "tlpFilter";
            tlpFilter.Padding = new Padding(14, 12, 14, 10);
            tlpFilter.RowCount = 2;
            tlpFilter.RowStyles.Add(new RowStyle());
            tlpFilter.RowStyles.Add(new RowStyle());
            tlpFilter.TabIndex = 0;
            lblTitle.AutoSize = true;
            tlpFilter.SetColumnSpan(lblTitle, 4);
            lblTitle.Margin = new Padding(3, 0, 3, 8);
            lblTitle.Name = "lblTitle";
            lblTitle.Text = "Each transferred month per feeder: total kWh, active and inactive consumers";
            lblYear.Anchor = AnchorStyles.Left;
            lblYear.AutoSize = true;
            lblYear.Name = "lblYear";
            lblYear.Text = "Year";
            cboYear.Anchor = AnchorStyles.Left;
            cboYear.DropDownStyle = ComboBoxStyle.DropDownList;
            cboYear.Margin = new Padding(8, 3, 16, 3);
            cboYear.Name = "cboYear";
            cboYear.Size = new Size(110, 25);
            cboYear.TabIndex = 0;
            cboYear.SelectedIndexChanged += cboYear_SelectedIndexChanged;
            lblSummary.Anchor = AnchorStyles.Left;
            lblSummary.AutoSize = true;
            lblSummary.Name = "lblSummary";
            btnExport.AutoSize = true;
            btnExport.Enabled = false;
            btnExport.MinimumSize = new Size(160, 34);
            btnExport.Name = "btnExport";
            btnExport.TabIndex = 1;
            btnExport.Text = "Export to Excel…";
            btnExport.Click += btnExport_Click;
            lblMessage.AutoSize = true;
            lblMessage.Margin = new Padding(3, 12, 3, 6);
            lblMessage.Name = "lblMessage";
            lblMessage.Visible = false;
            dgvSummary.AllowUserToAddRows = false;
            dgvSummary.AllowUserToDeleteRows = false;
            dgvSummary.AllowUserToResizeRows = false;
            dgvSummary.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvSummary.Columns.AddRange(new DataGridViewColumn[] { colMonth, colFeeder, colTotalKwh, colActive, colInactive });
            dgvSummary.Dock = DockStyle.Fill;
            dgvSummary.Margin = new Padding(3);
            dgvSummary.MultiSelect = false;
            dgvSummary.Name = "dgvSummary";
            dgvSummary.ReadOnly = true;
            dgvSummary.RowHeadersVisible = false;
            dgvSummary.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvSummary.TabIndex = 2;
            colMonth.FillWeight = 14F;
            colMonth.HeaderText = "Month";
            colMonth.Name = "colMonth";
            colMonth.ReadOnly = true;
            colMonth.SortMode = DataGridViewColumnSortMode.NotSortable;
            colFeeder.FillWeight = 26F;
            colFeeder.HeaderText = "Feeder";
            colFeeder.Name = "colFeeder";
            colFeeder.ReadOnly = true;
            colFeeder.SortMode = DataGridViewColumnSortMode.NotSortable;
            colTotalKwh.FillWeight = 22F;
            colTotalKwh.HeaderText = "Total kWh";
            colTotalKwh.Name = "colTotalKwh";
            colTotalKwh.ReadOnly = true;
            colTotalKwh.SortMode = DataGridViewColumnSortMode.NotSortable;
            colActive.FillWeight = 14F;
            colActive.HeaderText = "Active";
            colActive.Name = "colActive";
            colActive.ReadOnly = true;
            colActive.SortMode = DataGridViewColumnSortMode.NotSortable;
            colInactive.FillWeight = 14F;
            colInactive.HeaderText = "Inactive";
            colInactive.Name = "colInactive";
            colInactive.ReadOnly = true;
            colInactive.SortMode = DataGridViewColumnSortMode.NotSortable;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(tlpRoot);
            Name = "MonitoringView";
            Size = new Size(1100, 680);
            tlpRoot.ResumeLayout(false);
            tlpRoot.PerformLayout();
            tlpFilter.ResumeLayout(false);
            tlpFilter.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSummary).EndInit();
            ResumeLayout(false);
        }

        private TableLayoutPanel tlpRoot;
        private TableLayoutPanel tlpFilter;
        private Label lblTitle;
        private Label lblYear;
        private ComboBox cboYear;
        private Label lblSummary;
        private Button btnExport;
        private Label lblMessage;
        private DataGridView dgvSummary;
        private DataGridViewTextBoxColumn colMonth;
        private DataGridViewTextBoxColumn colFeeder;
        private DataGridViewTextBoxColumn colTotalKwh;
        private DataGridViewTextBoxColumn colActive;
        private DataGridViewTextBoxColumn colInactive;
    }
}
