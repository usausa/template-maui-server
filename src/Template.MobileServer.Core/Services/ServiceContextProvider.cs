namespace Template.MobileServer.Services;

public abstract class ServiceContextProvider
{
    public abstract ServiceContext Current { get; }
}
