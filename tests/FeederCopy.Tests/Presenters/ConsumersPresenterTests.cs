using FeederCopy.Core.Models.Billing;
using FeederCopy.Core.Models.Consumers;
using FeederCopy.Core.Models.Copy;
using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Models.Monitoring;
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
            _presenter = new ConsumersPresenter(_view, _feeders, _readings, () => new BillMonth(2026, 10), _log);
            _presenter.StatusChanged += () => _statusChanged++;
            _presenter.OnShown();
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
            Assert.Equal(new BillMonth(2026, 10), _view.SuggestedInactiveMonth);
            Assert.Equal(["c1"], _view.Rows.Select(r => r.ConsumerId));
            Assert.Equal("9.00", _view.Rows[0].Months[11]);
            Assert.Equal("1 active consumer", _view.Summary);
            Assert.Equal((1, 1), _view.StatusCounts);
        }

        [Fact]
        public void InactiveTab_ShowsOnlyInactive_WithFeederRowCount()
        {
            _feeders.CreateFeeder("Abuyog", [new ConsumerEntry("A", "c1"), new ConsumerEntry("A", "c2", false), new ConsumerEntry("A", "c3")]);
            _presenter.Load();
            _presenter.OnFilterChanged(null, false, 2026, null);
            Assert.Equal([("c2", 2)], _view.Rows.Select(r => (r.ConsumerId, r.Count)));
            Assert.Equal("1 inactive consumer", _view.Summary);
            Assert.Equal((2, 1), _view.StatusCounts);
        }

        [Fact]
        public void FilterChanged_AppliesFilterAndYear()
        {
            _feeders.CreateFeeder("Abuyog", [new ConsumerEntry("A", "c1"), new ConsumerEntry("A", "c2", false)]);
            _readings.SaveMonth(new BillMonth(2026, 3), [new ReadingRecord("c1", "202603", 4, "Active")]);
            _presenter.Load();
            _presenter.OnFilterChanged(null, true, 2026, "1");
            Assert.Equal(["c1"], _view.Rows.Select(r => r.ConsumerId));
            Assert.Equal("4.00", _view.Rows[0].Months[2]);
        }

        [Fact]
        public void SetInactive_SavesMonth_MovesToInactiveTab_RaisesStatusChanged()
        {
            _feeders.CreateFeeder("Abuyog", [new ConsumerEntry("A", "c1")]);
            _readings.SaveMonth(new BillMonth(2026, 6), [new ReadingRecord("c1", "202606", 12, "Active")]);
            _presenter.Load();
            _presenter.OnSetInactiveClicked(["c1"], 7, 2026);
            Assert.False(_feeders.GetAllConsumers().Single().Entry.Active);
            Assert.Empty(_view.Rows);
            Assert.Equal((0, 1), _view.StatusCounts);
            Assert.Equal(1, _statusChanged);

            _presenter.OnFilterChanged(null, false, 2026, null);
            ConsumerYearRow row = _view.Rows.Single();
            Assert.Equal("Inactive from Jul 2026", row.StatusLabel);
            Assert.Equal(6, row.ActiveMonths);
            Assert.Equal("12.00", row.Months[5]);
        }

        [Fact]
        public void SetInactive_NoMonth_ShowsErrorAndSavesNothing()
        {
            _feeders.CreateFeeder("Abuyog", [new ConsumerEntry("A", "c1")]);
            _presenter.Load();
            _presenter.OnSetInactiveClicked(["c1"], null, 2026);
            Assert.Equal(["Choose the month the consumer became inactive."], _view.Errors);
            Assert.True(_feeders.GetAllConsumers().Single().Entry.Active);
            Assert.Equal(0, _statusChanged);
        }

        [Fact]
        public void SetActive_ClearsMonth_ReselectsInActiveTab()
        {
            _feeders.CreateFeeder("Abuyog", [new ConsumerEntry("A", "c1")]);
            _feeders.SetActive("c1", false, new BillMonth(2026, 7));
            _presenter.Load();
            _presenter.OnSetActiveClicked(["c1"]);
            Assert.True(_feeders.GetAllConsumers().Single().Entry.Active);
            Assert.Empty(_feeders.GetInactiveFrom());
            Assert.Equal("c1", _view.SelectedConsumer);
            Assert.Equal("Active", _view.Rows.Single().StatusLabel);
            Assert.Equal(1, _statusChanged);
        }

        [Fact]
        public void DataChanged_ReloadsYearsKeepingFilter()
        {
            _feeders.CreateFeeder("Abuyog", [new ConsumerEntry("A", "c1")]);
            _presenter.Load();
            _presenter.OnFilterChanged("Abuyog", true, 2026, null);
            _readings.SaveMonth(new BillMonth(2026, 1), [new ReadingRecord("c1", "202601", 1, "Active")]);
            _presenter.OnReadingsChanged();
            Assert.Equal("1.00", _view.Rows.Single().Months[0]);
            Assert.Equal(2026, _view.SelectedYear);
        }

        [Fact]
        public void StatusChange_DoesNotReadReadingsAgain()
        {
            var counting = new CountingReadings(_readings);
            var presenter = new ConsumersPresenter(_view, _feeders, counting, () => new BillMonth(2026, 10), _log);
            _feeders.CreateFeeder("Abuyog", [new ConsumerEntry("A", "c1"), new ConsumerEntry("A", "c2")]);
            presenter.Load();
            presenter.OnShown();
            presenter.OnSetInactiveClicked(["c1"], 7, 2026);
            presenter.OnFilterChanged(null, false, 2026, null);
            presenter.OnSetActiveClicked(["c1"]);
            Assert.Equal(1, counting.YearReads);
            Assert.Equal((2, 0), _view.StatusCounts);
        }

        [Fact]
        public void Hidden_WaitsUntilShown()
        {
            var view = new FakeConsumersView();
            var presenter = new ConsumersPresenter(view, _feeders, _readings, () => new BillMonth(2026, 10), _log);
            _feeders.CreateFeeder("Abuyog", [new ConsumerEntry("A", "c1")]);
            presenter.Load();
            presenter.OnFeedersChanged();
            Assert.Empty(view.Rows);
            presenter.OnShown();
            Assert.Equal(["c1"], view.Rows.Select(r => r.ConsumerId));
            presenter.OnHidden();
            _feeders.CreateFeeder("Bacon", [new ConsumerEntry("B", "c2")]);
            presenter.OnFeedersChanged();
            Assert.Equal(["c1"], view.Rows.Select(r => r.ConsumerId));
            presenter.OnShown();
            Assert.Equal(["c1", "c2"], view.Rows.Select(r => r.ConsumerId));
        }

        [Fact]
        public void SetInactive_Many_ConfirmsThenMovesAll()
        {
            _feeders.CreateFeeder("Abuyog", [new ConsumerEntry("A", "c1"), new ConsumerEntry("A", "c2"), new ConsumerEntry("A", "c3")]);
            _presenter.Load();
            _presenter.OnSetInactiveClicked(["c1", "c3"], 7, 2026);
            Assert.Equal(["Set 2 consumers inactive from Jul 2026?"], _view.Confirms);
            Assert.Equal(["c2"], _view.Rows.Select(r => r.ConsumerId));
            Assert.Equal((1, 2), _view.StatusCounts);
            Assert.Equal(new BillMonth(2026, 7), _feeders.GetInactiveFrom()["c3"]);
            Assert.Equal(1, _statusChanged);
        }

        [Fact]
        public void SetInactive_Many_ConfirmNo_NoChange()
        {
            _feeders.CreateFeeder("Abuyog", [new ConsumerEntry("A", "c1"), new ConsumerEntry("A", "c2")]);
            _view.ConfirmAnswer = false;
            _presenter.Load();
            _presenter.OnSetInactiveClicked(["c1", "c2"], 7, 2026);
            Assert.All(_feeders.GetAllConsumers(), c => Assert.True(c.Entry.Active));
            Assert.Equal(0, _statusChanged);
        }

        [Fact]
        public void SetActive_Many_ClearsMonths()
        {
            _feeders.CreateFeeder("Abuyog", [new ConsumerEntry("A", "c1"), new ConsumerEntry("A", "c2")]);
            _feeders.SetActive(["c1", "c2"], false, new BillMonth(2026, 5));
            _presenter.Load();
            _presenter.OnFilterChanged(null, false, 2026, null);
            _presenter.OnSetActiveClicked(["c1", "c2"]);
            Assert.Equal(["Set 2 consumers active?"], _view.Confirms);
            Assert.Empty(_view.Rows);
            Assert.Empty(_feeders.GetInactiveFrom());
        }

        private sealed class CountingReadings(IReadingService inner) : IReadingService
        {
            public int YearReads { get; private set; }

            public void SaveMonth(BillMonth month, IReadOnlyList<ReadingRecord> readings) => inner.SaveMonth(month, readings);

            public IReadOnlyList<int> GetYears() => inner.GetYears();

            public IReadOnlyList<ReadingRecord> GetMonth(BillMonth month) => inner.GetMonth(month);

            public IReadOnlyList<BillMonth> GetMonths() => inner.GetMonths();

            public IReadOnlyList<FeederMonthTotals> GetFeederTotals(int year) => inner.GetFeederTotals(year);

            public IReadOnlyList<ReadingRecord> GetYear(int year)
            {
                YearReads++;
                return inner.GetYear(year);
            }
        }

        [Fact]
        public void Drop_ReordersInsideBus_SavesAndRaisesOrderChanged()
        {
            int orderChanged = 0;
            _presenter.OrderChanged += () => orderChanged++;
            long id = _feeders.CreateFeeder("Abuyog", [new ConsumerEntry("A", "c1"), new ConsumerEntry("A", "c2"), new ConsumerEntry("A", "c3"), new ConsumerEntry("B", "c4")]);
            _presenter.Load();
            _presenter.OnConsumersDropped(["c3", "c2"], "c1", after: false);
            Assert.Equal(["c2", "c3", "c1", "c4"], _feeders.GetConsumers(id).Select(e => e.ConsumerId));
            Assert.Equal(["c2", "c3", "c1", "c4"], _view.Rows.Select(r => r.ConsumerId));
            Assert.Equal(["c2", "c3"], _view.SelectedConsumers);
            Assert.Equal(1, orderChanged);
        }

        [Fact]
        public void Drop_IntoAnotherBus_ShowsErrorAndKeepsOrder()
        {
            long id = _feeders.CreateFeeder("Abuyog", [new ConsumerEntry("A", "c1"), new ConsumerEntry("B", "c2")]);
            _presenter.Load();
            _presenter.OnConsumersDropped(["c1"], "c2", after: true);
            Assert.Equal(["c1", "c2"], _feeders.GetConsumers(id).Select(e => e.ConsumerId));
            Assert.Equal(["Drop consumers inside their own bus (Abuyog, bus B). To move a consumer to another bus, change its Bus ID in the Feeders tab."], _view.Errors);
        }
    }
}
