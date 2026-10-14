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

    public ValueTask<DeviceEntity?> QueryAsync(string deviceId) =>
        DeviceAccessor.QueryAsync(deviceId);

    // 登録済みなら Duplicate。登録日時を補う
    public async ValueTask<DataWriteStatus> InsertAsync(DeviceEntity entity)
    {
        var context = ContextProvider.Current;

        entity.RegisteredAt = context.Now.UtcDateTime;
        var rows = await DeviceAccessor.InsertAsync(entity.DeviceId, entity.Name, entity.GroupName, entity.Note, entity.IsEnabled, entity.RegisteredAt);
        return rows > 0 ? DataWriteStatus.Success : DataWriteStatus.Duplicate;
    }

    // 名前・グループ・メモ・有効を更新し、更新後の行を返す (無ければ null)
    public ValueTask<DeviceEntity?> UpdateAsync(DeviceEntity entity) =>
        DeviceAccessor.UpdateAsync(entity.DeviceId, entity.Name, entity.GroupName, entity.Note, entity.IsEnabled);

    // 名前だけを更新し、更新後の行を返す (無ければ null)
    public ValueTask<DeviceEntity?> UpdateNameAsync(string deviceId, string name) =>
        DeviceAccessor.UpdateNameAsync(deviceId, name);

    // 登録と、その端末宛ての通知を削除する
    public ValueTask<DataWriteStatus> DeleteAsync(string deviceId) =>
        Provider.UsingTxAsync(async (_, tx) =>
        {
            await PushAccessor.DeleteByDeviceAsync(tx, deviceId);
            var rows = await DeviceAccessor.DeleteAsync(tx, deviceId);
            await tx.CommitAsync();
            return rows > 0 ? DataWriteStatus.Success : DataWriteStatus.NotFound;
        });
}
