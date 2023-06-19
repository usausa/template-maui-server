namespace Template.MobileServer.Web.Components.Authentication;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.IdentityModel.Tokens;

public static class TokenHelper
{
    public static string BuildToken(ClaimsIdentity identity, byte[] secretKey, string issuer, int expire)
    {
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = identity,
            Expires = DateTime.UtcNow.AddDays(expire),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature),
            Audience = issuer,
            Issuer = issuer
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public static ClaimsPrincipal? ParseToken(string value, byte[] secretKey, string issuer)
    {
        try
        {
            var parameter = new TokenValidationParameters
            {
                IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                ValidAudience = issuer,
                ValidIssuer = issuer
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(value, parameter, out var validatedToken);
            if (validatedToken.ValidTo < DateTime.UtcNow)
            {
                return null;
            }

            return principal;
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }
}
