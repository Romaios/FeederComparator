using FeederCopy.App.Utils;
using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Presenters.BusSort;

namespace FeederCopy.App.Forms.BusSort
{
    public sealed partial class BusSortView : UserControl, IBusSortView
    {
        private const int DropLineHeight = 3;
        private const int ScrollZone = 24;

        private BusSortPresenter? _presenter;
        private IReadOnlyList<BusBlockRow> _blocks = [];
        private IReadOnlyList<BusTagRow> _tags = [];
        private bool _populating;
        private int[] _selection = [];
        private int[] _previousSelection = [];
        private bool _selectionChangedByMouse;
        private int[] _dragRows = [];
        private Rectangle _dragBox = Rectangle.Empty;
        private int _dropIndex = -1;

        public BusSortView()
        {
            InitializeComponent();
            ApplyTheme();
        }

        public void Attach(BusSortPresenter presenter) => _presenter = presenter;

        public void ShowFeeders(IReadOnlyList<FeederSummary> feeders, long selectedId)
        {
            _populating = true;
            cboFeeder.BeginUpdate();
            cboFeeder.Items.Clear();
            foreach (FeederSummary feeder in feeders)
                cboFeeder.Items.Add(new FeederItem(feeder));
            cboFeeder.EndUpdate();
            cboFeeder.SelectedIndex = feeders.ToList().FindIndex(f => f.Id == selectedId);
            _populating = false;
        }

        public void ShowPlan(string summary, IReadOnlyList<BusBlockRow> blocks, IReadOnlyList<BusTagRow> tags, bool hasChanges)
        {
            lblEmpty.Visible = false;
            tlpBody.Visible = true;
            tlpHeader.Visible = true;
            lblSummary.Text = summary;
            btnUndo.Enabled = hasChanges;
            btnSave.Enabled = hasChanges;

            int firstBlock = dgvBlocks.FirstDisplayedScrollingRowIndex;
            _blocks = blocks;
            dgvBlocks.RowCount = 0;
            dgvBlocks.RowCount = blocks.Count;
            if (firstBlock > 0 && firstBlock < blocks.Count && dgvBlocks.DisplayedRowCount(false) > 0)
                dgvBlocks.FirstDisplayedScrollingRowIndex = firstBlock;
            dgvBlocks.Invalidate();

            if (!ReferenceEquals(tags, _tags))
            {
                _populating = true;
                _tags = tags;
                dgvTags.Rows.Clear();
                foreach (BusTagRow tag in tags)
                    dgvTags.Rows.Add(tag.Code, tag.Tag, tag.Buses.ToString("N0"), tag.Example, tag.HandPlaced);
                _populating = false;
            }
            UpdateMoveButtons();
        }

        public void SelectBlocks(IReadOnlyList<int> indexes)
        {
            List<int> rows = indexes.Where(i => i >= 0 && i < dgvBlocks.RowCount).ToList();
            if (rows.Count == 0)
                return;
            dgvBlocks.CurrentCell = dgvBlocks.Rows[rows[0]].Cells[colBus.Index];
            dgvBlocks.ClearSelection();
            foreach (int row in rows)
                dgvBlocks.Rows[row].Selected = true;
            int displayed = dgvBlocks.DisplayedRowCount(false);
            int first = dgvBlocks.FirstDisplayedScrollingRowIndex;
            if (displayed > 0 && (rows[0] < first || rows[^1] >= first + displayed))
                dgvBlocks.FirstDisplayedScrollingRowIndex = Math.Max(0, rows[0] - 3);
        }

        public void ShowEmpty(string message)
        {
            _blocks = [];
            _tags = [];
            dgvBlocks.RowCount = 0;
            dgvTags.Rows.Clear();
            tlpBody.Visible = false;
            tlpHeader.Visible = false;
            lblEmpty.Text = message;
            lblEmpty.Visible = true;
        }

        public bool Confirm(string title, string message) => Dialogs.Confirm(Owner, title, message);

        public void ShowInfo(string message) => Dialogs.ShowInfo(Owner, message);

        public void ShowError(string message) => Dialogs.ShowError(Owner, message);

        private IWin32Window Owner => (IWin32Window?)FindForm() ?? this;

