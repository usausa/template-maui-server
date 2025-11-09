namespace Template.MobileServer.Web.State;

public sealed class ExecutingState
{
    public bool Executing { get; private set; }

    public async Task Begin(Func<Task> func)
    {
        if (Executing)
        {
            return;
        }

        using (new Scope(this))
        {
            await func();
        }
    }

    private sealed class Scope : IDisposable
    {
        private readonly ExecutingState state;

        public Scope(ExecutingState state)
        {
            state.Executing = true;
            this.state = state;
        }

        public void Dispose()
        {
            state.Executing = false;
        }
    }
}
