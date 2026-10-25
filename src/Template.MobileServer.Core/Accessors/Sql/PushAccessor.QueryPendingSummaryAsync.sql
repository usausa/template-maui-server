SELECT
    TerminalId,
    COUNT(*) AS Count
FROM
    PushMessage
WHERE
    DeliveredAt IS NULL
GROUP BY
    TerminalId
