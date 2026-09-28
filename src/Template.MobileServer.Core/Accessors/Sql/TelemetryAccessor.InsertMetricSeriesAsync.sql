INSERT INTO
    MetricSeries (Name, ScopeName, Unit, Kind, Temporality, IsMonotonic, AttributesJson)
VALUES
    (/*@ name */'', /*@ scopeName */'', /*@ unit */'', /*@ kind */'Gauge', /*@ temporality */'Unspecified', /*@ isMonotonic */0, /*@ attributesJson */'')
RETURNING
    Id
