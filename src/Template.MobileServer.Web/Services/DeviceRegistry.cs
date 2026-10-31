namespace Template.MobileServer.Web.Services;

using System.Collections.Concurrent;

using Template.MobileServer.Web.Hubs;

public sealed record DeviceEntry(
    string ConnectionId,
    DateTimeOffset ConnectedAt,
    DateTimeOffset LastReceivedAt,
    string? TerminalId,
    string? Model,
    string? Platform,
    double? Battery,
    string? BatteryState,
    string? Network);

public sealed class DeviceRegistry
{
    private readonly ConcurrentDictionary<string, DeviceEntry> entries = new();

    private readonly ConcurrentDictionary<string, Action> aborts = new();

    public event EventHandler? Changed;

    //--------------------------------------------------------------------------------
    // Query
    //--------------------------------------------------------------------------------

    public int Count => entries.Count;

    public IReadOnlyList<DeviceEntry> Entries => entries.Values.OrderBy(static x => x.ConnectedAt).ToArray();

    //--------------------------------------------------------------------------------
    // Write
    //--------------------------------------------------------------------------------

    public void Register(string connectionId, DateTimeOffset now, Action? abort = null)
    {
        entries[connectionId] = new DeviceEntry(connectionId, now, now, null, null, null, null, null, null);
        if (abort is not null)
        {
            aborts[connectionId] = abort;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool Update(string connectionId, DeviceStatusMessage status, DateTimeOffset now)
    {
        if (!entries.TryGetValue(connectionId, out var entry))
        {
            return false;
        }

        entries[connectionId] = entry with
        {
            LastReceivedAt = now,
            TerminalId = status.TerminalId,
            Model = status.Model,
            Platform = status.Platform,
            Battery = status.Battery,
            BatteryState = status.BatteryState,
            Network = status.Network
        };
        Changed?.Invoke(this, EventArgs.Empty);

        return true;
    }

    public bool Remove(string connectionId)
    {
        aborts.TryRemove(connectionId, out _);
        if (!entries.TryRemove(connectionId, out _))
        {
            return false;
        }

        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    //--------------------------------------------------------------------------------
    // Connection
    //--------------------------------------------------------------------------------

    public bool Disconnect(string connectionId)
    {
        if (!aborts.TryGetValue(connectionId, out var abort))
        {
            return false;
        }

        abort();
        return true;
    }
}
