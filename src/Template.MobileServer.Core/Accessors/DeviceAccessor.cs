namespace Template.MobileServer.Accessors;

[DataAccessor]
[ExecuteConfig(typeof(DataProfile))]
public sealed partial class DeviceAccessor
{
    //--------------------------------------------------------------------------------
    // Query
    //--------------------------------------------------------------------------------

    [Query]
    public partial ValueTask<List<DeviceEntity>> QueryAllAsync(CancellationToken cancellationToken);

    [QueryFirst]
    [SelectSingle(typeof(DeviceEntity))]
    public partial ValueTask<DeviceEntity?> QueryAsync(string terminalId);

    //--------------------------------------------------------------------------------
    // Write
    //--------------------------------------------------------------------------------

    [Execute]
    public partial ValueTask<int> InsertAsync(string terminalId, string name, string? groupName, string? note, bool isEnabled, DateTime registeredAt);

    [QueryFirst]
    public partial ValueTask<DeviceEntity?> UpdateAsync(DeviceEntity entity);

    [QueryFirst]
    public partial ValueTask<DeviceEntity?> UpdateNameAsync(string terminalId, string name);

    [Execute]
    [Delete(typeof(DeviceEntity))]
    public partial ValueTask<int> DeleteAsync(DbTransaction tx, string terminalId);
}
