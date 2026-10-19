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

public sealed class TelemetryBus
{
    private ILogger<TelemetryBus> Log { get; }

    public event EventHandler<TelemetryReceivedEventArgs>? Received;

    public event EventHandler<TelemetryDeviceChangedEventArgs>? DeviceChanged;

    public TelemetryBus(ILogger<TelemetryBus> log)
    {
        Log = log;
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
                Log.ErrorTelemetryHandlerFailed(ex);
            }
        }
    }
}
