# 起動とテスト

## 🚀 起動方法

```
dotnet run --project src/Template.MobileServer.Web
```

- ポート構成(`appsettings.json` の `Kestrel:Endpoints`): **8080 = Web / API(HTTP/1.1)**、**9090 = gRPC(HTTP/2 h2c、アプリケーション用)**、**4317 = OTEL(HTTP/2 h2c、OTLP/gRPC の受信口)**、**4318 = OTEL(HTTP/1.1、OTLP/HTTP の受信口)**。ほかに **9464 = Prometheus の取得口**(`Prometheus:Uri`。サーバー自身のメトリクス。空なら開かない)
- gRPC のサービスと OTLP/HTTP のエンドポイントは `RequirePort`(`Infrastructure/Routing/PortRouting.cs`。`Connection.LocalPort` で判定する `PortMatcherPolicy`)でポートを限定している(チャット / サーバー情報 = 9090、OTLP/gRPC = 4317、OTLP/HTTP = 4318)。他のポートからの呼び出しは 404(gRPC クライアントには `Unimplemented`)
  (gRPC の平文 h2c は HTTP/1.1 と同居できないためポートを分離)
- Aspire を使う場合: `dotnet run --project src/Template.MobileServer.AppHost`
- データベース(SQLite)は起動時に `Assets/Data/Schema.sql` を実行して作成(`GenericAccessor.ExecuteSchemaAsync`)。サンプルデータは `Assets/Data/SampleData.sql`(手動)。ストレージディレクトリも起動時に作成。日時の列は UTC(管理画面はローカル時刻で表示)
- リバースプロキシの後ろに置くときは `ForwardedHeaders` の `KnownNetworks`(CIDR)/ `KnownProxies`(IP アドレス)にプロキシを書く(既定はループバックだけ。それ以外からの `X-Forwarded-*` は無視する)。`ForwardLimit` はプロキシの段数(既定 1)

## 🧪 テスト

```
dotnet run --project tests/Template.MobileServer.UnitTests
dotnet run --project tests/Template.MobileServer.IntegrationTests
```

- xunit.v3 + Microsoft Testing Platform(テストのプロジェクトは実行ファイル。`dotnet run` で実行する)
- `UnitTests` = Core のサービス・受信の処理・ワーカー・ハブ・管理画面の部品(bunit)
- `IntegrationTests` = `WebApplicationFactory` でサーバーを実際のポートに起動し(テストクラスごとに空きポートと一時フォルダーの DB・ストレージ)、API・gRPC・SignalR・OTLP/HTTP を端末と同じ形で呼ぶ
