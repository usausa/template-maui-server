namespace Template.MobileServer.Services;

using Template.MobileServer.Accessors;

// 端末の登録 (data.db)
public sealed class DeviceService
{
    private readonly DeviceAccessor deviceAccessor;

    private readonly PushAccessor pushAccessor;

    private readonly ServiceContextProvider contextProvider;

    public DeviceService(
        DeviceAccessor deviceAccessor,
        PushAccessor pushAccessor,
        ServiceContextProvider contextProvider)
    {
        this.deviceAccessor = deviceAccessor;
        this.pushAccessor = pushAccessor;
        this.contextProvider = contextProvider;
    }

    public ValueTask<List<DeviceEntity>> QueryAllAsync(CancellationToken cancellationToken = default) =>
        deviceAccessor.QueryAllAsync(cancellationToken);

    public ValueTask<DeviceEntity?> QueryAsync(string deviceId, CancellationToken cancellationToken = default) =>
        deviceAccessor.QueryAsync(deviceId, cancellationToken);

    // 登録済みなら Duplicate。登録日時を補う
    public async ValueTask<DataWriteStatus> InsertAsync(DeviceEntity entity, CancellationToken cancellationToken = default)
    {
        var context = contextProvider.Current;

        entity.RegisteredAt = context.Now.UtcDateTime;
        var rows = await deviceAccessor.InsertAsync(entity.DeviceId, entity.Name, entity.GroupName, entity.Note, entity.IsEnabled, entity.RegisteredAt, cancellationToken);
        return rows > 0 ? DataWriteStatus.Success : DataWriteStatus.Duplicate;
    }

    // 名前・グループ・メモ・有効を更新し、更新後の行を返す (無ければ null)
    public ValueTask<DeviceEntity?> UpdateAsync(DeviceEntity entity, CancellationToken cancellationToken = default) =>
        deviceAccessor.UpdateAsync(entity.DeviceId, entity.Name, entity.GroupName, entity.Note, entity.IsEnabled, cancellationToken);

    // 名前だけを更新し、更新後の行を返す (無ければ null)
    public ValueTask<DeviceEntity?> UpdateNameAsync(string deviceId, string name, CancellationToken cancellationToken = default) =>
        deviceAccessor.UpdateNameAsync(deviceId, name, cancellationToken);

    // 登録と、その端末宛ての通知を削除する
    public async ValueTask<DataWriteStatus> DeleteAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        await pushAccessor.DeleteByDeviceAsync(deviceId, cancellationToken);
        return await deviceAccessor.DeleteAsync(deviceId, cancellationToken) > 0 ? DataWriteStatus.Success : DataWriteStatus.NotFound;
    }
}
