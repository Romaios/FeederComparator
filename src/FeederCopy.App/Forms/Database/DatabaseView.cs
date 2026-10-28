using System.Diagnostics;
using FeederCopy.App.Utils;
using FeederCopy.Core.Presenters.Database;

namespace FeederCopy.App.Forms.Database
{
    public sealed partial class DatabaseView : UserControl, IDatabaseView
    {
        private const string ScriptsFolder = "sql";
        private const string SavedPasswordHint = "(saved — type to change)";

        private DatabasePresenter? _presenter;

        public DatabaseView()
        {
            InitializeComponent();
            ApplyTheme();
        }

        public void Attach(DatabasePresenter presenter) => _presenter = presenter;

        public void ShowConnection(string host, int port, string database, string user, bool hasPassword)
        {
            txtHost.Text = host;
            txtPort.Text = port.ToString();
            txtDatabase.Text = database;
            txtUser.Text = user;
            txtPassword.Text = string.Empty;
            txtPassword.PlaceholderText = hasPassword ? SavedPasswordHint : string.Empty;
        }

        public void ShowStatus(string message, DatabaseTone tone)
        {
            lblStatus.Text = message;
            switch (tone)
            {
                case DatabaseTone.Ok:
                    AppTheme.StyleSuccessLabel(lblStatus);
                    break;
                case DatabaseTone.Error:
                    AppTheme.StyleErrorLabel(lblStatus);
                    break;
                case DatabaseTone.Warning:
                    AppTheme.StyleWarningLabel(lblStatus);
                    break;
                default:
                    AppTheme.StyleMutedLabel(lblStatus);
                    break;
            }
        }

        public void SetBusy(bool busy)
        {
            tlpCard.Enabled = !busy;
            Cursor.Current = busy ? Cursors.WaitCursor : Cursors.Default;
            if (busy)
                Update();
        }

        public string? PickOldDatabase() =>
            Dialogs.PickOpenFile((IWin32Window?)FindForm() ?? this, "Choose the feeders.db file from the old program folder", "Old program data (feeders.db)|*.db|All files (*.*)|*.*");

        public void ShowImportResult(string message, DatabaseTone tone)
        {
            lblImportResult.Text = message;
            lblImportResult.Visible = true;
            switch (tone)
            {
                case DatabaseTone.Ok:
                    AppTheme.StyleSuccessLabel(lblImportResult);
                    break;
                case DatabaseTone.Error:
                    AppTheme.StyleErrorLabel(lblImportResult);
                    break;
                case DatabaseTone.Warning:
                    AppTheme.StyleWarningLabel(lblImportResult);
                    break;
                default:
                    AppTheme.StyleMutedLabel(lblImportResult);
                    break;
            }
            if (tone == DatabaseTone.Info)
                Update();
        }

        public void OpenScriptsFolder()
        {
            string folder = Path.Combine(AppContext.BaseDirectory, ScriptsFolder);
            if (!Directory.Exists(folder))
            {
                Dialogs.ShowError((IWin32Window?)FindForm() ?? this, $"The scripts folder is missing: {folder}. Reinstall the program.");
                return;
            }
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
        }

        private DatabaseForm ReadForm() => new(txtHost.Text, txtPort.Text, txtDatabase.Text, txtUser.Text, txtPassword.Text);

        private void ApplyTheme()
        {
            AppTheme.StyleView(this);
            AppTheme.StyleCard(tlpCard, lblTitle);
            foreach (Label label in new[] { lblHost, lblPort, lblDatabase, lblUser, lblPassword })
                AppTheme.StyleFieldLabel(label);
            foreach (TextBox box in new[] { txtHost, txtPort, txtDatabase, txtUser, txtPassword })
                AppTheme.StyleTextBox(box);
            AppTheme.StylePrimaryButton(btnSave);
            AppTheme.StyleOutlineButton(btnTest);
            AppTheme.StyleOutlineButton(btnScripts);
            AppTheme.StyleMutedLabel(lblStatus);
            AppTheme.StyleFieldLabel(lblOldTitle);
            AppTheme.StyleMutedLabel(lblOldIntro);
            AppTheme.StyleOutlineButton(btnImportOld);
            AppTheme.StyleMutedLabel(lblImportResult);
        }

        private void btnSave_Click(object? sender, EventArgs e) => _presenter?.OnSaveClicked(ReadForm());

        private void btnTest_Click(object? sender, EventArgs e) => _presenter?.OnTestClicked(ReadForm());

        private void btnScripts_Click(object? sender, EventArgs e) => _presenter?.OnOpenScriptsClicked();

        private async void btnImportOld_Click(object? sender, EventArgs e)
        {
            if (_presenter is not null)
                await _presenter.OnImportOldDataClickedAsync();
        }
    }
}
