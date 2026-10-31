UPDATE
    Device
SET
    Name = /*@ entity.Name */'',
    GroupName = /*@ entity.GroupName */NULL,
    Note = /*@ entity.Note */NULL,
    IsEnabled = /*@ entity.IsEnabled */1
WHERE
    TerminalId = /*@ entity.TerminalId */''
RETURNING
    *
