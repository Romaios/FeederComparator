using FeederCopy.App.Utils;
using FeederCopy.Core.Models.Updates;
using FeederCopy.Core.Presenters.Updates;

namespace FeederCopy.App.Forms.Updates
{
    public sealed partial class UpdatesView : UserControl, IUpdatesView
    {
        private UpdatesPresenter? _presenter;

        public UpdatesView()
        {
            InitializeComponent();
            ApplyTheme();
        }

        public void Attach(UpdatesPresenter presenter) => _presenter = presenter;

        public void ShowSource(string repository, bool hasToken, string currentVersion)
        {
            txtRepository.Text = repository;
            txtToken.Text = string.Empty;
            txtToken.PlaceholderText = hasToken ? "A token is saved" : "Paste the access token";
            lblCurrentVersion.Text = $"Installed version {currentVersion}";
        }

        public void ShowSourceMessage(string message, bool isError)
        {
            lblSourceMessage.ForeColor = isError ? AppTheme.Danger : AppTheme.Success;
            lblSourceMessage.Text = message;
        }

        public void ShowScreen(UpdateScreen screen)
        {
            lblUpdateTitle.Text = screen.Title;
            lblUpdateTitle.ForeColor = screen.Stage == UpdateStage.Failed ? AppTheme.Danger : AppTheme.TextPrimary;
            lblUpdateMessage.Text = screen.Message;
            btnCheck.Enabled = screen.CanCheck;
            btnUpdateNow.Visible = screen.CanUpdate;
            btnCancelDownload.Visible = screen.CanCancel;
            prgDownload.Style = screen.Stage == UpdateStage.Downloading ? ProgressBarStyle.Continuous : ProgressBarStyle.Marquee;
            prgDownload.Visible = screen.ShowProgress;
            lblProgress.Text = string.Empty;
            tmrActivity.Enabled = screen.ShowProgress;
        }

        public void ShowMessage(string message) => lblUpdateMessage.Text = message;

        public void ShowDetail(string detail) => lblProgress.Text = detail;

        public void ShowProgress(int percent, string detail)
        {
            int value = Math.Clamp(percent, 0, prgDownload.Maximum - 1);
            prgDownload.Value = value + 1;
            prgDownload.Value = value;
            lblProgress.Text = detail;
        }

        private async void btnCheck_Click(object? sender, EventArgs e)
        {
            if (_presenter is not null)
                await _presenter.OnCheckClickedAsync();
        }

        private async void btnUpdateNow_Click(object? sender, EventArgs e)
        {
            if (_presenter is not null)
                await _presenter.OnUpdateNowClickedAsync();
        }

        private void tmrActivity_Tick(object? sender, EventArgs e) => _presenter?.OnTimerTick();

        private void btnCancelDownload_Click(object? sender, EventArgs e) => _presenter?.OnCancelClicked();

        private void btnSaveSource_Click(object? sender, EventArgs e) => _presenter?.OnSaveSourceClicked(txtRepository.Text, txtToken.Text);

        private void ApplyTheme()
        {
            BackColor = AppTheme.Background;
            pnlScroll.BackColor = AppTheme.Background;
            AppTheme.StyleCard(pnlCard, lblUpdatesSection);
            AppTheme.StyleSectionLabel(lblUpdatesSection);
            AppTheme.StyleSectionLabel(lblSourceSection);
            AppTheme.StyleMutedLabel(lblCurrentVersion);
            lblUpdateTitle.Font = AppTheme.BodyStrongFont;
            lblUpdateTitle.ForeColor = AppTheme.TextPrimary;
            lblUpdateMessage.Font = AppTheme.BodyFont;
            lblUpdateMessage.ForeColor = AppTheme.TextSecondary;
            AppTheme.StyleMutedLabel(lblProgress);
            AppTheme.StylePrimaryButton(btnCheck);
            AppTheme.StylePrimaryButton(btnUpdateNow);
            AppTheme.StyleOutlineButton(btnCancelDownload);
            AppTheme.StyleFieldLabel(lblRepository);
            AppTheme.StyleFieldLabel(lblToken);
            AppTheme.StyleTextBox(txtRepository);
            AppTheme.StyleTextBox(txtToken);
            AppTheme.StyleMutedLabel(lblTokenHint);
            AppTheme.StyleOutlineButton(btnSaveSource);
            lblSourceMessage.Font = AppTheme.BodyStrongFont;
        }
    }
}
