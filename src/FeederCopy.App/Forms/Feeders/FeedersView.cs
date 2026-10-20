using FeederCopy.App.Utils;
using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Presenters.Feeders;

namespace FeederCopy.App.Forms.Feeders
{
    public sealed partial class FeedersView : UserControl, IFeedersView
    {
        private FeedersPresenter? _presenter;
        private IReadOnlyList<ConsumerRow> _rows = [];
        private bool _populating;

        public FeedersView()
        {
            InitializeComponent();
            ApplyTheme();
            UpdateSelectionButtons();
        }

        public event Action<string?>? StatusChanged;

        public void Attach(FeedersPresenter presenter) => _presenter = presenter;

        public void ShowFeeders(IReadOnlyList<FeederSummary> feeders, long? selectedId)
        {
            _populating = true;
            lstFeeders.BeginUpdate();
            lstFeeders.Items.Clear();
            foreach (FeederSummary feeder in feeders)
                lstFeeders.Items.Add(new FeederItem(feeder));
            lstFeeders.SelectedIndex = feeders.ToList().FindIndex(f => f.Id == selectedId);
            lstFeeders.EndUpdate();
            _populating = false;
            tlpWorkspace.Visible = true;
            lblEmpty.Visible = false;
        }

        public void ShowNoFeeders()
        {
            _populating = true;
            lstFeeders.Items.Clear();
            _populating = false;
            _rows = [];
            dgvConsumers.RowCount = 0;
            tlpWorkspace.Visible = false;
            lblEmpty.Visible = true;
        }

        public void ShowConsumers(FeederSummary feeder, IReadOnlyList<ConsumerRow> rows, IReadOnlyList<string> buses, IReadOnlyList<FeederSummary> otherFeeders)
        {
            lblFeederName.Text = $"{feeder.Name}  ·  {rows.Count:N0} consumers";
            txtRename.Text = feeder.Name;
            SetItems(cboAddBus, buses);
            SetItems(cboSetBus, buses);
            cboMoveTo.Items.Clear();
            foreach (FeederSummary other in otherFeeders)
                cboMoveTo.Items.Add(new FeederItem(other));
            if (cboMoveTo.Items.Count > 0)
                cboMoveTo.SelectedIndex = 0;

            _rows = rows;
            dgvConsumers.RowCount = 0;
            dgvConsumers.RowCount = rows.Count;
            dgvConsumers.Invalidate();
            UpdateSelectionButtons();
        }

        public void ShowPreview(ImportPreview preview)
        {
            lblPreviewTitle.Text = preview.Title;
            lstPreviewFeeders.BeginUpdate();
            lstPreviewFeeders.Items.Clear();
            foreach (string feeder in preview.Feeders)
                lstPreviewFeeders.Items.Add(feeder);
            lstPreviewFeeders.EndUpdate();
            lstPreviewFeeders.Visible = preview.Feeders.Count > 0;
            lblPreviewSummary.Text = preview.CanSave
                ? "Check the list, then click Save feeders."
                : "Fix the problems below in the workbook, then import it again.";
            lblPreviewSummary.ForeColor = preview.CanSave ? AppTheme.TextPrimary : AppTheme.Danger;
            lstPreviewProblems.BeginUpdate();
            lstPreviewProblems.Items.Clear();
            foreach (string problem in preview.Problems)
                lstPreviewProblems.Items.Add($"Problem: {problem}");
            foreach (string note in preview.Notes)
                lstPreviewProblems.Items.Add($"Note: {note}");
            lstPreviewProblems.EndUpdate();
            lstPreviewProblems.Visible = lstPreviewProblems.Items.Count > 0;
            btnSavePreview.Enabled = preview.CanSave;
            tlpPreview.Visible = true;
        }

        public void HidePreview() => tlpPreview.Visible = false;

        public void SetBusy(bool busy, string? message)
        {
            tlpRoot.Enabled = !busy;
            UseWaitCursor = busy;
            StatusChanged?.Invoke(message);
        }

        public void ClearAddForm()
        {
            txtAddConsumer.Clear();
            txtAddConsumer.Focus();
        }

