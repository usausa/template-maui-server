UPDATE
    Devices
SET
    Name = /*@ name */'',
    GroupName = /*@ groupName */NULL,
    Note = /*@ note */NULL,
    IsEnabled = /*@ isEnabled */1
WHERE
    DeviceId = /*@ deviceId */''
RETURNING
    *
