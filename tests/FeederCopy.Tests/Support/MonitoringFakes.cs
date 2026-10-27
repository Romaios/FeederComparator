using FeederCopy.Core.Models.Monitoring;
using FeederCopy.Core.Presenters.Monitoring;
using FeederCopy.Core.Presenters.Reports;

namespace FeederCopy.Tests.Support
{
    internal sealed class FakeMonitoringView : IMonitoringView
    {
        public IReadOnlyList<int> Years { get; private set; } = [];
        public int SelectedYear { get; private set; }
        public IReadOnlyList<MonthSummaryRow> Rows { get; private set; } = [];
        public string? Summary { get; private set; }
        public string? EmptyMessage { get; private set; }
        public bool ExportEnabled { get; private set; }
        public string? NextSaveFile { get; set; }
        public List<(string Filter, string DefaultName)> SavePrompts { get; } = [];
        public List<string> Errors { get; } = [];
        public List<string> Infos { get; } = [];

        public void ShowYears(IReadOnlyList<int> years, int selectedYear)
        {
            Years = years;
            SelectedYear = selectedYear;
        }

        public void ShowRows(IReadOnlyList<MonthSummaryRow> rows, string summary)
        {
            Rows = rows;
            Summary = summary;
            EmptyMessage = null;
        }

        public void ShowEmpty(string message)
        {
            Rows = [];
            EmptyMessage = message;
        }

        public void ShowLoading(string message)
        {
            Rows = [];
            LoadingMessage = message;
        }

        public string? LoadingMessage { get; private set; }

        public void SetExportEnabled(bool enabled) => ExportEnabled = enabled;

        public string? PickSaveFile(string filter, string defaultName)
        {
            SavePrompts.Add((filter, defaultName));
            return NextSaveFile;
        }

        public void ShowError(string message) => Errors.Add(message);

        public void ShowInfo(string message) => Infos.Add(message);
    }

    internal sealed class FakeReportsView : IReportsView
    {
        public IReadOnlyList<int> Years { get; private set; } = [];
        public int SelectedYear { get; private set; }
        public IReadOnlyList<string> Measures { get; private set; } = [];
        public int SelectedMeasure { get; private set; }
        public string? Title { get; private set; }
        public string? TotalHeader { get; private set; }
        public bool Kwh { get; private set; }
        public IReadOnlyList<FeederMonthsRow> Rows { get; private set; } = [];
        public string? Summary { get; private set; }
        public string? EmptyMessage { get; private set; }
        public bool ExportEnabled { get; private set; }
        public string? NextSaveFile { get; set; }
        public List<(string Filter, string DefaultName)> SavePrompts { get; } = [];
        public List<string> Errors { get; } = [];
        public List<string> Infos { get; } = [];

        public void ShowFilters(IReadOnlyList<int> years, int selectedYear, IReadOnlyList<string> measures, int selectedMeasure)
        {
            Years = years;
            SelectedYear = selectedYear;
            Measures = measures;
            SelectedMeasure = selectedMeasure;
        }

        public void ShowRows(string title, string totalHeader, bool kwh, IReadOnlyList<FeederMonthsRow> rows, string summary)
        {
            Title = title;
            TotalHeader = totalHeader;
            Kwh = kwh;
            Rows = rows;
            Summary = summary;
            EmptyMessage = null;
        }

        public void ShowEmpty(string message)
        {
            Rows = [];
            EmptyMessage = message;
        }

        public void ShowLoading(string message)
        {
            Rows = [];
            LoadingMessage = message;
        }

        public string? LoadingMessage { get; private set; }

        public void SetExportEnabled(bool enabled) => ExportEnabled = enabled;

        public string? PickSaveFile(string filter, string defaultName)
        {
            SavePrompts.Add((filter, defaultName));
            return NextSaveFile;
        }

        public void ShowError(string message) => Errors.Add(message);

        public void ShowInfo(string message) => Infos.Add(message);
    }
}
