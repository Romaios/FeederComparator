using FeederCopy.App.Utils;
using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Presenters.BusSort;
using FeederCopy.Core.Presenters.Consumers;
using FeederCopy.Core.Presenters.Database;
using FeederCopy.Core.Presenters.Copy;
using FeederCopy.Core.Presenters.Feeders;
using FeederCopy.Core.Presenters.Monitoring;
using FeederCopy.Core.Presenters.Reports;
using FeederCopy.Core.Presenters.Settings;
using FeederCopy.Core.Presenters.Updates;
using FeederCopy.Core.Services.Data;

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
        private readonly MonitoringPresenter _monitoring;
        private readonly ReportsPresenter _reports;
        private readonly FeederTotalsCache _totals;
        private readonly UpdatesPresenter _updates;
        private readonly DatabasePresenter _database;
        private readonly Dictionary<RadioButton, RadioButton[]> _groupTabs;
        private readonly Dictionary<RadioButton, RadioButton> _lastTab = [];
        private readonly string _appTitle = $"{Dialogs.AppName} {typeof(MainForm).Assembly.GetName().Version?.ToString(3)}";
        private bool _headerCollapsed;
        private bool _dataLoaded;

        public MainForm(AppServices services)
        {
            InitializeComponent();
            _groupTabs = new Dictionary<RadioButton, RadioButton[]>
            {
                [rdoGroupFeeders] = [rdoFeeders, rdoConsumers, rdoBusSort],
                [rdoGroupComparator] = [rdoCopy],
                [rdoGroupMonitoring] = [rdoMonitoring, rdoReports],
                [rdoGroupSystem] = [rdoSettings, rdoUpdates],
            };
            Icon = AppAssets.Icon;
            ApplyTheme();
            Text = _appTitle;
            lblAppTitle.Text = _appTitle;

            _totals = new FeederTotalsCache(services.Readings);
            _settings = new SettingsPresenter(settingsView, services.Settings);
            _database = new DatabasePresenter(settingsView.DatabaseCard, services.DatabaseSettings, services.Database, services.Log);
            _feeders = new FeedersPresenter(feedersView, services.Feeders, () => _settings.Current, services.Log);
            _copy = new CopyPresenter(copyView, services.Feeders, services.Readings, services.AsBilled, services.Exporter,
                () => _settings.Current, _settings.RememberExportStyle, services.Log);
            _consumers = new ConsumersPresenter(consumersView, services.Feeders, services.Readings, () => new BillMonth(DateTime.Today.Year, DateTime.Today.Month), services.Log);
            _busSort = new BusSortPresenter(busSortView, services.Feeders, () => _settings.Current, _settings.RememberHandPlacedTags, services.Log);
            _monitoring = new MonitoringPresenter(monitoringView, services.Feeders, services.Readings, _totals, services.MonitoringExporter, services.Log);
            _reports = new ReportsPresenter(reportsView, services.Feeders, services.Readings, _totals, services.MonitoringExporter, services.Log);
            _updates = new UpdatesPresenter(updatesView, services.Updater, services.UpdateSettings, services.Log);
            _feeders.FeedersChanged += _totals.Clear;
            _copy.ReadingsSaved += _totals.Clear;
            _feeders.FeedersChanged += _copy.OnFeedersChanged;
            _feeders.FeedersChanged += _consumers.OnFeedersChanged;
            _feeders.FeedersChanged += _busSort.OnFeedersChanged;
            _feeders.FeedersChanged += _monitoring.OnDataChanged;
            _feeders.FeedersChanged += _reports.OnDataChanged;
            _feeders.StatusChanged += _consumers.OnFeedersChanged;
            _copy.ReadingsSaved += _consumers.OnReadingsChanged;
            _copy.ReadingsSaved += _monitoring.OnDataChanged;
            _copy.ReadingsSaved += _reports.OnDataChanged;
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
            monitoringView.Attach(_monitoring);
            reportsView.Attach(_reports);
            updatesView.Attach(_updates);
            feedersView.StatusChanged += SetStatus;
            copyView.StatusChanged += SetStatus;
        }

        private void ApplyTheme()
        {
            AppTheme.StyleForm(this);
            AppTheme.StyleHeaderBar(tlpHeader);
            AppTheme.StyleBrand(lblAppTitle, picLogo);
            AppTheme.StyleHeaderIconButton(btnBack);
            int i = 0;
            foreach ((RadioButton group, RadioButton[] tabs) in _groupTabs)
            {
                Color accent = AppTheme.NavColor(i++);
                AppTheme.StyleTopTab(group, accent);
                foreach (RadioButton tab in tabs)
                    AppTheme.StyleTopTab(tab, accent);
            }
            AppTheme.StyleHeaderIconButton(btnCollapse);
            AppTheme.StyleWarningBanner(tlpWarning, lblWarning, btnDismissWarning);
            AppTheme.StylePrimaryButton(btnWarningAction);
            AppTheme.StyleStatusStrip(statusStrip);
            StyleHeader(null);
        }

        private void StyleHeader(RadioButton? group) =>
            AppTheme.StyleNavGroup(tlpHeader, group?.ForeColor, picLogo, lblAppTitle, pnlNav, flpTabs, flpSubTabs, btnBack, btnCollapse);

        private void OpenGroup(RadioButton group, RadioButton? tab = null)
        {
            SuspendLayout();
            group.Checked = true;
            foreach ((RadioButton g, RadioButton[] tabs) in _groupTabs)
            {
                foreach (RadioButton t in tabs)
                    t.Visible = g == group;
            }
            lblAppTitle.Text = group.Text;
            AppTheme.StyleNavGroupLabel(lblAppTitle, group.ForeColor);
            StyleHeader(group);
            flpSubTabs.Visible = true;
            btnBack.Focus();
            flpTabs.Visible = false;
            (tab ?? _lastTab.GetValueOrDefault(group) ?? _groupTabs[group][0]).Checked = true;
            ResumeLayout(true);
        }

        private void ShowMainBar()
        {
            SuspendLayout();
            flpTabs.Visible = true;
            _groupTabs.Keys.First(g => g.Checked).Focus();
            flpSubTabs.Visible = false;
            lblAppTitle.Text = _appTitle;
            AppTheme.StyleBrand(lblAppTitle, picLogo);
            StyleHeader(null);
            ResumeLayout(true);
        }

        private RadioButton GroupOf(RadioButton tab) => _groupTabs.First(g => g.Value.Contains(tab)).Key;

        private void SetStatus(string? message) => lblStatus.Text = message ?? ReadyText;

        private void LoadData()
        {
            if (lblWarning.Text == DatabaseNotReady)
                tlpWarning.Visible = false;
            _dataLoaded = true;
            _totals.Clear();
            _feeders.Load();
            _ = _copy.LoadAsync();
            _consumers.Load();
            _busSort.OnFeedersChanged();
            _monitoring.Load();
            _reports.Load();
        }

        private void ApplyHeaderSize()
        {
            SuspendLayout();
            tlpShell.RowStyles[0].Height = LogicalToDeviceUnits(_headerCollapsed ? AppTheme.HeaderCollapsedHeight : AppTheme.HeaderExpandedHeight);
            var size = new Size(
                LogicalToDeviceUnits(_headerCollapsed ? AppTheme.TopTabCollapsedWidth : AppTheme.TopTabExpandedWidth),
                LogicalToDeviceUnits(_headerCollapsed ? AppTheme.TopTabCollapsedHeight : AppTheme.TopTabExpandedHeight));
            foreach ((RadioButton group, RadioButton[] tabs) in _groupTabs)
            {
                group.Size = size;
                foreach (RadioButton tab in tabs)
                    tab.Size = size;
            }
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
                OpenGroup(rdoGroupSystem, rdoSettings);
            }
            _updates.Load();
            await _updates.CheckQuietlyAsync();
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_updates.OnAppClosing(e.CloseReason == CloseReason.UserClosing))
                e.Cancel = true;
        }

        private void navGroup_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && sender is RadioButton group)
                OpenGroup(group);
        }

        private void navGroup_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Enter or Keys.Space && sender is RadioButton group)
                OpenGroup(group);
        }

        private void btnBack_Click(object? sender, EventArgs e) => ShowMainBar();

        private async void navTab_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is not RadioButton { Checked: true } tab)
                return;
            _lastTab[GroupOf(tab)] = tab;
            feedersView.Visible = tab == rdoFeeders;
            copyView.Visible = tab == rdoCopy;
            consumersView.Visible = tab == rdoConsumers;
            busSortView.Visible = tab == rdoBusSort;
            monitoringView.Visible = tab == rdoMonitoring;
            reportsView.Visible = tab == rdoReports;
            Cursor.Current = Cursors.WaitCursor;
            settingsView.Visible = tab == rdoSettings;
            updatesView.Visible = tab == rdoUpdates;
            if (!_dataLoaded)
                return;
            if (tab != rdoConsumers)
                _consumers.OnHidden();
            if (tab == rdoBusSort)
                _busSort.OnShown();
            else
                _busSort.OnHidden();
            if (tab != rdoMonitoring)
                _monitoring.OnHidden();
            if (tab != rdoReports)
                _reports.OnHidden();
            if (tab == rdoConsumers)
                await _consumers.OnShownAsync();
            else if (tab == rdoMonitoring)
                await _monitoring.OnShownAsync();
            else if (tab == rdoReports)
                await _reports.OnShownAsync();
        }

        private async void btnWarningAction_Click(object? sender, EventArgs e)
        {
            tlpWarning.Visible = false;
            OpenGroup(rdoGroupSystem, rdoUpdates);
            await _updates.OnUpdateNowClickedAsync();
        }

        private void btnDismissWarning_Click(object? sender, EventArgs e) => tlpWarning.Visible = false;
    }
}
