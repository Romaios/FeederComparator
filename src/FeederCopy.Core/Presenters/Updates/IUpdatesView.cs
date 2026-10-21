using FeederCopy.Core.Models.Updates;

namespace FeederCopy.Core.Presenters.Updates
{
    public interface IUpdatesView
    {
        void ShowSource(string repository, bool hasToken, string currentVersion);
        void ShowSourceMessage(string message, bool isError);
        void ShowScreen(UpdateScreen screen);
        void ShowProgress(int percent, string detail);
        void ShowMessage(string message);
        void ShowDetail(string detail);
    }
}
