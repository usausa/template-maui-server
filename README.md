# Template project for Mobile Server

template-maui 系(maui / maui-blazor)の通信処理の対向となるサーバーテンプレート。
template-blazor-server をベースに、モバイル契約 API と管理画面を提供する。

## 機能一覧

- モバイル契約 API(Minimal API、camelCase JSON。認証確認用の 1 本だけ JWT Bearer 認証)
- ファイルストレージ API(簡易 FTP: 一覧 / ダウンロード / アップロード / 削除)
- gRPC チャット(双方向ストリーミング)+ サーバー情報(単項 RPC)。ポート 9090、認証なし
- テレメトリの受信口(OTLP のトレース / メトリクス / ログ。HTTP/protobuf = ポート 4318、gRPC = ポート 4317)。認証なし。受信した内容は端末ごとの SQLite ファイルに保存する。端末は受信で自動的に登録し(端末からの登録の API もある)、無効にした端末の受信は保存しない
- SignalR ハブ(端末の常時接続: サーバー状態の配信 / 端末状態の受信 / 通知の送信、認証なし)
- 端末への通知(独自プッシュ。SignalR ハブ `/hubs/push` で送り、未接続の端末宛ては `data.db` に溜めて接続したときに届ける。認証なし)
- 管理画面(Blazor Server + MudBlazor、認証なし)
- OpenAPI(開発時 `/swagger` / `/redoc`)、ヘルスチェック(`/health` / `/alive`)
- Serilog / OpenTelemetry / FeatureManagement / Aspire AppHost

## 構成(Web プロジェクト)

