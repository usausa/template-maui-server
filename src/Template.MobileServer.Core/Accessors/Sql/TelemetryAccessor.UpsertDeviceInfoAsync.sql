INSERT INTO
    DeviceInfo
    (DeviceId, InstallationId, Manufacturer, Model, OsName, OsVersion, ServiceName, ServiceVersion, FirstReceivedAt, LastReceivedAt)
VALUES
    (/*@ entity.DeviceId */'', /*@ entity.InstallationId */'', /*@ entity.Manufacturer */'', /*@ entity.Model */'', /*@ entity.OsName */'', /*@ entity.OsVersion */'', /*@ entity.ServiceName */'', /*@ entity.ServiceVersion */'', /*@ entity.FirstReceivedAt */0, /*@ entity.LastReceivedAt */0)
ON CONFLICT (DeviceId) DO UPDATE SET
    InstallationId = excluded.InstallationId,
    Manufacturer = excluded.Manufacturer,
    Model = excluded.Model,
    OsName = excluded.OsName,
    OsVersion = excluded.OsVersion,
    ServiceName = excluded.ServiceName,
    ServiceVersion = excluded.ServiceVersion,
    LastReceivedAt = excluded.LastReceivedAt
