namespace Template.MobileServer.Components;

using System.Collections;
using System.Resources;

using MudBlazor;

using Template.MobileServer.Web.Components;

public sealed class JapaneseMudLocalizerTests
{
    [Fact]
    public void AllMudBlazorTextsAreTranslated()
    {
        // Arrange
        var localizer = new JapaneseMudLocalizer();
        using var stream = typeof(MudLocalizer).Assembly.GetManifestResourceStream("MudBlazor.Resources.LanguageResource.resources");
        Assert.NotNull(stream);
        using var reader = new ResourceReader(stream);
        var keys = reader.Cast<DictionaryEntry>().Select(static x => (string)x.Key).ToList();

        // Act
        var missing = keys.Where(x => localizer[x].ResourceNotFound).ToList();

        // Assert
        Assert.NotEmpty(keys);
        Assert.Empty(missing);
    }

    [Fact]
    public void FormatsArgumentsAndReportsUnknownKey()
    {
        // Arrange
        var localizer = new JapaneseMudLocalizer();

        // Act
        var info = localizer["MudDataGridPager_InfoFormat", 1, 10, 48];
        var unknown = localizer["Unknown"];

        // Assert
        Assert.Equal("1-10 / 48 件", info.Value);
        Assert.True(unknown.ResourceNotFound);
    }
}
