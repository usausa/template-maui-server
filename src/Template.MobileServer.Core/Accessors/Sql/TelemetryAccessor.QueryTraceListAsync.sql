SELECT
    *
FROM
    Traces
WHERE
    StartTimeUnixNano >= /*@ since */0
/*% if (errorOnly) { */
    AND ErrorCount > 0
/*% } */
/*% if (name != null) { */
    AND RootName LIKE /*@ name */'' ESCAPE '\'
/*% } */
ORDER BY
    StartTimeUnixNano DESC
LIMIT /*@ limit */100
