using FeederCopy.Core.Models.Feeders;
using FeederCopy.Core.Models.Settings;
using FeederCopy.Core.Presenters.Feeders;
using FeederCopy.Core.Services.Data;

namespace FeederCopy.Tests.Presenters
{
    public sealed class FeedersPresenterTests : IDisposable
    {
        private readonly TempDatabase _temp = new();
        private readonly FeederService _service;
        private readonly FakeFeedersView _view = new();
        private readonly FakeErrorLog _log = new();
        private readonly FeedersPresenter _presenter;
        private int _changed;

        public FeedersPresenterTests()
        {
            _service = new FeederService(_temp.Db);
            _presenter = new FeedersPresenter(_view, _service, () => AppSettings.Defaults, _log);
            _presenter.FeedersChanged += () => _changed++;
        }

        public void Dispose() => _temp.Dispose();

        private static ConsumerEntry E(string? bus, string id) => new(bus, id);

        private static string ListFile(string name, params object?[][] rows)
        {
            string path = Path.Combine(TestFiles.TempFolder(), name + ".xlsx");
            return TestFiles.WriteXlsx([["Count", "Bus ID", "Consumer ID"], .. rows], path);
        }

        private long Seed(string name, params ConsumerEntry[] entries)
        {
            long id = _service.CreateFeeder(name, entries);
            _presenter.Load();
            _presenter.OnFeederSelected(id);
            return id;
        }

        [Fact]
        public void Load_NoFeeders_ShowsEmpty()
        {
            _presenter.Load();
            Assert.Equal(1, _view.NoFeedersShown);
        }

        [Fact]
        public void Load_SelectsFirstFeeder()
        {
            _service.CreateFeeder("B Feeder", [E("A", "c1")]);
            _service.CreateFeeder("A Feeder", [E("A", "c2")]);
            _presenter.Load();
            Assert.Equal("A Feeder", _view.ConsumersFeeder!.Name);
            Assert.Equal(["B Feeder"], _view.OtherFeeders.Select(f => f.Name));
        }

        [Fact]
        public async Task ImportNew_ShowsPreviewWithSummary()
        {
            _presenter.Load();
            _view.NextOpenFile = ListFile("New York Feeder", [1, "A", "c1"], [2, "B", "c2"], [3, "A", null]);
            await _presenter.OnImportNewClickedAsync();
            FeederPreview p = _view.Preview!;
            Assert.Equal(PreviewMode.NewFeeder, p.Mode);
            Assert.Equal("New York Feeder", p.Name);
            Assert.Equal("2 consumers · 2 buses · 1 row skipped", p.Summary);
            Assert.Equal(["Row 4: Bus ID without a consumer ID; skipped"], p.Problems);
            Assert.True(p.CanSave);
            Assert.Equal((false, (string?)null), _view.Busy[^1]);
        }

        [Fact]
        public async Task ImportNew_ConsumerInOtherFeeder_CannotSave()
        {
            Seed("Washington", E("X", "c1"));
            _view.NextOpenFile = ListFile("New York", [1, "A", "c1"]);
            await _presenter.OnImportNewClickedAsync();
            Assert.Contains("Consumer c1 is already in Washington under bus X.", _view.Preview!.Problems);
            Assert.False(_view.Preview.CanSave);
        }

        [Fact]
        public async Task ImportNew_Fatal_CannotSave()
        {
            _presenter.Load();
            _view.NextOpenFile = ListFile("Dupes", [1, "A", "c1"], [2, "A", "c1"]);
            await _presenter.OnImportNewClickedAsync();
            Assert.Equal("Consumer ID c1 appears more than once (rows 2, 3).", _view.Preview!.Problems[0]);
            Assert.False(_view.Preview.CanSave);
        }

        [Fact]
        public async Task ImportNew_Save_CreatesAndSelects_RaisesChanged()
        {
            Seed("Washington", E("X", "c9"));
            _view.NextOpenFile = ListFile("New York", [1, "A", "c1"], [2, "B", "c2"]);
            await _presenter.OnImportNewClickedAsync();
            _presenter.OnSavePreviewClicked();
            Assert.Null(_view.Preview);
            Assert.Equal("New York", _view.ConsumersFeeder!.Name);
            Assert.Equal([E("A", "c1"), E("B", "c2")], _service.GetConsumers(_view.ConsumersFeeder.Id));
            Assert.Equal(1, _changed);
        }

        [Fact]
        public async Task ImportNew_DuplicateName_CannotSave()
        {
            Seed("New York", E("X", "c9"));
            _view.NextOpenFile = ListFile("new york", [1, "A", "c1"]);
            await _presenter.OnImportNewClickedAsync();
            Assert.False(_view.Preview!.CanSave);
            Assert.Contains("A feeder named 'new york' already exists.", _view.Preview.Problems);
            _presenter.OnPreviewNameChanged("Brooklyn");
            Assert.True(_view.Preview!.CanSave);
            Assert.Equal("Brooklyn", _view.Preview.Name);
        }

        [Fact]
        public async Task ImportNew_PickerCancelled_NoPreview()
        {
            _presenter.Load();
            _view.NextOpenFile = null;
            await _presenter.OnImportNewClickedAsync();
            Assert.Null(_view.Preview);
        }

