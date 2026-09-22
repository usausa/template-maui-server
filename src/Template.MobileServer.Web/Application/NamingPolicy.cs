namespace Template.MobileServer.Web.Application;

using System.Text.Json;

public static class NamingPolicy
{
    public static JsonNamingPolicy JsonPropertyNaming => JsonNamingPolicy.CamelCase;

    public static JsonNamingPolicy JsonDictionaryKeyNaming => JsonNamingPolicy.CamelCase;
}
