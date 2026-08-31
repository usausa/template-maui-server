namespace Template.MobileServer.ChatClient;

using System.Windows;

internal sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var viewModel = new MainWindowViewModel();
        DataContext = viewModel;

        // 新着時に末尾へ自動スクロールする(View側の責務)
        viewModel.Messages.CollectionChanged += (_, _) =>
        {
            if (MessageList.Items.Count > 0)
            {
                MessageList.ScrollIntoView(MessageList.Items[^1]);
            }
        };

        // ウィンドウ終了時に切断する
        Closed += async (_, _) => await viewModel.DisposeAsync().ConfigureAwait(false);
    }
}
