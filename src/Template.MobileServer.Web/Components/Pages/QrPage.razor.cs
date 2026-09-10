namespace Template.MobileServer.Web.Components.Pages;

using Microsoft.AspNetCore.Components;

using QRCoder;

// 設定QRコード表示ページ
// [MEMO] template-maui の SettingParser 互換フォーマット(行単位の Key=Value)で生成する
public sealed partial class QrPage
{
    private string? apiEndPoint;

    private string? monitorEndPoint;

    private string? aiServiceEndPoint;

    private string? aiServiceKey;

    private string qrText = string.Empty;

    private string qrImage = string.Empty;

    [Inject]
    public required NavigationManager Navigation { get; set; }

    protected override void OnInitialized()
    {
        // 既定値はサーバー自身のURL
        apiEndPoint = Navigation.BaseUri;
        Update();
    }

    // 入力値からQRコードを再生成する
    private void Update()
    {
        var builder = new StringBuilder();
        AppendValue(builder, "ApiEndPoint", apiEndPoint);
        AppendValue(builder, "MonitorEndPoint", monitorEndPoint);
        AppendValue(builder, "AIServiceEndPoint", aiServiceEndPoint);
        AppendValue(builder, "AIServiceKey", aiServiceKey);
        qrText = builder.ToString();

        if (qrText.Length == 0)
        {
            qrImage = string.Empty;
            return;
        }

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(data);
        qrImage = "data:image/png;base64," + Convert.ToBase64String(qrCode.GetGraphic(8));
    }

    // 空欄の項目は出力しない
    private static void AppendValue(StringBuilder builder, string key, string? value)
    {
        var trimmed = value?.Trim();
        if (!String.IsNullOrEmpty(trimmed))
        {
            builder.Append(key).Append('=').Append(trimmed).Append('\n');
        }
    }
}
