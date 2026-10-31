INSERT INTO MetricSeries (
    Name,
    ScopeName,
    Unit,
    Kind,
    Temporality,
    IsMonotonic,
    AttributesJson
) VALUES (
    /*@ entity.Name */'',
    /*@ entity.ScopeName */'',
    /*@ entity.Unit */'',
    /*@ entity.Kind */'Gauge',
    /*@ entity.Temporality */'Unspecified',
    /*@ entity.IsMonotonic */0,
    /*@ entity.AttributesJson */''
)
RETURNING
    Id
