CREATE TABLE IF NOT EXISTS SchemaScripts (
    Name VARCHAR(100) NOT NULL PRIMARY KEY,
    AppliedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4;

CREATE TABLE Feeders (
    Id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UNIQUE KEY UX_Feeders_Name (Name)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

CREATE TABLE FeederConsumers (
    Id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    FeederId BIGINT NOT NULL,
    BusId VARCHAR(100) NULL,
    ConsumerId VARCHAR(50) NOT NULL,
    Position INT NOT NULL,
    Active BOOLEAN NOT NULL DEFAULT TRUE,
    X DOUBLE NULL,
    Y DOUBLE NULL,
    UNIQUE KEY UX_FeederConsumers_ConsumerId (ConsumerId),
    KEY IX_FeederConsumers_Feeder_Position (FeederId, Position),
    CONSTRAINT FK_FeederConsumers_Feeders FOREIGN KEY (FeederId) REFERENCES Feeders (Id) ON DELETE CASCADE
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

CREATE TABLE Readings (
    ConsumerId VARCHAR(50) NOT NULL,
    BillMonth CHAR(6) NOT NULL,
    Kwh DOUBLE NULL,
    Status VARCHAR(40) NOT NULL,
    PRIMARY KEY (ConsumerId, BillMonth),
    KEY IX_Readings_BillMonth (BillMonth)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

CREATE TABLE ConsumerInactiveFrom (
    ConsumerId VARCHAR(50) NOT NULL PRIMARY KEY,
    BillMonth CHAR(6) NOT NULL
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

CREATE TABLE NetworkLines (
    Id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    FromNode VARCHAR(100) NOT NULL,
    FromX DOUBLE NULL,
    FromY DOUBLE NULL,
    ToNode VARCHAR(100) NOT NULL,
    ToX DOUBLE NULL,
    ToY DOUBLE NULL,
    KEY IX_NetworkLines_FromNode (FromNode),
    KEY IX_NetworkLines_ToNode (ToNode)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

INSERT INTO SchemaScripts (Name) VALUES ('2026-10-23-01-create-tables');
