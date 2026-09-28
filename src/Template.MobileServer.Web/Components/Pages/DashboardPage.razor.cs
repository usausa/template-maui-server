namespace Template.MobileServer.Web.Components.Pages;

using Microsoft.AspNetCore.Components;

using MudBlazor;

using Template.MobileServer.Web.Components.Dialogs;
using Template.MobileServer.Web.Components.Shared;
using Template.MobileServer.Web.Components.Telemetry;
using Template.MobileServer.Web.Telemetry;

// テレメトリのサマリと端末の一覧・管理。キャッシュ (TelemetryDeviceRegistry) だけを読み、バスの通知と一定の間隔で読み直す
public sealed partial class DashboardPage
{
    // 通知が無くても読み直す間隔 (途絶への変化と経過時間を進める)
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(10);

    private const int ErrorLimit = 10;

    private RefreshTimer refreshTimer = default!;

    private TelemetryDeviceSummary[] devices = [];

    private Dictionary<string, string> names = [];

    private TelemetryErrorEntry[] errors = [];

    private double[] ingestHistory = [];

    private string? search;

    private int enabledCount;

    private int receivingCount;

    private int stoppedCount;

    private int noDataCount;

    private int errorCount;

    private int crashCount;

    private int lowBatteryCount;

    private int ingestRate;

    //--------------------------------------------------------------------------------
    // Property
    //--------------------------------------------------------------------------------

    [Inject]
    public required TimeProvider TimeProvider { get; set; }

    [Inject]
    public required TelemetryDeviceRegistry Registry { get; set; }

    [Inject]
    public required TelemetryBus Bus { get; set; }

    [Inject]
    public required IDialogService DialogService { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    [Inject]
    public required NavigationManager Navigation { get; set; }

    private TelemetryDeviceSummary[] VisibleDevices
    {
        get
        {
            var text = search?.Trim();
            return String.IsNullOrEmpty(text) ? devices : devices.Where(x => IsMatch(x, text)).ToArray();
        }
    }

    private bool HasErrors => errors.Length > 0;

    private string StateSummary => $"受信中 {receivingCount}・途絶 {stoppedCount}・受信なし {noDataCount}";

    // 状態の帯 (有効な端末の内訳)
    private string ReceivingStyle => $"flex-grow: {receivingCount}";

    private string StoppedStyle => $"flex-grow: {stoppedCount}";

    private string NoDataStyle => $"flex-grow: {noDataCount}";

    private Color ErrorColor => errorCount > 0 ? Color.Warning : Color.Default;

    private Color CrashColor => crashCount > 0 ? Color.Error : Color.Default;

    private Color BatteryColor => lowBatteryCount > 0 ? Color.Error : Color.Default;

    private static string LowBatteryText => $"残量 {TelemetryFormat.FormatPercent(TelemetryFormat.BatteryCriticalRatio)} 未満";

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    protected override void OnInitialized()
    {
        Load();
        Bus.Received += OnReceived;
        Bus.DeviceChanged += OnDeviceChanged;
        refreshTimer = new RefreshTimer(TimeProvider, RefreshInterval, RefreshAsync);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Bus.Received -= OnReceived;
            Bus.DeviceChanged -= OnDeviceChanged;
            refreshTimer.Dispose();
        }

        base.Dispose(disposing);
    }

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private void OnReceived(object? sender, TelemetryReceivedEventArgs e) => refreshTimer.Request();

    private void OnDeviceChanged(object? sender, TelemetryDeviceChangedEventArgs e) => refreshTimer.Request();

    private Task RefreshAsync() =>
        InvokeAsync(() =>
        {
            Load();
            StateHasChanged();
        });

    //--------------------------------------------------------------------------------
    // Action
    //--------------------------------------------------------------------------------

    private async Task AddAsync()
    {
        var device = await DialogService.ShowDeviceEditDialog("端末の追加", null);
        if (device is null)
        {
            return;
        }

        if (await Registry.AddAsync(device) == DataWriteStatus.Success)
        {
            Snackbar.AddSuccess("追加しました。");
            Load();
        }
        else
        {
            Snackbar.AddError("端末 ID が重複しています。");
        }
    }

    private async Task EditAsync(TelemetryDeviceSummary summary)
    {
        var device = await DialogService.ShowDeviceEditDialog("端末の編集", summary.Device);
        if (device is null)
        {
            return;
        }

        if (await Registry.UpdateAsync(device) == DataWriteStatus.Success)
        {
            Snackbar.AddSuccess("更新しました。");
        }
        else
        {
            Snackbar.AddError("対象が存在しません。");
        }

        Load();
    }

