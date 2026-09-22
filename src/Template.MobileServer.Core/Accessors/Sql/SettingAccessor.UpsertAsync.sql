INSERT INTO
    Setting (Key, Value, UpdatedAt)
VALUES
    (/*@ key */'', /*@ value */'', /*@ updatedAt */'')
ON CONFLICT (Key) DO UPDATE SET
    Value = excluded.Value,
    UpdatedAt = excluded.UpdatedAt
