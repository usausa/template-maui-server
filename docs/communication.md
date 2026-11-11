# 端末との通信

## 💬 チャット(gRPC)

- proto: `src/Template.MobileServer.Web/Handlers/Protos/chat.proto`(`chat.ChatRoom/Connect`、双方向ストリーミング)
- 認証なし。`ChatMessage.user` はクライアントが指定(空なら `unknown`)、`timestamp` はサーバー時刻(Unix ミリ秒)
- 接続時にレスポンスヘッダーを即時送信(クライアントの接続確立検知用)、続いて直近 50 件の履歴を受信、以降は全参加者の発言をリアルタイム受信
- 管理画面 `/chat` は同じプロセス内ハブ(ChatService)に直結した**完全参加者**: gRPC クライアントの発言は `/chat` に表示され、`/chat` からの送信は全 gRPC クライアントへ配送される
- `Handlers/Protos/server.proto`(`info.ServerInfo/GetServerTime`、単項 RPC、匿名): サーバー時刻を Unix ミリ秒で返す。接続前の疎通確認用

## 📡 端末の監視(SignalR)

- ハブ: `/hubs/monitor`(`Hubs/MonitorHub.cs`)。認証なし(接続は `ConnectionId` 単位で `DeviceRegistry` に登録し、端末の識別は `ReportDeviceStatus` の端末 ID)
- KeepAlive 15 秒 / ClientTimeout 30 秒(クライアントの `KeepAliveInterval` / `ServerTimeout` と対にする)
- 端末 → サーバー: `ReportDeviceStatus(DeviceStatusMessage)`(端末 ID / 機種 / OS / 電池 / ネットワーク)。接続中の端末は `DeviceRegistry` に保持し、管理画面 `/notification` にリアルタイム表示。`/notification` の「切断」は `HubCallerContext.Abort()` で接続を閉じる(端末には再接続なしの Close が届き、端末側は初回接続からやり直す)
- サーバー → 端末: `ServerStatus(ServerStatusMessage)`(`ServerStatus:Interval` ミリ秒ごと、既定 1 秒。プロセスの CPU 使用率(`Environment.CpuUsage` の差分、1 コア = 100%)/ ワーキングセット / 接続数。接続が無いときは配信しない)、`Notify(NotificationMessage)`(管理画面 `/notification` からの送信と、`NotificationBus` の通知(`Notification:Enable` の定期通知)の中継。中継は `MonitorNotifier` がバスを購読して行う)
- メッセージの DTO は `Hubs/MonitorHub.cs` の先頭(`DeviceStatusMessage` / `ServerStatusMessage` / `NotificationMessage`。時刻は `DateTimeOffset`)

## 🔔 端末への通知(SignalR)

- ハブ: `/hubs/push`(`Hubs/PushHub.cs`)。認証なし。端末は接続の URL のクエリ `terminalId` で端末 ID を渡す(形式が違えば切る)。ハブは接続を端末 ID のグループに入れ、未達の通知を古い順に送る
- サーバー → 端末: `Receive(PushMessage)`(`Id` / `Title` / `Body` / `CreatedAt` = 送った日時(UTC))。端末 → サーバー: `Acknowledge(id)`(表示の後に呼ぶ。届いた日時を記録する)。応答の無いまま切れた通知は次の接続で送り直す(端末は同じ `Id` を 2 回出さない)
- 送信: `POST /api/push`(`Endpoints/PushEndpoints.cs`)と管理画面のダッシュボード(端末の行 / 全端末)が `Services/PushNotifier` を呼び、行を作ってから宛先の端末が接続中ならすぐに送る。送信と応答で未達が変わると `PushNotifier.Changed` で知らせる(ダッシュボードの未達の件数を読み直す)。行は `data.db` の `PushMessage`(宛先の端末ごとに 1 行。全端末宛ては送った時点の登録済みで有効な端末ごと)。件名は 100 文字、本文は 500 文字まで
- 保持期間(`PushRetention`、`Workers/PushRetentionWorker`): 起動の直後と `IntervalMinutes`(既定 60)ごとに、送ってから `Days`(既定 7)日を過ぎた通知を届いた / 届いていないにかかわらず削除する。端末の登録を削除すると、その端末宛ての通知も削除する
- ログ: 送信(宛先と件数)・接続(端末 ID と未達の件数)・切断は Information、形式の違う端末 ID の接続は Warning

