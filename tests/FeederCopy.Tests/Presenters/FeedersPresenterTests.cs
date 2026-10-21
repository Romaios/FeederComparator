using FeederCopy.Core.Models.Billing;
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

        private static string Workbook(params (string Name, object?[][] Rows)[] sheets)
        {
            string path = Path.Combine(TestFiles.TempFolder(), "feeders.xlsx");
            object?[] header = ["Count", "From Bus ID", "To Customer ID"];
            return TestFiles.WriteXlsxSheets(sheets.Select(s => (s.Name, (IEnumerable<object?[]>)[header, .. s.Rows])), path);
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
        public async Task Import_NewFeeders_ShowsPreview()
        {
            _presenter.Load();
            _view.NextOpenFile = Workbook(("Abuyog", [[1, "A", "c1"], [2, "B", "c2"], [3, "A", null]]), ("Bacon", [[1, "X", "c3"]]));
            await _presenter.OnImportClickedAsync();
            ImportPreview p = _view.Preview!;
            Assert.Equal("Import from feeders.xlsx", p.Title);
            Assert.Equal(["Abuyog — new feeder · 2 consumers · 2 buses", "Bacon — new feeder · 1 consumer · 1 bus"], p.Feeders);
            Assert.Equal(["Abuyog row 4: Bus ID without a consumer ID; skipped"], p.Notes);
            Assert.Empty(p.Problems);
            Assert.True(p.CanSave);
            Assert.Equal((false, (string?)null), _view.Busy[^1]);
        }

        [Fact]
        public async Task Import_ExistingFeeder_ShowsReplaceWithDiff()
        {
            Seed("Abuyog", E("A", "c1"), E("A", "c2"), E("B", "c3"));
            _view.NextOpenFile = Workbook(("ABUYOG", [[1, "A", "c1"], [2, "B", "c2"], [3, "B", "c4"]]));
            await _presenter.OnImportClickedAsync();
            Assert.Equal(["Abuyog — replaces the saved list · 1 added, 1 removed, 1 moved to a different bus · 3 consumers"], _view.Preview!.Feeders);
            Assert.True(_view.Preview.CanSave);
        }

        [Fact]
        public async Task Import_ConsumerInFeederNotInFile_CannotSave()
        {
            Seed("Washington", E("X", "c1"));
            _view.NextOpenFile = Workbook(("Abuyog", [[1, "A", "c1"]]));
            await _presenter.OnImportClickedAsync();
            Assert.Equal(["Consumer c1 is already in Washington under bus X."], _view.Preview!.Problems);
            Assert.False(_view.Preview.CanSave);
        }

        [Fact]
        public async Task Import_ConsumerMovedBetweenFeedersInFile_Allowed()
        {
            Seed("Abuyog", E("A", "c1"));
            _view.NextOpenFile = Workbook(("Abuyog", [[1, "A", "c2"]]), ("Bacon", [[1, "B", "c1"]]));
            await _presenter.OnImportClickedAsync();
            Assert.Empty(_view.Preview!.Problems);
            _presenter.OnSavePreviewClicked();
            long bacon = _service.GetFeeders().Single(f => f.Name == "Bacon").Id;
            Assert.Equal([E("B", "c1")], _service.GetConsumers(bacon));
        }

        [Fact]
        public async Task Import_ParserProblem_CannotSave()
        {
            _presenter.Load();
            string path = Path.Combine(TestFiles.TempFolder(), "feeders.xlsx");
            TestFiles.WriteXlsxSheets([("Abuyog", [["Count", "From Bus ID", "To Customer ID", "Status"], [1, "A", "c1", "x"]])], path);
            _view.NextOpenFile = path;
            await _presenter.OnImportClickedAsync();
            Assert.Equal(["Sheet Abuyog row 2: status 'x' must be 1 or 0."], _view.Preview!.Problems);
            Assert.False(_view.Preview.CanSave);
        }

        [Fact]
        public async Task Import_DuplicateIds_CanStillSave()
        {
            _presenter.Load();
            _view.NextOpenFile = Workbook(("Abuyog", [[1, "A", "c1"], [2, "A", "c1"]]), ("Bacon", [[1, "B", "c1"], [2, "B", "c2"]]));
            await _presenter.OnImportClickedAsync();
            Assert.Empty(_view.Preview!.Problems);
            Assert.True(_view.Preview.CanSave);
            _presenter.OnSavePreviewClicked();
            Assert.Equal("Abuyog", _service.FindConsumer("c1")!.FeederName);
        }

        [Fact]
        public async Task Import_Save_CreatesReplacesSelectsFirst_RaisesChanged()
        {
            long abuyog = Seed("Abuyog", E("A", "c1"));
            Seed("Zed", E("Z", "c9"));
            _view.NextOpenFile = Workbook(("Abuyog", [[1, "A", "c1"], [2, "A", "c2"]]), ("Bacon", [[1, "B", "c3"]]));
            await _presenter.OnImportClickedAsync();
            _presenter.OnSavePreviewClicked();
            Assert.Null(_view.Preview);
            Assert.Equal(abuyog, _view.ConsumersFeeder!.Id);
            Assert.Equal([E("A", "c1"), E("A", "c2")], _service.GetConsumers(abuyog));
            Assert.Equal(["Abuyog", "Bacon", "Zed"], _service.GetFeeders().Select(f => f.Name));
            Assert.Equal(1, _changed);
        }

        [Fact]
        public async Task Import_PickerCancelled_NoPreview()
        {
            _presenter.Load();
            _view.NextOpenFile = null;
            await _presenter.OnImportClickedAsync();
            Assert.Null(_view.Preview);
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
        public void Add_ExistingBusWith25Rows_NewBecomesRow26_RestMoveDownOne()
        {
            ConsumerEntry[] rows =
            [
                .. Enumerable.Range(1, 25).Select(i => E("AFSEC3jnc1", $"a{i}")),
                .. Enumerable.Range(1, 5).Select(i => E("AFSEC3jnc2", $"b{i}")),
            ];
            Seed("Abuyog", rows);
            Assert.Equal(26, _view.Rows.Single(r => r.ConsumerId == "b1").Count);

            _presenter.OnAddClicked("AFSEC3jnc1", "NEW");

            Assert.Equal(new ConsumerRow(26, "AFSEC3jnc1", "NEW"), _view.Rows.Single(r => r.ConsumerId == "NEW"));
            Assert.Equal(25, _view.Rows.Single(r => r.ConsumerId == "a25").Count);
            Assert.Equal(27, _view.Rows.Single(r => r.ConsumerId == "b1").Count);
            Assert.Equal(31, _view.Rows.Single(r => r.ConsumerId == "b5").Count);
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
        public void SetActive_False_PersistsAndShows()
        {
            long id = Seed("F", E("A", "c1"), E("A", "c2"));
            _presenter.OnSetActiveClicked("c2", false);
            Assert.Equal([E("A", "c1"), new ConsumerEntry("A", "c2", false)], _service.GetConsumers(id));
            Assert.False(_view.Rows[1].Active);
            Assert.Equal("c2", _view.SelectedConsumer);
            Assert.Equal(1, _changed);
        }

        [Fact]
        public void Move_KeepsInactive()
        {
            long other = Seed("Other", E("A", "x1"));
            Seed("F", new ConsumerEntry("A", "c1", false));
            _presenter.OnMoveClicked("c1", other);
            Assert.Equal([E("A", "x1"), new ConsumerEntry("A", "c1", false)], _service.GetConsumers(other));
        }

        [Fact]
        public async Task Import_PreviewCountsInactive()
        {
            _presenter.Load();
            string path = Path.Combine(TestFiles.TempFolder(), "feeders.xlsx");
            TestFiles.WriteXlsxSheets([("Abuyog", [["Count", "From Bus ID", "To Customer ID", "Status"], [1, "A", "c1", 1], [2, "A", "c2", 0]])], path);
            _view.NextOpenFile = path;
            await _presenter.OnImportClickedAsync();
            Assert.Equal(["Abuyog — new feeder · 2 consumers (1 inactive) · 1 bus"], _view.Preview!.Feeders);
        }

        [Fact]
        public void Sort_ConfirmYes_SortsAndSaves()
        {
            long id = Seed("F", E("A2", "c1"), E("A1", "c2"));
            _presenter.OnSortClicked();
            Assert.Equal(["Sort the bus IDs in F? 1 of 2 rows will move. Buses with hand-placed tags stay under their parent bus in the order you placed them."], _view.ConfirmMessages);
            Assert.Equal([E("A1", "c2"), E("A2", "c1")], _service.GetConsumers(id));
            Assert.Equal(1, _changed);
        }

        [Fact]
        public void Sort_ConfirmNo_NoChange()
        {
            long id = Seed("F", E("A2", "c1"), E("A1", "c2"));
            _view.ConfirmAnswer = false;
            _presenter.OnSortClicked();
            Assert.Equal([E("A2", "c1"), E("A1", "c2")], _service.GetConsumers(id));
            Assert.Equal(0, _changed);
        }

        [Fact]
        public void Sort_AlreadyInOrder_SaysSo()
        {
            Seed("F", E("A1", "c1"), E("A2", "c2"));
            _presenter.OnSortClicked();
            Assert.Empty(_view.ConfirmMessages);
            Assert.Equal(["The bus IDs in F are already in order."], _view.Infos);
        }

        [Fact]
        public void Add_NewHandPlacedBus_Automatic_AsksWhereItGoes()
        {
            long id = Seed("F", E("A1", "c1"), E("A2", "c2"));
            _presenter.OnAddClicked("A1jnc1", "c9", null);
            Assert.Equal("A1jnc1 has a hand-placed tag (jnc). Choose where it goes in 'Place new bus after'.", _view.AddError);
            Assert.Equal(2, _service.GetConsumers(id).Count);
        }

        [Fact]
        public void Add_PlaceAfter_InsertsRightAfterThatBus()
        {
            long id = Seed("F", E("A1", "c1"), E("A2", "c2"));
            _presenter.OnAddClicked("A1jnc1", "c9", "A1");
            Assert.Equal([E("A1", "c1"), E("A1jnc1", "c9"), E("A2", "c2")], _service.GetConsumers(id));
        }

        [Fact]
        public void Add_PlaceAfter_IgnoredWhenBusAlreadyExists()
        {
            long id = Seed("F", E("A1", "c1"), E("A2", "c2"));
            _presenter.OnAddClicked("A2", "c9", "A1");
            Assert.Equal([E("A1", "c1"), E("A2", "c2"), E("A2", "c9")], _service.GetConsumers(id));
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
            public IReadOnlyList<(string FeederName, ConsumerEntry Entry)> GetAllConsumers() => [];
            public void SetActive(string consumerId, bool active, BillMonth? inactiveFrom = null) => throw new InvalidOperationException("boom");
            public IReadOnlyDictionary<string, BillMonth> GetInactiveFrom() => new Dictionary<string, BillMonth>();
            public ConsumerLocation? FindConsumer(string consumerId) => null;
            public IReadOnlyList<(string ConsumerId, ConsumerLocation Where)> FindConsumers(IEnumerable<string> consumerIds) => [];
            public long CreateFeeder(string name, IReadOnlyList<ConsumerEntry> consumers) => throw new InvalidOperationException("boom");
            public void SaveConsumers(IReadOnlyDictionary<long, IReadOnlyList<ConsumerEntry>> byFeeder) => throw new InvalidOperationException("boom");
            public IReadOnlyList<long> ImportFeeders(IReadOnlyList<(string Name, IReadOnlyList<ConsumerEntry> Consumers)> feeders) => throw new InvalidOperationException("boom");
            public void RenameFeeder(long feederId, string name) => throw new InvalidOperationException("boom");
            public void DeleteFeeder(long feederId) => throw new InvalidOperationException("boom");
        }
    }
}
