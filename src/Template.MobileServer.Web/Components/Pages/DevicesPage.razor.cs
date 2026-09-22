namespace Template.MobileServer.Web.Components.Pages;

using Microsoft.AspNetCore.Components;

using MudBlazor;

using Template.MobileServer.Web.Services;

public sealed partial class DevicesPage
{
    private const string AllTargets = "*";

    private IReadOnlyList<DeviceEntry> devices = [];

    private bool NoDevices => devices.Count == 0;

    private string target = AllTargets;

    private string title = "お知らせ";

    private string body = string.Empty;

    //--------------------------------------------------------------------------------
    // Property
    //--------------------------------------------------------------------------------

    [Inject]
    public required DeviceRegistry Registry { get; set; }

    [Inject]
    public required MonitorNotifier Notifier { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    protected override void OnInitialized()
    {
        devices = Registry.Entries;
        Registry.Changed += OnChanged;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Registry.Changed -= OnChanged;
        }

        base.Dispose(disposing);
    }

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private void OnChanged(object? sender, EventArgs e)
    {
        _ = InvokeAsync(() =>
        {
            devices = Registry.Entries;

            if ((target != AllTargets) && devices.All(x => x.ConnectionId != target))
            {
                target = AllTargets;
            }

            StateHasChanged();
        });
    }

    //--------------------------------------------------------------------------------
    // Action
    //--------------------------------------------------------------------------------

    private async Task SendAsync()
    {
        var notifyTitle = title.Trim();
        var notifyBody = body.Trim();
        if (notifyTitle.Length == 0)
        {
            Snackbar.AddWarning("タイトルを入力してください");
            return;
        }

        if (target == AllTargets)
        {
            await Notifier.NotifyAllAsync(notifyTitle, notifyBody);
        }
        else
        {
            await Notifier.NotifyAsync(target, notifyTitle, notifyBody);
        }

        Snackbar.AddInfo("送信しました");
        body = string.Empty;
    }

    private void Disconnect(DeviceEntry device)
    {
        if (Registry.Disconnect(device.ConnectionId))
        {
            Snackbar.AddInfo("切断しました");
        }
    }

    //--------------------------------------------------------------------------------
    // Format
    //--------------------------------------------------------------------------------

    private static string FormatBattery(DeviceEntry device) =>
        device.Battery is { } battery ? $"{battery:P0} {device.BatteryState}" : string.Empty;

    private static string FormatTarget(DeviceEntry device) =>
        device.Model is { Length: > 0 } model ? $"{device.DeviceId ?? device.ConnectionId} ({model})" : device.DeviceId ?? device.ConnectionId;
}
