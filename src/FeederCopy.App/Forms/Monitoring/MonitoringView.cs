using FeederCopy.App.Utils;
using FeederCopy.Core.Models.Monitoring;
using FeederCopy.Core.Presenters.Monitoring;

namespace FeederCopy.App.Forms.Monitoring
{
    public sealed partial class MonitoringView : UserControl, IMonitoringView
    {
        private MonitoringPresenter? _presenter;
        private bool _updating;

        public MonitoringView()
        {
            InitializeComponent();
            ApplyTheme();
        }

        public void Attach(MonitoringPresenter presenter) => _presenter = presenter;

        public void ShowYears(IReadOnlyList<int> years, int selectedYear)
        {
            _updating = true;
            cboYear.Items.Clear();
            foreach (int year in years)
                cboYear.Items.Add(year);
            cboYear.SelectedItem = years.Contains(selectedYear) ? selectedYear : null;
            cboYear.Enabled = years.Count > 0;
            _updating = false;
        }

        public void ShowSections(IReadOnlyList<MonitoringSection> sections, IReadOnlyList<int> months, string summary)
        {
            lblSummary.Text = summary;
            lblMessage.Visible = false;
            dgvMonths.Visible = true;
            DataGridViewColumn[] monthColumns = MonthColumns;
            for (int m = 0; m < monthColumns.Length; m++)
                monthColumns[m].Visible = months.Contains(m + 1);
            dgvMonths.Rows.Clear();
            foreach (MonitoringSection section in sections)
            {
                int header = dgvMonths.Rows.Add(section.Title);
                AppTheme.StyleSectionRow(dgvMonths.Rows[header]);
                string format = section.Kwh ? "N2" : "N0";
                foreach (FeederMonthsRow row in section.Rows)
                {
                    object[] cells = [row.Feeder, .. row.Months.Select(v => v is double value ? value.ToString(format) : string.Empty)];
                    int index = dgvMonths.Rows.Add(cells);
                    if (row.IsTotal)
                        AppTheme.StyleTotalRow(dgvMonths.Rows[index]);
                }
            }
        }

        public void ShowEmpty(string message)
        {
            lblSummary.Text = string.Empty;
            dgvMonths.Rows.Clear();
            dgvMonths.Visible = false;
            lblMessage.Text = message;
            lblMessage.Visible = true;
        }

        public void ShowLoading(string message)
        {
            lblSummary.Text = string.Empty;
            dgvMonths.Visible = false;
            lblMessage.Text = message;
            lblMessage.Visible = true;
        }

        public void SetExportEnabled(bool enabled) => btnExport.Enabled = enabled;

        public string? PickSaveFile(string filter, string defaultName) => Dialogs.PickSaveFile(Owner, filter, defaultName);

        public void ShowError(string message) => Dialogs.ShowError(Owner, message);

        public void ShowInfo(string message) => Dialogs.ShowInfo(Owner, message);

        private IWin32Window Owner => (IWin32Window?)FindForm() ?? this;

        private DataGridViewColumn[] MonthColumns => [colJan, colFeb, colMar, colApr, colMay, colJun, colJul, colAug, colSep, colOct, colNov, colDec];

        private CheckBox[] ShowBoxes => [chkKwh, chkActive, chkInactive];

        private void ApplyTheme()
        {
            AppTheme.StyleView(this);
            AppTheme.StyleCard(tlpFilter, lblTitle);
            AppTheme.StyleFieldLabel(lblYear);
            AppTheme.StyleComboBox(cboYear);
            AppTheme.StyleMutedLabel(lblSummary);
            AppTheme.StylePrimaryButton(btnExport);
            AppTheme.StyleFieldLabel(lblShow);
            foreach (CheckBox box in ShowBoxes)
                AppTheme.StyleCheckBox(box);
            AppTheme.StyleEmptyState(lblMessage);
            AppTheme.StyleGrid(dgvMonths);
            foreach (DataGridViewColumn column in MonthColumns)
                AppTheme.AlignRight(column);
        }

        private async void cboYear_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!_updating && cboYear.SelectedItem is int year && _presenter is not null)
                await _presenter.OnYearChangedAsync(year);
        }

        private void show_CheckedChanged(object? sender, EventArgs e) =>
            _presenter?.OnMeasuresChanged(ShowBoxes.Select((box, i) => (box, i)).Where(x => x.box.Checked).Select(x => x.i).ToList());

        private void btnExport_Click(object? sender, EventArgs e) => _presenter?.OnExportClicked();
    }
}