        public void ShowAddError(string? message) => lblAddError.Text = message ?? string.Empty;

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

        public void ShowSearchResult(string? message) => lblSearchResult.Text = message ?? string.Empty;

        public void ShowCreateError(string? message) => lblCreateError.Text = message ?? string.Empty;

        public void ClearCreateForm() => txtNewFeeder.Clear();

        public string? PickOpenFile(string title, string filter) => Dialogs.PickOpenFile(Owner, title, filter);

        public bool Confirm(string title, string message) => Dialogs.Confirm(Owner, title, message);

        public void ShowError(string message) => Dialogs.ShowError(Owner, message);

        private IWin32Window Owner => (IWin32Window?)FindForm() ?? this;

        private string? SelectedConsumer =>
            dgvConsumers.CurrentRow is { Index: var i } && i >= 0 && i < _rows.Count ? _rows[i].ConsumerId : null;

        private void ApplyTheme()
        {
            AppTheme.StyleView(this);
            AppTheme.StyleCardPanel(tlpLeft);
            AppTheme.StyleSectionLabel(lblFeedersTitle);
            AppTheme.StylePrimaryButton(btnImport);
            AppTheme.StyleListBox(lstFeeders);
            AppTheme.StyleFieldLabel(lblNewFeeder);
            AppTheme.StyleTextBox(txtNewFeeder);
            AppTheme.StyleOutlineButton(btnCreate);
            AppTheme.StyleErrorLabel(lblCreateError);
            AppTheme.StyleCard(tlpPreview, lblPreviewTitle);
            AppTheme.StyleListBox(lstPreviewFeeders);
            lblPreviewSummary.Font = AppTheme.BodyStrongFont;
            lblPreviewSummary.ForeColor = AppTheme.TextPrimary;
            AppTheme.StyleListBox(lstPreviewProblems);
            lstPreviewProblems.ForeColor = AppTheme.Danger;
            lstPreviewProblems.BackColor = AppTheme.SurfaceAlt;
            AppTheme.StylePrimaryButton(btnSavePreview);
            AppTheme.StyleOutlineButton(btnCancelPreview);
            AppTheme.StyleSectionLabel(lblFeederName);
            AppTheme.StyleTextBox(txtRename);
            AppTheme.StyleOutlineButton(btnRename);
            AppTheme.StyleDangerButton(btnDelete);
            AppTheme.StyleCard(tlpAdd, lblAddTitle);
            AppTheme.StyleFieldLabel(lblAddBus);
            AppTheme.StyleFieldLabel(lblAddConsumer);
            AppTheme.StyleComboBox(cboAddBus);
            AppTheme.StyleTextBox(txtAddConsumer);
            AppTheme.StylePrimaryButton(btnAdd);
            AppTheme.StyleErrorLabel(lblAddError);
            AppTheme.StyleFieldLabel(lblSearch);
            AppTheme.StyleTextBox(txtSearch);
            AppTheme.StyleOutlineButton(btnSearch);
            AppTheme.StyleErrorLabel(lblSearchResult);
            AppTheme.StyleGrid(dgvConsumers);
            AppTheme.AlignRight(colCount);
            AppTheme.StyleCard(tlpSelected, lblSelected);
            AppTheme.StyleFieldLabel(lblChangeBus);
            AppTheme.StyleFieldLabel(lblMoveTo);
            AppTheme.StyleComboBox(cboSetBus);
            AppTheme.StyleOutlineButton(btnSetBus);
            AppTheme.StyleComboBox(cboMoveTo);
            AppTheme.StyleOutlineButton(btnMove);
            AppTheme.StyleDangerButton(btnRemove);
            AppTheme.StyleEmptyState(lblEmpty);
        }

