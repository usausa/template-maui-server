namespace Template.MobileServer.Web.Telemetry;

using Template.MobileServer.Web.Application.Context;

// 端末の登録とテレメトリの要約をメモリに持つ (ダッシュボード用のキャッシュ)。登録の変更は data.db に書いてから反映し、バスで知らせる
public sealed class TelemetryDeviceRegistry
{
    // 受信中とみなす最後の受信からの時間 (メトリクスは 30 秒ごとに届く)
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

    private readonly ILogger<TelemetryDeviceRegistry> log;

    private readonly TimeProvider timeProvider;

    private readonly ApplicationServiceContextProvider contextProvider;

    private readonly DeviceService deviceService;

    private readonly TelemetryService telemetryService;

    private readonly TelemetryBus bus;

    public TelemetryDeviceRegistry(
        ILogger<TelemetryDeviceRegistry> log,
        TimeProvider timeProvider,
        ApplicationServiceContextProvider contextProvider,
        DeviceService deviceService,
        TelemetryService telemetryService,
        TelemetryBus bus)
    {
        this.log = log;
        this.timeProvider = timeProvider;
        this.contextProvider = contextProvider;
        this.deviceService = deviceService;
        this.telemetryService = telemetryService;
        this.bus = bus;
    }

    //--------------------------------------------------------------------------------
    // Snapshot
    //--------------------------------------------------------------------------------

    // 端末 ID の順
    public IReadOnlyList<TelemetryDeviceSummary> Devices
    {
        get
        {
            var hour = CurrentHour();
            lock (sync)
            {
                return entries.Values.OrderBy(static x => x.Device.DeviceId, StringComparer.Ordinal).Select(x => x.ToSummary(hour)).ToArray();
            }
        }
    }

    // 新しい順
    public IReadOnlyList<TelemetryErrorEntry> RecentErrors
    {
        get
        {
            lock (sync)
            {
                return errors.ToArray();
            }
        }
    }

    // 1 分ごとの受信件数 (点・スパン・ログ)。古い順に HistoryMinutes 個、最後が今の分
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

    public TelemetryDeviceSummary? Find(string deviceId)
    {
        var hour = CurrentHour();
        lock (sync)
        {
            return entries.TryGetValue(deviceId, out var entry) ? entry.ToSummary(hour) : null;
        }
    }

    public bool IsReceiving(TelemetryDeviceSummary device) =>
        (device.Info is not null) &&
        (timeProvider.GetUtcNow() - DateTimeOffset.FromUnixTimeMilliseconds(device.Info.LastReceivedAt / NanosecondsPerMillisecond) < ReceivingThreshold);

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

    // 起動時に登録と全端末のファイルから作る。登録の無いファイルは登録する
    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        using var scope = contextProvider.Begin(() => new ServiceContext(timeProvider.GetLocalNow(), SystemUserId));

        var since = (CurrentHour() - CountHours + 1) * NanosecondsPerHour;
        var registered = (await deviceService.QueryAllAsync(cancellationToken)).ToDictionary(static x => x.DeviceId, StringComparer.Ordinal);
        var loaded = new List<Entry>();
        var recent = new List<TelemetryErrorEntry>();
        foreach (var deviceId in telemetryService.EnumerateDevices())
        {
            TelemetryDeviceSummaryView? summary;
            try
            {
                summary = await telemetryService.QuerySummaryAsync(deviceId, LatestNames, since, ErrorLimit, cancellationToken);
            }
            catch (Exception ex) when (OtlpReceiver.IsStorageError(ex))
            {
                log.ErrorTelemetryLoadFailed(deviceId, ex);
                continue;
            }

            if (summary is null)
            {
                continue;
            }

            if (!registered.Remove(deviceId, out var device))
            {
                device = CreateDevice(summary.DeviceInfo);
                await deviceService.InsertAsync(device, cancellationToken);
                log.InfoDeviceRegistered(device.DeviceId, device.Name);
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

            recent.AddRange(summary.RecentErrors.Select(x => CreateError(deviceId, x)));
            loaded.Add(entry);
        }

        lock (sync)
        {
            entries.Clear();
            foreach (var entry in loaded)
            {
                entries[entry.Device.DeviceId] = entry;
            }

            // Registered devices that have not sent telemetry
            foreach (var device in registered.Values)
            {
                entries[device.DeviceId] = new Entry(device);
            }

            errors.Clear();
            errors.AddRange(recent.OrderByDescending(static x => x.TimeUnixNano).Take(ErrorLimit));
        }
    }

    //--------------------------------------------------------------------------------
    // Receive
    //--------------------------------------------------------------------------------

    // 受信した端末の登録を確かめる (未登録なら登録する)。無効の端末は false
    public async ValueTask<bool> EnsureRegisteredAsync(TelemetryDeviceInfoEntity info, CancellationToken cancellationToken)
    {
        lock (sync)
        {
            if (entries.TryGetValue(info.DeviceId, out var entry))
            {
                return entry.Device.IsEnabled;
            }
        }

        var device = CreateDevice(info);
        if (await deviceService.InsertAsync(device, cancellationToken) == DataWriteStatus.Success)
        {
            log.InfoDeviceRegistered(device.DeviceId, device.Name);
        }
        else
        {
            // Registered at the same time by another request
            device = await deviceService.QueryAsync(info.DeviceId, cancellationToken) ?? device;
        }

        bool enabled;
        lock (sync)
        {
            if (!entries.TryGetValue(device.DeviceId, out var entry))
            {
                entry = new Entry(device);
                entries[device.DeviceId] = entry;
            }

            enabled = entry.Device.IsEnabled;
        }

        bus.PublishDeviceChanged(device.DeviceId);
        return enabled;
    }

