namespace Template.MobileServer.Web.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.FeatureManagement;

using MudBlazor;

using Template.MobileServer.Infrastructure.Storage;
using Template.MobileServer.Web.Application;
using Template.MobileServer.Web.Infrastructure.Components;
using Template.MobileServer.Web.Infrastructure.Notifications;

public sealed partial class Home
{
    private string serverTime = string.Empty;

    private string storageUsage = string.Empty;

    private int dataCount;

    private string? lastNotification;

    private bool featureEnabled;

    [Inject]
    public required TimeProvider TimeProvider { get; set; }

    [Inject]
    public required DataService DataService { get; set; }

    [Inject]
    public required FileStorageOptions StorageOptions { get; set; }

    [Inject]
    public required NotificationBus NotificationBus { get; set; }

    [Inject]
    public required IFeatureManager FeatureManager { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // Subscribe server notification (unsubscribed on dispose)
        NotificationBus.Received += OnNotificationReceived;

        // Feature flag example
        featureEnabled = await FeatureManager.IsEnabledAsync(FeatureFlags.CustomOption);

        // 簡易ステータス表示
        serverTime = ViewHelper.FormatTimestamp(TimeProvider.GetLocalNow().DateTime);
        storageUsage = MakeStorageUsage();
        dataCount = await DataService.CountAsync(null);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            NotificationBus.Received -= OnNotificationReceived;
        }

        base.Dispose(disposing);
    }

    // ストレージルートのドライブ使用量を取得する
    private string MakeStorageUsage()
    {
        var root = Path.GetFullPath(StorageOptions.Root);
        var drive = new DriveInfo(Path.GetPathRoot(root)!);
        var used = drive.TotalSize - drive.AvailableFreeSpace;
        return $"{ViewHelper.FormatBytes(used)} / {ViewHelper.FormatBytes(drive.TotalSize)}";
    }

    private void OnNotificationReceived(object? sender, NotificationEventArgs e)
    {
        _ = InvokeAsync(() =>
        {
            lastNotification = e.Message;
            Snackbar.AddInfo(e.Message);
            StateHasChanged();
        });
    }
}
