CREATE TABLE Feeders (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL UNIQUE COLLATE NOCASE,
    CreatedAt TEXT NOT NULL
);
CREATE TABLE FeederConsumers (
    Id INTEGER PRIMARY KEY,
    FeederId INTEGER NOT NULL REFERENCES Feeders(Id) ON DELETE CASCADE,
    BusId TEXT NULL,
    ConsumerId TEXT NOT NULL UNIQUE,
    Position INTEGER NOT NULL
);
CREATE INDEX IX_FeederConsumers_Feeder_Position ON FeederConsumers(FeederId, Position);
