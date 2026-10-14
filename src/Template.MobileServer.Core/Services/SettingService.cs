namespace Template.MobileServer.Services;

using Template.MobileServer.Accessors;
using Template.MobileServer.Models.Entity;

public sealed class SettingService
{
    private SettingAccessor SettingAccessor { get; }

    private ServiceContextProvider ContextProvider { get; }

    public SettingService(
        SettingAccessor settingAccessor,
        ServiceContextProvider contextProvider)
    {
        SettingAccessor = settingAccessor;
        ContextProvider = contextProvider;
    }

    public ValueTask<List<SettingEntity>> QueryAllAsync(CancellationToken cancellationToken = default) =>
        SettingAccessor.QueryAllAsync(cancellationToken);

    public ValueTask<int> UpdateAsync(string key, string? value)
    {
        var context = ContextProvider.Current;

        var trimmed = value?.Trim();
        return String.IsNullOrEmpty(trimmed)
            ? SettingAccessor.DeleteAsync(key)
            : SettingAccessor.UpsertAsync(key, trimmed, context.Now.UtcDateTime);
    }
}
