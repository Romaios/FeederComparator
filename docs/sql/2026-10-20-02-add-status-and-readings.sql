ALTER TABLE FeederConsumers ADD COLUMN Active INTEGER NOT NULL DEFAULT 1;
CREATE TABLE Readings (
    ConsumerId TEXT NOT NULL,
    BillMonth TEXT NOT NULL,
    Kwh REAL NULL,
    Status TEXT NOT NULL,
    PRIMARY KEY (ConsumerId, BillMonth)
);
CREATE INDEX IX_Readings_BillMonth ON Readings(BillMonth);