    // 直近のエラーの端末のログ (ERROR 以上、エラーの時刻を含む範囲)
    private void OpenLogs(TelemetryErrorEntry error)
    {
        var elapsed = TimeProvider.GetUtcNow() - TelemetryFormat.ToDateTimeOffset(error.TimeUnixNano);
        Navigation.NavigateTo(TelemetryLinks.Logs(error.DeviceId, TelemetryRange.Covering(elapsed), TelemetryLogLevel.Error));
    }

    // 送信が続く端末は受信で登録し直される (止めるときは無効にする)
    private async Task DeleteAsync(TelemetryDeviceSummary summary)
    {
        var device = summary.Device;
        if (!await DialogService.ShowConfirm("端末の削除", $"{device.Name} ({device.DeviceId}) の登録とテレメトリを削除してよろしいですか？送信を続けている端末は、次の受信で登録し直されます。"))
        {
            return;
        }

        if (await Registry.DeleteAsync(device.DeviceId) == DataWriteStatus.Success)
        {
            Snackbar.AddSuccess("削除しました。");
        }
        else
        {
            Snackbar.AddError("対象が存在しません。");
        }

        Load();
    }

    //--------------------------------------------------------------------------------
    // Load
    //--------------------------------------------------------------------------------

    // 無効の端末はサマリに数えない
    private void Load()
    {
        devices = Registry.Devices
            .OrderBy(static x => x.Device.Name, StringComparer.CurrentCulture)
            .ThenBy(static x => x.Device.DeviceId, StringComparer.Ordinal)
            .ToArray();
        names = devices.ToDictionary(static x => x.Device.DeviceId, static x => x.Device.Name, StringComparer.Ordinal);
        errors = Registry.RecentErrors.Take(ErrorLimit).ToArray();

        // The last minute is in progress
        var history = Registry.IngestHistory;
        ingestHistory = history.Select(static x => (double)x).ToArray();
        ingestRate = history.Count > 1 ? history[^2] : 0;

        var enabled = devices.Where(static x => x.Device.IsEnabled).ToArray();
        var states = enabled.Select(Registry.GetState).ToArray();
        enabledCount = enabled.Length;
        receivingCount = states.Count(static x => x == TelemetryDeviceState.Receiving);
        stoppedCount = states.Count(static x => x == TelemetryDeviceState.Stopped);
        noDataCount = states.Count(static x => x == TelemetryDeviceState.NoData);
        errorCount = enabled.Sum(static x => x.ErrorCount);
        crashCount = enabled.Sum(static x => x.CrashCount);
        lowBatteryCount = enabled.Count(static x => x.Battery is { } battery && (TelemetryFormat.BatteryLevel(battery.Value) == TelemetryLevel.Critical));
    }

    private static bool IsMatch(TelemetryDeviceSummary device, string text) =>
        device.Device.DeviceId.Contains(text, StringComparison.OrdinalIgnoreCase) ||
        device.Device.Name.Contains(text, StringComparison.OrdinalIgnoreCase) ||
        (device.Device.GroupName?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false);

    //--------------------------------------------------------------------------------
    // Format
    //--------------------------------------------------------------------------------

    private string StateText(TelemetryDeviceSummary device) => TelemetryFormat.FormatState(Registry.GetState(device));

    private Color StateColor(TelemetryDeviceSummary device) => TelemetryFormat.StateColor(Registry.GetState(device));

    private Variant StateVariant(TelemetryDeviceSummary device) => TelemetryFormat.StateVariant(Registry.GetState(device));

    private static string TelemetryLink(TelemetryDeviceSummary device) => TelemetryLinks.Device(device.Device.DeviceId);

    private static string RowClass(TelemetryDeviceSummary device) =>
        device.Device.IsEnabled ? "device-row" : "device-row device-disabled";

    private static string FormatCaption(TelemetryDeviceSummary device) =>
        device.Device.GroupName is { Length: > 0 } group ? $"{device.Device.DeviceId} / {group}" : device.Device.DeviceId;

    private static string FormatModel(TelemetryDeviceSummary device) => device.Info?.Model ?? string.Empty;

    // OS とアプリの版
    private static string FormatPlatform(TelemetryDeviceSummary device) =>
        device.Info is { } info ? $"{info.OsName} {info.OsVersion}・アプリ {info.ServiceVersion}" : string.Empty;

    private string FormatLastReceived(TelemetryDeviceSummary device) =>
        device.Info is { } info ? TelemetryFormat.FormatElapsed(TimeProvider.GetUtcNow() - TelemetryFormat.ToDateTimeOffset(info.LastReceivedAt)) : string.Empty;

    private static string FormatLastReceivedTime(TelemetryDeviceSummary device) =>
        device.Info is { } info ? TelemetryFormat.FormatTime(info.LastReceivedAt) : string.Empty;

    private string FindName(string deviceId) => names.GetValueOrDefault(deviceId, deviceId);
}
