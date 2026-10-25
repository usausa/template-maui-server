UPDATE
    Device
SET
    Name = /*@ name */'',
    GroupName = /*@ groupName */NULL,
    Note = /*@ note */NULL,
    IsEnabled = /*@ isEnabled */1
WHERE
    TerminalId = /*@ terminalId */''
RETURNING
    *
