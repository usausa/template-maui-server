namespace Template.MobileServer.Web.Components.Pages;

using System.Collections.Concurrent;

using Microsoft.AspNetCore.Components;

using MudBlazor;

using Template.MobileServer.Web.Components.Telemetry;
using Template.MobileServer.Web.Telemetry;

public sealed partial class TelemetryPage
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(10);

    private static readonly string[] Tabs = [TelemetryLinks.MetricsTab, TelemetryLinks.TracesTab, TelemetryLinks.LogsTab];

    private const int MetricsTab = 0;

    private const int TracesTab = 1;

    private const int TraceLimit = 200;

    private const int LogLimit = 100;

    private const long NanosecondsPerMillisecond = 1_000_000;

    private readonly ConcurrentQueue<TelemetrySaveResult> received = new();

    private RefreshTimer refreshTimer = default!;

    private TelemetryDeviceSummary[] devices = [];

    private TelemetryDeviceSummary? summary;

    private TelemetryRange range = TelemetryRange.Default;

    private string? loadedTerminalId;

    private bool loading;

    private long from;

    private long to;

    // Metric

    private string? metricsKey;

    private int metricsVersion;

    private MetricChartModel? metrics;

    // Trace

    private string? tracesKey;

    private int tracesVersion;

    private List<TelemetryTraceEntity> traces = [];

    private bool errorOnly;

    private string? traceSearch;

    private string? detailTraceId;

    private int detailVersion;

    private TelemetryTraceDetailResult? traceDetail;

    private WaterfallModel? waterfall;

    private string? selectedSpanId;

    // Log

    private TelemetryLogLevel level = TelemetryLogLevel.Default;

    private string? logsKey;

    private int logsVersion;

    private List<TelemetryLogEntity> logs = [];

    private bool hasMoreLogs;

    private string? logSearch;

    private readonly HashSet<long> expandedLogs = [];

    private readonly Dictionary<long, TelemetryResourceEntity> logResources = [];

    //--------------------------------------------------------------------------------
    // Property
    //--------------------------------------------------------------------------------

    [Parameter]
    public string? TerminalId { get; set; }

    [SupplyParameterFromQuery(Name = "tab")]
    public string? Tab { get; set; }

    [SupplyParameterFromQuery(Name = "range")]
    public string? RangeKey { get; set; }

    [SupplyParameterFromQuery(Name = "trace")]
    public string? TraceId { get; set; }

    [SupplyParameterFromQuery(Name = "level")]
    public string? LevelKey { get; set; }

    [Inject]
    public required TimeProvider TimeProvider { get; set; }

    [Inject]
    public required RefreshTimerFactory RefreshTimerFactory { get; set; }

    [Inject]
    public required TelemetryDeviceRegistry Registry { get; set; }

    [Inject]
    public required TelemetryService TelemetryService { get; set; }

    [Inject]
    public required TelemetryBus Bus { get; set; }

    [Inject]
    public required NavigationManager Navigation { get; set; }

    private bool HasDevice => summary is not null;

    private string EmptyMessage => TerminalId is null ? "端末を選んでください。" : "端末が見つかりません。";

    private int ActiveTab => Math.Max(Array.IndexOf(Tabs, Tab), 0);

    // Header

    private TelemetryDeviceState State => summary is null ? TelemetryDeviceState.NoData : Registry.GetState(summary);

    private string StateText => ViewHelper.FormatState(State);

    private Color StateColor => ViewHelper.StateColor(State);

    private Variant StateVariant => ViewHelper.StateVariant(State);

    private string DeviceName => summary?.Device.Name ?? string.Empty;

    private string DeviceCaption =>
        summary?.Device is { } device ? (device.GroupName is { Length: > 0 } group ? $"{device.TerminalId} / {group}" : device.TerminalId) : string.Empty;

    private bool IsDisabled => summary is { Device.IsEnabled: false };

    private string? Note => summary?.Device.Note;

    private bool HasNote => !String.IsNullOrEmpty(Note);

    private string Model => summary?.Info?.Model ?? "-";

    private string Os => summary?.Info is { } info ? $"{info.OsName} {info.OsVersion}" : "-";

    private string App => summary?.Info?.ServiceVersion ?? "-";

    private string LastReceived =>
        summary?.Info is { } info ? ViewHelper.FormatElapsed(TimeProvider.GetUtcNow() - ViewHelper.ToDateTimeOffset(info.LastReceivedAt)) : "-";

    private string? LastReceivedTime => summary?.Info is { } info ? ViewHelper.FormatTime(info.LastReceivedAt) : null;

    private TelemetryValue? Battery => summary?.Battery;

    private TelemetryValue? WiFi => summary?.WiFi;

    private TelemetryValue? Cpu => summary?.Cpu;

    private TelemetryValue? Memory => summary?.Memory;

    // Metric

    private IReadOnlyList<MetricChart> Charts => metrics?.Charts ?? [];

    private bool HasCharts => Charts.Count > 0;

    // Trace

    private string TracesKey => $"{range.Key}\n{errorOnly}\n{traceSearch}";

    private bool HasTraces => traces.Count > 0;

    private string TraceCountText => traces.Count >= TraceLimit ? $"新しい {TraceLimit} 件" : $"{traces.Count} 件";

    private bool HasTraceId => TraceId is not null;

    private bool HasTraceDetail => (traceDetail is not null) && (waterfall is not null);

    private string TraceTitle => traceDetail?.Trace.RootName ?? string.Empty;

    private string TraceFacts =>
        traceDetail?.Trace is { } trace
            ? $"{ViewHelper.FormatTime(trace.StartTimeUnixNano)}・{ViewHelper.FormatDuration(trace.EndTimeUnixNano - trace.StartTimeUnixNano)}・スパン {trace.SpanCount}・エラー {trace.ErrorCount}"
            : string.Empty;

    private TelemetrySpanEntity? SelectedSpan => traceDetail?.Spans.FirstOrDefault(x => x.SpanId == selectedSpanId);

    private bool HasSelectedSpan => SelectedSpan is not null;

    private TelemetryResourceEntity? SelectedResource =>
        SelectedSpan is { } span ? traceDetail?.Resources.FirstOrDefault(x => x.Id == span.ResourceId) : null;

    private IReadOnlyList<TelemetryLogEntity> TraceLogs => traceDetail?.Logs ?? [];

    private bool HasTraceLogs => TraceLogs.Count > 0;

    // Log

    private string LogsKey => $"{range.Key}\n{level.Key}\n{logSearch}\n{TraceId}";

    private bool HasLogs => logs.Count > 0;

    private string LogCountText => hasMoreLogs ? $"{logs.Count} 件 (続きあり)" : $"{logs.Count} 件";

    private bool HasTraceFilter => TraceId is not null;

    private string TraceFilterText => $"トレース {TraceId}";

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    protected override void OnInitialized()
    {
        Bus.Received += OnReceived;
        Bus.DeviceChanged += OnDeviceChanged;
        refreshTimer = RefreshTimerFactory.Create(RefreshInterval, RefreshAsync);
    }

    protected override Task OnParametersSetAsync()
    {
        range = TelemetryRange.Find(RangeKey);
        level = TelemetryLogLevel.Find(LevelKey);
        LoadDevices();
        if (TerminalId != loadedTerminalId)
        {
            loadedTerminalId = TerminalId;
            ResetTabs();
        }

        return LoadActiveTabAsync();
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

    private void OnReceived(object? sender, TelemetryReceivedEventArgs e)
    {
        if (e.Result.TerminalId == loadedTerminalId)
        {
            received.Enqueue(e.Result);
            refreshTimer.Request();
        }
    }

    private void OnDeviceChanged(object? sender, TelemetryDeviceChangedEventArgs e) => refreshTimer.Request();

    private Task RefreshAsync() =>
        InvokeAsync(async () =>
        {
            LoadDevices();
            UpdateRange();
            var detailChanged = false;
            while (received.TryDequeue(out var result))
            {
                metrics?.Add(result.Metrics, from);
                if (tracesKey is not null)
                {
                    MergeTraces(result.Traces);
                }

                if (logsKey is not null)
                {
                    MergeLogs(result.Logs);
                }

                detailChanged |= IsDetailChanged(result);
            }

            metrics?.Trim(from);
            traces.RemoveAll(x => x.StartTimeUnixNano < from);
            logs.RemoveAll(x => x.TimeUnixNano < from);
            if (detailChanged && (summary is not null))
            {
                await LoadTraceDetailAsync(summary.Device.TerminalId);
            }

            StateHasChanged();
        });

    //--------------------------------------------------------------------------------
    // Action
    //--------------------------------------------------------------------------------

    private void SelectDevice(string? terminalId)
    {
        if ((terminalId is null) || (terminalId == TerminalId))
        {
            return;
        }

        var uri = Navigation.ToAbsoluteUri(TelemetryLinks.Device(terminalId)).AbsoluteUri;
        Navigation.NavigateTo(Navigation.GetUriWithQueryParameters(uri, new Dictionary<string, object?> { ["tab"] = Tab, ["range"] = RangeKey }));
    }

    private void SelectRange(string? key) =>
        Navigation.NavigateTo(Navigation.GetUriWithQueryParameter("range", key), replace: true);

    private void SelectTab(int index) =>
        Navigation.NavigateTo(Navigation.GetUriWithQueryParameter("tab", Tabs[index]), replace: true);

    private void SelectTrace(string traceId) =>
        Navigation.NavigateTo(Navigation.GetUriWithQueryParameter("trace", traceId));

    private void SelectSpan(string spanId) => selectedSpanId = spanId;

    private Task SetErrorOnlyAsync(bool value)
    {
        errorOnly = value;
        return LoadActiveTabAsync();
    }

    private Task SetTraceSearchAsync(string? value)
    {
        traceSearch = value;
        return LoadActiveTabAsync();
    }

    private void SelectLevel(string? key) =>
        Navigation.NavigateTo(Navigation.GetUriWithQueryParameter("level", key == TelemetryLogLevel.Default.Key ? null : key), replace: true);

    private Task SetLogSearchAsync(string? value)
    {
        logSearch = value;
        return LoadActiveTabAsync();
    }

    private void ClearTraceFilter() =>
        Navigation.NavigateTo(Navigation.GetUriWithQueryParameter("trace", (string?)null), replace: true);

    private Task LoadMoreLogsAsync() =>
        summary is null ? Task.CompletedTask : LoadLogsAsync(summary.Device.TerminalId, true);

    private async Task ToggleLogAsync(TelemetryLogEntity log)
    {
        if (expandedLogs.Remove(log.Id))
        {
            return;
        }

        expandedLogs.Add(log.Id);
        if ((summary is not null) && !logResources.ContainsKey(log.ResourceId) &&
            (await TelemetryService.QueryResourceAsync(summary.Device.TerminalId, log.ResourceId) is { } resource))
        {
            logResources[resource.Id] = resource;
        }
    }

    //--------------------------------------------------------------------------------
    // Load
    //--------------------------------------------------------------------------------

    private void LoadDevices()
    {
        devices = Registry.Devices
            .OrderBy(static x => x.Device.Name, StringComparer.CurrentCulture)
            .ThenBy(static x => x.Device.TerminalId, StringComparer.Ordinal)
            .ToArray();
        summary = TerminalId is null ? null : Array.Find(devices, x => x.Device.TerminalId == TerminalId);
    }

    private void ResetTabs()
    {
        received.Clear();
        metricsKey = null;
        metrics = null;
        tracesKey = null;
        traces = [];
        detailTraceId = null;
        traceDetail = null;
        waterfall = null;
        selectedSpanId = null;
        logsKey = null;
        logs = [];
        hasMoreLogs = false;
        expandedLogs.Clear();
        logResources.Clear();
    }

    private async Task LoadActiveTabAsync()
    {
        UpdateRange();
        if (summary is null)
        {
            return;
        }

        var terminalId = summary.Device.TerminalId;
        switch (ActiveTab)
        {
            case MetricsTab:
                if (metricsKey != range.Key)
                {
                    await LoadMetricsAsync(terminalId);
                }

                break;
            case TracesTab:
                if (tracesKey != TracesKey)
                {
                    await LoadTracesAsync(terminalId);
                }

                if (detailTraceId != TraceId)
                {
                    await LoadTraceDetailAsync(terminalId);
                }

                break;
            default:
                if (logsKey != LogsKey)
                {
                    await LoadLogsAsync(terminalId, false);
                }

                break;
        }
    }

    private async Task LoadMetricsAsync(string terminalId)
    {
        var version = ++metricsVersion;
        metricsKey = range.Key;
        metrics = null;
        loading = true;
        var interval = range.IntervalNanoseconds;
        var history = await TelemetryService.QueryMetricHistoryAsync(terminalId, from / interval * interval, interval);
        if (version != metricsVersion)
        {
            return;
        }

        var model = new MetricChartModel(interval);
        model.Load(history);
        metrics = model;
        loading = false;
    }

    private async Task LoadTracesAsync(string terminalId)
    {
        var version = ++tracesVersion;
        tracesKey = TracesKey;
        loading = true;
        var list = await TelemetryService.QueryTraceListAsync(terminalId, from, errorOnly, traceSearch, TraceLimit);
        if (version != tracesVersion)
        {
            return;
        }

        traces = list;
        loading = false;
    }

    private async Task LoadTraceDetailAsync(string terminalId)
    {
        var version = ++detailVersion;
        var reload = detailTraceId == TraceId;
        detailTraceId = TraceId;
        if (TraceId is null)
        {
            traceDetail = null;
            waterfall = null;
            selectedSpanId = null;
            return;
        }

        var detail = await TelemetryService.QueryTraceDetailAsync(terminalId, TraceId);
        if (version != detailVersion)
        {
            return;
        }

        traceDetail = detail;
        waterfall = detail is null ? null : new WaterfallModel(detail.Spans, reload ? waterfall?.CollapsedSpans : null);
        if (!reload || (detail?.Spans.All(x => x.SpanId != selectedSpanId) ?? true))
        {
            selectedSpanId = waterfall is { Rows.Count: > 0 } ? waterfall.Rows[0].Span.SpanId : null;
        }
    }

    private async Task LoadLogsAsync(string terminalId, bool more)
    {
        var version = ++logsVersion;
        logsKey = LogsKey;
        loading = true;
        var last = more && (logs.Count > 0) ? logs[^1] : null;
        var list = await TelemetryService.QueryLogListAsync(
            terminalId,
            new TelemetryLogQuery
            {
                Since = from,
                Severity = level.Severity,
                Body = logSearch,
                TraceId = TraceId,
                BeforeTime = last?.TimeUnixNano,
                BeforeId = last?.Id ?? 0,
                Limit = LogLimit
            });
        if (version != logsVersion)
        {
            return;
        }

        if (!more)
        {
            logs = [];
            expandedLogs.Clear();
        }

        logs.AddRange(list);
        hasMoreLogs = list.Count >= LogLimit;
        loading = false;
    }

    private void UpdateRange()
    {
        to = TimeProvider.GetUtcNow().ToUnixTimeMilliseconds() * NanosecondsPerMillisecond;
        from = to - range.DurationNanoseconds;
    }

    //--------------------------------------------------------------------------------
    // Trace
    //--------------------------------------------------------------------------------

    private void MergeTraces(IEnumerable<TelemetryTraceEntity> items)
    {
        foreach (var trace in items)
        {
            traces.RemoveAll(x => x.TraceId == trace.TraceId);
            if (IsVisible(trace))
            {
                traces.Add(trace);
            }
        }

        traces.Sort(static (x, y) => y.StartTimeUnixNano.CompareTo(x.StartTimeUnixNano));
        if (traces.Count > TraceLimit)
        {
            traces.RemoveRange(TraceLimit, traces.Count - TraceLimit);
        }
    }

    private bool IsVisible(TelemetryTraceEntity trace) =>
        (trace.StartTimeUnixNano >= from) &&
        (!errorOnly || (trace.ErrorCount > 0)) &&
        (String.IsNullOrWhiteSpace(traceSearch) || trace.RootName.Contains(traceSearch.Trim(), StringComparison.OrdinalIgnoreCase));

    //--------------------------------------------------------------------------------
    // Log
    //--------------------------------------------------------------------------------

    private void MergeLogs(IEnumerable<TelemetryLogEntity> items)
    {
        var count = logs.Count;
        logs.AddRange(items.Where(IsVisible));
        if (logs.Count > count)
        {
            logs.Sort(static (x, y) => x.TimeUnixNano != y.TimeUnixNano ? y.TimeUnixNano.CompareTo(x.TimeUnixNano) : y.Id.CompareTo(x.Id));
        }
    }

    private bool IsVisible(TelemetryLogEntity log) =>
        (log.TimeUnixNano >= from) &&
        (log.SeverityNumber >= level.Severity) &&
        ((TraceId is null) || (log.TraceId == TraceId)) &&
        (String.IsNullOrWhiteSpace(logSearch) || log.Body.Contains(logSearch.Trim(), StringComparison.OrdinalIgnoreCase));

    private bool IsExpanded(TelemetryLogEntity log) => expandedLogs.Contains(log.Id);

    private TelemetryResourceEntity? FindResource(TelemetryLogEntity log) => logResources.GetValueOrDefault(log.ResourceId);

    private static bool HasTrace(TelemetryLogEntity log) => log.TraceId.Length > 0;

    private string TraceLink(TelemetryLogEntity log) =>
        Navigation.GetUriWithQueryParameters(new Dictionary<string, object?> { ["tab"] = Tabs[TracesTab], ["trace"] = log.TraceId });

    private static string ShortScope(TelemetryLogEntity log)
    {
        var index = log.ScopeName.LastIndexOf('.');
        return index >= 0 ? log.ScopeName[(index + 1)..] : log.ScopeName;
    }

    private bool IsDetailChanged(TelemetrySaveResult result) =>
        (detailTraceId is not null) &&
        (result.Traces.Any(x => x.TraceId == detailTraceId) || result.Logs.Any(x => x.TraceId == detailTraceId));

    //--------------------------------------------------------------------------------
    // Format
    //--------------------------------------------------------------------------------

    private string? FormatDeviceOption(string? terminalId) =>
        Array.Find(devices, x => x.Device.TerminalId == terminalId) is { } device ? $"{device.Device.Name} ({device.Device.TerminalId})" : terminalId;

    private Color OptionColor(TelemetryDeviceSummary device) => ViewHelper.StateColor(Registry.GetState(device));

    private static bool IsDisabledDevice(TelemetryDeviceSummary device) => !device.Device.IsEnabled;

    private string TraceRowClass(TelemetryTraceEntity trace) => trace.TraceId == TraceId ? "trace-row trace-selected" : "trace-row";

    private static string FormatTraceDuration(TelemetryTraceEntity trace) => ViewHelper.FormatDuration(trace.EndTimeUnixNano - trace.StartTimeUnixNano);

    private static string FirstLine(string text)
    {
        var index = text.IndexOfAny(['\r', '\n']);
        return index >= 0 ? text[..index] : text;
    }

    private string FindSpanName(string spanId) =>
        traceDetail?.Spans.FirstOrDefault(x => x.SpanId == spanId)?.Name ?? spanId;
}
