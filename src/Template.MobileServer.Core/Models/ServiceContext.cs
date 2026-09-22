namespace Template.MobileServer.Models;

// 処理時刻と実行ユーザー。境界 (API のフィルター / Blazor のイベント / ワーカーの仕事 1 件) がスコープ開始時に 1 回だけ作る不変のスナップショット
public sealed record ServiceContext(DateTimeOffset Now, string UserId, string UserName)
{
    public const string SystemUserId = "system";

    public static ServiceContext System(TimeProvider timeProvider) =>
        new(timeProvider.GetLocalNow(), SystemUserId, SystemUserId);
}
