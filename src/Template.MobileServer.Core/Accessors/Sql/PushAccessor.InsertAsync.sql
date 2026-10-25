INSERT INTO PushMessage (
    TerminalId,
    Title,
    Body,
    CreatedAt
)
SELECT
    TerminalId, /*@ title */'', /*@ body */'', /*@ createdAt */''
FROM
    Device
WHERE
    IsEnabled = 1
/*% if (terminalId != null) { */
    AND TerminalId = /*@ terminalId */''
/*% } */
ORDER BY
    TerminalId
RETURNING
    *
