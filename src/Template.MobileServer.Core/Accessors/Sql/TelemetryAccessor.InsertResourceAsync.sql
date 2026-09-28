INSERT INTO
    Resource (Hash, ServiceInstanceId, ServiceVersion, AttributesJson, FirstSeenAt)
VALUES
    (/*@ entity.Hash */'', /*@ entity.ServiceInstanceId */'', /*@ entity.ServiceVersion */'', /*@ entity.AttributesJson */'', /*@ entity.FirstSeenAt */0)
RETURNING
    Id
