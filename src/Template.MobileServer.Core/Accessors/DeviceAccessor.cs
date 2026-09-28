namespace Template.MobileServer.Accessors;

[DataAccessor]
[ExecuteConfig(typeof(DataProfile))]
public sealed partial class DeviceAccessor
{
    [Query]
    public partial ValueTask<List<DeviceEntity>> QueryAllAsync(CancellationToken cancellationToken);

    [QueryFirst]
    [SelectSingle(typeof(DeviceEntity))]
    public partial ValueTask<DeviceEntity?> QueryAsync(string deviceId, CancellationToken cancellationToken);

    // 登録済みなら入れない (0 件)
    [Execute]
    public partial ValueTask<int> InsertAsync(string deviceId, string name, string? groupName, string? note, bool isEnabled, DateTime registeredAt, CancellationToken cancellationToken);

    [QueryFirst]
    public partial ValueTask<DeviceEntity?> UpdateAsync(string deviceId, string name, string? groupName, string? note, bool isEnabled, CancellationToken cancellationToken);

    [QueryFirst]
    public partial ValueTask<DeviceEntity?> UpdateNameAsync(string deviceId, string name, CancellationToken cancellationToken);

    [Execute]
    [Delete(typeof(DeviceEntity))]
    public partial ValueTask<int> DeleteAsync(string deviceId, CancellationToken cancellationToken);
}
