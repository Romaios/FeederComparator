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

        public void ShowRows(IReadOnlyList<MonthSummaryRow> rows, string summary)
        {
            lblSummary.Text = summary;
            lblMessage.Visible = false;
            dgvSummary.Visible = true;
            dgvSummary.Rows.Clear();
            foreach (MonthSummaryRow row in rows)
            {
                int index = dgvSummary.Rows.Add(row.Month.ToString(), row.Feeder, row.TotalKwh.ToString("N2"), row.Active.ToString("N0"), row.Inactive.ToString("N0"));
                if (row.IsTotal)
                    AppTheme.StyleTotalRow(dgvSummary.Rows[index]);
            }
        }

        public void ShowEmpty(string message)
        {
            lblSummary.Text = string.Empty;
            dgvSummary.Rows.Clear();
            dgvSummary.Visible = false;
            lblMessage.Text = message;
            lblMessage.Visible = true;
        }

        public void ShowLoading(string message)
        {
            lblSummary.Text = string.Empty;
            dgvSummary.Visible = false;
            lblMessage.Text = message;
            lblMessage.Visible = true;
        }

        public void SetExportEnabled(bool enabled) => btnExport.Enabled = enabled;

        public string? PickSaveFile(string filter, string defaultName) => Dialogs.PickSaveFile(Owner, filter, defaultName);

        public void ShowError(string message) => Dialogs.ShowError(Owner, message);

        public void ShowInfo(string message) => Dialogs.ShowInfo(Owner, message);

        private IWin32Window Owner => (IWin32Window?)FindForm() ?? this;

        private void ApplyTheme()
        {
            AppTheme.StyleView(this);
            AppTheme.StyleCard(tlpFilter, lblTitle);
            AppTheme.StyleFieldLabel(lblYear);
            AppTheme.StyleComboBox(cboYear);
            AppTheme.StyleMutedLabel(lblSummary);
            AppTheme.StylePrimaryButton(btnExport);
            AppTheme.StyleEmptyState(lblMessage);
            AppTheme.StyleGrid(dgvSummary);
            foreach (DataGridViewColumn column in new[] { colTotalKwh, colActive, colInactive })
                AppTheme.AlignRight(column);
        }

        private async void cboYear_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!_updating && cboYear.SelectedItem is int year && _presenter is not null)
                await _presenter.OnYearChangedAsync(year);
        }

        private void btnExport_Click(object? sender, EventArgs e) => _presenter?.OnExportClicked();
    }
}
