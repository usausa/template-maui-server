SELECT
    *
FROM
    PushMessage
WHERE
    TerminalId = /*@ terminalId */''
    AND DeliveredAt IS NULL
ORDER BY
    Id