        private void ApplyTheme()
        {
            AppTheme.StyleView(this);
            AppTheme.StyleCardPanel(tlpHeader);
            AppTheme.StyleFieldLabel(lblFeeder);
            AppTheme.StyleComboBox(cboFeeder);
            lblSummary.Font = AppTheme.BodyStrongFont;
            lblSummary.ForeColor = AppTheme.TextSecondary;
            AppTheme.StyleOutlineButton(btnSort);
            AppTheme.StyleOutlineButton(btnMoveUp);
            AppTheme.StyleOutlineButton(btnMoveDown);
            AppTheme.StyleOutlineButton(btnUndo);
            AppTheme.StylePrimaryButton(btnSave);
            AppTheme.StyleEmptyState(lblEmpty);
            AppTheme.StyleCard(tlpBlocksCard, lblBlocksTitle);
            AppTheme.StyleMutedLabel(lblBlocksHelp);
            AppTheme.StyleGrid(dgvBlocks);
            AppTheme.AlignRight(colRowsNow);
            AppTheme.AlignRight(colCount);
            AppTheme.AlignRight(colRowsAfter);
            AppTheme.StyleCard(tlpTagsCard, lblTagsTitle);
            AppTheme.StyleMutedLabel(lblTagsHelp);
            AppTheme.StyleGrid(dgvTags);
            AppTheme.AlignRight(colTagBuses);
        }

        private static string Range(int from, int to) => from == to ? from.ToString("N0") : $"{from:N0} – {to:N0}";

        private int[] SelectedIndexes()
        {
            var rows = new List<int>();
            int index = dgvBlocks.Rows.GetFirstRow(DataGridViewElementStates.Selected);
            while (index >= 0)
            {
                rows.Add(index);
                index = dgvBlocks.Rows.GetNextRow(index, DataGridViewElementStates.Selected);
            }
            return rows.ToArray();
        }

        private void UpdateMoveButtons()
        {
            btnMoveUp.Enabled = _selection.Length > 0 && _selection.Min() > 0;
            btnMoveDown.Enabled = _selection.Length > 0 && _selection.Max() < _blocks.Count - 1;
        }

        private int DropIndexAt(Point client)
        {
            DataGridView.HitTestInfo hit = dgvBlocks.HitTest(client.X, client.Y);
            if (hit.RowIndex < 0)
                return client.Y <= dgvBlocks.ColumnHeadersHeight ? dgvBlocks.FirstDisplayedScrollingRowIndex : dgvBlocks.RowCount;
            Rectangle row = dgvBlocks.GetRowDisplayRectangle(hit.RowIndex, false);
            return client.Y > row.Top + row.Height / 2 ? hit.RowIndex + 1 : hit.RowIndex;
        }

