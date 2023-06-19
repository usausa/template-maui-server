namespace Template.MobileServer.Components.Security;

public sealed class SaltHashPasswordOptions
{
    public int SaltLength { get; set; } = 2;

    // ReSharper disable StringLiteralTypo
    public string SaltCharacters { get; set; } = "1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    // ReSharper restore StringLiteralTypo
}
