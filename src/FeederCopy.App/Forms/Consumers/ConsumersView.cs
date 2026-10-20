using FeederCopy.App.Utils;
using FeederCopy.Core.Models.Consumers;
using FeederCopy.Core.Presenters.Consumers;

namespace FeederCopy.App.Forms.Consumers
{
    public sealed partial class ConsumersView : UserControl, IConsumersView
    {
        private const string AllFeeders = "All feeders";
        private static readonly HashSet<string> ProblemCells = ["Missing", "Duplicate", "Unreadable"];

        private ConsumersPresenter? _presenter;
        private IReadOnlyList<ConsumerYearRow> _rows = [];
        private bool _populating;

        public ConsumersView()
        {
            InitializeComponent();
            ApplyTheme();
            _populating = true;
            cboStatus.SelectedIndex = 0;
            _populating = false;
        }

        public void Attach(ConsumersPresenter presenter) => _presenter = presenter;

        public void ShowFilters(IReadOnlyList<string> feederNames, IReadOnlyList<int> years, int selectedYear)
        {
            _populating = true;
            string? feeder = cboFeeder.SelectedItem as string;
            cboFeeder.BeginUpdate();
            cboFeeder.Items.Clear();
            cboFeeder.Items.Add(AllFeeders);
            foreach (string name in feederNames)
                cboFeeder.Items.Add(name);
            cboFeeder.EndUpdate();
            int index = feeder is null ? 0 : cboFeeder.Items.IndexOf(feeder);
            cboFeeder.SelectedIndex = index < 0 ? 0 : index;

            cboYear.BeginUpdate();
            cboYear.Items.Clear();
            foreach (int year in years)
                cboYear.Items.Add(year);
            cboYear.EndUpdate();
            cboYear.SelectedItem = selectedYear;
            _populating = false;
        }

        public void ShowRows(IReadOnlyList<ConsumerYearRow> rows, string summary)
        {
            _rows = rows;
            lblSummary.Text = summary;
            lblEmpty.Visible = false;
            dgvConsumers.RowCount = 0;
            dgvConsumers.RowCount = rows.Count;
            dgvConsumers.Invalidate();
            UpdateSelection();
        }

        public void ShowEmpty(string message)
        {
            _rows = [];
            dgvConsumers.RowCount = 0;
            lblSummary.Text = string.Empty;
            lblEmpty.Text = message;
            lblEmpty.Visible = true;
            UpdateSelection();
        }

        public void SelectConsumer(string consumerId)
        {
            int index = _rows.ToList().FindIndex(r => r.ConsumerId == consumerId);
            if (index < 0)
                return;
            dgvConsumers.ClearSelection();
            dgvConsumers.CurrentCell = dgvConsumers.Rows[index].Cells[colConsumer.Index];
            dgvConsumers.Rows[index].Selected = true;
            if (dgvConsumers.DisplayedRowCount(false) > 0)
                dgvConsumers.FirstDisplayedScrollingRowIndex = Math.Max(0, index - 3);
        }

        public void ShowError(string message) => Dialogs.ShowError((IWin32Window?)FindForm() ?? this, message);

        private ConsumerYearRow? SelectedRow =>
            dgvConsumers.CurrentRow is { Index: var i } && i >= 0 && i < _rows.Count ? _rows[i] : null;

        private void ApplyTheme()
        {
            AppTheme.StyleView(this);
            AppTheme.StyleCardPanel(tlpFilters);
            foreach (Label label in new[] { lblFeeder, lblStatusFilter, lblYear, lblFind })
                AppTheme.StyleFieldLabel(label);
            AppTheme.StyleComboBox(cboFeeder);
            AppTheme.StyleComboBox(cboStatus);
            AppTheme.StyleComboBox(cboYear);
            AppTheme.StyleTextBox(txtFind);
            AppTheme.StyleOutlineButton(btnFind);
            lblSummary.Font = AppTheme.BodyStrongFont;
            lblSummary.ForeColor = AppTheme.TextSecondary;
            AppTheme.StyleEmptyState(lblEmpty);
            AppTheme.StyleGrid(dgvConsumers);
            for (int i = colJan.Index; i <= colTotal.Index; i++)
                AppTheme.AlignRight(dgvConsumers.Columns[i]);
            AppTheme.StyleCard(tlpSelected, lblSelected);
            AppTheme.StyleOutlineButton(btnSetActive);
            AppTheme.StyleOutlineButton(btnSetInactive);
        }

        private void NotifyFilter()
        {
            if (_populating || _presenter is null || cboYear.SelectedItem is not int year)
                return;
            string? feeder = cboFeeder.SelectedIndex > 0 ? cboFeeder.SelectedItem as string : null;
            bool? active = cboStatus.SelectedIndex switch
            {
                1 => true,
                2 => false,
                _ => null,
            };
            _presenter.OnFilterChanged(feeder, active, year, txtFind.Text);
        }

        private void UpdateSelection()
        {
            ConsumerYearRow? row = SelectedRow;
            btnSetActive.Enabled = row is { Active: false };
            btnSetInactive.Enabled = row is { Active: true };
            lblSelected.Text = row is null
                ? "Click a consumer to set its status."
                : $"Selected: consumer {row.ConsumerId}  ·  {row.Feeder}  ·  bus {row.BusLabel}  ·  {row.StatusLabel}";
        }

        private void filter_Changed(object? sender, EventArgs e) => NotifyFilter();

        private void btnFind_Click(object? sender, EventArgs e) => NotifyFilter();

        private void txtFind_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;
            e.SuppressKeyPress = true;
            NotifyFilter();
        }

        private void btnSetActive_Click(object? sender, EventArgs e)
        {
            if (SelectedRow is { } row)
                _presenter?.OnSetActiveClicked(row.ConsumerId, true);
        }

        private void btnSetInactive_Click(object? sender, EventArgs e)
        {
            if (SelectedRow is { } row)
                _presenter?.OnSetActiveClicked(row.ConsumerId, false);
        }

        private void dgvConsumers_SelectionChanged(object? sender, EventArgs e) => UpdateSelection();

        private void dgvConsumers_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _rows.Count)
                return;
            ConsumerYearRow row = _rows[e.RowIndex];
            int column = e.ColumnIndex;
            e.Value = column == colFeeder.Index ? row.Feeder
                : column == colBus.Index ? row.BusLabel
                : column == colConsumer.Index ? row.ConsumerId
                : column == colStatus.Index ? row.StatusLabel
                : column == colTotal.Index ? row.Total.ToString("N2")
                : row.Months[column - colJan.Index];
        }

        private void dgvConsumers_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.CellStyle is null || e.RowIndex < 0 || e.RowIndex >= _rows.Count)
                return;
            ConsumerYearRow row = _rows[e.RowIndex];
            if (!row.Active)
                AppTheme.StyleMutedCell(e.CellStyle);
            if (e.ColumnIndex >= colJan.Index && e.ColumnIndex <= colDec.Index && ProblemCells.Contains(row.Months[e.ColumnIndex - colJan.Index]))
                AppTheme.StyleProblemCell(e.CellStyle);
        }
    }
}
