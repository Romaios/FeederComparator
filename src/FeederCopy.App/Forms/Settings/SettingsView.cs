using FeederCopy.App.Utils;
using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Models.Settings;
using FeederCopy.Core.Presenters.Settings;

namespace FeederCopy.App.Forms.Settings
{
    public sealed partial class SettingsView : UserControl, ISettingsView
    {
        private SettingsPresenter? _presenter;

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
            dgvTags.Rows.Clear();
            foreach (string tag in BusSorter.ParseTags(settings.HandPlacedBusTags))
                dgvTags.Rows.Add(tag);
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
            HandPlacedBusTags = string.Join(", ", dgvTags.Rows.Cast<DataGridViewRow>()
                .Where(r => !r.IsNewRow)
                .Select(r => (r.Cells[colTag.Index].Value as string)?.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)),
            Theme = cboTheme.SelectedIndex == (int)ThemeMode.Dark ? ThemeMode.Dark : ThemeMode.Light,
        };

        public bool Confirm(string title, string message) => Dialogs.Confirm((IWin32Window?)FindForm() ?? this, title, message);

        public void RestartApplication() => Application.Restart();

        private void btnRemoveTag_Click(object? sender, EventArgs e)
        {
            if (dgvTags.CurrentRow is { IsNewRow: false } row)
                dgvTags.Rows.Remove(row);
        }

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
            AppTheme.StyleGrid(dgvTags);
            AppTheme.StyleOutlineButton(btnRemoveTag);
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

        private void btnSave_Click(object? sender, EventArgs e) => _presenter?.OnSaveClicked();

        private void btnRevert_Click(object? sender, EventArgs e) => _presenter?.OnRevertClicked();

        private void btnDefaults_Click(object? sender, EventArgs e) => _presenter?.OnDefaultsClicked();
    }
}
