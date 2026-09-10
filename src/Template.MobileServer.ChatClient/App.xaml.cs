namespace Template.MobileServer.ChatClient;

using System.Windows;

internal sealed partial class App
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        MainWindow = new MainWindow();
        MainWindow.Show();
    }
}
