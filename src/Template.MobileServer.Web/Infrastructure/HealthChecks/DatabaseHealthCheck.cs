namespace Template.MobileServer.Web.Infrastructure.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using Smart.Data;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDbProvider dbProvider;

    public DatabaseHealthCheck(IDbProvider dbProvider)
    {
        this.dbProvider = dbProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var con = dbProvider.CreateConnection();
            await con.OpenAsync(cancellationToken);

            await using var command = con.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (DbException e)
        {
            return HealthCheckResult.Unhealthy("Database connection failed.", e);
        }
    }
}