## 📈 テレメトリの受信(OTLP)

- 受信口(OTLP 仕様 1.11.0、認証なし): OTLP/HTTP = `Telemetry/OtlpHttpEndpoints.cs` の `POST /v1/traces` / `/v1/metrics` / `/v1/logs`(4318 だけ)、OTLP/gRPC = `Telemetry/OtlpTraceHandler.cs` / `OtlpMetricsHandler.cs` / `OtlpLogsHandler.cs` の `Export`(4317 だけ)。受信の処理は共通の `Telemetry/OtlpReceiver.cs`
- 端末は OTLP/HTTP で送る。送信先は `http://<サーバー>:4318/`(QR の `OtelEndPoint`。端末がシグナルごとの `v1/traces` などを付ける)
- 受信した内容は端末ごとの SQLite ファイル `<TelemetryStorage:Root>/<端末 ID>.db` に保存する(既定の `Root` は実行フォルダーの `telemetry`)。端末は Resource の `app.installation.id`(端末 ID。英数字・`-`・`_` の 64 文字以内)。スキーマは `Assets/Data/TelemetrySchema.sql`(WAL。端末ごとにプロセスで最初の保存の前に `user_version` で確かめる)。1 回の Export の 1 端末分を 1 トランザクションで保存し、送り直しで同じ内容(スパンの ID、系列と時刻、ログの時刻と内容のハッシュ)が届いても 1 件にする
- 端末を識別できないリソース、ID の長さが違うスパン、時刻の無い点とログは保存せず、`partial_success` の拒否件数と理由で返す。保存の失敗(DB・I/O)は HTTP = 503、gRPC = `UNAVAILABLE`(端末は送り直す)。HTTP は `Content-Type: application/x-protobuf` だけ受け(メディアタイプで比べ、パラメーターは見ない。JSON は 415)、`Content-Encoding: gzip` の本文は展開する(壊れた本文は 400)。gRPC の gzip は gRPC の既定で展開する
- 端末の登録(`data.db` の `Device`。主キーは端末 ID(`TerminalId`。アプリのインストールごとに一意の値で、端末そのものの ID ではない)。名前・グループ・メモ・有効): 未登録の端末は受信で登録する(名前の既定は機種)。端末からは `PUT /api/device/{terminalId}` で登録する(登録済みなら名前だけを更新する。グループ・メモ・有効は管理画面で変える)。無効の端末の受信は保存せず `partial_success` で返す(端末は送り直さない)
- 端末の登録とテレメトリの要約(最新値、直近 24 時間のエラーとクラッシュの件数、新しい順に 20 件のエラー(読むときに直近 24 時間に絞る)、1 分ごとの受信件数)は `TelemetryDeviceRegistry` がメモリに持つ(起動時に登録と全端末のファイルから作る。登録の無いファイルは登録する)。保存と登録の変更は `TelemetryBus` で画面へ知らせる
- 保持期間(`TelemetryRetention`、`Workers/TelemetryRetentionWorker`): 起動の直後と `IntervalMinutes`(既定 60)ごとに、ログ(`LogDays` = 7)・トレース(`TraceDays` = 7。トレースの開始で判定)・メトリクス(`MetricDays` = 30)の期限を過ぎた行を端末ごとに削除する。最後の受信から `DeviceDays`(30)を過ぎた端末は、テレメトリのファイルを削除する(登録は残る)。時刻は端末が付けた時刻で判定する
- ログ: 1 回ごとの受信は Debug(サービス名、端末、受けた件数と保存した件数)、端末を識別できないリソースは Warning、無効の端末は Debug、端末の自動登録は Information、保存の失敗は Error
- 受信上限は `TelemetryReceiver:MaxReceiveMessageSize`(既定 16 MiB、1〜16 MiB。HTTP は展開後の本文に適用し、超えたら 413。gRPC の既定は 4 MB。単項の呼び出しは Kestrel の `MaxRequestBodySize`(30 MB)も受けるため上限を 16 MiB にしている)
- サーバー自身のトレース(ASP.NET Core の計装)から受信口のパス(`/v1` と `/opentelemetry.proto.collector.` で始まるパス)を除く
