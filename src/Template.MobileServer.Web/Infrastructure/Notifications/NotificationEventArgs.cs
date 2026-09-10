namespace Template.MobileServer.Web.Infrastructure.Notifications;

public sealed class NotificationEventArgs : EventArgs
{
    public string Message { get; }

    public NotificationEventArgs(string message)
    {
        Message = message;
    }
}
