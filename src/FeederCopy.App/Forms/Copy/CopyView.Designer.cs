namespace FeederCopy.App.Forms.Copy
{
    partial class CopyView
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
            tlpFile = new TableLayoutPanel();
            lblFileTitle = new Label();
            btnChooseFile = new Button();
            lblFile = new Label();
            flpMonth = new FlowLayoutPanel();
            lblMonth = new Label();
            cboMonth = new ComboBox();
            lblYear = new Label();
            numYear = new NumericUpDown();
            btnCopy = new Button();
            lblHeading = new Label();
            lblMessage = new Label();
            dgvTotals = new DataGridView();
            colFeeder = new DataGridViewTextBoxColumn();
            colConsumers = new DataGridViewTextBoxColumn();
            colCopied = new DataGridViewTextBoxColumn();
            colNotFound = new DataGridViewTextBoxColumn();
            colDuplicates = new DataGridViewTextBoxColumn();
            colUnreadable = new DataGridViewTextBoxColumn();
            colTotalKwh = new DataGridViewTextBoxColumn();
            tlpProblemsHeader = new TableLayoutPanel();
            lblProblemsTitle = new Label();
            btnSaveProblems = new Button();
            dgvProblems = new DataGridView();
            colProblemFeeder = new DataGridViewTextBoxColumn();
            colProblemCount = new DataGridViewTextBoxColumn();
            colProblemBus = new DataGridViewTextBoxColumn();
            colProblemConsumer = new DataGridViewTextBoxColumn();
            colProblemText = new DataGridViewTextBoxColumn();
            tlpExport = new TableLayoutPanel();
            lblExportAs = new Label();
            rdoWorkbook = new RadioButton();
            rdoSeparate = new RadioButton();
            btnExport = new Button();
            tlpRoot.SuspendLayout();
            tlpFile.SuspendLayout();
            flpMonth.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numYear).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvTotals).BeginInit();
            tlpProblemsHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvProblems).BeginInit();
            tlpExport.SuspendLayout();
            SuspendLayout();
            //
            // tlpRoot
            //
            tlpRoot.ColumnCount = 1;
            tlpRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpRoot.Controls.Add(tlpFile, 0, 0);
            tlpRoot.Controls.Add(lblHeading, 0, 1);
            tlpRoot.Controls.Add(lblMessage, 0, 2);
            tlpRoot.Controls.Add(dgvTotals, 0, 3);
            tlpRoot.Controls.Add(tlpProblemsHeader, 0, 4);
            tlpRoot.Controls.Add(dgvProblems, 0, 5);
            tlpRoot.Controls.Add(tlpExport, 0, 6);
            tlpRoot.Dock = DockStyle.Fill;
            tlpRoot.Name = "tlpRoot";
            tlpRoot.Padding = new Padding(12);
            tlpRoot.RowCount = 7;
            tlpRoot.RowStyles.Add(new RowStyle());
            tlpRoot.RowStyles.Add(new RowStyle());
            tlpRoot.RowStyles.Add(new RowStyle());
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 190F));
            tlpRoot.RowStyles.Add(new RowStyle());
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpRoot.RowStyles.Add(new RowStyle());
            tlpRoot.Size = new Size(1100, 680);
            tlpRoot.TabIndex = 0;
            //
            // tlpFile
            //
            tlpFile.AutoSize = true;
            tlpFile.ColumnCount = 2;
            tlpFile.ColumnStyles.Add(new ColumnStyle());
            tlpFile.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpFile.Controls.Add(lblFileTitle, 0, 0);
            tlpFile.Controls.Add(btnChooseFile, 0, 1);
            tlpFile.Controls.Add(lblFile, 1, 1);
            tlpFile.Controls.Add(flpMonth, 0, 2);
            tlpFile.Dock = DockStyle.Fill;
            tlpFile.Margin = new Padding(3, 3, 3, 10);
            tlpFile.Name = "tlpFile";
            tlpFile.Padding = new Padding(14, 12, 14, 10);
            tlpFile.RowCount = 3;
            tlpFile.RowStyles.Add(new RowStyle());
            tlpFile.RowStyles.Add(new RowStyle());
            tlpFile.RowStyles.Add(new RowStyle());
            tlpFile.TabIndex = 0;
            //
            // lblFileTitle
            //
            lblFileTitle.AutoSize = true;
            tlpFile.SetColumnSpan(lblFileTitle, 2);
            lblFileTitle.Margin = new Padding(3, 0, 3, 8);
            lblFileTitle.Name = "lblFileTitle";
            lblFileTitle.Text = "1. Choose this month's As-Billed file, 2. check the month, 3. click Copy kWh";
            //
            // btnChooseFile
            //
            btnChooseFile.AutoSize = true;
            btnChooseFile.MinimumSize = new Size(190, 34);
            btnChooseFile.Name = "btnChooseFile";
            btnChooseFile.TabIndex = 0;
            btnChooseFile.Text = "Choose As-Billed file…";
            btnChooseFile.Click += btnChooseFile_Click;
            //
            // lblFile
            //
            lblFile.Anchor = AnchorStyles.Left;
            lblFile.AutoSize = true;
            lblFile.Margin = new Padding(10, 0, 3, 0);
            lblFile.Name = "lblFile";
            lblFile.Text = "No file chosen yet.";
            //
            // flpMonth
            //
            flpMonth.AutoSize = true;
            tlpFile.SetColumnSpan(flpMonth, 2);
            flpMonth.Controls.Add(lblMonth);
            flpMonth.Controls.Add(cboMonth);
            flpMonth.Controls.Add(lblYear);
            flpMonth.Controls.Add(numYear);
            flpMonth.Controls.Add(btnCopy);
            flpMonth.Margin = new Padding(0, 8, 0, 0);
            flpMonth.Name = "flpMonth";
            flpMonth.TabIndex = 1;
            flpMonth.WrapContents = false;
            //
            // lblMonth
            //
            lblMonth.Anchor = AnchorStyles.Left;
            lblMonth.AutoSize = true;
            lblMonth.Name = "lblMonth";
            lblMonth.Text = "Month";
            //
            // cboMonth
            //
            cboMonth.Anchor = AnchorStyles.Left;
            cboMonth.DropDownStyle = ComboBoxStyle.DropDownList;
            cboMonth.Items.AddRange(new object[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" });
            cboMonth.Margin = new Padding(6, 4, 16, 4);
            cboMonth.Name = "cboMonth";
            cboMonth.Size = new Size(150, 25);
            cboMonth.TabIndex = 0;
            //
            // lblYear
            //
            lblYear.Anchor = AnchorStyles.Left;
            lblYear.AutoSize = true;
            lblYear.Name = "lblYear";
            lblYear.Text = "Year";
            //
            // numYear
            //
            numYear.Anchor = AnchorStyles.Left;
            numYear.Margin = new Padding(6, 4, 16, 4);
            numYear.Maximum = new decimal(new int[] { 2100, 0, 0, 0 });
            numYear.Minimum = new decimal(new int[] { 1900, 0, 0, 0 });
            numYear.Name = "numYear";
            numYear.Size = new Size(90, 25);
            numYear.TabIndex = 1;
            numYear.Value = new decimal(new int[] { 2026, 0, 0, 0 });
            //
            // btnCopy
            //
            btnCopy.AutoSize = true;
            btnCopy.Enabled = false;
            btnCopy.MinimumSize = new Size(120, 34);
            btnCopy.Name = "btnCopy";
            btnCopy.TabIndex = 2;
            btnCopy.Text = "Copy kWh";
            btnCopy.Click += btnCopy_Click;
            //
            // lblHeading
            //
            lblHeading.AutoSize = true;
            lblHeading.Margin = new Padding(3, 4, 3, 6);
            lblHeading.Name = "lblHeading";
            //
            // lblMessage
            //
            lblMessage.AutoSize = true;
            lblMessage.Margin = new Padding(3, 0, 3, 6);
            lblMessage.Name = "lblMessage";
            //
            // dgvTotals
            //
            dgvTotals.AllowUserToAddRows = false;
            dgvTotals.AllowUserToDeleteRows = false;
            dgvTotals.AllowUserToResizeRows = false;
            dgvTotals.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvTotals.Columns.AddRange(new DataGridViewColumn[] { colFeeder, colConsumers, colCopied, colNotFound, colDuplicates, colUnreadable, colTotalKwh });
            dgvTotals.Dock = DockStyle.Fill;
            dgvTotals.Margin = new Padding(3, 3, 3, 10);
            dgvTotals.MultiSelect = false;
            dgvTotals.Name = "dgvTotals";
            dgvTotals.ReadOnly = true;
            dgvTotals.RowHeadersVisible = false;
            dgvTotals.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvTotals.TabIndex = 1;
            //
            // colFeeder
            //
            colFeeder.FillWeight = 30F;
            colFeeder.HeaderText = "Feeder";
            colFeeder.Name = "colFeeder";
            colFeeder.ReadOnly = true;
            //
            // colConsumers
            //
            colConsumers.FillWeight = 11F;
            colConsumers.HeaderText = "Consumers";
            colConsumers.Name = "colConsumers";
            colConsumers.ReadOnly = true;
            //
            // colCopied
            //
            colCopied.FillWeight = 11F;
            colCopied.HeaderText = "Copied";
            colCopied.Name = "colCopied";
            colCopied.ReadOnly = true;
            //
            // colNotFound
            //
            colNotFound.FillWeight = 11F;
            colNotFound.HeaderText = "Not found";
            colNotFound.Name = "colNotFound";
            colNotFound.ReadOnly = true;
            //
            // colDuplicates
            //
            colDuplicates.FillWeight = 11F;
            colDuplicates.HeaderText = "Duplicates";
            colDuplicates.Name = "colDuplicates";
            colDuplicates.ReadOnly = true;
            //
            // colUnreadable
            //
            colUnreadable.FillWeight = 11F;
            colUnreadable.HeaderText = "Unreadable";
            colUnreadable.Name = "colUnreadable";
            colUnreadable.ReadOnly = true;
            //
            // colTotalKwh
            //
            colTotalKwh.FillWeight = 15F;
            colTotalKwh.HeaderText = "kWh total";
            colTotalKwh.Name = "colTotalKwh";
            colTotalKwh.ReadOnly = true;
            //
            // tlpProblemsHeader
            //
            tlpProblemsHeader.AutoSize = true;
            tlpProblemsHeader.ColumnCount = 2;
            tlpProblemsHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpProblemsHeader.ColumnStyles.Add(new ColumnStyle());
            tlpProblemsHeader.Controls.Add(lblProblemsTitle, 0, 0);
            tlpProblemsHeader.Controls.Add(btnSaveProblems, 1, 0);
            tlpProblemsHeader.Dock = DockStyle.Fill;
            tlpProblemsHeader.Margin = new Padding(0);
            tlpProblemsHeader.Name = "tlpProblemsHeader";
            tlpProblemsHeader.RowCount = 1;
            tlpProblemsHeader.RowStyles.Add(new RowStyle());
            tlpProblemsHeader.TabIndex = 2;
            //
            // lblProblemsTitle
            //
            lblProblemsTitle.Anchor = AnchorStyles.Left;
            lblProblemsTitle.AutoSize = true;
            lblProblemsTitle.Name = "lblProblemsTitle";
            lblProblemsTitle.Text = "Problems (kWh left blank and highlighted in the export)";
            //
            // btnSaveProblems
            //
            btnSaveProblems.AutoSize = true;
            btnSaveProblems.Enabled = false;
            btnSaveProblems.MinimumSize = new Size(170, 32);
            btnSaveProblems.Name = "btnSaveProblems";
            btnSaveProblems.TabIndex = 0;
            btnSaveProblems.Text = "Save problem list…";
            btnSaveProblems.Click += btnSaveProblems_Click;
            //
            // dgvProblems
            //
            dgvProblems.AllowUserToAddRows = false;
            dgvProblems.AllowUserToDeleteRows = false;
            dgvProblems.AllowUserToResizeRows = false;
            dgvProblems.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvProblems.Columns.AddRange(new DataGridViewColumn[] { colProblemFeeder, colProblemCount, colProblemBus, colProblemConsumer, colProblemText });
            dgvProblems.Dock = DockStyle.Fill;
            dgvProblems.MultiSelect = false;
            dgvProblems.Name = "dgvProblems";
            dgvProblems.ReadOnly = true;
            dgvProblems.RowHeadersVisible = false;
            dgvProblems.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvProblems.TabIndex = 3;
            dgvProblems.VirtualMode = true;
            dgvProblems.CellValueNeeded += dgvProblems_CellValueNeeded;
            //
            // colProblemFeeder
            //
            colProblemFeeder.FillWeight = 25F;
            colProblemFeeder.HeaderText = "Feeder";
            colProblemFeeder.Name = "colProblemFeeder";
            colProblemFeeder.ReadOnly = true;
            colProblemFeeder.SortMode = DataGridViewColumnSortMode.NotSortable;
            //
            // colProblemCount
            //
            colProblemCount.FillWeight = 8F;
            colProblemCount.HeaderText = "Count";
            colProblemCount.Name = "colProblemCount";
            colProblemCount.ReadOnly = true;
            colProblemCount.SortMode = DataGridViewColumnSortMode.NotSortable;
            //
            // colProblemBus
            //
            colProblemBus.FillWeight = 20F;
            colProblemBus.HeaderText = "Bus ID";
            colProblemBus.Name = "colProblemBus";
            colProblemBus.ReadOnly = true;
            colProblemBus.SortMode = DataGridViewColumnSortMode.NotSortable;
            //
            // colProblemConsumer
            //
            colProblemConsumer.FillWeight = 20F;
            colProblemConsumer.HeaderText = "Consumer ID";
            colProblemConsumer.Name = "colProblemConsumer";
            colProblemConsumer.ReadOnly = true;
            colProblemConsumer.SortMode = DataGridViewColumnSortMode.NotSortable;
            //
            // colProblemText
            //
            colProblemText.FillWeight = 27F;
            colProblemText.HeaderText = "Problem";
            colProblemText.Name = "colProblemText";
            colProblemText.ReadOnly = true;
            colProblemText.SortMode = DataGridViewColumnSortMode.NotSortable;
            //
            // tlpExport
            //
            tlpExport.AutoSize = true;
            tlpExport.ColumnCount = 5;
            tlpExport.ColumnStyles.Add(new ColumnStyle());
            tlpExport.ColumnStyles.Add(new ColumnStyle());
            tlpExport.ColumnStyles.Add(new ColumnStyle());
            tlpExport.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpExport.ColumnStyles.Add(new ColumnStyle());
            tlpExport.Controls.Add(lblExportAs, 0, 0);
            tlpExport.Controls.Add(rdoWorkbook, 1, 0);
            tlpExport.Controls.Add(rdoSeparate, 2, 0);
            tlpExport.Controls.Add(btnExport, 4, 0);
            tlpExport.Dock = DockStyle.Fill;
            tlpExport.Margin = new Padding(3, 10, 3, 3);
            tlpExport.Name = "tlpExport";
            tlpExport.Padding = new Padding(14, 10, 14, 10);
            tlpExport.RowCount = 1;
            tlpExport.RowStyles.Add(new RowStyle());
            tlpExport.TabIndex = 4;
            //
            // lblExportAs
            //
            lblExportAs.Anchor = AnchorStyles.Left;
            lblExportAs.AutoSize = true;
            lblExportAs.Name = "lblExportAs";
            lblExportAs.Text = "Export as";
            //
            // rdoWorkbook
            //
            rdoWorkbook.Anchor = AnchorStyles.Left;
            rdoWorkbook.AutoSize = true;
            rdoWorkbook.Checked = true;
            rdoWorkbook.Margin = new Padding(12, 3, 12, 3);
            rdoWorkbook.Name = "rdoWorkbook";
            rdoWorkbook.TabIndex = 0;
            rdoWorkbook.TabStop = true;
            rdoWorkbook.Text = "One workbook, sheet per feeder";
            //
            // rdoSeparate
            //
            rdoSeparate.Anchor = AnchorStyles.Left;
            rdoSeparate.AutoSize = true;
            rdoSeparate.Margin = new Padding(3, 3, 12, 3);
            rdoSeparate.Name = "rdoSeparate";
            rdoSeparate.TabIndex = 1;
            rdoSeparate.Text = "Separate file per feeder";
            //
            // btnExport
            //
            btnExport.AutoSize = true;
            btnExport.Enabled = false;
            btnExport.MinimumSize = new Size(160, 34);
            btnExport.Name = "btnExport";
            btnExport.TabIndex = 2;
            btnExport.Text = "Export to Excel…";
            btnExport.Click += btnExport_Click;
            //
            // CopyView
            //
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(tlpRoot);
            Name = "CopyView";
            Size = new Size(1100, 680);
            tlpRoot.ResumeLayout(false);
            tlpRoot.PerformLayout();
            tlpFile.ResumeLayout(false);
            tlpFile.PerformLayout();
            flpMonth.ResumeLayout(false);
            flpMonth.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numYear).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvTotals).EndInit();
            tlpProblemsHeader.ResumeLayout(false);
            tlpProblemsHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvProblems).EndInit();
            tlpExport.ResumeLayout(false);
            tlpExport.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel tlpRoot;
        private TableLayoutPanel tlpFile;
        private Label lblFileTitle;
        private Button btnChooseFile;
        private Label lblFile;
        private FlowLayoutPanel flpMonth;
        private Label lblMonth;
        private ComboBox cboMonth;
        private Label lblYear;
        private NumericUpDown numYear;
        private Button btnCopy;
        private Label lblHeading;
        private Label lblMessage;
        private DataGridView dgvTotals;
        private DataGridViewTextBoxColumn colFeeder;
        private DataGridViewTextBoxColumn colConsumers;
        private DataGridViewTextBoxColumn colCopied;
        private DataGridViewTextBoxColumn colNotFound;
        private DataGridViewTextBoxColumn colDuplicates;
        private DataGridViewTextBoxColumn colUnreadable;
        private DataGridViewTextBoxColumn colTotalKwh;
        private TableLayoutPanel tlpProblemsHeader;
        private Label lblProblemsTitle;
        private Button btnSaveProblems;
        private DataGridView dgvProblems;
        private DataGridViewTextBoxColumn colProblemFeeder;
        private DataGridViewTextBoxColumn colProblemCount;
        private DataGridViewTextBoxColumn colProblemBus;
        private DataGridViewTextBoxColumn colProblemConsumer;
        private DataGridViewTextBoxColumn colProblemText;
        private TableLayoutPanel tlpExport;
        private Label lblExportAs;
        private RadioButton rdoWorkbook;
        private RadioButton rdoSeparate;
        private Button btnExport;
    }
}
