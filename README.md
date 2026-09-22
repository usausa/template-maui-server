# Template project for Mobile Server

template-maui 系(maui / maui-blazor)の通信処理の対向となるサーバーテンプレート。
template-blazor-server をベースに、モバイル契約 API と管理画面を提供する。

## 機能一覧

- モバイル契約 API(Minimal API、camelCase JSON。認証確認用の 1 本だけ JWT Bearer 認証)
- ファイルストレージ API(簡易 FTP: 一覧 / ダウンロード / アップロード / 削除)
- gRPC チャット(双方向ストリーミング)+ サーバー情報(単項 RPC)。ポート 9090、認証なし
- SignalR ハブ(端末の常時接続: サーバー状態の配信 / 端末状態の受信 / 通知の送信、認証なし)
- 管理画面(Blazor Server + MudBlazor、認証なし)
- OpenAPI(開発時 `/swagger` / `/redoc`)、ヘルスチェック(`/health` / `/alive`)
- Serilog / OpenTelemetry / FeatureManagement / Aspire AppHost

## 構成(Web プロジェクト)

- `Endpoints/` = Minimal API、`Handlers/` = gRPC(proto は `Handlers/Protos/`)、`Hubs/` = SignalR、`Workers/` = 常駐処理、`Components/` = 管理画面(View まわりのヘルパー `ViewHelper`(`_Imports.razor` で static インポート)/ `Styles` / `AppComponentBase` / `SnackbarExtensions` / `ErrorBoundaryLogger` は直下。razor の分岐と繰り返しは `@if` / `@foreach` を書かず Smart.Blazor の `Condition` / `ListItem`、式は code-behind のプロパティに寄せる)、`Assets/Data/` = スキーマ / サンプルデータの SQL
- 契約の DTO は使う側と同じファイルの先頭に置く: REST(`<対象><操作>Request` / `Response`、一覧の要素は `<対象>ListEntry`)は各 `Endpoints/*Endpoints.cs`、SignalR のメッセージ(`<内容>Message`)は `Hubs/MonitorHub.cs`、gRPC の生成型(単項は `Request` / `Reply`、ストリームは `Message`)は `Handlers` 名前空間
- `Services/` = アプリケーション固有の機能(チャットのハブ、端末の登録と通知)。サービス・ワーカーの設定は適用先と同じ場所の `*Option`(`Workers/ServerStatusWorkerOption` ← `ServerStatus` / `Workers/NotificationWorkerOption` ← `Notification` / Core の `FileStorageOption` ← `Storage`)、パイプラインの設定は `Settings/*Setting`
- `Application/` = アプリケーションの組み立てと横断的な定義。直下は汎用のヘルパー・定義だけ(DI 登録、`Log`、命名、ポリシー、`RequestHelper`)、`Log` や設定に依存するコンポーネントと Blazor の基盤側はサブフォルダ(`Telemetry/` = 計測とリクエストメトリクスのフィルター、`HealthChecks/`、`Authentication/` = JWT 発行と `JwtSetting`、`ExceptionHandling/` = API の未処理例外を ProblemDetails 500 に変換、`Circuits/` = 回線追跡、`Context/` = 処理時刻と実行ユーザーの `ServiceContext`)。ログメッセージ(`Log`)は使う名前空間ごとに置く(`Application/Log.cs` = 起動 / 回線 / リクエスト / API の未処理例外、`Workers/Log.cs`、`Hubs/Log.cs`、`Components/Log.cs` = ErrorBoundary)
- 処理時刻と実行ユーザー(`Core/Models/ServiceContext`)は Service 層が `ServiceContextProvider.Current` から読む(監査列 `CreatedAt` / `UpdatedAt`。`TimeProvider` は計測・期限・配信時刻など処理時刻以外だけ)。スコープは境界が開始する: API は `MapApiGroup` の `ServiceContextEndpointFilter`(ユーザーは JWT の sub、匿名は `anonymous`)、管理画面は `AppComponentBase` がイベントと初期化を包む(認証なしなので `guest`)、ワーカーや起動処理は `AmbientServiceContextProvider.Begin(ServiceContext.System(timeProvider))` を明示。未開始で読むと例外(設計は `D:\GitHubTemplate\aspnet-operation-context.md`)
- `Infrastructure/` = `Application` / `Settings` / `Services` に依存せず他へ持ち出せる部品だけ(セキュリティヘッダー(`SecurityHeadersOption` で CSP を渡す。`{nonce}` は `CspNonce` に置換)、進捗ストリーム、Serilog エンリッチャー、通知バス、ポート限定のルーティング)。`StorageException` → 400 は `StorageEndpoints` のグループのフィルター(インライン)
- Core: `Accessors/` = Smart.Data.Accessor(SQL は `Sql/` に 1 メソッド 1 ファイル。整形は template-maui-pos と同じ)、`Services/` = 業務処理(`DatabaseService` = スキーマの実行)、`Models/Parameters/` = 一覧の並び順(`DataSort`。列挙名 = 列名、先頭が既定)、`Infrastructure/` = ストレージ / JSON