        private void cboFeeder_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!_populating && cboFeeder.SelectedItem is FeederItem item)
                _presenter?.OnFeederSelected(item.Feeder.Id);
        }

        private void btnSort_Click(object? sender, EventArgs e) => _presenter?.OnSortClicked();

        private void btnMoveUp_Click(object? sender, EventArgs e) => _presenter?.OnMoveUpClicked(SelectedIndexes());

        private void btnMoveDown_Click(object? sender, EventArgs e) => _presenter?.OnMoveDownClicked(SelectedIndexes());

        private void btnUndo_Click(object? sender, EventArgs e) => _presenter?.OnUndoClicked();

        private void btnSave_Click(object? sender, EventArgs e) => _presenter?.OnSaveClicked();

        private void dgvBlocks_SelectionChanged(object? sender, EventArgs e)
        {
            _previousSelection = _selection;
            _selection = SelectedIndexes();
            _selectionChangedByMouse = MouseButtons == MouseButtons.Left;
            UpdateMoveButtons();
        }

        private void dgvBlocks_MouseDown(object? sender, MouseEventArgs e)
        {
            _dragBox = Rectangle.Empty;
            DataGridView.HitTestInfo hit = dgvBlocks.HitTest(e.X, e.Y);
            if (e.Button != MouseButtons.Left || hit.RowIndex < 0 || ModifierKeys != Keys.None)
                return;
            bool clickedInsideGroup = _selectionChangedByMouse && _previousSelection.Length > 1 && _previousSelection.Contains(hit.RowIndex);
            _dragRows = clickedInsideGroup ? _previousSelection : _selection.Contains(hit.RowIndex) ? _selection : [hit.RowIndex];
            _selectionChangedByMouse = false;
            Size size = SystemInformation.DragSize;
            _dragBox = new Rectangle(new Point(e.X - size.Width / 2, e.Y - size.Height / 2), size);
        }

        private void dgvBlocks_MouseMove(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || _dragBox == Rectangle.Empty || _dragBox.Contains(e.Location))
                return;
            _dragBox = Rectangle.Empty;
            int[] rows = _dragRows;
            if (rows.Length > 1)
                SelectBlocks(rows);
            dgvBlocks.DoDragDrop(rows, DragDropEffects.Move);
        }

        private void dgvBlocks_DragOver(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetData(typeof(int[])) is not int[])
            {
                e.Effect = DragDropEffects.None;
                return;
            }
            e.Effect = DragDropEffects.Move;
            Point client = dgvBlocks.PointToClient(new Point(e.X, e.Y));
            int first = dgvBlocks.FirstDisplayedScrollingRowIndex;
            if (client.Y < dgvBlocks.ColumnHeadersHeight + ScrollZone && first > 0)
                dgvBlocks.FirstDisplayedScrollingRowIndex = first - 1;
            else if (client.Y > dgvBlocks.ClientSize.Height - ScrollZone && first >= 0 && first + dgvBlocks.DisplayedRowCount(false) < dgvBlocks.RowCount)
                dgvBlocks.FirstDisplayedScrollingRowIndex = first + 1;
            int drop = DropIndexAt(client);
            if (drop != _dropIndex)
            {
                _dropIndex = drop;
                dgvBlocks.Invalidate();
            }
        }

        private void dgvBlocks_DragLeave(object? sender, EventArgs e)
        {
            _dropIndex = -1;
            dgvBlocks.Invalidate();
        }

        private void dgvBlocks_DragDrop(object? sender, DragEventArgs e)
        {
            int drop = DropIndexAt(dgvBlocks.PointToClient(new Point(e.X, e.Y)));
            _dropIndex = -1;
            dgvBlocks.Invalidate();
            if (e.Data?.GetData(typeof(int[])) is int[] rows)
                _presenter?.OnBlocksMoved(rows, drop);
        }

        private void dgvBlocks_Paint(object? sender, PaintEventArgs e)
        {
            if (_dropIndex < 0 || dgvBlocks.RowCount == 0)
                return;
            int y = _dropIndex < dgvBlocks.RowCount
                ? dgvBlocks.GetRowDisplayRectangle(_dropIndex, false).Top
                : dgvBlocks.GetRowDisplayRectangle(dgvBlocks.RowCount - 1, false).Bottom;
            if (y <= 0)
                return;
            using var brush = new SolidBrush(AppTheme.Highlight);
            e.Graphics.FillRectangle(brush, 0, Math.Max(dgvBlocks.ColumnHeadersHeight, y - DropLineHeight / 2), dgvBlocks.ClientSize.Width, DropLineHeight);
        }

        private void dgvBlocks_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _blocks.Count)
                return;
            BusBlockRow block = _blocks[e.RowIndex];
            int column = e.ColumnIndex;
            e.Value = column == colBus.Index ? block.Bus
                : column == colRowsNow.Index ? Range(block.From, block.To)
                : column == colCount.Index ? block.Count.ToString("N0")
                : column == colRowsAfter.Index ? Range(block.NewFrom, block.NewTo)
                : block.HandPlacedTag is { } tag ? $"Placed by hand ({tag})" : string.Empty;
        }

        private void dgvBlocks_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.CellStyle is not null && e.RowIndex >= 0 && e.RowIndex < _blocks.Count && !_blocks[e.RowIndex].Moves)
                AppTheme.StyleMutedCell(e.CellStyle);
        }

        private void dgvTags_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (dgvTags.IsCurrentCellDirty && dgvTags.CurrentCell?.ColumnIndex == colTagHand.Index)
                dgvTags.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void dgvTags_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (_populating || e.RowIndex < 0 || e.RowIndex >= _tags.Count || e.ColumnIndex != colTagHand.Index)
                return;
            bool handPlaced = dgvTags.Rows[e.RowIndex].Cells[colTagHand.Index].Value is true;
            BusTagRow tag = _tags[e.RowIndex];
            BeginInvoke(() => _presenter?.OnTagToggled(tag.Code, tag.Tag, handPlaced));
        }

        private sealed record FeederItem(FeederSummary Feeder)
        {
            public override string ToString() => $"{Feeder.Name}  ({Feeder.Consumers:N0})";
        }
    }
}
