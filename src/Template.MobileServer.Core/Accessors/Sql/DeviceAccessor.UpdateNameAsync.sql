UPDATE
    Device
SET
    Name = /*@ name */''
WHERE
    DeviceId = /*@ deviceId */''
RETURNING
    *
