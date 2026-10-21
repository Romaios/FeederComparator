using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Models.Settings;
using FeederCopy.Core.Services.Settings;

namespace FeederCopy.Core.Presenters.Settings
{
    public sealed class SettingsPresenter
    {
        private readonly ISettingsView _view;
        private readonly ISettingsStore _store;
        private readonly Func<IEnumerable<(string Feeder, string Bus)>> _buses;
        private ThemeMode _appliedTheme = ThemeMode.Light;
        private List<BusTagRow> _tagRows = [];
        private string _tagBase = AppSettings.Defaults.HandPlacedBusTags;
        private string? _tagFilter;

        public SettingsPresenter(ISettingsView view, ISettingsStore store, Func<IEnumerable<(string Feeder, string Bus)>>? buses = null)
        {
            _view = view;
            _store = store;
            _buses = buses ?? (() => []);
        }

        public AppSettings Current { get; private set; } = AppSettings.Defaults;

        public event Action? SettingsChanged;

        public string? Load()
        {
            SettingsLoadResult result = _store.Load();
            Current = result.Settings;
            _appliedTheme = Current.Theme;
            _view.ShowSettings(Current);
            RefreshTags(Current.HandPlacedBusTags);
            return result.Problem;
        }

        public void OnSaveClicked()
        {
            AppSettings entered = _view.ReadSettings() with { HandPlacedBusTags = TagSetting() };
            IReadOnlyList<string> errors = SettingsValidator.Validate(entered);
            _view.ShowValidation(errors);
            if (errors.Count > 0)
                return;

            _store.Save(entered);
            Current = entered;
            _tagBase = entered.HandPlacedBusTags;
            _view.ShowSaved();
            SettingsChanged?.Invoke();

            if (entered.Theme != _appliedTheme
                && _view.Confirm("Theme", $"{entered.Theme} mode is saved. Restart the program now to switch?\n\nChoose No to switch the next time you open the program."))
                _view.RestartApplication();
        }

        public void OnRevertClicked()
        {
            _view.ShowSettings(Current);
            _view.ShowValidation([]);
            RefreshTags(Current.HandPlacedBusTags);
        }

        public void OnDefaultsClicked()
        {
            _view.ShowSettings(AppSettings.Defaults);
            _view.ShowValidation([]);
            RefreshTags(AppSettings.Defaults.HandPlacedBusTags);
        }

        public void OnBusTagFilterChanged(string? feeder)
        {
            _tagFilter = string.IsNullOrWhiteSpace(feeder) ? null : feeder;
            ShowTags();
        }

        public void OnBusTagToggled(string code, string tag, bool handPlaced)
        {
            _tagRows = _tagRows
                .Select(r => string.Equals(r.Code, code, StringComparison.OrdinalIgnoreCase) && string.Equals(r.Tag, tag, StringComparison.OrdinalIgnoreCase)
                    ? r with { HandPlaced = handPlaced }
                    : r)
                .ToList();
            ShowTags();
        }

        public void OnFeedersChanged() => RefreshTags(TagSetting());

        public void RememberExportStyle(ExportStyle style)
        {
            if (Current.Export == style)
                return;
            Current = Current with { Export = style };
            _store.Save(Current);
            _view.ShowSettings(Current);
        }

        private string TagSetting() =>
            _tagRows.Count > 0 ? BusTags.ToSetting(_tagRows, BusTags.ParseRules(_tagBase)) : _tagBase;

        private void RefreshTags(string setting)
        {
            _tagBase = setting;
            _tagRows = BusTags.Discover(_buses(), BusTags.ParseRules(setting)).ToList();
            if (_tagFilter is not null && !_tagRows.Any(r => r.Feeder == _tagFilter))
                _tagFilter = null;
            ShowTags();
        }

        private void ShowTags()
        {
            IReadOnlyList<string> feeders = _tagRows.Select(r => r.Feeder).Distinct().ToList();
            IReadOnlyList<BusTagRow> rows = _tagFilter is null ? _tagRows : _tagRows.Where(r => r.Feeder == _tagFilter).ToList();
            _view.ShowBusTags(feeders, _tagFilter, rows);
        }
    }
}
