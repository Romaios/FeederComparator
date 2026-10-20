namespace FeederCopy.App.Utils
{
    public static class Dialogs
    {
        public const string AppName = "Feeder kWh Copy";

        public static void ShowError(IWin32Window owner, string message) =>
            MessageBox.Show(owner, message, AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);

        public static void ShowInfo(IWin32Window owner, string message) =>
            MessageBox.Show(owner, message, AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);

        public static bool Confirm(IWin32Window owner, string title, string message) =>
            MessageBox.Show(owner, message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

        public static string? PickSaveFile(IWin32Window owner, string filter, string defaultFileName)
        {
            using var dialog = new SaveFileDialog { Filter = filter, FileName = defaultFileName, OverwritePrompt = true };
            return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.FileName : null;
        }

        public static string? PickOpenFile(IWin32Window owner, string title, string filter)
        {
            using var dialog = new OpenFileDialog { Title = title, Filter = filter, Multiselect = false };
            return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.FileName : null;
        }

        public static string? PickFolder(IWin32Window owner)
        {
            using var dialog = new FolderBrowserDialog { Description = "Choose the folder for the feeder files", UseDescriptionForTitle = true };
            return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.SelectedPath : null;
        }
    }
}
