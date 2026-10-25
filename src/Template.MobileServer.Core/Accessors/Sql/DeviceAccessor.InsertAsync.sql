INSERT INTO Device (
    TerminalId,
    Name,
    GroupName,
    Note,
    IsEnabled,
    RegisteredAt
) VALUES (
    /*@ terminalId */'',
    /*@ name */'',
    /*@ groupName */NULL,
    /*@ note */NULL,
    /*@ isEnabled */1,
    /*@ registeredAt */''
)
ON CONFLICT (TerminalId) DO NOTHING
