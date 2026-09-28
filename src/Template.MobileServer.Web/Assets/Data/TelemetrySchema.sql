PRAGMA journal_mode = WAL;

CREATE TABLE IF NOT EXISTS DeviceInfo (
    DeviceId              TEXT     NOT NULL,
    InstallationId        TEXT     NOT NULL,
    Manufacturer          TEXT     NOT NULL,
    Model                 TEXT     NOT NULL,
    OsName                TEXT     NOT NULL,
    OsVersion             TEXT     NOT NULL,
    ServiceName           TEXT     NOT NULL,
    ServiceVersion        TEXT     NOT NULL,
    FirstReceivedAt       INTEGER  NOT NULL,
    LastReceivedAt        INTEGER  NOT NULL,
    PRIMARY KEY (DeviceId)
);

CREATE TABLE IF NOT EXISTS Resources (
    Id                    INTEGER  NOT NULL,
    Hash                  TEXT     NOT NULL,
    ServiceInstanceId     TEXT     NOT NULL,
    ServiceVersion        TEXT     NOT NULL,
    AttributesJson        TEXT     NOT NULL,
    FirstSeenAt           INTEGER  NOT NULL,
    PRIMARY KEY (Id),
    UNIQUE (Hash)
);

CREATE TABLE IF NOT EXISTS MetricSeries (
    Id                    INTEGER  NOT NULL,
    Name                  TEXT     NOT NULL,
    ScopeName             TEXT     NOT NULL,
    Unit                  TEXT     NOT NULL,
    Kind                  TEXT     NOT NULL,
    Temporality           TEXT     NOT NULL,
    IsMonotonic           INTEGER  NOT NULL,
    AttributesJson        TEXT     NOT NULL,
    PRIMARY KEY (Id),
    UNIQUE (Name, ScopeName, AttributesJson)
);

CREATE TABLE IF NOT EXISTS MetricPoints (
    SeriesId              INTEGER  NOT NULL,
    TimeUnixNano          INTEGER  NOT NULL,
    StartTimeUnixNano     INTEGER  NOT NULL,
    Value                 REAL,
    Count                 INTEGER,
    Sum                   REAL,
    Min                   REAL,
    Max                   REAL,
    Detail                TEXT,
    PRIMARY KEY (SeriesId, TimeUnixNano)
) WITHOUT ROWID;
CREATE INDEX IF NOT EXISTS IX_MetricPoints_TimeUnixNano ON MetricPoints (TimeUnixNano);

CREATE TABLE IF NOT EXISTS Spans (
    TraceId               TEXT     NOT NULL,
    SpanId                TEXT     NOT NULL,
    ParentSpanId          TEXT     NOT NULL,
    Name                  TEXT     NOT NULL,
    Kind                  TEXT     NOT NULL,
    StartTimeUnixNano     INTEGER  NOT NULL,
    EndTimeUnixNano       INTEGER  NOT NULL,
    StatusCode            TEXT     NOT NULL,
    StatusMessage         TEXT     NOT NULL,
    ScopeName             TEXT     NOT NULL,
    ResourceId            INTEGER  NOT NULL,
    AttributesJson        TEXT     NOT NULL,
    EventsJson            TEXT,
    LinksJson             TEXT,
    PRIMARY KEY (TraceId, SpanId)
) WITHOUT ROWID;
CREATE INDEX IF NOT EXISTS IX_Spans_StartTimeUnixNano ON Spans (StartTimeUnixNano);

CREATE TABLE IF NOT EXISTS Traces (
    TraceId               TEXT     NOT NULL,
    RootName              TEXT     NOT NULL,
    StartTimeUnixNano     INTEGER  NOT NULL,
    EndTimeUnixNano       INTEGER  NOT NULL,
    SpanCount             INTEGER  NOT NULL,
    ErrorCount            INTEGER  NOT NULL,
    PRIMARY KEY (TraceId)
) WITHOUT ROWID;
CREATE INDEX IF NOT EXISTS IX_Traces_StartTimeUnixNano ON Traces (StartTimeUnixNano);

CREATE TABLE IF NOT EXISTS Logs (
    Id                    INTEGER  NOT NULL,
    TimeUnixNano          INTEGER  NOT NULL,
    ObservedTimeUnixNano  INTEGER  NOT NULL,
    SeverityNumber        INTEGER  NOT NULL,
    SeverityText          TEXT     NOT NULL,
    EventName             TEXT     NOT NULL,
    Body                  TEXT     NOT NULL,
    TraceId               TEXT     NOT NULL,
    SpanId                TEXT     NOT NULL,
    ScopeName             TEXT     NOT NULL,
    ResourceId            INTEGER  NOT NULL,
    AttributesJson        TEXT     NOT NULL,
    Hash                  INTEGER  NOT NULL,
    PRIMARY KEY (Id),
    UNIQUE (TimeUnixNano, Hash)
);
CREATE INDEX IF NOT EXISTS IX_Logs_TraceId ON Logs (TraceId) WHERE TraceId <> '';

PRAGMA user_version = 1;
