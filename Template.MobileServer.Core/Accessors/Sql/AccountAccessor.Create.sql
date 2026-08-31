CREATE TABLE IF NOT EXISTS Account (
    Id         INTEGER  NOT NULL,
    Name       TEXT     NOT NULL,
    Password   BLOB     NOT NULL,
    Role       TEXT     NOT NULL,
    CreatedAt  TEXT     NOT NULL,
    PRIMARY KEY (Id AUTOINCREMENT),
    UNIQUE (Name)
);
