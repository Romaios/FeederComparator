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
        public void Shown_ListsSavedOrderWithTags()
        {
            _service.CreateFeeder("Abuyog", [E("A2", "c1"), E("A2", "c2"), E("A1", "c3"), E("A1jnc1", "c4")]);
            _presenter.OnShown();
            Assert.Equal("3 buses  ·  saved order", _view.Summary);
            Assert.False(_view.HasChanges);
            Assert.Equal([("A2", 1, 2, (string?)null), ("A1", 3, 3, null), ("A1jnc1", 4, 4, "jnc")], _view.Blocks.Select(b => (b.Bus, b.From, b.To, b.HandPlacedTag)));
            Assert.Equal([("A", "jnc", true)], _view.Tags.Select(t => (t.Code, t.Tag, t.HandPlaced)));
        }

        [Fact]
        public void Drag_MovesBlock_ShowsUnsavedRows_SelectsIt()
        {
            long id = _service.CreateFeeder("F", [E("A2", "c1"), E("A2", "c2"), E("A1", "c3")]);
            _presenter.OnShown();
            _presenter.OnBlocksMoved([1], 0);
            Assert.Equal([("A1", 3, 3, 1, 1), ("A2", 1, 2, 2, 3)], _view.Blocks.Select(b => (b.Bus, b.From, b.To, b.NewFrom, b.NewTo)));
            Assert.Equal([0], _view.SelectedBlocks);
            Assert.True(_view.HasChanges);
            Assert.Equal("2 buses  ·  not saved: 1 of 3 rows move", _view.Summary);
            Assert.Equal([E("A2", "c1"), E("A2", "c2"), E("A1", "c3")], _service.GetConsumers(id));
        }

        [Fact]
        public void MoveUpDown_AtEdges_DoNothing()
        {
            _service.CreateFeeder("F", [E("A1", "c1"), E("A2", "c2")]);
            _presenter.OnShown();
            _presenter.OnMoveUpClicked([0]);
            _presenter.OnMoveDownClicked([1]);
            Assert.False(_view.HasChanges);
            _presenter.OnMoveDownClicked([0]);
            Assert.Equal(["A2", "A1"], _view.Blocks.Select(b => b.Bus));
            Assert.Equal([1], _view.SelectedBlocks);
        }

        [Fact]
        public void SortByNumber_ChangesDraftOnly_SaveConfirmsAndSaves()
        {
            long id = _service.CreateFeeder("F", [E("A2", "c1"), E("A1", "c2")]);
            _presenter.OnShown();
            _presenter.OnSortClicked();
            Assert.Equal(["A1", "A2"], _view.Blocks.Select(b => b.Bus));
            Assert.Equal([E("A2", "c1"), E("A1", "c2")], _service.GetConsumers(id));
            _presenter.OnSaveClicked();
            Assert.Equal(["Save the new bus order for F? 1 of 2 rows will move."], _view.ConfirmMessages);
            Assert.Equal([E("A1", "c2"), E("A2", "c1")], _service.GetConsumers(id));
            Assert.Equal(1, _sorted);
            Assert.False(_view.HasChanges);
        }

        [Fact]
        public void Save_ConfirmNo_KeepsDraft()
        {
            long id = _service.CreateFeeder("F", [E("A2", "c1"), E("A1", "c2")]);
            _view.ConfirmAnswer = false;
            _presenter.OnShown();
            _presenter.OnBlocksMoved([1], 0);
            _presenter.OnSaveClicked();
            Assert.Equal([E("A2", "c1"), E("A1", "c2")], _service.GetConsumers(id));
            Assert.True(_view.HasChanges);
            Assert.Equal(0, _sorted);
        }

        [Fact]
        public void Undo_RestoresSavedOrder()
        {
            _service.CreateFeeder("F", [E("A2", "c1"), E("A1", "c2")]);
            _presenter.OnShown();
            _presenter.OnBlocksMoved([1], 0);
            _presenter.OnUndoClicked();
            Assert.Equal(["A2", "A1"], _view.Blocks.Select(b => b.Bus));
            Assert.False(_view.HasChanges);
        }

        [Fact]
        public void SortByNumber_AlreadyInOrder_SaysSo()
        {
            _service.CreateFeeder("F", [E("A1", "c1"), E("A2", "c2")]);
            _presenter.OnShown();
            _presenter.OnSortClicked();
            Assert.Equal(["The bus IDs in F are already in number order."], _view.Infos);
            Assert.False(_view.HasChanges);
        }

        [Fact]
        public void FeederSelected_WithUnsavedOrder_AsksBeforeDiscarding()
        {
            long abuyog = _service.CreateFeeder("Abuyog", [E("A2", "c1"), E("A1", "c2")]);
            long bacon = _service.CreateFeeder("Bacon", [E("B1", "c3")]);
            _presenter.OnShown();
            _presenter.OnBlocksMoved([1], 0);
            _view.ConfirmAnswer = false;
            _presenter.OnFeederSelected(bacon);
            Assert.Equal(["The new bus order for Abuyog isn't saved. Discard it?"], _view.ConfirmMessages);
            Assert.Equal(abuyog, _view.SelectedFeederId);
            Assert.True(_view.HasChanges);
            _view.ConfirmAnswer = true;
            _presenter.OnFeederSelected(bacon);
            Assert.Equal(bacon, _view.SelectedFeederId);
            Assert.Equal(["B1"], _view.Blocks.Select(b => b.Bus));
        }

        [Fact]
        public void TagToggled_SavesSettingAndKeepsDraft()
        {
            _service.CreateFeeder("Abuyog", [E("A1jnc1", "c1"), E("A1", "c2")]);
            _presenter.OnShown();
            _presenter.OnBlocksMoved([1], 0);
            _presenter.OnTagToggled("A", "jnc", false);
            Assert.Equal(["sp, jnc, -A:jnc"], _savedTags);
            Assert.Equal(["A1", "A1jnc1"], _view.Blocks.Select(b => b.Bus));
            Assert.Null(_view.Blocks[1].HandPlacedTag);
            Assert.False(_view.Tags.Single().HandPlaced);
        }

        [Fact]
        public void SameBusInTwoPlaces_NoChangesAndNoPromptOnSwitch()
        {
            _service.CreateFeeder("Abuyog", [E("A5", "c1"), E("A5jnc1", "c2"), E("A5", "c3")]);
            long bacon = _service.CreateFeeder("Bacon", [E("B1", "c4")]);
            _presenter.OnShown();
            Assert.False(_view.HasChanges);
            Assert.Equal("2 buses  ·  saved order", _view.Summary);
            _presenter.OnFeederSelected(bacon);
            Assert.Empty(_view.ConfirmMessages);
            Assert.Equal(bacon, _view.SelectedFeederId);
        }
    }
}