        private void UpdateSelectionButtons()
        {
            ConsumerRow? row = dgvConsumers.CurrentRow is { Index: var i } && i >= 0 && i < _rows.Count ? _rows[i] : null;
            bool selected = row is not null;
            cboSetBus.Enabled = selected;
            btnSetBus.Enabled = selected;
            cboMoveTo.Enabled = selected && cboMoveTo.Items.Count > 0;
            btnMove.Enabled = cboMoveTo.Enabled;
            btnRemove.Enabled = selected;
            lblSelected.Text = row is null
                ? "Click a consumer in the list to change its bus ID, move it, or remove it."
                : $"Selected: consumer {row.ConsumerId}  ·  bus {row.BusLabel}";
            if (row is not null)
                cboSetBus.Text = row.BusLabel == BusLabels.Unknown ? string.Empty : row.BusLabel;
        }

        private static void SetItems(ComboBox combo, IReadOnlyList<string> items)
        {
            string text = combo.Text;
            combo.BeginUpdate();
            combo.Items.Clear();
            foreach (string item in items)
                combo.Items.Add(item);
            combo.EndUpdate();
            combo.Text = text;
        }

        private async void btnImport_Click(object? sender, EventArgs e)
        {
            if (_presenter is not null)
                await _presenter.OnImportClickedAsync();
        }

        private void lstFeeders_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!_populating && lstFeeders.SelectedItem is FeederItem item)
                _presenter?.OnFeederSelected(item.Feeder.Id);
        }

        private void btnCreate_Click(object? sender, EventArgs e) => _presenter?.OnCreateClicked(txtNewFeeder.Text);

        private void txtNewFeeder_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;
            e.SuppressKeyPress = true;
            _presenter?.OnCreateClicked(txtNewFeeder.Text);
        }

        private void btnSavePreview_Click(object? sender, EventArgs e) => _presenter?.OnSavePreviewClicked();

        private void btnCancelPreview_Click(object? sender, EventArgs e) => _presenter?.OnCancelPreviewClicked();

        private void btnRename_Click(object? sender, EventArgs e) => _presenter?.OnRenameClicked(txtRename.Text);

        private void btnDelete_Click(object? sender, EventArgs e) => _presenter?.OnDeleteClicked();

        private void btnAdd_Click(object? sender, EventArgs e) => _presenter?.OnAddClicked(cboAddBus.Text, txtAddConsumer.Text);

        private void txtAddConsumer_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;
            e.SuppressKeyPress = true;
            _presenter?.OnAddClicked(cboAddBus.Text, txtAddConsumer.Text);
        }

        private void btnSearch_Click(object? sender, EventArgs e) => _presenter?.OnSearch(txtSearch.Text);

        private void txtSearch_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;
            e.SuppressKeyPress = true;
            _presenter?.OnSearch(txtSearch.Text);
        }

        private void btnSetBus_Click(object? sender, EventArgs e)
        {
            if (SelectedConsumer is string id)
                _presenter?.OnSetBusClicked(id, cboSetBus.Text);
        }

        private void btnMove_Click(object? sender, EventArgs e)
        {
            if (SelectedConsumer is string id && cboMoveTo.SelectedItem is FeederItem target)
                _presenter?.OnMoveClicked(id, target.Feeder.Id);
        }

        private void btnRemove_Click(object? sender, EventArgs e)
        {
            if (SelectedConsumer is string id)
                _presenter?.OnRemoveClicked(id);
        }

        private void dgvConsumers_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _rows.Count)
                return;
            ConsumerRow row = _rows[e.RowIndex];
            e.Value = e.ColumnIndex == colCount.Index ? row.Count.ToString("N0")
                : e.ColumnIndex == colBus.Index ? row.BusLabel
                : row.ConsumerId;
        }

        private void dgvConsumers_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.CellStyle is not null && e.ColumnIndex == colBus.Index && e.RowIndex >= 0 && e.RowIndex < _rows.Count
                && _rows[e.RowIndex].BusLabel == BusLabels.Unknown)
                AppTheme.StyleMutedCell(e.CellStyle);
        }

        private void dgvConsumers_SelectionChanged(object? sender, EventArgs e) => UpdateSelectionButtons();

        private sealed record FeederItem(FeederSummary Feeder)
        {
            public override string ToString() => $"{Feeder.Name}  ({Feeder.Consumers:N0})";
        }
    }
}