- `Endpoints/` = Minimal API、`Handlers/` = gRPC(proto は `Handlers/Protos/`)、`Telemetry/` = OTLP の受信口(HTTP と gRPC。proto は `Telemetry/Protos/` 直下の opentelemetry-proto v1.11.0。上流の `opentelemetry/proto/<種類>/v1/` の階層は使わず、`import` はファイル名だけに書き換えてある。生成型の名前空間は上流の `OpenTelemetry.Proto.*`。保存用の変換 `OtlpMapper`、端末ごとの DB の接続 `SqliteTelemetryDbProvider`、端末ごとの書き込みのロックと保存済みの Id `TelemetryStore`、端末の登録とテレメトリの要約のキャッシュ `TelemetryDeviceRegistry`、受信と登録の変更を画面へ知らせる `TelemetryBus` も置く。サーバー自身の計測は `Application/Telemetry/`)、`Hubs/` = SignalR、`Workers/` = 常駐処理、`Components/` = 管理画面(`Pages/` と `Dialogs/` = 業務のページ・ダイアログで基底は `AppPageBase`、`Shared/` = 汎用部品(`MessageBox` / `InputDialog` と `DialogServiceExtensions`)、`Telemetry/` = テレメトリの表示部品(小さな折れ線 `Sparkline` / 最新値のセル `MetricCell` / 件数のバッジ `CountBadge`)、`Layout/`。View まわりのヘルパー `ViewHelper`(テレメトリの値の表示と色を含む。`_Imports.razor` で static インポート)/ `Styles` / `AppComponentBase` / `AppPageBase` / `SnackbarExtensions` / バスの通知をまとめて描画する `RefreshTimer` は直下。razor の分岐と繰り返しは `@if` / `@foreach` を書かず Smart.Blazor の `Condition` / `ListItem`、式は code-behind のプロパティに寄せる)、`Assets/Data/` = スキーマ / サンプルデータの SQL
- 契約の DTO は使う側と同じファイルの先頭に置く: REST(`<対象><操作>Request` / `Response`、一覧の要素は `<対象>ListEntry`)は各 `Endpoints/*Endpoints.cs`、SignalR のメッセージ(`<内容>Message`)は `Hubs/MonitorHub.cs` / `PushHub.cs`、gRPC の生成型(単項は `Request` / `Reply`、ストリームは `Message`)は `Handlers` 名前空間
- `Services/` = アプリケーション固有の機能(チャットのハブ、端末の登録と通知、端末への通知の送信 `PushNotifier`)。サービス・ワーカーの設定は適用先と同じ場所の `*Option`(`Workers/ServerStatusWorkerOption` ← `ServerStatus` / `Workers/NotificationWorkerOption` ← `Notification` / `Telemetry/TelemetryReceiverOption` ← `TelemetryReceiver` / `Telemetry/TelemetryStorageOption` ← `TelemetryStorage` / `Workers/TelemetryRetentionWorkerOption` ← `TelemetryRetention` / `Workers/PushRetentionWorkerOption` ← `PushRetention` / Core の `FileStorageOption` ← `Storage`)、パイプラインの設定は `Settings/*Setting`
- `Application/` = アプリケーションの組み立てと横断的な定義。直下は汎用のヘルパー・定義だけ(DI 登録、`Log`、命名、ポリシー、`RequestHelper`)、`Log` や設定に依存するコンポーネントと Blazor の基盤側はサブフォルダ(`Telemetry/` = 計測とリクエストメトリクスのフィルター、`HealthChecks/`、`Authentication/` = JWT 発行と `JwtSetting`、`ExceptionHandling/` = API の未処理例外を ProblemDetails 500 に変換、`Circuits/` = 回線追跡、`Context/` = 処理時刻と実行ユーザーの `ServiceContext`)。ログメッセージ(`Log`)は使う名前空間ごとに置く(`Application/Log.cs` = 起動 / 回線 / リクエスト / API の未処理例外、`Workers/Log.cs`、`Hubs/Log.cs`、`Telemetry/Log.cs` = OTLP の受信、`Components/Log.cs` = ErrorBoundary)
- 処理時刻と実行ユーザー(`Core/Services/ServiceContext`)は Service 層が `ServiceContextProvider.Current` から読む(監査列 `CreatedAt` / `UpdatedAt`。`TimeProvider` は計測・期限・配信時刻など処理時刻以外だけ)。スコープは境界が開始する: API は `MapApiGroup` の `ServiceContextEndpointFilter`(ユーザーは JWT の sub、匿名は `anonymous`。OTLP/HTTP の受信口にも付ける)、管理画面はページの基底 `AppPageBase`(`Pages/_Imports.razor`。Service を呼ぶダイアログにも使う)がイベントと初期化を包む(認証なしなので `guest`)。それ以外のコンポーネントは `AppComponentBase` でスコープを持たない、gRPC は `ServiceContextInterceptor`(RPC 1 回。ストリーミングは RPC 全体)、SignalR は `ServiceContextHubFilter`(ハブメソッド 1 回)、ワーカー・起動処理・サーバーからの Push は `ApplicationServiceContextProvider.Begin(() => new ServiceContext(...))` を明示(起動時の端末のキャッシュの読み込み。ユーザーは `system`)。値は最初に読まれたときに 1 回だけ作る(読まない操作では作らない)、未開始で読むと例外(設計は `D:\GitHubTemplate\aspnet-operation-context.md`)
- `Infrastructure/` = `Application` / `Settings` / `Services` に依存せず他へ持ち出せる部品だけ(セキュリティヘッダー(`SecurityHeadersOption` で CSP を渡す。`{nonce}` は `CspNonce` に置換)、進捗ストリーム、Serilog エンリッチャー、通知バス、ポート限定のルーティング)。`StorageException` → 400 は `StorageEndpoints` のグループのフィルター(インライン)
- Core: `Accessors/` = Smart.Data.Accessor(SQL は `Sql/` に 1 メソッド 1 ファイル。書き方は example-maui-pos と同じ。列挙型は `DataProfile` の `EnumTextConverter` で名前の文字列にする)、`Services/` = 業務処理(`DatabaseService` = スキーマの実行、`DeviceService` = 端末の登録、`PushService` = 端末への通知の保存と未達の照会、`TelemetryService` = テレメトリの保存と照会。Core のサービスは状態を持たない)、`Models/Entity/` = テーブルの行(テーブル名は `[Name]`)、`Models/Views/` = 集計・結合の結果、`Models/Enums/` = 列挙型、`Models/Parameters/` = Service への入力(一覧の並び順 `DataSort`(列挙名 = 列名、先頭が既定)、テレメトリの保存のまとまり)、`Infrastructure/` = ストレージ / JSON / データの変換(列挙型と日時の文字列)/ 端末ごとのテレメトリの DB の interface、`Domain/` = 長さの定数、端末 ID の形式、ログの重大度の区切り

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
| PUT | `/api/device/{deviceId}` | 匿名 | 端末からの登録(本文は名前)。未登録なら登録して 201、登録済みなら名前を更新して 200。端末 ID は英数字・`-`・`_` の 64 文字以内(違反は 400) |
| POST | `/api/push` | 匿名 | 端末への通知の送信(本文は宛先の `deviceId`(省略で全端末)・`title`・`body`)。送った件数 `count` を返す。宛先の端末が無い(未登録か無効)なら 404、検証 400 |
| GET | `/api/storage/{**path}` | 匿名 | 末尾 `/` または空 = 一覧(名前/種別/サイズ/更新日)、それ以外 = ダウンロード |
| POST | `/api/storage/{**path}` | 匿名 | 生ボディ保存(親ディレクトリ自動作成、gzip 展開対応、本文サイズ上限なし) |
| DELETE | `/api/storage/{**path}` | 匿名 | ファイル / ディレクトリ(再帰)削除 |
| GET | `/api/test/error/{code}` | 匿名 | テスト用エラー(400/403/404/例外) |
| GET | `/api/test/delay/{timeout}` | 匿名 | テスト用遅延(ms) |
| GET | `/health` `/alive` | 匿名 | ヘルスチェック |
| POST | `/v1/traces` / `/v1/metrics` / `/v1/logs`(4318) | 匿名 | テレメトリの受信(OTLP/HTTP protobuf。下記) |
| gRPC | `chat.ChatRoom/Connect`(9090) | 匿名 | チャット双方向ストリーミング(ユーザー名は `ChatMessage.user`) |
| gRPC | `info.ServerInfo/GetServerTime`(9090) | 匿名 | サーバー時刻(Unix ミリ秒)。接続前の疎通確認用 |
| gRPC | `opentelemetry.proto.collector.trace.v1.TraceService/Export` / `...metrics.v1.MetricsService/Export` / `...logs.v1.LogsService/Export`(4317) | 匿名 | テレメトリの受信(OTLP/gRPC。下記) |
| SignalR | `/hubs/monitor` | 匿名 | 端末の常時接続(下記) |
| SignalR | `/hubs/push?deviceId=` | 匿名 | 端末への通知(下記) |

