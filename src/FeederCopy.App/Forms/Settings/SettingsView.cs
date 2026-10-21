using FeederCopy.App.Utils;
using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Models.Settings;
using FeederCopy.Core.Presenters.Settings;

namespace FeederCopy.App.Forms.Settings
{
    public sealed partial class SettingsView : UserControl, ISettingsView
    {
        private const string AllFeeders = "All feeders";

        private SettingsPresenter? _presenter;
        private IReadOnlyList<BusTagRow> _tagRows = [];
        private bool _populating;

        public SettingsView()
        {
            InitializeComponent();
            ApplyTheme();
        }

        public void Attach(SettingsPresenter presenter) => _presenter = presenter;

        public void ShowSettings(AppSettings settings)
        {
            txtBusId.Text = settings.Feeder.BusId;
            txtConsumerId.Text = settings.Feeder.ConsumerId;
            txtStatusColumn.Text = settings.Feeder.Status;
            txtAccount.Text = settings.AsBilled.AccountNumber;
            txtKwh.Text = settings.AsBilled.KwhUsed;
            txtMonth.Text = settings.AsBilled.BillMonth;
            cboExportStyle.SelectedIndex = (int)settings.Export;
            cboTheme.SelectedIndex = (int)settings.Theme;
            lblSaved.Text = string.Empty;
        }

        public AppSettings ReadSettings() => new()
        {
            Feeder = new FeederColumns { BusId = txtBusId.Text.Trim(), ConsumerId = txtConsumerId.Text.Trim(), Status = txtStatusColumn.Text.Trim() },
            AsBilled = new AsBilledColumns { AccountNumber = txtAccount.Text.Trim(), KwhUsed = txtKwh.Text.Trim(), BillMonth = txtMonth.Text.Trim() },
            Export = cboExportStyle.SelectedIndex == (int)ExportStyle.SeparateFiles ? ExportStyle.SeparateFiles : ExportStyle.OneWorkbook,
            Theme = cboTheme.SelectedIndex == (int)ThemeMode.Dark ? ThemeMode.Dark : ThemeMode.Light,
        };

        public void ShowBusTags(IReadOnlyList<string> feeders, string? selectedFeeder, IReadOnlyList<BusTagRow> rows)
        {
            _populating = true;
            cboTagFeeder.BeginUpdate();
            cboTagFeeder.Items.Clear();
            cboTagFeeder.Items.Add(AllFeeders);
            foreach (string feeder in feeders)
                cboTagFeeder.Items.Add(feeder);
            cboTagFeeder.EndUpdate();
            int index = selectedFeeder is null ? 0 : cboTagFeeder.Items.IndexOf(selectedFeeder);
            cboTagFeeder.SelectedIndex = index < 0 ? 0 : index;

            int firstRow = dgvTags.FirstDisplayedScrollingRowIndex;
            _tagRows = rows;
            dgvTags.Rows.Clear();
            foreach (BusTagRow row in rows)
                dgvTags.Rows.Add(row.Feeder, row.Code, row.Tag, row.Buses.ToString("N0"), row.Example, row.HandPlaced);
            if (firstRow > 0 && firstRow < dgvTags.RowCount && dgvTags.DisplayedRowCount(false) > 0)
                dgvTags.FirstDisplayedScrollingRowIndex = firstRow;
            lblTagFeeder.Text = rows.Count == 0 ? "Feeder (import feeders to see their tags)" : "Feeder";
            _populating = false;
        }

        public bool Confirm(string title, string message) => Dialogs.Confirm((IWin32Window?)FindForm() ?? this, title, message);

        public void RestartApplication() => Application.Restart();

        public void ShowValidation(IReadOnlyList<string> errors)
        {
            lblValidation.Text = string.Join(Environment.NewLine, errors);
            if (errors.Count > 0)
                lblSaved.Text = string.Empty;
        }

        public void ShowSaved() => lblSaved.Text = "Saved.";

        private void ApplyTheme()
        {
            AppTheme.StyleView(this);
            AppTheme.StyleTitleLabel(lblTitle);
            AppTheme.StyleMutedLabel(lblIntro);
            AppTheme.StyleCard(tlpFeederCard, lblFeederTitle);
            AppTheme.StyleCard(tlpAsBilledCard, lblAsBilledTitle);
            AppTheme.StyleCard(tlpExportCard, lblExportTitle);
            AppTheme.StyleCard(tlpSortCard, lblSortTitle);
            AppTheme.StyleMutedLabel(lblSortHelp);
            AppTheme.StyleFieldLabel(lblTagFeeder);
            AppTheme.StyleComboBox(cboTagFeeder);
            AppTheme.StyleGrid(dgvTags);
            AppTheme.AlignRight(colTagBuses);
            foreach (Label label in new[] { lblBusId, lblConsumerId, lblStatusColumn, lblAccount, lblKwh, lblMonth, lblExportStyle, lblTheme })
                AppTheme.StyleFieldLabel(label);
            AppTheme.StyleComboBox(cboTheme);
            foreach (TextBox box in new[] { txtBusId, txtConsumerId, txtStatusColumn, txtAccount, txtKwh, txtMonth })
                AppTheme.StyleTextBox(box);
            AppTheme.StyleComboBox(cboExportStyle);
            AppTheme.StylePrimaryButton(btnSave);
            AppTheme.StyleOutlineButton(btnRevert);
            AppTheme.StyleOutlineButton(btnDefaults);
            AppTheme.StyleSuccessLabel(lblSaved);
            AppTheme.StyleErrorLabel(lblValidation);
        }

        private void cboTagFeeder_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!_populating)
                _presenter?.OnBusTagFilterChanged(cboTagFeeder.SelectedIndex > 0 ? cboTagFeeder.SelectedItem as string : null);
        }

        private void dgvTags_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (dgvTags.IsCurrentCellDirty && dgvTags.CurrentCell?.ColumnIndex == colTagHand.Index)
                dgvTags.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void dgvTags_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (_populating || e.RowIndex < 0 || e.RowIndex >= _tagRows.Count || e.ColumnIndex != colTagHand.Index)
                return;
            bool handPlaced = dgvTags.Rows[e.RowIndex].Cells[colTagHand.Index].Value is true;
            BusTagRow row = _tagRows[e.RowIndex];
            BeginInvoke(() => _presenter?.OnBusTagToggled(row.Code, row.Tag, handPlaced));
        }

        private void btnSave_Click(object? sender, EventArgs e) => _presenter?.OnSaveClicked();

        private void btnRevert_Click(object? sender, EventArgs e) => _presenter?.OnRevertClicked();

        private void btnDefaults_Click(object? sender, EventArgs e) => _presenter?.OnDefaultsClicked();
    }
}
