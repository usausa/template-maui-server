namespace Template.MobileServer.Accessors;

[DataAccessor]
public sealed partial class SettingAccessor
{
    [Query]
    public partial ValueTask<List<SettingEntity>> QueryAllAsync(CancellationToken cancellationToken);

    [Execute]
    public partial ValueTask<int> UpsertAsync(string key, string value, DateTime updatedAt);

    [Execute]
    public partial ValueTask<int> DeleteAsync(string key);
}
