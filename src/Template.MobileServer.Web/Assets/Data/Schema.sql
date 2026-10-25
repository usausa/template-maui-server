CREATE TABLE IF NOT EXISTS Data (
    Id         INTEGER  NOT NULL,
    Name       TEXT     NOT NULL,
    Value      INTEGER  NOT NULL,
    CreatedAt  TEXT     NOT NULL,
    PRIMARY KEY (Id AUTOINCREMENT),
    UNIQUE (Name)
);

CREATE TABLE IF NOT EXISTS Setting (
    Key        TEXT     NOT NULL,
    Value      TEXT     NOT NULL,
    UpdatedAt  TEXT     NOT NULL,
    PRIMARY KEY (Key)
);

CREATE TABLE IF NOT EXISTS Device (
    TerminalId    TEXT     NOT NULL,
    Name          TEXT     NOT NULL,
    GroupName     TEXT,
    Note          TEXT,
    IsEnabled     INTEGER  NOT NULL,
    RegisteredAt  TEXT     NOT NULL,
    PRIMARY KEY (TerminalId)
);

CREATE TABLE IF NOT EXISTS PushMessage (
    Id           INTEGER  NOT NULL,
    TerminalId   TEXT     NOT NULL,
    Title        TEXT     NOT NULL,
    Body         TEXT     NOT NULL,
    CreatedAt    TEXT     NOT NULL,
    DeliveredAt  TEXT,
    PRIMARY KEY (Id AUTOINCREMENT)
);
CREATE INDEX IF NOT EXISTS IX_PushMessage_TerminalId ON PushMessage (TerminalId) WHERE DeliveredAt IS NULL;
CREATE INDEX IF NOT EXISTS IX_PushMessage_CreatedAt ON PushMessage (CreatedAt);
