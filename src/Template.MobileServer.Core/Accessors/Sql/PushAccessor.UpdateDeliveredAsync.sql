UPDATE
    PushMessage
SET
    DeliveredAt = /*@ deliveredAt */''
WHERE
    Id = /*@ id */0
    AND TerminalId = /*@ terminalId */''
    AND DeliveredAt IS NULL
