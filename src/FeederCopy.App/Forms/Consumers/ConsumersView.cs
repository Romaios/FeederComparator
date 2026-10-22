using FeederCopy.App.Utils;
using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Consumers;
using FeederCopy.Core.Presenters.Consumers;

namespace FeederCopy.App.Forms.Consumers
{
    public sealed partial class ConsumersView : UserControl, IConsumersView
    {
        private const string AllFeeders = "All feeders";
        private static readonly HashSet<string> ProblemCells = [ConsumerYearCalculator.MissingCell, "Duplicate", "Unreadable"];
        private const string InactiveCell = "Inactive";
        private const int DropLineHeight = 3;
        private const int ScrollZone = 24;

        private ConsumersPresenter? _presenter;
        private IReadOnlyList<ConsumerYearRow> _rows = [];
        private bool _populating;
        private bool _keepPosition;
        private int[] _selection = [];
        private int[] _previousSelection = [];
        private bool _selectionChangedByMouse;
        private int[] _dragRows = [];
        private Rectangle _dragBox = Rectangle.Empty;
        private int _dropIndex = -1;

        public ConsumersView()
        {
            InitializeComponent();
            ApplyTheme();
            _populating = true;
            cboShow.SelectedIndex = 0;
            _populating = false;
        }

        public void Attach(ConsumersPresenter presenter) => _presenter = presenter;

        public void ShowFilters(IReadOnlyList<string> feederNames, IReadOnlyList<int> years, int selectedYear, BillMonth suggestedInactiveMonth)
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
            if (cboInactiveMonth.SelectedIndex < 0)
                SetInactiveMonth(suggestedInactiveMonth);
            _populating = false;
        }

        public void ShowRows(IReadOnlyList<ConsumerYearRow> rows, string summary, int activeCount, int inactiveCount)
        {
            int firstRow = dgvConsumers.FirstDisplayedScrollingRowIndex;
            int currentRow = dgvConsumers.CurrentCell?.RowIndex ?? -1;
            _rows = rows;
            rdoActiveTab.Text = $"Active ({activeCount:N0})";
            rdoInactiveTab.Text = $"Inactive ({inactiveCount:N0})";
            lblSummary.Text = summary;
            lblEmpty.Visible = false;
            dgvConsumers.RowCount = 0;
            dgvConsumers.RowCount = rows.Count;
            if (_keepPosition && rows.Count > 0 && currentRow >= 0)
            {
                int row = Math.Min(currentRow, rows.Count - 1);
                dgvConsumers.CurrentCell = dgvConsumers.Rows[row].Cells[colConsumer.Index];
                if (firstRow >= 0 && dgvConsumers.DisplayedRowCount(false) > 0)
                    dgvConsumers.FirstDisplayedScrollingRowIndex = Math.Min(firstRow, rows.Count - 1);
            }
            _keepPosition = false;
            dgvConsumers.Invalidate();
            UpdateSelection();
        }

