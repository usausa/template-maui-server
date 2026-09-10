namespace Template.MobileServer.Infrastructure.Data;

public sealed class SqlHelperTests
{
    private static readonly string[] Allowed = ["Name", "Value", "CreatedAt"];

    [Theory]
    [InlineData("Name", false, "Name")]
    [InlineData("Value", false, "Value")]
    [InlineData("CreatedAt", false, "CreatedAt")]
    [InlineData("Name", true, "Name DESC")]
    public void NormalizeSortReturnsAllowedColumn(string sort, bool desc, string expected)
    {
        Assert.Equal(expected, SqlHelper.NormalizeSort(Allowed, "Id", sort, desc));
    }

    // 生SQLへ展開されるため、許可外の入力が素通りしないことが本質
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Id")]
    [InlineData("name")]
    [InlineData("Unknown")]
    [InlineData("Name; DROP TABLE Data--")]
    [InlineData("Name, Value")]
    [InlineData("(SELECT 1)")]
    public void NormalizeSortFallsBackToDefaultForDisallowedInput(string? sort)
    {
        Assert.Equal("Id", SqlHelper.NormalizeSort(Allowed, "Id", sort, false));
        Assert.Equal("Id DESC", SqlHelper.NormalizeSort(Allowed, "Id", sort, true));
    }

    // 既定列はテーブルごとに異なるため、呼び出し側の指定がそのまま使われる
    [Fact]
    public void NormalizeSortUsesCallerSuppliedDefaultColumn()
    {
        Assert.Equal("Code", SqlHelper.NormalizeSort(Allowed, "Code", "Unknown", false));
        Assert.Equal("Code DESC", SqlHelper.NormalizeSort(Allowed, "Code", null, true));
    }
}
