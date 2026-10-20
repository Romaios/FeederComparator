using FeederCopy.App.Utils;
using FeederCopy.Core.Presenters.Consumers;
using FeederCopy.Core.Presenters.Copy;
using FeederCopy.Core.Presenters.Feeders;
using FeederCopy.Core.Presenters.Settings;

namespace FeederCopy.App.Forms.Shell
{
    public sealed partial class MainForm : Form
    {
        private const string ReadyText = "Ready";

        private readonly SettingsPresenter _settings;
        private readonly FeedersPresenter _feeders;
        private readonly CopyPresenter _copy;
        private readonly ConsumersPresenter _consumers;

        public MainForm(AppServices services)
        {
            InitializeComponent();
            ApplyTheme();
            Text = $"{Dialogs.AppName} {typeof(MainForm).Assembly.GetName().Version?.ToString(3)}";

            _settings = new SettingsPresenter(settingsView, services.Settings);
            _feeders = new FeedersPresenter(feedersView, services.Feeders, () => _settings.Current, services.Log);
            _copy = new CopyPresenter(copyView, services.Feeders, services.Readings, services.AsBilled, services.Exporter,
                () => _settings.Current, _settings.RememberExportStyle, services.Log);
            _consumers = new ConsumersPresenter(consumersView, services.Feeders, services.Readings, () => DateTime.Today.Year, services.Log);
            _feeders.FeedersChanged += _copy.OnFeedersChanged;
            _feeders.FeedersChanged += _consumers.OnDataChanged;
            _copy.ReadingsSaved += _consumers.OnDataChanged;
            _consumers.StatusChanged += _feeders.Load;
            _consumers.StatusChanged += _copy.OnFeedersChanged;

            settingsView.Attach(_settings);
            feedersView.Attach(_feeders);
            copyView.Attach(_copy);
            consumersView.Attach(_consumers);
            feedersView.StatusChanged += SetStatus;
            copyView.StatusChanged += SetStatus;
        }

        private void ApplyTheme()
        {
            AppTheme.StyleForm(this);
            AppTheme.StyleHeaderBar(tlpHeader);
            AppTheme.StyleTitleLabel(lblAppTitle);
            AppTheme.StyleNavTab(rdoFeeders);
            AppTheme.StyleNavTab(rdoCopy);
            AppTheme.StyleNavTab(rdoConsumers);
            AppTheme.StyleNavTab(rdoSettings);
            AppTheme.StyleWarningBanner(tlpWarning, lblWarning, btnDismissWarning);
            AppTheme.StyleStatusStrip(statusStrip);
        }

        private void SetStatus(string? message) => lblStatus.Text = message ?? ReadyText;

        private void MainForm_Shown(object? sender, EventArgs e)
        {
            string? problem = _settings.Load();
            if (problem is not null)
            {
                lblWarning.Text = problem;
                tlpWarning.Visible = true;
            }
            _feeders.Load();
            _copy.Load();
            _consumers.Load();
        }

        private void navTab_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is not RadioButton { Checked: true } tab)
                return;
            feedersView.Visible = tab == rdoFeeders;
            copyView.Visible = tab == rdoCopy;
            consumersView.Visible = tab == rdoConsumers;
            settingsView.Visible = tab == rdoSettings;
        }

        private void btnDismissWarning_Click(object? sender, EventArgs e) => tlpWarning.Visible = false;
    }
}
