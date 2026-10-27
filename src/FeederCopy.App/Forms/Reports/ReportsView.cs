using FeederCopy.App.Utils;
using FeederCopy.Core.Models.Monitoring;
using FeederCopy.Core.Presenters.Reports;

namespace FeederCopy.App.Forms.Reports
{
    public sealed partial class ReportsView : UserControl, IReportsView
    {
        private ReportsPresenter? _presenter;
        private bool _updating;

        public ReportsView()
        {
            InitializeComponent();
            ApplyTheme();
        }

        public void Attach(ReportsPresenter presenter) => _presenter = presenter;

        public void ShowFilters(IReadOnlyList<int> years, int selectedYear, IReadOnlyList<string> measures, int selectedMeasure)
        {
            _updating = true;
            cboYear.Items.Clear();
            foreach (int year in years)
                cboYear.Items.Add(year);
            cboYear.SelectedItem = years.Contains(selectedYear) ? selectedYear : null;
            cboYear.Enabled = years.Count > 0;
            cboShow.Items.Clear();
            foreach (string measure in measures)
                cboShow.Items.Add(measure);
            cboShow.SelectedIndex = selectedMeasure;
            cboShow.Enabled = years.Count > 0;
            _updating = false;
        }

        public void ShowRows(string title, string totalHeader, bool kwh, IReadOnlyList<FeederMonthsRow> rows, string summary)
        {
            lblHeading.Text = title;
            lblSummary.Text = summary;
            colTotal.HeaderText = totalHeader;
            lblMessage.Visible = false;
            dgvMonths.Visible = true;
            string format = kwh ? "N2" : "N0";
            dgvMonths.Rows.Clear();
            foreach (FeederMonthsRow row in rows)
            {
                object[] cells = [row.Feeder, .. row.Months.Select(v => Cell(v, format)), Cell(row.Total, format)];
                int index = dgvMonths.Rows.Add(cells);
                if (row.IsTotal)
                    AppTheme.StyleTotalRow(dgvMonths.Rows[index]);
            }
        }

        public void ShowEmpty(string message)
        {
            lblHeading.Text = string.Empty;
            lblSummary.Text = string.Empty;
            dgvMonths.Rows.Clear();
            dgvMonths.Visible = false;
            lblMessage.Text = message;
            lblMessage.Visible = true;
        }

        public void ShowLoading(string message)
        {
            lblHeading.Text = string.Empty;
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

        private CheckBox[] SheetBoxes => [chkKwh, chkActive, chkInactive, chkConsumers, chkDuplicates, chkUnreadable];

        private static string Cell(double? value, string format) => value is double v ? v.ToString(format) : string.Empty;

        private void ApplyTheme()
        {
            AppTheme.StyleView(this);
            AppTheme.StyleCard(tlpFilter, lblTitle);
            AppTheme.StyleFieldLabel(lblYear);
            AppTheme.StyleComboBox(cboYear);
            AppTheme.StyleFieldLabel(lblShow);
            AppTheme.StyleComboBox(cboShow);
            AppTheme.StyleMutedLabel(lblSummary);
            AppTheme.StyleSectionLabel(lblHeading);
            AppTheme.StyleEmptyState(lblMessage);
            AppTheme.StyleGrid(dgvMonths);
            foreach (DataGridViewColumn column in dgvMonths.Columns)
            {
                if (column != colFeeder)
                    AppTheme.AlignRight(column);
            }
            AppTheme.StyleCard(tlpExport, lblSheets);
            foreach (CheckBox box in SheetBoxes)
                AppTheme.StyleCheckBox(box);
            AppTheme.StylePrimaryButton(btnExport);
        }

        private async void filter_Changed(object? sender, EventArgs e)
        {
            if (!_updating && cboYear.SelectedItem is int year && _presenter is not null)
                await _presenter.OnFilterChangedAsync(year, cboShow.SelectedIndex);
        }

        private void btnExport_Click(object? sender, EventArgs e) =>
            _presenter?.OnExportClicked(SheetBoxes.Select((box, i) => (box, i)).Where(x => x.box.Checked).Select(x => x.i).ToList());
    }
}
