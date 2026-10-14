namespace Template.MobileServer.Web.Application.Circuits;

using Microsoft.AspNetCore.Components.Server.Circuits;

public sealed class AppCircuitHandler : CircuitHandler
{
    private ILogger<AppCircuitHandler> Log { get; }

    private TimeProvider TimeProvider { get; }

    private CircuitTracker Tracker { get; }

    public AppCircuitHandler(
        ILogger<AppCircuitHandler> log,
        TimeProvider timeProvider,
        CircuitTracker tracker)
    {
        Log = log;
        TimeProvider = timeProvider;
        Tracker = tracker;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        Tracker.Add(new CircuitInfo(circuit.Id, TimeProvider.GetLocalNow(), true));
        Log.InfoCircuitOpened(circuit.Id, Tracker.Count);
        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        Tracker.SetConnected(circuit.Id, true);
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        Tracker.SetConnected(circuit.Id, false);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        Tracker.Remove(circuit.Id);
        Log.InfoCircuitClosed(circuit.Id, Tracker.Count);
        return Task.CompletedTask;
    }
}