## API 一覧

| メソッド | パス | 認証 | 内容 |
|---|---|---|---|
| GET | `/api/server/time` | 匿名 | サーバー時刻 |
| POST | `/api/account/login` | 匿名 | Id のみで JWT 発行 |
| GET | `/api/secret/message` | JWT | 認証確認用メッセージ(JWT が要る唯一の API) |
| GET | `/api/data/list` | 匿名 | Data 一覧(モバイル契約)。`?offset=&size=`(size 1〜200)で範囲取得、応答に `Total` |
| GET | `/api/data/{id}` | 匿名 | Data 取得 |
| POST | `/api/data` | 匿名 | Data 作成(重複 409 / 検証 400) |
| PUT | `/api/data/{id}` | 匿名 | Data 更新(404 / 409) |
| DELETE | `/api/data/{id}` | 匿名 | Data 削除(404) |
| GET | `/api/storage/{**path}` | 匿名 | 末尾 `/` または空 = 一覧(名前/種別/サイズ/更新日)、それ以外 = ダウンロード |
| POST | `/api/storage/{**path}` | 匿名 | 生ボディ保存(親ディレクトリ自動作成、gzip 展開対応、本文サイズ上限なし) |
| DELETE | `/api/storage/{**path}` | 匿名 | ファイル / ディレクトリ(再帰)削除 |
| GET | `/api/test/error/{code}` | 匿名 | テスト用エラー(400/403/404/例外) |
| GET | `/api/test/delay/{timeout}` | 匿名 | テスト用遅延(ms) |
| GET | `/health` `/alive` | 匿名 | ヘルスチェック |
| gRPC | `chat.ChatRoom/Connect`(9090) | 匿名 | チャット双方向ストリーミング(ユーザー名は `ChatMessage.user`) |
| gRPC | `info.ServerInfo/GetServerTime`(9090) | 匿名 | サーバー時刻(Unix ミリ秒)。接続前の疎通確認用 |
| SignalR | `/hubs/monitor` | 匿名 | 端末の常時接続(下記) |

## 管理画面一覧

管理画面に認証はない。

| 画面 | ルート | 内容 |
|---|---|---|
| ホーム | `/` | 簡易ステータス(サーバー時刻 / ストレージ使用量 / Data 件数) |
| データ | `/data` | MudDataGrid による CRUD |
| ファイル | `/files/{*path}` | ストレージブラウザ(階層ブラウズ / アップロード / フォルダ作成 / 削除) |
| チャット | `/chat` | チャット(gRPC クライアントとプロセス内ハブを共有、リアルタイム表示)。送信者名は入力欄(既定 `web`) |
| 端末 | `/devices` | SignalR で接続中の端末一覧(端末 ID / 機種 / 電池 / ネットワーク、リアルタイム更新)、通知の送信(全端末 / 端末指定)、切断 |
| QR | `/qr` | 設定 QR コード表示(template-maui の設定読取フォーマット互換)。値の編集と保存(`Setting` テーブル) |

## 起動方法

```
dotnet run --project Template.MobileServer.Web
```

- ポート構成(`appsettings.json` の `Kestrel:Endpoints`): **8080 = Web / API(HTTP/1.1)**、**9090 = gRPC(HTTP/2 h2c、アプリケーション用)**、**4317 = OTEL(HTTP/2 h2c、OTLP 受け口用。現状は未使用)**
- gRPC のサービスは `RequirePort`(`Infrastructure/Routing/PortRouting.cs`。`Connection.LocalPort` で判定する `PortMatcherPolicy`)で 9090 に限定している。他のポートからの呼び出しは 404(gRPC クライアントには `Unimplemented`)
  (gRPC の平文 h2c は HTTP/1.1 と同居できないためポートを分離)
- Aspire を使う場合: `dotnet run --project Template.MobileServer.AppHost`
- データベース(SQLite)は起動時に `Assets/Data/Schema.sql` を実行して作成(`GenericAccessor.ExecuteSchemaAsync`)。サンプルデータは `Assets/Data/SampleData.sql`(手動)。ストレージディレクトリも起動時に作成

## モバイル契約の要点

