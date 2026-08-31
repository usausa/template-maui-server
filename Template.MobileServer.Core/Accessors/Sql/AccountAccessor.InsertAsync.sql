INSERT INTO Account (Name, Password, Role, CreatedAt) VALUES (/*@ name */'', /*@ password */NULL, /*@ role */'', /*@ createdAt */'');
SELECT last_insert_rowid();