## 管理画面一覧

管理画面に認証はない。

| 画面 | ルート | 内容 |
|---|---|---|
| ダッシュボード | `/` | テレメトリのサマリ(受信中の端末・24 時間のエラーとクラッシュ・電池の少ない端末・受信件数の推移)、端末の一覧(状態・電池の残量と無線 LAN の電波のアイコン・CPU の横棒・メモリ・アプリケーション固有値の値 1・2 の横棒(0〜100)・エラーとクラッシュ・未達の通知の件数。値はホバーで出す)と管理(検索・追加・編集・削除)、通知の送信(端末の行 / 全端末。件名と本文のダイアログ)、直近のエラー(選ぶとその端末のログへ)。端末とテレメトリはキャッシュ、未達の件数は `data.db` から読み、受信と登録の変更、通知の送信と応答で更新する |
| テレメトリ | `/telemetry/{DeviceId?}` | 端末を選んでテレメトリを見る。見出し(状態・端末の情報・最新値)、範囲(15 分〜30 日。`range` クエリ)、メトリクスのグラフ(計器ごと、属性の組み合わせごとの線。既知の計器は名前と単位を整え、アプリケーション固有値 `application.custom.value{N}` は「値 N」)、トレース(一覧と絞り込み、ウォーターフォール、スパンの詳細、トレースのログ。`trace` クエリ)、ログ(重大度(`level` クエリ)・本文・トレースで絞り込み、行を開くと属性と例外のスタックトレース、続きを読み込む)。表示中の受信を足して時間軸を進める |
| データ | `/data` | MudDataGrid による CRUD |
| ファイル | `/files/{*path}` | ストレージブラウザ(階層ブラウズ / アップロード / フォルダ作成 / 削除) |
| チャット | `/chat` | チャット(gRPC クライアントとプロセス内ハブを共有、リアルタイム表示)。送信者名は入力欄(既定 `web`) |
| 端末 | `/devices` | SignalR で接続中の端末一覧(端末 ID / 機種 / 電池 / ネットワーク、リアルタイム更新)、通知の送信(全端末 / 端末指定)、切断 |
| QR | `/qr` | 設定 QR コード表示(template-maui の設定読取フォーマット互換)。値の編集と保存(`Setting` テーブル) |
| サーバー | `/server` | 簡易ステータス(サーバー時刻 / ストレージ使用量 / Data 件数 / 接続中の端末 / 接続中の画面) |

