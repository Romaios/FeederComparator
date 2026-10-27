using FeederCopy.Core.Models.Monitoring;

namespace FeederCopy.Core.Services.Export
{
    public interface IMonitoringExporter
    {
        void WriteMonitoring(string path, int year, IReadOnlyList<int> months, IReadOnlyList<MonitoringSection> sections);
        void WriteReport(string path, int year, IReadOnlyList<(MonitoringMeasure Measure, IReadOnlyList<FeederMonthsRow> Rows)> sheets);
    }
}
