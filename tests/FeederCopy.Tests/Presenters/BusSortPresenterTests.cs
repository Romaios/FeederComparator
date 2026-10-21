using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Models.Settings;
using FeederCopy.Core.Presenters.BusSort;
using FeederCopy.Core.Services.Data;

namespace FeederCopy.Tests.Presenters
{
    public sealed class BusSortPresenterTests : IDisposable
    {
        private readonly TempDatabase _temp = new();
        private readonly FeederService _service;
        private readonly FakeBusSortView _view = new();
        private readonly FakeErrorLog _log = new();
        private readonly BusSortPresenter _presenter;
        private AppSettings _settings = AppSettings.Defaults;
        private readonly List<string> _savedTags = [];
        private int _sorted;

        public BusSortPresenterTests()
        {
            _service = new FeederService(_temp.Db);
            _presenter = new BusSortPresenter(_view, _service, () => _settings, tags =>
            {
                _savedTags.Add(tags);
                _settings = _settings with { HandPlacedBusTags = tags };
            }, _log);
            _presenter.Sorted += () => _sorted++;
        }

        public void Dispose() => _temp.Dispose();

        private static ConsumerEntry E(string? bus, string id) => new(bus, id);

        [Fact]
        public void Hidden_DoesNotLoadUntilShown()
        {
            _service.CreateFeeder("F", [E("A1", "c1")]);
            _presenter.OnFeedersChanged();
            Assert.Equal(0, _view.PlansShown);
            _presenter.OnShown();
            Assert.Equal(1, _view.PlansShown);
            _presenter.OnShown();
            Assert.Equal(1, _view.PlansShown);
        }

        [Fact]
        public void NoFeeders_ShowsEmpty()
        {
            _presenter.OnShown();
            Assert.Equal("No feeders yet. Import a feeder workbook in the Feeders tab.", _view.EmptyMessage);
        }

        [Fact]
        public void Shown_ListsBlocksWithRowsNowAndAfterSort()
        {
            _service.CreateFeeder("Abuyog", [E("A2", "c1"), E("A2", "c2"), E("A1", "c3"), E("A1jnc1", "c4")]);
            _presenter.OnShown();
            Assert.Equal("3 buses  ·  2 of 4 rows move when sorted", _view.Summary);
            Assert.True(_view.CanSort);
            Assert.Equal(
                [("A2", 1, 2, 3, 4, (string?)null), ("A1", 3, 3, 1, 1, null), ("A1jnc1", 4, 4, 2, 2, "jnc")],
                _view.Blocks.Select(b => (b.Bus, b.From, b.To, b.NewFrom, b.NewTo, b.HandPlacedTag)));
            Assert.Equal([("A", "jnc", true)], _view.Tags.Select(t => (t.Code, t.Tag, t.HandPlaced)));
        }

        [Fact]
        public void FeederSelected_ShowsThatFeeder()
        {
            _service.CreateFeeder("Abuyog", [E("A1", "c1")]);
            long bacon = _service.CreateFeeder("Bacon", [E("B1", "c2"), E("B1", "c3")]);
            _presenter.OnShown();
            _presenter.OnFeederSelected(bacon);
            Assert.Equal(bacon, _view.SelectedFeederId);
            Assert.Equal([("B1", 1, 2)], _view.Blocks.Select(b => (b.Bus, b.From, b.To)));
            Assert.False(_view.CanSort);
            Assert.Equal("1 bus  ·  already in order", _view.Summary);
        }

        [Fact]
        public void TagToggled_SavesSettingAndReplans()
        {
            _service.CreateFeeder("Abuyog", [E("A1jnc1", "c1"), E("A1", "c2")]);
            _presenter.OnShown();
            Assert.Equal("jnc", _view.Blocks[0].HandPlacedTag);
            _presenter.OnTagToggled("A", "jnc", false);
            Assert.Equal(["sp, jnc, -A:jnc"], _savedTags);
            Assert.Null(_view.Blocks[0].HandPlacedTag);
            Assert.False(_view.Tags.Single().HandPlaced);
        }

        [Fact]
        public void Sort_ConfirmYes_SortsSavesRaisesSorted()
        {
            long id = _service.CreateFeeder("F", [E("A2", "c1"), E("A1", "c2")]);
            _presenter.OnShown();
            _presenter.OnSortClicked();
            Assert.Equal(["Sort the bus IDs in F? 1 of 2 rows will move. Buses with hand-placed tags stay under their parent bus in the order you placed them."], _view.ConfirmMessages);
            Assert.Equal([E("A1", "c2"), E("A2", "c1")], _service.GetConsumers(id));
            Assert.Equal(1, _sorted);
            Assert.False(_view.CanSort);
        }

        [Fact]
        public void Sort_ConfirmNo_NoChange()
        {
            long id = _service.CreateFeeder("F", [E("A2", "c1"), E("A1", "c2")]);
            _view.ConfirmAnswer = false;
            _presenter.OnShown();
            _presenter.OnSortClicked();
            Assert.Equal([E("A2", "c1"), E("A1", "c2")], _service.GetConsumers(id));
            Assert.Equal(0, _sorted);
        }

        [Fact]
        public void Sort_AlreadyInOrder_SaysSo()
        {
            _service.CreateFeeder("F", [E("A1", "c1"), E("A2", "c2")]);
            _presenter.OnShown();
            _presenter.OnSortClicked();
            Assert.Empty(_view.ConfirmMessages);
            Assert.Equal(["The bus IDs in F are already in order."], _view.Infos);
            Assert.Equal(0, _sorted);
        }
    }
}
