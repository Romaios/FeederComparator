using FeederCopy.App.Utils;
using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Presenters.BusSort;

namespace FeederCopy.App.Forms.BusSort
{
    public sealed partial class BusSortView : UserControl, IBusSortView
    {
        private BusSortPresenter? _presenter;
        private IReadOnlyList<BusBlockRow> _blocks = [];
        private IReadOnlyList<BusTagRow> _tags = [];
        private bool _populating;

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

        public void ShowPlan(string summary, IReadOnlyList<BusBlockRow> blocks, IReadOnlyList<BusTagRow> tags, bool canSort)
        {
            lblEmpty.Visible = false;
            tlpBody.Visible = true;
            tlpHeader.Visible = true;
            lblSummary.Text = summary;
            btnSort.Enabled = canSort;

            int firstBlock = dgvBlocks.FirstDisplayedScrollingRowIndex;
            _blocks = blocks;
            dgvBlocks.RowCount = 0;
            dgvBlocks.RowCount = blocks.Count;
            if (firstBlock > 0 && firstBlock < blocks.Count && dgvBlocks.DisplayedRowCount(false) > 0)
                dgvBlocks.FirstDisplayedScrollingRowIndex = firstBlock;
            dgvBlocks.Invalidate();

            _populating = true;
            _tags = tags;
            dgvTags.Rows.Clear();
            foreach (BusTagRow tag in tags)
                dgvTags.Rows.Add(tag.Code, tag.Tag, tag.Buses.ToString("N0"), tag.Example, tag.HandPlaced);
            _populating = false;
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
            AppTheme.StylePrimaryButton(btnSort);
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

        private void cboFeeder_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!_populating && cboFeeder.SelectedItem is FeederItem item)
                _presenter?.OnFeederSelected(item.Feeder.Id);
        }

        private void btnSort_Click(object? sender, EventArgs e) => _presenter?.OnSortClicked();

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