## 起動方法

```
dotnet run --project Template.MobileServer.Web
```

- ポート構成(`appsettings.json` の `Kestrel:Endpoints`): **8080 = Web / API(HTTP/1.1)**、**9090 = gRPC(HTTP/2 h2c、アプリケーション用)**、**4317 = OTEL(HTTP/2 h2c、OTLP/gRPC の受信口)**、**4318 = OTEL(HTTP/1.1、OTLP/HTTP の受信口)**
- gRPC のサービスと OTLP/HTTP のエンドポイントは `RequirePort`(`Infrastructure/Routing/PortRouting.cs`。`Connection.LocalPort` で判定する `PortMatcherPolicy`)でポートを限定している(チャット / サーバー情報 = 9090、OTLP/gRPC = 4317、OTLP/HTTP = 4318)。他のポートからの呼び出しは 404(gRPC クライアントには `Unimplemented`)
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

## 端末への通知(SignalR)

- ハブ: `/hubs/push`(`Hubs/PushHub.cs`)。認証なし。端末は接続の URL のクエリ `deviceId` で端末 ID を渡す(形式が違えば切る)。ハブは接続を端末 ID のグループに入れ、未達の通知を古い順に送る
- サーバー → 端末: `Receive(PushMessage)`(`Id` / `Title` / `Body` / `CreatedAt` = 送った日時(UTC))。端末 → サーバー: `Acknowledge(id)`(表示の後に呼ぶ。届いた日時を記録する)。応答の無いまま切れた通知は次の接続で送り直す(端末は同じ `Id` を 2 回出さない)
- 送信: `POST /api/push`(`Endpoints/PushEndpoints.cs`)と管理画面のダッシュボード(端末の行 / 全端末)が `Services/PushNotifier` を呼び、行を作ってから宛先の端末が接続中ならすぐに送る。送信と応答で未達が変わると `PushNotifier.Changed` で知らせる(ダッシュボードの未達の件数を読み直す)。行は `data.db` の `PushMessage`(宛先の端末ごとに 1 行。全端末宛ては送った時点の登録済みで有効な端末ごと)。件名は 100 文字、本文は 500 文字まで
- 保持期間(`PushRetention`、`Workers/PushRetentionWorker`): 起動の直後と `IntervalMinutes`(既定 60)ごとに、送ってから `Days`(既定 7)日を過ぎた通知を届いた / 届いていないにかかわらず削除する。端末の登録を削除すると、その端末宛ての通知も削除する
- ログ: 送信(宛先と件数)・接続(端末 ID と未達の件数)・切断は Information、形式の違う端末 ID の接続は Warning

## テレメトリの受信(OTLP)

