using FeederCopy.Core.Models.Monitoring;

namespace FeederCopy.Core.Services.Export
{
    public interface IMonitoringExporter
    {
        void WriteSummary(string path, int year, IReadOnlyList<MonthSummaryRow> rows);
        void WriteReport(string path, int year, IReadOnlyList<(MonitoringMeasure Measure, IReadOnlyList<FeederMonthsRow> Rows)> sheets);
    }
}
