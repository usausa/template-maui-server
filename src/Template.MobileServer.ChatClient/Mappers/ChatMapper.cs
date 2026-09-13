namespace Template.MobileServer.ChatClient.Mappers;

using Smart.Mapper;

using Template.MobileServer.Chat;
using Template.MobileServer.ChatClient.Chat;

internal static partial class ChatMapper
{
    // タイムスタンプはUnixミリ秒からローカル時刻へ(record のコンストラクター引数は Converter で変換する)
    [Mapper]
    [MapProperty(nameof(ChatMessageEntry.Timestamp), nameof(ChatMessage.Timestamp), Converter = nameof(ToLocalTime))]
    public static partial ChatMessageEntry ToEntry(this ChatMessage message);

    private static DateTime ToLocalTime(long timestamp) =>
        DateTimeOffset.FromUnixTimeMilliseconds(timestamp).LocalDateTime;
}
