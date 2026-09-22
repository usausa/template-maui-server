namespace Template.MobileServer.Web.Infrastructure.Security;

using System.Security.Cryptography;

public sealed class CspNonce
{
    public string Value { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
}
