using FeederCopy.App.Utils;
using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Copy;
using FeederCopy.Core.Models.Settings;
using FeederCopy.Core.Presenters.Copy;

namespace FeederCopy.App.Forms.Copy
{
    public sealed partial class CopyView : UserControl, ICopyView
    {
        private CopyPresenter? _presenter;
        private IReadOnlyList<CopyProblem> _problems = [];

        public CopyView()
        {
            InitializeComponent();
            ApplyTheme();
            numYear.Value = Math.Clamp(DateTime.Today.Year, (int)numYear.Minimum, (int)numYear.Maximum);
        }

        public event Action<string?>? StatusChanged;

        public void Attach(CopyPresenter presenter) => _presenter = presenter;

        public void ShowFileRead(string fileName, BillMonth? fileMonth, int rows, int skipped)
        {
            string month = fileMonth?.ToString() ?? "month not found";
            lblFile.Text = $"{fileName}  ·  {month}  ·  {rows:N0} rows  ·  {skipped:N0} skipped";
        }

        public void SetMonth(int? month, int? year)
        {
            cboMonth.SelectedIndex = month is int m ? m - 1 : -1;
            if (year is int y)
                numYear.Value = Math.Clamp(y, (int)numYear.Minimum, (int)numYear.Maximum);
        }

        public void ShowResult(IReadOnlyList<FeederTotals> totals, IReadOnlyList<CopyProblem> problems, string heading)
        {
            lblHeading.Text = heading;
            lblMessage.Visible = false;
            dgvTotals.Rows.Clear();
            foreach (FeederTotals t in totals)
                dgvTotals.Rows.Add(t.Name, t.Consumers.ToString("N0"), t.Active.ToString("N0"), t.Inactive.ToString("N0"),
                    t.Copied.ToString("N0"), t.MissingReading.ToString("N0"), t.InactiveButBilled.ToString("N0"),
                    t.Duplicates.ToString("N0"), t.Unreadable.ToString("N0"), t.TotalKwh.ToString("N2"));
            _problems = problems;
            dgvProblems.RowCount = 0;
            dgvProblems.RowCount = problems.Count;
            dgvProblems.Invalidate();
        }

        public void ClearResult(string message)
        {
            lblHeading.Text = string.Empty;
            lblMessage.Text = message;
            lblMessage.Visible = true;
            dgvTotals.Rows.Clear();
            _problems = [];
            dgvProblems.RowCount = 0;
        }

        public void SetExportStyle(ExportStyle style)
        {
            rdoWorkbook.Checked = style == ExportStyle.OneWorkbook;
            rdoSeparate.Checked = style == ExportStyle.SeparateFiles;
        }

        public void SetBusy(bool busy, string? message)
        {
            tlpRoot.Enabled = !busy;
            UseWaitCursor = busy;
            StatusChanged?.Invoke(message);
        }

        public void SetCopyEnabled(bool enabled) => btnCopy.Enabled = enabled;

        public void SetExportEnabled(bool enabled)
        {
            btnExport.Enabled = enabled;
            btnSaveProblems.Enabled = enabled;
        }

        public string? PickOpenFile(string title, string filter) => Dialogs.PickOpenFile(Owner, title, filter);

        public string? PickSaveFile(string filter, string defaultName) => Dialogs.PickSaveFile(Owner, filter, defaultName);

        public string? PickFolder() => Dialogs.PickFolder(Owner);

        public bool Confirm(string title, string message) => Dialogs.Confirm(Owner, title, message);

        public void ShowError(string message) => Dialogs.ShowError(Owner, message);

        public void ShowInfo(string message) => Dialogs.ShowInfo(Owner, message);

        private IWin32Window Owner => (IWin32Window?)FindForm() ?? this;

        private void ApplyTheme()
        {
            AppTheme.StyleView(this);
            AppTheme.StyleCard(tlpFile, lblFileTitle);
            AppTheme.StylePrimaryButton(btnChooseFile);
            lblFile.Font = AppTheme.BodyStrongFont;
            lblFile.ForeColor = AppTheme.TextSecondary;
            AppTheme.StyleFieldLabel(lblMonth);
            AppTheme.StyleComboBox(cboMonth);
            AppTheme.StyleFieldLabel(lblYear);
            AppTheme.StyleNumeric(numYear);
            AppTheme.StylePrimaryButton(btnCopy);
            AppTheme.StyleSectionLabel(lblHeading);
            AppTheme.StyleMutedLabel(lblMessage);
            AppTheme.StyleGrid(dgvTotals);
            foreach (DataGridViewColumn column in new[] { colConsumers, colActive, colInactive, colCopied, colMissing, colInactiveBilled, colDuplicates, colUnreadable, colTotalKwh })
                AppTheme.AlignRight(column);
            lblProblemsTitle.Font = AppTheme.BodyStrongFont;
            lblProblemsTitle.ForeColor = AppTheme.TextPrimary;
            AppTheme.StyleOutlineButton(btnSaveProblems);
            AppTheme.StyleGrid(dgvProblems);
            AppTheme.AlignRight(colProblemCount);
            AppTheme.StyleProblemCell(colProblemText.DefaultCellStyle);
            AppTheme.StyleCardPanel(tlpExport);
            AppTheme.StyleFieldLabel(lblExportAs);
            AppTheme.StyleRadio(rdoWorkbook);
            AppTheme.StyleRadio(rdoSeparate);
            AppTheme.StylePrimaryButton(btnExport);
        }

        private async void btnChooseFile_Click(object? sender, EventArgs e)
        {
            if (_presenter is not null)
                await _presenter.OnChooseFileClickedAsync();
        }

        private async void btnCopy_Click(object? sender, EventArgs e)
        {
            if (_presenter is not null)
                await _presenter.OnCopyClickedAsync(cboMonth.SelectedIndex >= 0 ? cboMonth.SelectedIndex + 1 : null, (int)numYear.Value);
        }

        private void btnSaveProblems_Click(object? sender, EventArgs e) => _presenter?.OnSaveProblemsClicked();

        private void btnExport_Click(object? sender, EventArgs e) =>
            _presenter?.OnExportClicked(rdoSeparate.Checked ? ExportStyle.SeparateFiles : ExportStyle.OneWorkbook);

        private void dgvProblems_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _problems.Count)
                return;
            CopyProblem p = _problems[e.RowIndex];
            e.Value = e.ColumnIndex == colProblemFeeder.Index ? p.Feeder
                : e.ColumnIndex == colProblemCount.Index ? p.Count.ToString("N0")
                : e.ColumnIndex == colProblemBus.Index ? p.BusLabel
                : e.ColumnIndex == colProblemConsumer.Index ? p.ConsumerId
                : p.Problem;
        }
    }
}
