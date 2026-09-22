namespace Template.MobileServer.Services;

using Template.MobileServer.Accessors;
using Template.MobileServer.Models.Entity;

public sealed class SettingService
{
    private readonly SettingAccessor settingAccessor;

    private readonly TimeProvider timeProvider;

    public SettingService(
        SettingAccessor settingAccessor,
        TimeProvider timeProvider)
    {
        this.settingAccessor = settingAccessor;
        this.timeProvider = timeProvider;
    }

    public ValueTask<List<SettingEntity>> QueryAllAsync(CancellationToken cancellationToken = default) =>
        settingAccessor.QueryAllAsync(cancellationToken);

    public ValueTask<int> UpdateAsync(string key, string? value)
    {
        var trimmed = value?.Trim();
        return String.IsNullOrEmpty(trimmed)
            ? settingAccessor.DeleteAsync(key)
            : settingAccessor.UpsertAsync(key, trimmed, timeProvider.GetLocalNow().DateTime);
    }
}