        public void ShowEmpty(string message)
        {
            _rows = [];
            dgvConsumers.RowCount = 0;
            rdoActiveTab.Text = "Active";
            rdoInactiveTab.Text = "Inactive";
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

        public void SelectConsumers(IReadOnlyList<string> consumerIds)
        {
            var wanted = new HashSet<string>(consumerIds, StringComparer.Ordinal);
            List<int> rows = Enumerable.Range(0, _rows.Count).Where(i => wanted.Contains(_rows[i].ConsumerId)).ToList();
            if (rows.Count == 0)
                return;
            dgvConsumers.CurrentCell = dgvConsumers.Rows[rows[0]].Cells[colConsumer.Index];
            dgvConsumers.ClearSelection();
            foreach (int row in rows)
                dgvConsumers.Rows[row].Selected = true;
            int displayed = dgvConsumers.DisplayedRowCount(false);
            int first = dgvConsumers.FirstDisplayedScrollingRowIndex;
            if (displayed > 0 && (rows[0] < first || rows[^1] >= first + displayed))
                dgvConsumers.FirstDisplayedScrollingRowIndex = Math.Max(0, rows[0] - 3);
        }

        public bool Confirm(string title, string message) => Dialogs.Confirm(Owner, title, message);

        public void ShowError(string message) => Dialogs.ShowError(Owner, message);

        private IWin32Window Owner => (IWin32Window?)FindForm() ?? this;

        private List<ConsumerYearRow> SelectedRows() =>
            SelectedIndexes().Where(i => i < _rows.Count).Select(i => _rows[i]).ToList();

        private int[] SelectedIndexes()
        {
            var rows = new List<int>();
            int index = dgvConsumers.Rows.GetFirstRow(DataGridViewElementStates.Selected);
            while (index >= 0)
            {
                rows.Add(index);
                index = dgvConsumers.Rows.GetNextRow(index, DataGridViewElementStates.Selected);
            }
            return rows.ToArray();
        }

        private int DropIndexAt(Point client)
        {
            DataGridView.HitTestInfo hit = dgvConsumers.HitTest(client.X, client.Y);
            if (hit.RowIndex < 0)
                return client.Y <= dgvConsumers.ColumnHeadersHeight ? dgvConsumers.FirstDisplayedScrollingRowIndex : dgvConsumers.RowCount;
            Rectangle row = dgvConsumers.GetRowDisplayRectangle(hit.RowIndex, false);
            return client.Y > row.Top + row.Height / 2 ? hit.RowIndex + 1 : hit.RowIndex;
        }

        private void ApplyTheme()
        {
            AppTheme.StyleView(this);
            AppTheme.StyleCardPanel(tlpFilters);
            foreach (Label label in new[] { lblFeeder, lblYear, lblShow, lblFind, lblInactiveFrom })
                AppTheme.StyleFieldLabel(label);
            AppTheme.StyleComboBox(cboFeeder);
            AppTheme.StyleComboBox(cboYear);
            AppTheme.StyleComboBox(cboShow);
            AppTheme.StyleTextBox(txtFind);
            AppTheme.StyleOutlineButton(btnFind);
            lblSummary.Font = AppTheme.BodyStrongFont;
            lblSummary.ForeColor = AppTheme.TextSecondary;
            AppTheme.StyleHeaderBar(tlpStatusTabs);
            AppTheme.StyleNavTab(rdoActiveTab);
            AppTheme.StyleNavTab(rdoInactiveTab);
            AppTheme.StyleEmptyState(lblEmpty);
            AppTheme.StyleGrid(dgvConsumers);
            AppTheme.AlignRight(colCount);
            for (int i = colJan.Index; i <= colTotal.Index; i++)
                AppTheme.AlignRight(dgvConsumers.Columns[i]);
            AppTheme.StyleCard(tlpSelected, lblSelected);
            AppTheme.StyleComboBox(cboInactiveMonth);
            AppTheme.StyleNumeric(numInactiveYear);
            AppTheme.StyleOutlineButton(btnSetInactive);
            AppTheme.StyleOutlineButton(btnSetActive);
        }

        private void SetInactiveMonth(BillMonth month)
        {
            cboInactiveMonth.SelectedIndex = month.Month - 1;
            numInactiveYear.Value = Math.Clamp(month.Year, (int)numInactiveYear.Minimum, (int)numInactiveYear.Maximum);
        }

        private void NotifyFilter()
        {
            if (_populating || _presenter is null || cboYear.SelectedItem is not int year)
                return;
            string? feeder = cboFeeder.SelectedIndex > 0 ? cboFeeder.SelectedItem as string : null;
            _presenter.OnFilterChanged(feeder, rdoActiveTab.Checked, year, txtFind.Text, (ConsumerProblem)Math.Max(0, cboShow.SelectedIndex));
        }

        private void UpdateSelection()
        {
            List<ConsumerYearRow> rows = SelectedRows();
            btnSetActive.Visible = rdoInactiveTab.Checked;
            btnSetActive.Enabled = rows.Any(r => !r.Active);
            btnSetInactive.Enabled = rows.Count > 0;
            btnSetInactive.Text = rows.Count > 0 && rows.All(r => !r.Active) ? "Change month" : "Set inactive";
            if (rows is [{ InactiveFrom: { } from }])
                SetInactiveMonth(from);
            lblSelected.Text = rows switch
            {
                [] => "Click a consumer to set its status. Hold Ctrl or Shift to pick more than one. Drag rows to reorder them inside their bus.",
                [var row] => $"Selected: {row.Feeder} row {row.Count:N0}  ·  bus {row.BusLabel}  ·  consumer {row.ConsumerId}  ·  {row.StatusLabel}",
                _ => $"Selected: {rows.Count:N0} consumers  ·  Ctrl+click or Shift+click to change the selection",
            };
        }

        private void filter_Changed(object? sender, EventArgs e) => NotifyFilter();

        private void statusTab_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is RadioButton { Checked: true })
                NotifyFilter();
        }

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
            _keepPosition = true;
            _presenter?.OnSetActiveClicked(SelectedRows().Where(r => !r.Active).Select(r => r.ConsumerId).ToList());
        }

        private void btnSetInactive_Click(object? sender, EventArgs e)
        {
            _keepPosition = true;
            _presenter?.OnSetInactiveClicked(SelectedRows().Select(r => r.ConsumerId).ToList(),
                cboInactiveMonth.SelectedIndex >= 0 ? cboInactiveMonth.SelectedIndex + 1 : null, (int)numInactiveYear.Value);
        }

        private void dgvConsumers_SelectionChanged(object? sender, EventArgs e)
        {
            _previousSelection = _selection;
            _selection = SelectedIndexes();
            _selectionChangedByMouse = MouseButtons == MouseButtons.Left;
            UpdateSelection();
        }

        private void dgvConsumers_MouseDown(object? sender, MouseEventArgs e)
        {
            _dragBox = Rectangle.Empty;
            DataGridView.HitTestInfo hit = dgvConsumers.HitTest(e.X, e.Y);
            if (e.Button != MouseButtons.Left || hit.RowIndex < 0 || ModifierKeys != Keys.None)
                return;
            bool clickedInsideGroup = _selectionChangedByMouse && _previousSelection.Length > 1 && _previousSelection.Contains(hit.RowIndex);
            _dragRows = clickedInsideGroup ? _previousSelection : _selection.Contains(hit.RowIndex) ? _selection : [hit.RowIndex];
            _selectionChangedByMouse = false;
            Size size = SystemInformation.DragSize;
            _dragBox = new Rectangle(new Point(e.X - size.Width / 2, e.Y - size.Height / 2), size);
        }

        private void dgvConsumers_MouseMove(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || _dragBox == Rectangle.Empty || _dragBox.Contains(e.Location))
                return;
            _dragBox = Rectangle.Empty;
            string[] ids = _dragRows.Where(i => i < _rows.Count).Select(i => _rows[i].ConsumerId).ToArray();
            if (ids.Length == 0)
                return;
            if (ids.Length > 1)
                SelectConsumers(ids);
            dgvConsumers.DoDragDrop(ids, DragDropEffects.Move);
        }

        private void dgvConsumers_DragOver(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetData(typeof(string[])) is not string[])
            {
                e.Effect = DragDropEffects.None;
                return;
            }
            e.Effect = DragDropEffects.Move;
            Point client = dgvConsumers.PointToClient(new Point(e.X, e.Y));
            int first = dgvConsumers.FirstDisplayedScrollingRowIndex;
            if (client.Y < dgvConsumers.ColumnHeadersHeight + ScrollZone && first > 0)
                dgvConsumers.FirstDisplayedScrollingRowIndex = first - 1;
            else if (client.Y > dgvConsumers.ClientSize.Height - ScrollZone && first >= 0 && first + dgvConsumers.DisplayedRowCount(false) < dgvConsumers.RowCount)
                dgvConsumers.FirstDisplayedScrollingRowIndex = first + 1;
            int drop = DropIndexAt(client);
            if (drop != _dropIndex)
            {
                _dropIndex = drop;
                dgvConsumers.Invalidate();
            }
        }

        private void dgvConsumers_DragLeave(object? sender, EventArgs e)
        {
            _dropIndex = -1;
            dgvConsumers.Invalidate();
        }

        private void dgvConsumers_DragDrop(object? sender, DragEventArgs e)
        {
            int drop = DropIndexAt(dgvConsumers.PointToClient(new Point(e.X, e.Y)));
            _dropIndex = -1;
            dgvConsumers.Invalidate();
            if (e.Data?.GetData(typeof(string[])) is not string[] ids || _rows.Count == 0)
                return;
            bool after = drop >= _rows.Count;
            ConsumerYearRow target = _rows[after ? _rows.Count - 1 : drop];
            _presenter?.OnConsumersDropped(ids, target.ConsumerId, after);
        }

        private void dgvConsumers_Paint(object? sender, PaintEventArgs e)
        {
            if (_dropIndex < 0 || dgvConsumers.RowCount == 0)
                return;
            int y = _dropIndex < dgvConsumers.RowCount
                ? dgvConsumers.GetRowDisplayRectangle(_dropIndex, false).Top
                : dgvConsumers.GetRowDisplayRectangle(dgvConsumers.RowCount - 1, false).Bottom;
            if (y <= 0)
                return;
            using var brush = new SolidBrush(AppTheme.Highlight);
            e.Graphics.FillRectangle(brush, 0, Math.Max(dgvConsumers.ColumnHeadersHeight, y - DropLineHeight / 2), dgvConsumers.ClientSize.Width, DropLineHeight);
        }

        private void dgvConsumers_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _rows.Count)
                return;
            ConsumerYearRow row = _rows[e.RowIndex];
            int column = e.ColumnIndex;
            e.Value = column == colCount.Index ? row.Count.ToString("N0")
                : column == colFeeder.Index ? row.Feeder
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
            bool isMonth = e.ColumnIndex >= colJan.Index && e.ColumnIndex <= colDec.Index;
            int month = e.ColumnIndex - colJan.Index;
            if (isMonth ? month >= row.ActiveMonths || row.Months[month] == InactiveCell : !row.Active)
                AppTheme.StyleMutedCell(e.CellStyle);
            if (isMonth && ProblemCells.Contains(row.Months[month]))
                AppTheme.StyleProblemCell(e.CellStyle);
        }
    }
}
