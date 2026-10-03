UPDATE
    PushMessage
SET
    DeliveredAt = /*@ deliveredAt */''
WHERE
    Id = /*@ id */0
    AND DeviceId = /*@ deviceId */''
    AND DeliveredAt IS NULL
