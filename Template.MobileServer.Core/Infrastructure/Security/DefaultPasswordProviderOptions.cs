namespace Template.MobileServer.Infrastructure.Security;

public sealed class DefaultPasswordProviderOptions
{
    public int SaltSize { get; set; } = 32;

    public int HashSize { get; set; } = 32;

    public int Iterations { get; set; } = 310000;

    public string HashAlgorithm { get; set; } = "SHA256";
}