    // 保存で新しく入った分で要約を更新する
    public void Apply(TelemetrySaveResult result)
    {
        var hour = CurrentHour();
        var minute = CurrentMinute();
        lock (sync)
        {
            if (!entries.TryGetValue(result.DeviceId, out var entry))
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
                AddRecentError(CreateError(result.DeviceId, logEntity));
            }

            entry.RemoveCounts(hour - CountHours);

            AddHistory(minute, count);
        }
    }

    //--------------------------------------------------------------------------------
    // Management
    //--------------------------------------------------------------------------------

    // 登録済みなら Duplicate
    public async ValueTask<DataWriteStatus> AddAsync(DeviceEntity device, CancellationToken cancellationToken = default)
    {
        var status = await deviceService.InsertAsync(device, cancellationToken);
        if (status == DataWriteStatus.Success)
        {
            lock (sync)
            {
                entries[device.DeviceId] = new Entry(device);
            }

            bus.PublishDeviceChanged(device.DeviceId);
        }

        return status;
    }

    // 端末からの登録。未登録なら登録し、登録済みなら名前だけを更新する (グループ・メモ・有効はそのまま)。登録したときは Created が true
    public async ValueTask<(DeviceEntity Device, bool Created)> RegisterAsync(string deviceId, string name, CancellationToken cancellationToken = default)
    {
        var device = new DeviceEntity { DeviceId = deviceId, Name = name, IsEnabled = true };
        var created = await deviceService.InsertAsync(device, cancellationToken) == DataWriteStatus.Success;
        if (created)
        {
            log.InfoDeviceRegistered(device.DeviceId, device.Name);
        }
        else
        {
            device = await deviceService.UpdateNameAsync(deviceId, name, cancellationToken) ?? device;
        }

        lock (sync)
        {
            if (entries.TryGetValue(device.DeviceId, out var entry))
            {
                entry.Device = device;
            }
            else
            {
                entries[device.DeviceId] = new Entry(device);
            }
        }

        bus.PublishDeviceChanged(device.DeviceId);
        return (device, created);
    }

    // 名前・グループ・メモ・有効を更新する
    public async ValueTask<DataWriteStatus> UpdateAsync(DeviceEntity device, CancellationToken cancellationToken = default)
    {
        var updated = await deviceService.UpdateAsync(device, cancellationToken);
        if (updated is null)
        {
            return DataWriteStatus.NotFound;
        }

        lock (sync)
        {
            if (entries.TryGetValue(updated.DeviceId, out var entry))
            {
                entry.Device = updated;
            }
            else
            {
                entries[updated.DeviceId] = new Entry(updated);
            }
        }

        bus.PublishDeviceChanged(updated.DeviceId);
        return DataWriteStatus.Success;
    }

    // 登録とテレメトリのファイルを削除する (送信が続けば自動で登録し直される)
    public async ValueTask<DataWriteStatus> DeleteAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        await telemetryService.DeleteDeviceAsync(deviceId, cancellationToken);
        var status = await deviceService.DeleteAsync(deviceId, cancellationToken);

        lock (sync)
        {
            entries.Remove(deviceId);
            errors.RemoveAll(x => x.DeviceId == deviceId);
        }

        bus.PublishDeviceChanged(deviceId);
        return status;
    }

    // 保持期間でテレメトリのファイルを削除した端末は、要約だけを消す (登録は残る)
    public void ClearTelemetry(string deviceId)
    {
        lock (sync)
        {
            if (!entries.TryGetValue(deviceId, out var entry))
            {
                return;
            }

            entries[deviceId] = new Entry(entry.Device);
            errors.RemoveAll(x => x.DeviceId == deviceId);
        }

        bus.PublishDeviceChanged(deviceId);
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private long CurrentHour() => timeProvider.GetUtcNow().ToUnixTimeSeconds() / 3600;

    private long CurrentMinute() => timeProvider.GetUtcNow().ToUnixTimeSeconds() / 60;

    // 名前の既定は機種 (無ければ端末 ID)
    private static DeviceEntity CreateDevice(TelemetryDeviceInfoEntity info) =>
        new()
        {
            DeviceId = info.DeviceId,
            Name = info.Model.Length > 0 ? info.Model : info.DeviceId,
            IsEnabled = true
        };

    private static TelemetryErrorEntry CreateError(string deviceId, TelemetryLogEntity entity)
    {
        var body = entity.Body;
        var index = body.IndexOfAny(['\r', '\n']);
        return new TelemetryErrorEntry(deviceId, entity.TimeUnixNano, entity.SeverityNumber, index >= 0 ? body[..index] : body);
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
            // Clear the buckets of the minutes without receiving
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

        // Hour (UTC Unix hour) → (errors, crashes)
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

        // The counts of the hour and before are no longer used
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
