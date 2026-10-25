UPDATE
    Device
SET
    Name = /*@ name */''
WHERE
    TerminalId = /*@ terminalId */''
RETURNING
    *
