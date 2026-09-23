namespace Template.MobileServer.Web.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;

using MudBlazor;

using QRCoder;

public static class ClientSettingKeys
{
    // ReSharper disable InconsistentNaming
    public const string ApiEndPoint = "ApiEndPoint";
    public const string GrpcEndPoint = "GrpcEndPoint";
    public const string OtelEndPoint = "OtelEndPoint";

    public const string AIServiceEndPoint = "AIServiceEndPoint";
    public const string AIServiceKey = "AIServiceKey";

    public const string OllamaEndPoint = "OllamaEndPoint";
    public const string OllamaModel = "OllamaModel";

    public const string ScpHost = "ScpHost";
    public const string ScpPort = "ScpPort";
    public const string ScpUser = "ScpUser";
    public const string ScpPassword = "ScpPassword";
    // ReSharper restore InconsistentNaming

    public static readonly string[] Stored = [AIServiceEndPoint, AIServiceKey, OllamaEndPoint, OllamaModel, ScpHost, ScpPort, ScpUser, ScpPassword];
}

public sealed partial class QrPage
{
    private const string GrpcEndpointConfigurationKey = "Kestrel:Endpoints:Grpc:Url";
    private const string OtelEndpointConfigurationKey = "Kestrel:Endpoints:Otel:Url";

    private readonly List<KeyValuePair<string, string>> connections = [];

    private readonly List<SettingItem> items = [];

    private string qrText = string.Empty;

    private string qrImage = string.Empty;

    private bool HasQrImage => !String.IsNullOrEmpty(qrImage);

    private bool IsDirty => items.Any(static x => x.IsDirty);

    //--------------------------------------------------------------------------------
    // Property
    //--------------------------------------------------------------------------------

    [Inject]
    public required NavigationManager Navigation { get; set; }

    [Inject]
    public required IConfiguration Configuration { get; set; }

    [Inject]
    public required SettingService SettingService { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    protected override Task OnInitializedAsync()
    {
        connections.Add(new(ClientSettingKeys.ApiEndPoint, Navigation.BaseUri));
        connections.Add(new(ClientSettingKeys.GrpcEndPoint, MakeGrpcEndPoint(Navigation.BaseUri, Configuration[GrpcEndpointConfigurationKey])));
        connections.Add(new(ClientSettingKeys.OtelEndPoint, MakeGrpcEndPoint(Navigation.BaseUri, Configuration[OtelEndpointConfigurationKey])));

        foreach (var key in ClientSettingKeys.Stored)
        {
            items.Add(new SettingItem(key, key == ClientSettingKeys.ScpPort ? "22" : string.Empty, key is ClientSettingKeys.AIServiceKey or ClientSettingKeys.ScpPassword));
        }

        return ReloadAsync();
    }

    //--------------------------------------------------------------------------------
    // Setting
    //--------------------------------------------------------------------------------

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

    //--------------------------------------------------------------------------------
    // Qr
    //--------------------------------------------------------------------------------

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

    private static void AppendValue(StringBuilder builder, string key, string value)
    {
        if (value.Length > 0)
        {
            builder.Append(key).Append('=').Append(value).Append('\n');
        }
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

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

    //--------------------------------------------------------------------------------
    // Item
    //--------------------------------------------------------------------------------

    private sealed class SettingItem
    {
        private string saved = string.Empty;

        public string Key { get; }

        public string Placeholder { get; }

        public bool IsSecret { get; }

        public InputType InputType { get; }

        public Dictionary<string, object> Attributes { get; }

        public string? Value { get; set; }

        public string Effective => String.IsNullOrWhiteSpace(Value) ? Placeholder : Value.Trim();

        public bool IsDirty => (Value?.Trim() ?? string.Empty) != saved;

        public SettingItem(string key, string placeholder, bool isSecret)
        {
            Key = key;
            Placeholder = placeholder;
            IsSecret = isSecret;
            InputType = isSecret ? InputType.Password : InputType.Text;
            Attributes = new Dictionary<string, object> { ["data-key"] = key };
        }

        public void Load(string value)
        {
            saved = value;
            Value = value;
        }
    }
}
