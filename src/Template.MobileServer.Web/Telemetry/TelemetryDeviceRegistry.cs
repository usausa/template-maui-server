namespace Template.MobileServer.Web.Telemetry;

using Template.MobileServer.Web.Application.Context;

public sealed class TelemetryDeviceRegistry
{
    public static readonly TimeSpan ReceivingThreshold = TimeSpan.FromMinutes(2);

    public const int HistoryMinutes = 60;

    private const string SystemUserId = "system";

    private const long NanosecondsPerMillisecond = 1_000_000;

    private const long NanosecondsPerHour = 3_600_000_000_000;

    private const int CountHours = 24;

    private const int ErrorLimit = 20;

    private const string BatteryName = "hw.battery.charge";
    private const string WiFiName = "application.wifi.signal_strength";
    private const string CpuName = "process.cpu.utilization";
    private const string MemoryName = "process.memory.usage";
    private const string CustomValue1Name = "application.custom.value1";
    private const string CustomValue2Name = "application.custom.value2";

    private static readonly string[] LatestNames = [BatteryName, WiFiName, CpuName, MemoryName, CustomValue1Name, CustomValue2Name];

    private readonly Lock sync = new();

    private readonly Dictionary<string, Entry> entries = [with(StringComparer.Ordinal)];

    private readonly List<TelemetryErrorEntry> errors = [];

    private readonly int[] history = new int[HistoryMinutes];

    private long historyMinute;

    private ILogger<TelemetryDeviceRegistry> Log { get; }

    private TimeProvider TimeProvider { get; }

    private ApplicationServiceContextProvider ContextProvider { get; }

    private DeviceService DeviceService { get; }

    private TelemetryService TelemetryService { get; }

    private TelemetryStore Store { get; }

    private TelemetryBus Bus { get; }

    public TelemetryDeviceRegistry(
        ILogger<TelemetryDeviceRegistry> log,
        TimeProvider timeProvider,
        ApplicationServiceContextProvider contextProvider,
        DeviceService deviceService,
        TelemetryService telemetryService,
        TelemetryStore store,
        TelemetryBus bus)
    {
        Log = log;
        TimeProvider = timeProvider;
        ContextProvider = contextProvider;
        DeviceService = deviceService;
        TelemetryService = telemetryService;
        Store = store;
        Bus = bus;
    }

    //--------------------------------------------------------------------------------
    // Snapshot
    //--------------------------------------------------------------------------------

    public IReadOnlyList<TelemetryDeviceSummary> Devices
    {
        get
        {
            var hour = CurrentHour();
            lock (sync)
            {
                return entries.Values.OrderBy(static x => x.Device.TerminalId, StringComparer.Ordinal).Select(x => x.ToSummary(hour)).ToArray();
            }
        }
    }

    public IReadOnlyList<TelemetryErrorEntry> RecentErrors
    {
        get
        {
            var since = (CurrentHour() - CountHours + 1) * NanosecondsPerHour;
            lock (sync)
            {
                return errors.Where(x => x.TimeUnixNano >= since).ToArray();
            }
        }
    }

    public IReadOnlyList<int> IngestHistory
    {
        get
        {
            var minute = CurrentMinute();
            lock (sync)
            {
                var values = new int[HistoryMinutes];
                for (var i = 0; i < HistoryMinutes; i++)
                {
                    var target = minute - HistoryMinutes + 1 + i;
                    if ((target <= historyMinute) && (target > historyMinute - HistoryMinutes))
                    {
                        values[i] = history[target % HistoryMinutes];
                    }
                }

                return values;
            }
        }
    }

    public TelemetryDeviceSummary? Find(string terminalId)
    {
        var hour = CurrentHour();
        lock (sync)
        {
            return entries.TryGetValue(terminalId, out var entry) ? entry.ToSummary(hour) : null;
        }
    }

    public bool IsReceiving(TelemetryDeviceSummary device) =>
        (device.Info is not null) &&
        (TimeProvider.GetUtcNow() - DateTimeOffset.FromUnixTimeMilliseconds(device.Info.LastReceivedAt / NanosecondsPerMillisecond) < ReceivingThreshold);

    public TelemetryDeviceState GetState(TelemetryDeviceSummary device)
    {
        if (device.Info is null)
        {
            return TelemetryDeviceState.NoData;
        }

        return IsReceiving(device) ? TelemetryDeviceState.Receiving : TelemetryDeviceState.Stopped;
    }

    //--------------------------------------------------------------------------------
    // Load
    //--------------------------------------------------------------------------------

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        using var scope = ContextProvider.Begin(() => new ServiceContext(TimeProvider.GetLocalNow(), SystemUserId));

