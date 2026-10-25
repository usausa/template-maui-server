INSERT INTO
    DeviceInfo
    (TerminalId, Manufacturer, Model, OsName, OsVersion, ServiceName, ServiceVersion, FirstReceivedAt, LastReceivedAt)
VALUES
    (/*@ entity.TerminalId */'', /*@ entity.Manufacturer */'', /*@ entity.Model */'', /*@ entity.OsName */'', /*@ entity.OsVersion */'', /*@ entity.ServiceName */'', /*@ entity.ServiceVersion */'', /*@ entity.FirstReceivedAt */0, /*@ entity.LastReceivedAt */0)
ON CONFLICT (TerminalId) DO UPDATE SET
    Manufacturer = excluded.Manufacturer,
    Model = excluded.Model,
    OsName = excluded.OsName,
    OsVersion = excluded.OsVersion,
    ServiceName = excluded.ServiceName,
    ServiceVersion = excluded.ServiceVersion,
    LastReceivedAt = excluded.LastReceivedAt