- JSON は **camelCase**(クライアント Rester の既定と同じ)、null プロパティは省略
- DateTime は `yyyy-MM-ddTHH:mm:ss.fffZ`(UTC 変換)で出力
- `Content-Encoding: gzip` のリクエストボディはサーバー側で自動展開(RequestDecompression)
- 要認証 API(`/api/secret/message`)は未認証時に 401 を返す
- ファイル不在は 404 の意味論を維持

## チャット(gRPC)

- proto: `Template.MobileServer.Web/Handlers/Protos/chat.proto`(`chat.ChatRoom/Connect`、双方向ストリーミング)
- 認証なし。`ChatMessage.user` はクライアントが指定(空なら `unknown`)、`timestamp` はサーバー時刻(Unix ミリ秒)
- 接続時にレスポンスヘッダーを即時送信(クライアントの接続確立検知用)、続いて直近 50 件の履歴を受信、以降は全参加者の発言をリアルタイム受信
- 管理画面 `/chat` は同じプロセス内ハブ(ChatService)に直結した**完全参加者**: gRPC クライアントの発言は `/chat` に表示され、`/chat` からの送信は全 gRPC クライアントへ配送される
- `Handlers/Protos/server.proto`(`info.ServerInfo/GetServerTime`、単項 RPC、匿名): サーバー時刻を Unix ミリ秒で返す。接続前の疎通確認用

## 端末の監視(SignalR)

- ハブ: `/hubs/monitor`(`Hubs/MonitorHub.cs`)。認証なし(接続は `ConnectionId` 単位で `DeviceRegistry` に登録し、端末の識別は `ReportDeviceStatus` の端末 ID)
- KeepAlive 15 秒 / ClientTimeout 30 秒(クライアントの `KeepAliveInterval` / `ServerTimeout` と対にする)
- 端末 → サーバー: `ReportDeviceStatus(DeviceStatusMessage)`(端末 ID / 機種 / OS / 電池 / ネットワーク)。接続中の端末は `DeviceRegistry` に保持し、管理画面 `/devices` にリアルタイム表示。`/devices` の「切断」は `HubCallerContext.Abort()` で接続を閉じる(端末には再接続なしの Close が届き、端末側は初回接続からやり直す)
- サーバー → 端末: `ServerStatus(ServerStatusMessage)`(`ServerStatus:Interval` ミリ秒ごと、既定 1 秒。プロセスの CPU 使用率(`Environment.CpuUsage` の差分、1 コア = 100%)/ ワーキングセット / 接続数。接続が無いときは配信しない)、`Notify(NotificationMessage)`(管理画面 `/devices` からの送信と、`NotificationBus` の通知(`Notification:Enable` の定期通知)の中継。中継は `MonitorNotifier` がバスを購読して行う)
- メッセージの DTO は `Hubs/MonitorHub.cs` の先頭(`DeviceStatusMessage` / `ServerStatusMessage` / `NotificationMessage`。時刻は `DateTimeOffset`)

## QR 設定フォーマット

`/qr` が生成する QR コードは行単位の `Key=Value` テキスト(template-maui の `SettingParser` 互換)。

```
ApiEndPoint=http://server:8080/
GrpcEndPoint=http://server:9090/
OtelEndPoint=...
AIServiceEndPoint=...
AIServiceKey=...
OllamaEndPoint=...
OllamaModel=...
ScpHost=...
ScpPort=22
ScpUser=...
ScpPassword=...
```

- キー: `ApiEndPoint` / `GrpcEndPoint` / `OtelEndPoint`(OTLP の受け口。本サーバーに OTEL 機能を追加する際に使う)/ `AIServiceEndPoint` / `AIServiceKey` / `OllamaEndPoint` / `OllamaModel` / `ScpHost` / `ScpPort` / `ScpUser` / `ScpPassword`(`Web/Settings/ClientSettingKeys.cs`。空欄は出力しない、未知キーは端末側で無視。`ScpPort` は `ScpHost` があるときだけ)
- 接続先(`ApiEndPoint` / `GrpcEndPoint` / `OtelEndPoint`)はサーバー自身の URL から決まり保存しない(`GrpcEndPoint` は同じホストに `Kestrel:Endpoints:Grpc:Url` のポート、`OtelEndPoint` は API と同じ URL)
- それ以外の値は `Setting` テーブル(Key / Value / UpdatedAt。`SettingService` / `SettingAccessor`、起動時に作成)で管理し、`/qr` 画面の表で編集して「保存」する(空欄は行の削除、`ScpPort` の空欄は 22。未保存の編集も QR には反映される)。キーやパスワードは DB ファイルに入るのでリポジトリには含めない(`*.db` は `.gitignore`)
