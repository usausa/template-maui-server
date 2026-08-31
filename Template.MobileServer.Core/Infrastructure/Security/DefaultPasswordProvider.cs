namespace Template.MobileServer.Infrastructure.Security;

using System.Security.Cryptography;

public sealed class DefaultPasswordProvider : IPasswordProvider
{
    private readonly DefaultPasswordProviderOptions options;

    public DefaultPasswordProvider(DefaultPasswordProviderOptions options)
    {
        this.options = options;
    }

    public bool Match(string password, byte[] hash)
    {
        if (hash.Length != options.SaltSize + options.HashSize)
        {
            return false;
        }

        var salt = hash.AsSpan(0, options.SaltSize);
        var bytes = hash.AsSpan(options.SaltSize);

        var compare = Rfc2898DeriveBytes.Pbkdf2(password, salt, options.Iterations, new HashAlgorithmName(options.HashAlgorithm), options.HashSize);
        return CryptographicOperations.FixedTimeEquals(bytes, compare);
    }

    public byte[] Generate(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(options.SaltSize);
        var bytes = Rfc2898DeriveBytes.Pbkdf2(password, salt, options.Iterations, new HashAlgorithmName(options.HashAlgorithm), options.HashSize);

        var hash = new byte[salt.Length + bytes.Length];
        salt.AsSpan().CopyTo(hash);
        bytes.AsSpan().CopyTo(hash.AsSpan(salt.Length));
        return hash;
    }
}
