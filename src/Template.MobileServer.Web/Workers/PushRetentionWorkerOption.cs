namespace Template.MobileServer.Web.Workers;

public sealed class PushRetentionWorkerOption
{
    public bool Enable { get; set; } = true;

    [Range(1, 1440)]
    public int IntervalMinutes { get; set; } = 60;

    // 送ってからこの日数を過ぎた通知は、届いた / 届いていないにかかわらず削除する
    [Range(1, 3650)]
    public int Days { get; set; } = 7;
}
