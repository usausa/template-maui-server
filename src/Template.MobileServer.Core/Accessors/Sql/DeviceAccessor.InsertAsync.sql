INSERT INTO
    Device (DeviceId, Name, GroupName, Note, IsEnabled, RegisteredAt)
VALUES
    (/*@ deviceId */'', /*@ name */'', /*@ groupName */NULL, /*@ note */NULL, /*@ isEnabled */1, /*@ registeredAt */'')
ON CONFLICT (DeviceId) DO NOTHING
