using FeederCopy.App.Utils;
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
            txtHandTags.Text = settings.HandPlacedBusTags;
            txtAccount.Text = settings.AsBilled.AccountNumber;
            txtKwh.Text = settings.AsBilled.KwhUsed;
            txtMonth.Text = settings.AsBilled.BillMonth;
            cboExportStyle.SelectedIndex = (int)settings.Export;
            lblSaved.Text = string.Empty;
        }

        public AppSettings ReadSettings() => new()
        {
            Feeder = new FeederColumns { BusId = txtBusId.Text.Trim(), ConsumerId = txtConsumerId.Text.Trim(), Status = txtStatusColumn.Text.Trim() },
            AsBilled = new AsBilledColumns { AccountNumber = txtAccount.Text.Trim(), KwhUsed = txtKwh.Text.Trim(), BillMonth = txtMonth.Text.Trim() },
            Export = cboExportStyle.SelectedIndex == (int)ExportStyle.SeparateFiles ? ExportStyle.SeparateFiles : ExportStyle.OneWorkbook,
            HandPlacedBusTags = txtHandTags.Text.Trim(),
        };

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
            foreach (Label label in new[] { lblBusId, lblConsumerId, lblStatusColumn, lblHandTags, lblAccount, lblKwh, lblMonth, lblExportStyle })
                AppTheme.StyleFieldLabel(label);
            foreach (TextBox box in new[] { txtBusId, txtConsumerId, txtStatusColumn, txtHandTags, txtAccount, txtKwh, txtMonth })
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
