namespace Template.MobileServer.Web.Telemetry;

public sealed class TelemetryReceivedEventArgs : EventArgs
{
    public TelemetrySaveResult Result { get; }

    public TelemetryReceivedEventArgs(TelemetrySaveResult result)
    {
        Result = result;
    }
}

public sealed class TelemetryDeviceChangedEventArgs : EventArgs
{
    public string DeviceId { get; }

    public TelemetryDeviceChangedEventArgs(string deviceId)
    {
        DeviceId = deviceId;
    }
}

// 受信と端末の登録の変更の通知 (プロセス内)。受け手は発行した側のスレッドで呼ばれるので、描画せずに更新を予約してすぐ戻る。受け手の例外はログに出して次の受け手へ進む
public sealed class TelemetryBus
{
    private readonly ILogger<TelemetryBus> log;

    public event EventHandler<TelemetryReceivedEventArgs>? Received;

    public event EventHandler<TelemetryDeviceChangedEventArgs>? DeviceChanged;

    public TelemetryBus(ILogger<TelemetryBus> log)
    {
        this.log = log;
    }

    public void PublishReceived(TelemetrySaveResult result) =>
        Invoke(Received, new TelemetryReceivedEventArgs(result));

    public void PublishDeviceChanged(string deviceId) =>
        Invoke(DeviceChanged, new TelemetryDeviceChangedEventArgs(deviceId));

    private void Invoke<T>(EventHandler<T>? handlers, T args)
        where T : EventArgs
    {
        if (handlers is null)
        {
            return;
        }

        foreach (var handler in handlers.GetInvocationList().Cast<EventHandler<T>>())
        {
            try
            {
                handler(this, args);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.ErrorTelemetryHandlerFailed(ex);
            }
        }
    }
}
