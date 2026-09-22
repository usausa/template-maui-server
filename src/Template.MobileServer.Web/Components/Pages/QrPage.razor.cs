namespace Template.MobileServer.Web.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;

using MudBlazor;

using QRCoder;

// モバイルアプリへ設定 QR で配布する値のキー (= アプリの Settings のプロパティ名)
public static class ClientSettingKeys
{
    // 接続先はサーバー自身の URL から決める (保存しない)
    public const string ApiEndPoint = "ApiEndPoint";

    public const string GrpcEndPoint = "GrpcEndPoint";

    public const string OtelEndPoint = "OtelEndPoint";

    // 以下は Setting テーブルで管理する
    // ReSharper disable InconsistentNaming (QRのキー名 = アプリのSettingsのプロパティ名に合わせる)
    public const string AIServiceEndPoint = "AIServiceEndPoint";

    public const string AIServiceKey = "AIServiceKey";

    // ReSharper restore InconsistentNaming

    public const string OllamaEndPoint = "OllamaEndPoint";

    public const string OllamaModel = "OllamaModel";

    public const string ScpHost = "ScpHost";

    public const string ScpPort = "ScpPort";

    public const string ScpUser = "ScpUser";

    public const string ScpPassword = "ScpPassword";

    public static readonly string[] Connections = [ApiEndPoint, GrpcEndPoint, OtelEndPoint];

    // QR に出力する順 (ScpPort は ScpHost があるときだけ)
    public static readonly string[] Stored = [AIServiceEndPoint, AIServiceKey, OllamaEndPoint, OllamaModel, ScpHost, ScpPort, ScpUser, ScpPassword];
}

// 設定QRコード表示ページ
// [MEMO] template-maui の SettingParser 互換フォーマット(行単位の Key=Value)で生成する。キー名は端末側 Settings のプロパティ名
public sealed partial class QrPage
{
    private const string GrpcEndpointConfigurationKey = "Kestrel:Endpoints:Grpc:Url";

    private readonly List<KeyValuePair<string, string>> connections = [];

    private readonly List<SettingItem> items = [];

    private string qrText = string.Empty;

    private string qrImage = string.Empty;

    [Inject]
    public required NavigationManager Navigation { get; set; }

    [Inject]
    public required IConfiguration Configuration { get; set; }

    [Inject]
    public required SettingService SettingService { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    private bool IsDirty => items.Any(static x => x.IsDirty);

    protected override Task OnInitializedAsync()
    {
        // 接続先はサーバー自身のURL(gRPCはKestrelのgRPCエンドポイントのポート、OTLP/HTTP はAPIと同じ)
        connections.Add(new(ClientSettingKeys.ApiEndPoint, Navigation.BaseUri));
        connections.Add(new(ClientSettingKeys.GrpcEndPoint, MakeGrpcEndPoint(Navigation.BaseUri, Configuration[GrpcEndpointConfigurationKey])));
        connections.Add(new(ClientSettingKeys.OtelEndPoint, Navigation.BaseUri));

        foreach (var key in ClientSettingKeys.Stored)
        {
            items.Add(new SettingItem(key, key == ClientSettingKeys.ScpPort ? "22" : string.Empty, key is ClientSettingKeys.AIServiceKey or ClientSettingKeys.ScpPassword));
        }

        return ReloadAsync();
    }

    // gRPCの接続先はサーバー自身のホストにgRPCエンドポイントのポートを組み合わせる
    internal static string MakeGrpcEndPoint(string baseUri, string? grpcUrl)
    {
        if (String.IsNullOrEmpty(grpcUrl) ||
            !Uri.TryCreate(grpcUrl.Replace("*", "localhost", StringComparison.Ordinal).Replace("+", "localhost", StringComparison.Ordinal), UriKind.Absolute, out var grpcUri))
        {
            return string.Empty;
        }

        var builder = new UriBuilder(baseUri) { Port = grpcUri.Port, Path = "/", Query = string.Empty };
        return builder.Uri.ToString();
    }

    private async Task ReloadAsync()
    {
        var entities = await SettingService.QueryAllAsync();
        var values = entities.ToDictionary(static x => x.Key, static x => x.Value, StringComparer.Ordinal);
        foreach (var item in items)
        {
            item.Load(values.GetValueOrDefault(item.Key, string.Empty));
        }

        Update();
    }

    private async Task SaveAsync()
    {
        foreach (var item in items.Where(static x => x.IsDirty))
        {
            await SettingService.UpdateAsync(item.Key, item.Value);
            item.Load(item.Value?.Trim() ?? string.Empty);
        }

        Snackbar.AddSuccess("設定を保存しました。");
    }

    // 接続先と入力値(空欄は既定値)からQRコードを再生成する
    private void Update()
    {
        var builder = new StringBuilder();
        foreach (var (key, value) in connections)
        {
            AppendValue(builder, key, value);
        }

        var scpHost = items.First(static x => x.Key == ClientSettingKeys.ScpHost).Effective;
        foreach (var item in items)
        {
            if ((item.Key == ClientSettingKeys.ScpPort) && (scpHost.Length == 0))
            {
                continue;
            }

            AppendValue(builder, item.Key, item.Effective);
        }

        qrText = builder.ToString();

        if (qrText.Length == 0)
        {
            qrImage = string.Empty;
            return;
        }

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(data);
        qrImage = "data:image/png;base64," + Convert.ToBase64String(qrCode.GetGraphic(5));
    }

    // 空欄の項目は出力しない
    private static void AppendValue(StringBuilder builder, string key, string value)
    {
        if (value.Length > 0)
        {
            builder.Append(key).Append('=').Append(value).Append('\n');
        }
    }

    // 画面の 1 行。保存済みの値と編集中の値を持ち、空欄なら既定値を QR に使う
    private sealed class SettingItem
    {
        private string saved = string.Empty;

        public string Key { get; }

        public string Placeholder { get; }

        public bool IsSecret { get; }

        public string? Value { get; set; }

        public string Effective => String.IsNullOrWhiteSpace(Value) ? Placeholder : Value.Trim();

        public bool IsDirty => (Value?.Trim() ?? string.Empty) != saved;

        public SettingItem(string key, string placeholder, bool isSecret)
        {
            Key = key;
            Placeholder = placeholder;
            IsSecret = isSecret;
        }

        public void Load(string value)
        {
            saved = value;
            Value = value;
        }
    }
}
