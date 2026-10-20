using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Consumers;
using FeederCopy.Core.Models.Copy;
using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Presenters.Consumers;
using FeederCopy.Core.Services.Data;

namespace FeederCopy.Tests.Presenters
{
    public sealed class ConsumersPresenterTests : IDisposable
    {
        private readonly TempDatabase _temp = new();
        private readonly FeederService _feeders;
        private readonly ReadingService _readings;
        private readonly FakeConsumersView _view = new();
        private readonly FakeErrorLog _log = new();
        private readonly ConsumersPresenter _presenter;
        private int _statusChanged;

        public ConsumersPresenterTests()
        {
            _feeders = new FeederService(_temp.Db);
            _readings = new ReadingService(_temp.Db);
            _presenter = new ConsumersPresenter(_view, _feeders, _readings, () => 2026, _log);
            _presenter.StatusChanged += () => _statusChanged++;
        }

        public void Dispose() => _temp.Dispose();

        [Fact]
        public void Load_NoFeeders_ShowsEmpty()
        {
            _presenter.Load();
            Assert.Equal("No feeders yet. Import a feeder workbook in the Feeders tab.", _view.EmptyMessage);
        }

        [Fact]
        public void Load_ShowsFiltersAndLatestSavedYear()
        {
            _feeders.CreateFeeder("Bacon", [new ConsumerEntry("B", "c1")]);
            _feeders.CreateFeeder("Abuyog", [new ConsumerEntry("A", "c2", false)]);
            _readings.SaveMonth(new BillMonth(2025, 12), [new ReadingRecord("c1", "202512", 9, "Active")]);
            _presenter.Load();
            Assert.Equal(["Abuyog", "Bacon"], _view.FeederNames);
            Assert.Equal([2026, 2025], _view.Years);
            Assert.Equal(2025, _view.SelectedYear);
            Assert.Equal(["c2", "c1"], _view.Rows.Select(r => r.ConsumerId));
            Assert.Equal("9.00", _view.Rows[1].Months[11]);
            Assert.Equal("2 consumers · 1 active · 1 inactive", _view.Summary);
        }

        [Fact]
        public void FilterChanged_AppliesFilterAndYear()
        {
            _feeders.CreateFeeder("Abuyog", [new ConsumerEntry("A", "c1"), new ConsumerEntry("A", "c2", false)]);
            _readings.SaveMonth(new BillMonth(2026, 3), [new ReadingRecord("c1", "202603", 4, "Active")]);
            _presenter.Load();
            _presenter.OnFilterChanged(null, true, 2026, null);
            Assert.Equal(["c1"], _view.Rows.Select(r => r.ConsumerId));
            Assert.Equal("4.00", _view.Rows[0].Months[2]);
        }

        [Fact]
        public void SetActive_PersistsReselectsRaisesStatusChanged()
        {
            _feeders.CreateFeeder("Abuyog", [new ConsumerEntry("A", "c1")]);
            _presenter.Load();
            _presenter.OnSetActiveClicked("c1", false);
            Assert.False(_feeders.GetAllConsumers().Single().Entry.Active);
            Assert.Equal("Inactive", _view.Rows.Single().StatusLabel);
            Assert.Equal("c1", _view.SelectedConsumer);
            Assert.Equal(1, _statusChanged);
        }

        [Fact]
        public void DataChanged_ReloadsYearsKeepingFilter()
        {
            _feeders.CreateFeeder("Abuyog", [new ConsumerEntry("A", "c1")]);
            _presenter.Load();
            _presenter.OnFilterChanged("Abuyog", null, 2026, null);
            _readings.SaveMonth(new BillMonth(2026, 1), [new ReadingRecord("c1", "202601", 1, "Active")]);
            _presenter.OnDataChanged();
            Assert.Equal("1.00", _view.Rows.Single().Months[0]);
            Assert.Equal(2026, _view.SelectedYear);
        }
    }
}
