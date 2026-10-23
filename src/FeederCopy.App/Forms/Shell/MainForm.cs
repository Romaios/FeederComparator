using FeederCopy.App.Utils;
using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Presenters.BusSort;
using FeederCopy.Core.Presenters.Consumers;
using FeederCopy.Core.Presenters.Database;
using FeederCopy.Core.Presenters.Copy;
using FeederCopy.Core.Presenters.Feeders;
using FeederCopy.Core.Presenters.Settings;
using FeederCopy.Core.Presenters.Updates;

namespace FeederCopy.App.Forms.Shell
{
    public sealed partial class MainForm : Form
    {
        private const string ReadyText = "Ready";
        private const string DatabaseNotReady = "The program can't use its MySQL database yet. Open Settings → Database to set it up.";
        private const string CollapseGlyph = "";
        private const string ExpandGlyph = "";

        private readonly SettingsPresenter _settings;
        private readonly FeedersPresenter _feeders;
        private readonly CopyPresenter _copy;
        private readonly ConsumersPresenter _consumers;
        private readonly BusSortPresenter _busSort;
        private readonly UpdatesPresenter _updates;
        private readonly DatabasePresenter _database;
        private bool _headerCollapsed;
        private bool _dataLoaded;

        public MainForm(AppServices services)
        {
            InitializeComponent();
            Icon = AppAssets.Icon;
            ApplyTheme();
            Text = $"{Dialogs.AppName} {typeof(MainForm).Assembly.GetName().Version?.ToString(3)}";

            _settings = new SettingsPresenter(settingsView, services.Settings);
            _database = new DatabasePresenter(settingsView.DatabaseCard, services.DatabaseSettings, services.Database, services.Log);
            _feeders = new FeedersPresenter(feedersView, services.Feeders, () => _settings.Current, services.Log);
            _copy = new CopyPresenter(copyView, services.Feeders, services.Readings, services.AsBilled, services.Exporter,
                () => _settings.Current, _settings.RememberExportStyle, services.Log);
            _consumers = new ConsumersPresenter(consumersView, services.Feeders, services.Readings, () => new BillMonth(DateTime.Today.Year, DateTime.Today.Month), services.Log);
            _busSort = new BusSortPresenter(busSortView, services.Feeders, () => _settings.Current, _settings.RememberHandPlacedTags, services.Log);
            _updates = new UpdatesPresenter(updatesView, services.Updater, services.UpdateSettings, services.Log);
            _feeders.FeedersChanged += _copy.OnFeedersChanged;
            _feeders.FeedersChanged += _consumers.OnFeedersChanged;
            _feeders.FeedersChanged += _busSort.OnFeedersChanged;
            _feeders.StatusChanged += _consumers.OnFeedersChanged;
            _copy.ReadingsSaved += _consumers.OnReadingsChanged;
            _consumers.StatusChanged += _feeders.OnStatusChanged;
            _consumers.OrderChanged += _feeders.Load;
            _consumers.OrderChanged += _copy.OnFeedersChanged;
            _consumers.OrderChanged += _busSort.OnFeedersChanged;
            _copy.StatusesUpdated += _feeders.OnStatusChanged;
            _copy.StatusesUpdated += _consumers.OnFeedersChanged;
            _busSort.Sorted += _feeders.Load;
            _busSort.Sorted += _copy.OnFeedersChanged;
            _busSort.Sorted += _consumers.OnFeedersChanged;
            _updates.UpdateAvailable += ShowUpdateOffer;
            _database.Connected += LoadData;

            settingsView.Attach(_settings);
            settingsView.DatabaseCard.Attach(_database);
            feedersView.Attach(_feeders);
            copyView.Attach(_copy);
            consumersView.Attach(_consumers);
            busSortView.Attach(_busSort);
            updatesView.Attach(_updates);
            feedersView.StatusChanged += SetStatus;
            copyView.StatusChanged += SetStatus;
        }

