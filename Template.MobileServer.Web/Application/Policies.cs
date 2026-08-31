namespace Template.MobileServer.Web.Application;

public static class Policies
{
    // 管理画面用(Cookie認証)
    public const string Administrator = nameof(Administrator);

    // モバイルAPI用(JWT Bearer認証)
    public const string MobileApi = nameof(MobileApi);
}

public static class Roles
{
    public const string Administrator = nameof(Administrator);
}
