using FeederCopy.Core.Models.Settings;
using FeederCopy.Core.Services.Settings;

namespace FeederCopy.Core.Presenters.Settings
{
    public sealed class SettingsPresenter
    {
        private readonly ISettingsView _view;
        private readonly ISettingsStore _store;

        public SettingsPresenter(ISettingsView view, ISettingsStore store)
        {
            _view = view;
            _store = store;
        }

        public AppSettings Current { get; private set; } = AppSettings.Defaults;

        public event Action? SettingsChanged;

        public string? Load()
        {
            SettingsLoadResult result = _store.Load();
            Current = result.Settings;
            _view.ShowSettings(Current);
            return result.Problem;
        }

        public void OnSaveClicked()
        {
            AppSettings entered = _view.ReadSettings();
            IReadOnlyList<string> errors = SettingsValidator.Validate(entered);
            _view.ShowValidation(errors);
            if (errors.Count > 0)
                return;

            _store.Save(entered);
            Current = entered;
            _view.ShowSaved();
            SettingsChanged?.Invoke();
        }

        public void OnRevertClicked()
        {
            _view.ShowSettings(Current);
            _view.ShowValidation([]);
        }

        public void OnDefaultsClicked()
        {
            _view.ShowSettings(AppSettings.Defaults);
            _view.ShowValidation([]);
        }

        public void RememberExportStyle(ExportStyle style)
        {
            if (Current.Export == style)
                return;
            Current = Current with { Export = style };
            _store.Save(Current);
            _view.ShowSettings(Current);
        }
    }
}
