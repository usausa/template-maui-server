namespace Template.MobileServer.Web.Application.Authentication;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

public sealed class JwtTokenProvider
{
    private static readonly JsonWebTokenHandler Handler = new();

    private readonly TimeProvider timeProvider;

    private readonly JwtSetting setting;

    private readonly SigningCredentials credentials;

    public JwtTokenProvider(TimeProvider timeProvider, JwtSetting setting)
    {
        this.timeProvider = timeProvider;
        this.setting = setting;
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
