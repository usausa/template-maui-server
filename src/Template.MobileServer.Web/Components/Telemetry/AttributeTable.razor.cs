namespace Template.MobileServer.Web.Components.Telemetry;

using System.Text.Json;

using Microsoft.AspNetCore.Components;

public sealed partial class AttributeTable
{
    private List<AttributeItem> items = [];

    //--------------------------------------------------------------------------------
    // Parameter
    //--------------------------------------------------------------------------------

    [Parameter]
    public string? Json { get; set; }

    [Parameter]
    public string? Exclude { get; set; }

    private bool HasItems => items.Count > 0;

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    protected override void OnParametersSet()
    {
        if (String.IsNullOrEmpty(Json))
        {
            items = [];
            return;
        }

        using var document = JsonDocument.Parse(Json);
        items = document.RootElement.EnumerateObject()
            .Where(x => x.Name != Exclude)
            .Select(static x => new AttributeItem(x.Name, FormatValue(x.Value)))
            .ToList();
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    public static string FormatValue(JsonElement value) =>
        value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.GetRawText();

    private sealed record AttributeItem(string Key, string Value);
}
