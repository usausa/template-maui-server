namespace Template.MobileServer.Infrastructure.Data;

using Smart.Data.Accessor.Converters;

#pragma warning disable CA1000
public sealed class EnumTextConverter<T> : IValueConverter<string, T>
    where T : struct, Enum
{
    public static T FromDb(string dbValue) => Enum.Parse<T>(dbValue);

    public static string ToDb(T clrValue) => clrValue.ToString();
}
#pragma warning restore CA1000
