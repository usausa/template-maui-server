namespace Template.MobileServer.Accessors;

[DataAccessor]
[ExecuteConfig(typeof(DataProfile))]
public sealed partial class DeviceAccessor
{
    [Query]
    public partial ValueTask<List<DeviceEntity>> QueryAllAsync(CancellationToken cancellationToken);

    [QueryFirst]
    [SelectSingle(typeof(DeviceEntity))]
    public partial ValueTask<DeviceEntity?> QueryAsync(string deviceId);

    // 登録済みなら入れない (0 件)
    [Execute]
    public partial ValueTask<int> InsertAsync(string deviceId, string name, string? groupName, string? note, bool isEnabled, DateTime registeredAt);

    [QueryFirst]
    public partial ValueTask<DeviceEntity?> UpdateAsync(string deviceId, string name, string? groupName, string? note, bool isEnabled);

    [QueryFirst]
    public partial ValueTask<DeviceEntity?> UpdateNameAsync(string deviceId, string name);

    [Execute]
    [Delete(typeof(DeviceEntity))]
    public partial ValueTask<int> DeleteAsync(DbTransaction tx, string deviceId);
}
