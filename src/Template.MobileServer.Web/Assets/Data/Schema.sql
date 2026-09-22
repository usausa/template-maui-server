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
