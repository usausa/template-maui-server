namespace Template.MobileServer.Web.Infrastructure.Authentication;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using Template.MobileServer.Web.Settings;

// モバイルAPI用のJWTトークン発行
public sealed class TokenService
{
    private static readonly JsonWebTokenHandler Handler = new();

    private readonly JwtSetting setting;

    private readonly TimeProvider timeProvider;

    private readonly SigningCredentials credentials;

    public TokenService(JwtSetting setting, TimeProvider timeProvider)
    {
        this.setting = setting;
        this.timeProvider = timeProvider;
        credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(setting.SecretKey)), SecurityAlgorithms.HmacSha256);
    }

    public string CreateToken(string id)
    {
        var now = timeProvider.GetUtcNow();

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = setting.Issuer,
            Audience = setting.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = now.AddMinutes(setting.ExpireMinutes).UtcDateTime,
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