        private void ApplyTheme()
        {
            AppTheme.StyleForm(this);
            AppTheme.StyleHeaderBar(tlpHeader);
            AppTheme.StyleBrand(lblAppTitle, picLogo);
            flpTabs.BackColor = AppTheme.CardBackground;
            RadioButton[] tabs = [rdoFeeders, rdoCopy, rdoConsumers, rdoBusSort, rdoSettings, rdoUpdates];
            for (int i = 0; i < tabs.Length; i++)
                AppTheme.StyleTopTab(tabs[i], AppTheme.NavColor(i));
            AppTheme.StyleHeaderIconButton(btnCollapse);
            AppTheme.StyleWarningBanner(tlpWarning, lblWarning, btnDismissWarning);
            AppTheme.StylePrimaryButton(btnWarningAction);
            AppTheme.StyleStatusStrip(statusStrip);
        }

        private void SetStatus(string? message) => lblStatus.Text = message ?? ReadyText;

        private void LoadData()
        {
            if (lblWarning.Text == DatabaseNotReady)
                tlpWarning.Visible = false;
            _dataLoaded = true;
            _feeders.Load();
            _copy.Load();
            _consumers.Load();
            _busSort.OnFeedersChanged();
        }

        private void ApplyHeaderSize()
        {
            SuspendLayout();
            tlpShell.RowStyles[0].Height = LogicalToDeviceUnits(_headerCollapsed ? AppTheme.HeaderCollapsedHeight : AppTheme.HeaderExpandedHeight);
            var size = new Size(
                LogicalToDeviceUnits(_headerCollapsed ? AppTheme.TopTabCollapsedWidth : AppTheme.TopTabExpandedWidth),
                LogicalToDeviceUnits(_headerCollapsed ? AppTheme.TopTabCollapsedHeight : AppTheme.TopTabExpandedHeight));
            foreach (Control tab in flpTabs.Controls)
                tab.Size = size;
            lblAppTitle.Visible = !_headerCollapsed;
            btnCollapse.Text = _headerCollapsed ? ExpandGlyph : CollapseGlyph;
            tipNav.SetToolTip(btnCollapse, _headerCollapsed ? "Expand the tab bar" : "Collapse the tab bar");
            ResumeLayout(true);
        }

        private void btnCollapse_Click(object? sender, EventArgs e)
        {
            _headerCollapsed = !_headerCollapsed;
            ApplyHeaderSize();
        }

        private void ShowWarning(string message, bool updateOffer)
        {
            lblWarning.Text = message;
            btnWarningAction.Visible = updateOffer;
            tlpWarning.Visible = true;
        }

        private void ShowUpdateOffer(string version) =>
            ShowWarning($"Version {version} of the program is available. Click Update now to download and install it.", updateOffer: true);

        private async void MainForm_Shown(object? sender, EventArgs e)
        {
            string? problem = _settings.Load();
            if (problem is not null)
                ShowWarning(problem, updateOffer: false);
            if (_database.Load() is null)
            {
                LoadData();
            }
            else
            {
                ShowWarning(DatabaseNotReady, updateOffer: false);
                rdoSettings.Checked = true;
            }
            _updates.Load();
            await _updates.CheckQuietlyAsync();
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_updates.OnAppClosing(e.CloseReason == CloseReason.UserClosing))
                e.Cancel = true;
        }

        private void navTab_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is not RadioButton { Checked: true } tab)
                return;
            feedersView.Visible = tab == rdoFeeders;
            copyView.Visible = tab == rdoCopy;
            consumersView.Visible = tab == rdoConsumers;
            busSortView.Visible = tab == rdoBusSort;
            Cursor.Current = Cursors.WaitCursor;
            settingsView.Visible = tab == rdoSettings;
            updatesView.Visible = tab == rdoUpdates;
            if (!_dataLoaded)
                return;
            if (tab == rdoConsumers)
                _consumers.OnShown();
            else
                _consumers.OnHidden();
            if (tab == rdoBusSort)
                _busSort.OnShown();
            else
                _busSort.OnHidden();
        }

        private async void btnWarningAction_Click(object? sender, EventArgs e)
        {
            tlpWarning.Visible = false;
            rdoUpdates.Checked = true;
            await _updates.OnUpdateNowClickedAsync();
        }

        private void btnDismissWarning_Click(object? sender, EventArgs e) => tlpWarning.Visible = false;
    }
}
