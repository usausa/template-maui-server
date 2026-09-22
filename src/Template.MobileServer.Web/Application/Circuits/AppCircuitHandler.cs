namespace Template.MobileServer.Web.Application.Circuits;

using Microsoft.AspNetCore.Components.Server.Circuits;

public sealed class AppCircuitHandler : CircuitHandler
{
    private readonly ILogger<AppCircuitHandler> log;

    private readonly TimeProvider timeProvider;

    private readonly CircuitTracker tracker;

    public AppCircuitHandler(
        ILogger<AppCircuitHandler> log,
        TimeProvider timeProvider,
        CircuitTracker tracker)
    {
        this.log = log;
        this.timeProvider = timeProvider;
        this.tracker = tracker;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        tracker.Add(new CircuitInfo(circuit.Id, timeProvider.GetLocalNow(), true));
        log.InfoCircuitOpened(circuit.Id, tracker.Count);
        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        tracker.SetConnected(circuit.Id, true);
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        tracker.SetConnected(circuit.Id, false);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        tracker.Remove(circuit.Id);
        log.InfoCircuitClosed(circuit.Id, tracker.Count);
        return Task.CompletedTask;
    }
}
