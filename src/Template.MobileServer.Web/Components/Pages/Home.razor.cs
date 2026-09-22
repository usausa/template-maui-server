namespace Template.MobileServer.Web.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.FeatureManagement;

using MudBlazor;

using Template.MobileServer.Infrastructure.Storage;
using Template.MobileServer.Web.Application;
using Template.MobileServer.Web.Application.Circuits;
using Template.MobileServer.Web.Infrastructure.Notifications;
using Template.MobileServer.Web.Services;

public sealed partial class Home
{
    private string serverTime = string.Empty;

    private string storageUsage = string.Empty;

    private int dataCount;

    private int deviceCount;

    private int circuitCount;

    private string? lastNotification;

    private bool HasNotification => !String.IsNullOrEmpty(lastNotification);

    private bool featureEnabled;

    //--------------------------------------------------------------------------------
    // Property
    //--------------------------------------------------------------------------------

    [Inject]
    public required TimeProvider TimeProvider { get; set; }

    [Inject]
    public required DataService DataService { get; set; }

    [Inject]
    public required FileStorageOption StorageOptions { get; set; }

    [Inject]
    public required NotificationBus NotificationBus { get; set; }

    [Inject]
    public required CircuitTracker CircuitTracker { get; set; }

    [Inject]
    public required DeviceRegistry DeviceRegistry { get; set; }

    [Inject]
    public required IFeatureManager FeatureManager { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    protected override async Task OnInitializedAsync()
    {
        NotificationBus.Received += OnNotificationReceived;
        CircuitTracker.Changed += OnCircuitChanged;
        circuitCount = CircuitTracker.Count;

        featureEnabled = await FeatureManager.IsEnabledAsync(FeatureFlags.CustomOption);

        serverTime = ViewHelper.FormatTimestamp(TimeProvider.GetLocalNow().DateTime);
        storageUsage = MakeStorageUsage();
        dataCount = await DataService.CountAsync(null);
        deviceCount = DeviceRegistry.Count;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            NotificationBus.Received -= OnNotificationReceived;
            CircuitTracker.Changed -= OnCircuitChanged;
        }

        base.Dispose(disposing);
    }

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private void OnCircuitChanged(object? sender, EventArgs e)
    {
        _ = InvokeAsync(() =>
        {
            circuitCount = CircuitTracker.Count;
            StateHasChanged();
        });
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

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private string MakeStorageUsage()
    {
        var root = Path.GetFullPath(StorageOptions.Root);
        var drive = new DriveInfo(Path.GetPathRoot(root)!);
        var used = drive.TotalSize - drive.AvailableFreeSpace;
        return $"{ViewHelper.FormatBytes(used)} / {ViewHelper.FormatBytes(drive.TotalSize)}";
    }
}
