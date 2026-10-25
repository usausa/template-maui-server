namespace Template.MobileServer.Services;

using Template.MobileServer.Accessors;

public sealed class DeviceService
{
    private IDbProvider Provider { get; }

    private DeviceAccessor DeviceAccessor { get; }

    private PushAccessor PushAccessor { get; }

    private ServiceContextProvider ContextProvider { get; }

    public DeviceService(
        IDbProvider provider,
        DeviceAccessor deviceAccessor,
        PushAccessor pushAccessor,
        ServiceContextProvider contextProvider)
    {
        Provider = provider;
        DeviceAccessor = deviceAccessor;
        PushAccessor = pushAccessor;
        ContextProvider = contextProvider;
    }

    public ValueTask<List<DeviceEntity>> QueryAllAsync(CancellationToken cancellationToken = default) =>
        DeviceAccessor.QueryAllAsync(cancellationToken);

    public ValueTask<DeviceEntity?> QueryAsync(string terminalId) =>
        DeviceAccessor.QueryAsync(terminalId);

    public async ValueTask<DataWriteStatus> InsertAsync(DeviceEntity entity)
    {
        var context = ContextProvider.Current;

        entity.RegisteredAt = context.Now.UtcDateTime;
        var rows = await DeviceAccessor.InsertAsync(entity.TerminalId, entity.Name, entity.GroupName, entity.Note, entity.IsEnabled, entity.RegisteredAt);
        return rows > 0 ? DataWriteStatus.Success : DataWriteStatus.Duplicate;
    }

    public ValueTask<DeviceEntity?> UpdateAsync(DeviceEntity entity) =>
        DeviceAccessor.UpdateAsync(entity.TerminalId, entity.Name, entity.GroupName, entity.Note, entity.IsEnabled);

    public ValueTask<DeviceEntity?> UpdateNameAsync(string terminalId, string name) =>
        DeviceAccessor.UpdateNameAsync(terminalId, name);

    public ValueTask<DataWriteStatus> DeleteAsync(string terminalId) =>
        Provider.UsingTxAsync(async (_, tx) =>
        {
            await PushAccessor.DeleteByDeviceAsync(tx, terminalId);
            var rows = await DeviceAccessor.DeleteAsync(tx, terminalId);
            await tx.CommitAsync();
            return rows > 0 ? DataWriteStatus.Success : DataWriteStatus.NotFound;
        });
}