        [Fact]
        public async Task Update_ShowsDiffSummary()
        {
            Seed("F", E("A", "c1"), E("A", "c2"), E("B", "c3"));
            _view.NextOpenFile = ListFile("whatever", [1, "A", "c1"], [2, "B", "c2"], [3, "B", "c4"]);
            await _presenter.OnUpdateClickedAsync();
            FeederPreview p = _view.Preview!;
            Assert.Equal(PreviewMode.Update, p.Mode);
            Assert.Equal("F", p.Name);
            Assert.Equal("1 added, 1 removed, 1 moved to a different bus · 3 consumers", p.Summary);
            Assert.True(p.CanSave);
        }

        [Fact]
        public async Task Update_Save_ReplacesList()
        {
            long id = Seed("F", E("A", "c1"), E("A", "c2"));
            _view.NextOpenFile = ListFile("whatever", [1, "B", "c2"], [2, "B", "c5"]);
            await _presenter.OnUpdateClickedAsync();
            _presenter.OnSavePreviewClicked();
            Assert.Equal([E("B", "c2"), E("B", "c5")], _service.GetConsumers(id));
            Assert.Equal(1, _changed);
        }

        [Fact]
        public async Task Update_ConsumerInOtherFeeder_CannotSave()
        {
            Seed("Other", E("Z", "c9"));
            Seed("F", E("A", "c1"));
            _view.NextOpenFile = ListFile("whatever", [1, "A", "c1"], [2, "A", "c9"]);
            await _presenter.OnUpdateClickedAsync();
            Assert.Equal(["Consumer c9 is already in Other under bus Z."], _view.Preview!.Problems);
            Assert.False(_view.Preview.CanSave);
        }

        [Fact]
        public void CancelPreview_Hides()
        {
            _presenter.OnCancelPreviewClicked();
            Assert.Null(_view.Preview);
        }

        [Fact]
        public void Add_ExistingBus_InsertedInBlock()
        {
            long id = Seed("F", E("A", "c1"), E("B", "c2"));
            _presenter.OnAddClicked("A", " c9 ");
            Assert.Equal([E("A", "c1"), E("A", "c9"), E("B", "c2")], _service.GetConsumers(id));
            Assert.Equal(1, _view.AddCleared);
            Assert.Null(_view.AddError);
            Assert.Equal("c9", _view.SelectedConsumer);
            Assert.Equal(1, _changed);
        }

        [Fact]
        public void Add_AlreadyInFeeder_ShowsAddError()
        {
            Seed("Other", E("Z", "c1"));
            long id = Seed("F", E("A", "c2"));
            _presenter.OnAddClicked("A", "c1");
            Assert.Equal("Consumer c1 is already in Other under bus Z.", _view.AddError);
            Assert.Equal([E("A", "c2")], _service.GetConsumers(id));
            Assert.Equal(0, _changed);
        }

        [Fact]
        public void Add_BlankConsumer_ShowsAddError()
        {
            Seed("F", E("A", "c2"));
            _presenter.OnAddClicked("A", " ");
            Assert.Equal("Enter a consumer ID.", _view.AddError);
        }

        [Fact]
        public void Add_BlankBus_UnknownAtBottom()
        {
            long id = Seed("F", E("A", "c1"), E("B", "c2"));
            _presenter.OnAddClicked(null, "c9");
            Assert.Equal(E(null, "c9"), _service.GetConsumers(id)[^1]);
            Assert.Equal("Unknown", _view.Rows[^1].BusLabel);
        }

        [Fact]
        public void SetBus_MovesIntoBlock()
        {
            long id = Seed("F", E("A", "c1"), E("B", "c2"), E(null, "c3"));
            _presenter.OnSetBusClicked("c3", "A");
            Assert.Equal([E("A", "c1"), E("A", "c3"), E("B", "c2")], _service.GetConsumers(id));
            Assert.Equal("c3", _view.SelectedConsumer);
        }

        [Fact]
        public void Move_ToOtherFeeder()
        {
            long other = Seed("Other", E("A", "x1"));
            long id = Seed("F", E("A", "c1"), E("B", "c2"));
            _presenter.OnMoveClicked("c1", other);
            Assert.Equal([E("B", "c2")], _service.GetConsumers(id));
            Assert.Equal([E("A", "x1"), E("A", "c1")], _service.GetConsumers(other));
            Assert.Equal(1, _changed);
        }

        [Fact]
        public void Remove_ConfirmNo_NoChange()
        {
            long id = Seed("F", E("A", "c1"));
            _view.ConfirmAnswer = false;
            _presenter.OnRemoveClicked("c1");
            Assert.Equal(["Remove consumer c1 from F?"], _view.ConfirmMessages);
            Assert.Single(_service.GetConsumers(id));
        }

        [Fact]
        public void Remove_ConfirmYes_Removed()
        {
            long id = Seed("F", E("A", "c1"), E("A", "c2"));
            _presenter.OnRemoveClicked("c1");
            Assert.Equal([E("A", "c2")], _service.GetConsumers(id));
        }

