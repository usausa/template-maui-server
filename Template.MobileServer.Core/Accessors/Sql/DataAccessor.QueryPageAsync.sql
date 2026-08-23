SELECT * FROM Data
WHERE (/*@ name */'' IS NULL) OR (Name LIKE '%' || /*@ name */'' || '%')
ORDER BY Id
LIMIT /*@ size */10 OFFSET /*@ offset */0