        var since = (CurrentHour() - CountHours + 1) * NanosecondsPerHour;
        var registered = (await DeviceService.QueryAllAsync(cancellationToken)).ToDictionary(static x => x.TerminalId, StringComparer.Ordinal);
        var loaded = new List<Entry>();
        var recent = new List<TelemetryErrorEntry>();
        foreach (var terminalId in Store.EnumerateDevices())
        {
            TelemetryDeviceSummaryResult? summary;
            try
            {
                summary = await TelemetryService.QuerySummaryAsync(terminalId, LatestNames, since, ErrorLimit, cancellationToken);
            }
            catch (Exception ex) when (OtlpReceiver.IsStorageError(ex))
            {
                Log.ErrorTelemetryLoadFailed(terminalId, ex);
                continue;
            }

            if (summary is null)
            {
                continue;
            }

            if (!registered.Remove(terminalId, out var device))
            {
                device = CreateDevice(summary.DeviceInfo);
                await DeviceService.InsertAsync(device);
                Log.InfoDeviceRegistered(device.TerminalId, device.Name);
            }

            var entry = new Entry(device) { Info = summary.DeviceInfo };
            foreach (var value in summary.LatestValues)
            {
                if (value.Value is { } number)
                {
                    entry.SetLatest(value.Name, value.TimeUnixNano, number);
                }
            }

            foreach (var count in summary.LogSummary)
            {
                entry.Counts[count.Hour] = (count.ErrorCount, count.CrashCount);
            }

            recent.AddRange(summary.RecentErrors.Select(x => CreateError(terminalId, x)));
            loaded.Add(entry);
        }