- 受信口(OTLP 仕様 1.11.0、認証なし): OTLP/HTTP = `Telemetry/OtlpHttpEndpoints.cs` の `POST /v1/traces` / `/v1/metrics` / `/v1/logs`(4318 だけ)、OTLP/gRPC = `Telemetry/OtlpTraceHandler.cs` / `OtlpMetricsHandler.cs` / `OtlpLogsHandler.cs` の `Export`(4317 だけ)。受信の処理は共通の `Telemetry/OtlpReceiver.cs`
- 端末は OTLP/HTTP で送る。送信先は `http://<サーバー>:4318/`(QR の `OtelEndPoint`。端末がシグナルごとの `v1/traces` などを付ける)
- 受信した内容は端末ごとの SQLite ファイル `<TelemetryStorage:Root>/<端末 ID>.db` に保存する(既定の `Root` は実行フォルダーの `telemetry`)。端末は Resource の `device.id`、無ければ `app.installation.id`(英数字・`-`・`_` の 64 文字以内)。スキーマは `Assets/Data/TelemetrySchema.sql`(WAL。プロセスで最初に開くときに `user_version` で確かめる)。1 回の Export の 1 端末分を 1 トランザクションで保存し、送り直しで同じ内容(スパンの ID、系列と時刻、ログの時刻と内容のハッシュ)が届いても 1 件にする
- 端末を識別できないリソース、ID の長さが違うスパン、時刻の無い点とログは保存せず、`partial_success` の拒否件数と理由で返す。保存の失敗(DB・I/O)は HTTP = 503、gRPC = `UNAVAILABLE`(端末は送り直す)。HTTP は `Content-Type: application/x-protobuf` だけ受け(JSON は 415)、`Content-Encoding: gzip` の本文は展開する(壊れた本文は 400)。gRPC の gzip は gRPC の既定で展開する
- 端末の登録(`data.db` の `Device`。名前・グループ・メモ・有効): 未登録の端末は受信で登録する(名前の既定は機種)。端末からは `PUT /api/device/{deviceId}` で登録する(登録済みなら名前だけを更新する。グループ・メモ・有効は管理画面で変える)。無効の端末の受信は保存せず `partial_success` で返す(端末は送り直さない)
- 端末の登録とテレメトリの要約(最新値、直近 24 時間のエラーとクラッシュ、直近のエラー、1 分ごとの受信件数)は `TelemetryDeviceRegistry` がメモリに持つ(起動時に登録と全端末のファイルから作る。登録の無いファイルは登録する)。保存と登録の変更は `TelemetryBus` で画面へ知らせる
- 保持期間(`TelemetryRetention`、`Workers/TelemetryRetentionWorker`): 起動の直後と `IntervalMinutes`(既定 60)ごとに、ログ(`LogDays` = 7)・トレース(`TraceDays` = 7。トレースの開始で判定)・メトリクス(`MetricDays` = 30)の期限を過ぎた行を端末ごとに削除する。最後の受信から `DeviceDays`(30)を過ぎた端末は、テレメトリのファイルを削除する(登録は残る)。時刻は端末が付けた時刻で判定する
- ログ: 1 回ごとの受信は Debug(サービス名、端末、受けた件数と保存した件数)、端末を識別できないリソースは Warning、無効の端末は Debug、端末の自動登録は Information、保存の失敗は Error
- 受信上限は `TelemetryReceiver:MaxReceiveMessageSize`(既定 16 MiB、1〜16 MiB。HTTP は展開後の本文に適用し、超えたら 413。gRPC の既定は 4 MB。単項の呼び出しは Kestrel の `MaxRequestBodySize`(30 MB)も受けるため上限を 16 MiB にしている)
- サーバー自身のトレース(ASP.NET Core の計装)から受信口のパス(`/v1` と `/opentelemetry.proto.collector.` で始まるパス)を除く

## QR 設定フォーマット

`/qr` が生成する QR コードは行単位の `Key=Value` テキスト(template-maui の `SettingParser` 互換)。

```
ApiEndPoint=http://server:8080/
GrpcEndPoint=http://server:9090/
OtelEndPoint=http://server:4318/
AIServiceEndPoint=...
AIServiceKey=...
OllamaEndPoint=...
OllamaModel=...
SshHost=...
SshPort=22
SshUser=...
SshPassword=...
```

- キー: `ApiEndPoint` / `GrpcEndPoint` / `OtelEndPoint`(OTLP/HTTP の受信口)/ `AIServiceEndPoint` / `AIServiceKey` / `OllamaEndPoint` / `OllamaModel` / `SshHost` / `SshPort` / `SshUser` / `SshPassword`(`Components/Pages/QrPage.razor.cs` の `ClientSettingKeys`。空欄は出力しない、未知キーは端末側で無視。`SshPort` は `SshHost` があるときだけ)
- 接続先(`ApiEndPoint` / `GrpcEndPoint` / `OtelEndPoint`)はサーバー自身の URL から決まり保存しない(`GrpcEndPoint` / `OtelEndPoint` は同じホストに `Kestrel:Endpoints:Grpc:Url` / `Kestrel:Endpoints:OtelHttp:Url` のポート)
- それ以外の値は `Setting` テーブル(Key / Value / UpdatedAt。`SettingService` / `SettingAccessor`、起動時に作成)で管理し、`/qr` 画面の表で編集して「保存」する(空欄は行の削除、`SshPort` の空欄は 22。未保存の編集も QR には反映される)。キーやパスワードは DB ファイルに入るのでリポジトリには含めない(`*.db` は `.gitignore`)