        [Fact]
        public void Delete_ConfirmYes_Removed()
        {
            Seed("F", E("A", "c1"), E("A", "c2"));
            _presenter.OnDeleteClicked();
            Assert.Equal(["Delete feeder 'F' and its 2 consumers?"], _view.ConfirmMessages);
            Assert.Empty(_service.GetFeeders());
            Assert.Equal(1, _view.NoFeedersShown);
            Assert.Equal(1, _changed);
        }

        [Fact]
        public void Delete_ConfirmNo_Kept()
        {
            Seed("F", E("A", "c1"));
            _view.ConfirmAnswer = false;
            _presenter.OnDeleteClicked();
            Assert.Single(_service.GetFeeders());
        }

        [Fact]
        public void Rename_Invalid_ShowsError()
        {
            Seed("Other");
            Seed("F");
            _presenter.OnRenameClicked("other");
            Assert.Equal(["A feeder named 'other' already exists."], _view.Errors);
        }

        [Fact]
        public void Rename_Valid_Renames()
        {
            Seed("F");
            _presenter.OnRenameClicked(" Brooklyn ");
            Assert.Equal("Brooklyn", _service.GetFeeders().Single().Name);
            Assert.Equal("Brooklyn", _view.ConsumersFeeder!.Name);
        }

        [Fact]
        public void Search_Found_SelectsFeederAndConsumer()
        {
            long other = Seed("Other", E("A", "x1"));
            Seed("F", E("A", "c1"));
            _presenter.OnSearch(" x1 ");
            Assert.Equal(other, _view.ConsumersFeeder!.Id);
            Assert.Equal("x1", _view.SelectedConsumer);
            Assert.Null(_view.SearchResult);
        }

        [Fact]
        public void Search_NotFound_Message()
        {
            Seed("F", E("A", "c1"));
            _presenter.OnSearch("c9");
            Assert.Equal("Consumer c9 isn't in any feeder.", _view.SearchResult);
        }

        [Fact]
        public void Create_FromNoFeeders_CreatesEmptyFeederAndSelectsIt()
        {
            _presenter.Load();
            _presenter.OnCreateClicked(" Brooklyn Feeder ");
            FeederSummary created = _service.GetFeeders().Single();
            Assert.Equal("Brooklyn Feeder", created.Name);
            Assert.Empty(_service.GetConsumers(created.Id));
            Assert.Equal(created.Id, _view.ConsumersFeeder!.Id);
            Assert.Empty(_view.Rows);
            Assert.Equal(1, _view.CreateCleared);
            Assert.Null(_view.CreateError);
            Assert.Equal(1, _changed);
        }

        [Fact]
        public void Create_ThenAddConsumer_GoesIntoNewFeeder()
        {
            Seed("New York", E("A", "c1"));
            _presenter.OnCreateClicked("Brooklyn");
            _presenter.OnAddClicked("Z", "c9");
            long brooklyn = _service.GetFeeders().Single(f => f.Name == "Brooklyn").Id;
            Assert.Equal([E("Z", "c9")], _service.GetConsumers(brooklyn));
        }

        [Fact]
        public void Create_DuplicateName_ShowsError()
        {
            Seed("New York", E("A", "c1"));
            _presenter.OnCreateClicked("new york");
            Assert.Equal("A feeder named 'new york' already exists.", _view.CreateError);
            Assert.Single(_service.GetFeeders());
            Assert.Equal(0, _view.CreateCleared);
            Assert.Equal(0, _changed);
        }

        [Fact]
        public void Create_BlankName_ShowsError()
        {
            _presenter.Load();
            _presenter.OnCreateClicked("  ");
            Assert.Equal("Enter a feeder name.", _view.CreateError);
            Assert.Empty(_service.GetFeeders());
        }

        [Fact]
        public void ServiceThrows_LoggedAndShown()
        {
            var presenter = new FeedersPresenter(_view, new FailingSaveService(), () => AppSettings.Defaults, _log);
            presenter.Load();
            presenter.OnAddClicked("A", "c1");
            Assert.Single(_log.Entries);
            Assert.Equal(["Could not save the change. boom"], _view.Errors);
        }

        private sealed class FailingSaveService : IFeederService
        {
            public IReadOnlyList<FeederSummary> GetFeeders() => [new FeederSummary(1, "F", 0, 0)];
            public IReadOnlyList<ConsumerEntry> GetConsumers(long feederId) => [];
            public ConsumerLocation? FindConsumer(string consumerId) => null;
            public IReadOnlyList<(string ConsumerId, ConsumerLocation Where)> FindConsumers(IEnumerable<string> consumerIds) => [];
            public long CreateFeeder(string name, IReadOnlyList<ConsumerEntry> consumers) => throw new InvalidOperationException("boom");
            public void SaveConsumers(IReadOnlyDictionary<long, IReadOnlyList<ConsumerEntry>> byFeeder) => throw new InvalidOperationException("boom");
            public void RenameFeeder(long feederId, string name) => throw new InvalidOperationException("boom");
            public void DeleteFeeder(long feederId) => throw new InvalidOperationException("boom");
        }
    }
}
