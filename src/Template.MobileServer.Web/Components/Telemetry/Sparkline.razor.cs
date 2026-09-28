namespace Template.MobileServer.Web.Components.Telemetry;

using Microsoft.AspNetCore.Components;

// 軸の無い小さな折れ線。0 を下端、最大を上端にして要素の大きさに伸ばす (線の太さは CSS の vector-effect で保つ)
public sealed partial class Sparkline
{
    private const int Height = 100;

    // 線が上下の端で切れないための余白
    private const double Padding = 5;

    private string viewBox = string.Empty;

    private string points = string.Empty;

    //--------------------------------------------------------------------------------
    // Parameter
    //--------------------------------------------------------------------------------

    [Parameter]
    public IReadOnlyList<double> Values { get; set; } = [];

    [Parameter]
    public string? Class { get; set; }

    private string CssClass => String.IsNullOrEmpty(Class) ? "sparkline" : $"sparkline {Class}";

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    protected override void OnParametersSet()
    {
        var max = Values.Count > 0 ? Values.Max() : 0;
        viewBox = FormattableString.Invariant($"0 0 {Math.Max(Values.Count - 1, 1)} {Height}");
        points = String.Join(' ', Values.Select((x, i) => FormattableString.Invariant($"{i},{ToY(x, max):0.##}")));
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static double ToY(double value, double max) =>
        max > 0 ? Height - Padding - ((value / max) * (Height - (2 * Padding))) : Height - Padding;
}
