namespace Template.MobileServer.Web.Infrastructure.Notifications;

public sealed class NotificationBus
{
    public event EventHandler<NotificationEventArgs>? Received;

    public void Publish(string message) =>
        Received?.Invoke(this, new NotificationEventArgs(message));
}
