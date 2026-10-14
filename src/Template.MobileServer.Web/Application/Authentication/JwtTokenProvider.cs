namespace Template.MobileServer.Web.Application.Authentication;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

// JWT の発行 (検証側の設定 JwtSetting と同じ値を使う)
public sealed class JwtTokenProvider
{
    private static readonly JsonWebTokenHandler Handler = new();

    private TimeProvider TimeProvider { get; }

    private JwtSetting Setting { get; }

    private readonly SigningCredentials credentials;

    public JwtTokenProvider(TimeProvider timeProvider, JwtSetting setting)
    {
        TimeProvider = timeProvider;
        Setting = setting;
        credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(setting.SecretKey)), SecurityAlgorithms.HmacSha256);
    }

    public string CreateToken(string id)
    {
        var now = TimeProvider.GetUtcNow();

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Setting.Issuer,
            Audience = Setting.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = now.AddMinutes(Setting.ExpireMinutes).UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = id,
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N")
            },
            SigningCredentials = credentials
        };

        return Handler.CreateToken(descriptor);
    }
}
