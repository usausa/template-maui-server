namespace Template.MobileServer.ChatClient.Chat;

// チャット接続状態
internal enum ChatConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting
}