        lock (sync)
        {
            entries.Clear();
            foreach (var entry in loaded)
            {
                entries[entry.Device.TerminalId] = entry;
            }

            foreach (var device in registered.Values)
            {
                entries[device.TerminalId] = new Entry(device);
            }

            errors.Clear();
            errors.AddRange(recent.OrderByDescending(static x => x.TimeUnixNano).Take(ErrorLimit));
        }
    }

    //--------------------------------------------------------------------------------
    // Receive
    //--------------------------------------------------------------------------------

    public async ValueTask<bool> EnsureRegisteredAsync(TelemetryDeviceInfoEntity info)
    {
        lock (sync)
        {
            if (entries.TryGetValue(info.TerminalId, out var entry))
            {
                return entry.Device.IsEnabled;
            }
        }

        var device = CreateDevice(info);
        if (await DeviceService.InsertAsync(device) == DataWriteStatus.Success)
        {
            Log.InfoDeviceRegistered(device.TerminalId, device.Name);
        }
        else
        {
            device = await DeviceService.QueryAsync(info.TerminalId) ?? device;
        }

        bool enabled;
        lock (sync)
        {
            if (!entries.TryGetValue(device.TerminalId, out var entry))
            {
                entry = new Entry(device);
                entries[device.TerminalId] = entry;
            }

            enabled = entry.Device.IsEnabled;
        }

        Bus.PublishDeviceChanged(device.TerminalId);
        return enabled;
    }

    public void Apply(TelemetrySaveResult result)
    {
        var hour = CurrentHour();
        var minute = CurrentMinute();
        lock (sync)
        {
            if (!entries.TryGetValue(result.TerminalId, out var entry))
            {
                return;
            }

            entry.Info = result.DeviceInfo;

            var count = result.Spans.Count + result.Logs.Count;
            foreach (var metric in result.Metrics)
            {
                count += metric.Points.Count;
                if (!LatestNames.Contains(metric.Series.Name))
                {
                    continue;
                }

                foreach (var point in metric.Points)
                {
                    if (point.Value is { } value)
                    {
                        entry.SetLatest(metric.Series.Name, point.TimeUnixNano, value);
                    }
                }
            }

            foreach (var logEntity in result.Logs.Where(static x => x.SeverityNumber >= TelemetrySeverity.Error))
            {
                entry.AddError(logEntity.TimeUnixNano / NanosecondsPerHour, logEntity.SeverityNumber >= TelemetrySeverity.Fatal);
                AddRecentError(CreateError(result.TerminalId, logEntity));
            }

            entry.RemoveCounts(hour - CountHours);

            AddHistory(minute, count);
        }
    }

    //--------------------------------------------------------------------------------
    // Management
    //--------------------------------------------------------------------------------

    public async ValueTask<DataWriteStatus> AddAsync(DeviceEntity device)
    {
        var status = await DeviceService.InsertAsync(device);
        if (status == DataWriteStatus.Success)
        {
            lock (sync)
            {
                entries[device.TerminalId] = new Entry(device);
            }

            Bus.PublishDeviceChanged(device.TerminalId);
        }

        return status;
    }

    public async ValueTask<(DeviceEntity Device, bool Created)> RegisterAsync(string terminalId, string name)
    {
        var device = new DeviceEntity { TerminalId = terminalId, Name = name, IsEnabled = true };
        var created = await DeviceService.InsertAsync(device) == DataWriteStatus.Success;
        if (created)
        {
            Log.InfoDeviceRegistered(device.TerminalId, device.Name);
        }
        else
        {
            device = await DeviceService.UpdateNameAsync(terminalId, name) ?? device;
        }

        lock (sync)
        {
            if (entries.TryGetValue(device.TerminalId, out var entry))
            {
                entry.Device = device;
            }
            else
            {
                entries[device.TerminalId] = new Entry(device);
            }
        }

        Bus.PublishDeviceChanged(device.TerminalId);
        return (device, created);
    }

    public async ValueTask<DataWriteStatus> UpdateAsync(DeviceEntity device)
    {
        var updated = await DeviceService.UpdateAsync(device);
        if (updated is null)
        {
            return DataWriteStatus.NotFound;
        }

        lock (sync)
        {
            if (entries.TryGetValue(updated.TerminalId, out var entry))
            {
                entry.Device = updated;
            }
            else
            {
                entries[updated.TerminalId] = new Entry(updated);
            }
        }

        Bus.PublishDeviceChanged(updated.TerminalId);
        return DataWriteStatus.Success;
    }

    public async ValueTask<DataWriteStatus> DeleteAsync(string terminalId)
    {
        await Store.DeleteDeviceAsync(terminalId);
        var status = await DeviceService.DeleteAsync(terminalId);

        lock (sync)
        {
            entries.Remove(terminalId);
            errors.RemoveAll(x => x.TerminalId == terminalId);
        }

        Bus.PublishDeviceChanged(terminalId);
        return status;
    }

    public void ClearTelemetry(string terminalId)
    {
        lock (sync)
        {
            if (!entries.TryGetValue(terminalId, out var entry))
            {
                return;
            }

            entries[terminalId] = new Entry(entry.Device);
            errors.RemoveAll(x => x.TerminalId == terminalId);
        }

        Bus.PublishDeviceChanged(terminalId);
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private long CurrentHour() => TimeProvider.GetUtcNow().ToUnixTimeSeconds() / 3600;

    private long CurrentMinute() => TimeProvider.GetUtcNow().ToUnixTimeSeconds() / 60;

    private static DeviceEntity CreateDevice(TelemetryDeviceInfoEntity info) =>
        new()
        {
            TerminalId = info.TerminalId,
            Name = info.Model.Length > 0 ? info.Model : info.TerminalId,
            IsEnabled = true
        };

    private static TelemetryErrorEntry CreateError(string terminalId, TelemetryLogEntity entity)
    {
        var body = entity.Body;
        var index = body.IndexOfAny(['\r', '\n']);
        return new TelemetryErrorEntry(terminalId, entity.TimeUnixNano, entity.SeverityNumber, index >= 0 ? body[..index] : body);
    }

    private void AddRecentError(TelemetryErrorEntry error)
    {
        var index = errors.FindIndex(x => x.TimeUnixNano < error.TimeUnixNano);
        errors.Insert(index >= 0 ? index : errors.Count, error);
        if (errors.Count > ErrorLimit)
        {
            errors.RemoveAt(errors.Count - 1);
        }
    }

    private void AddHistory(long minute, int count)
    {
        if (minute > historyMinute)
        {
            var clear = Math.Min(minute - historyMinute, HistoryMinutes);
            for (var i = 1; i <= clear; i++)
            {
                history[(historyMinute + i) % HistoryMinutes] = 0;
            }

            historyMinute = minute;
        }

        if (minute > historyMinute - HistoryMinutes)
        {
            history[minute % HistoryMinutes] += count;
        }
    }

    private sealed class Entry
    {
        public DeviceEntity Device { get; set; }

        public TelemetryDeviceInfoEntity? Info { get; set; }

        public Dictionary<string, TelemetryValue> Latest { get; } = [with(StringComparer.Ordinal)];

        public Dictionary<long, (int Errors, int Crashes)> Counts { get; } = [];

        public Entry(DeviceEntity device)
        {
            Device = device;
        }

        public void SetLatest(string name, long time, double value)
        {
            if (!Latest.TryGetValue(name, out var current) || (current.TimeUnixNano <= time))
            {
                Latest[name] = new TelemetryValue(time, value);
            }
        }

        public void AddError(long hour, bool crash)
        {
            var (errors, crashes) = Counts.GetValueOrDefault(hour);
            Counts[hour] = (errors + 1, crash ? crashes + 1 : crashes);
        }

        public void RemoveCounts(long hour)
        {
            foreach (var key in Counts.Keys.Where(x => x <= hour).ToList())
            {
                Counts.Remove(key);
            }
        }

        public TelemetryDeviceSummary ToSummary(long hour)
        {
            var errors = 0;
            var crashes = 0;
            foreach (var (key, value) in Counts)
            {
                if (key > hour - CountHours)
                {
                    errors += value.Errors;
                    crashes += value.Crashes;
                }
            }

            return new TelemetryDeviceSummary(
                Device,
                Info,
                FindLatest(BatteryName),
                FindLatest(WiFiName),
                FindLatest(CpuName),
                FindLatest(MemoryName),
                FindLatest(CustomValue1Name),
                FindLatest(CustomValue2Name),
                errors,
                crashes);
        }

        private TelemetryValue? FindLatest(string name) =>
            Latest.TryGetValue(name, out var value) ? value : null;
    }
}
