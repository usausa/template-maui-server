namespace Template.MobileServer.Services;

using Template.MobileServer.Accessors;
using Template.MobileServer.Models.Entity;

public sealed class SettingService
{
    private readonly SettingAccessor settingAccessor;

    private readonly ServiceContextProvider contextProvider;

    public SettingService(
        SettingAccessor settingAccessor,
        ServiceContextProvider contextProvider)
    {
        this.settingAccessor = settingAccessor;
        this.contextProvider = contextProvider;
    }

    public ValueTask<List<SettingEntity>> QueryAllAsync(CancellationToken cancellationToken = default) =>
        settingAccessor.QueryAllAsync(cancellationToken);

    public ValueTask<int> UpdateAsync(string key, string? value)
    {
        var context = contextProvider.Current;

        var trimmed = value?.Trim();
        return String.IsNullOrEmpty(trimmed)
            ? settingAccessor.DeleteAsync(key)
            : settingAccessor.UpsertAsync(key, trimmed, context.Now.DateTime);
    }
}
