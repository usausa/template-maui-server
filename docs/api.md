# API

## 🔌 API 一覧

| メソッド | パス | 認証 | 内容 |
|---|---|---|---|
| GET | `/api/server/time` | 匿名 | サーバー時刻 |
| POST | `/api/account/login` | 匿名 | Id のみで JWT 発行 |
| GET | `/api/secret/message` | JWT | 認証確認用メッセージ(JWT が要る唯一の API) |
| GET | `/api/data/list` | 匿名 | Data 一覧(モバイル契約)。`?offset=&size=`(size 1〜200)で範囲取得、応答に `total` |
| GET | `/api/data/{id}` | 匿名 | Data 取得 |
| POST | `/api/data` | 匿名 | Data 作成(重複 409 / 検証 400) |
| PUT | `/api/data/{id}` | 匿名 | Data 更新(404 / 409) |
| DELETE | `/api/data/{id}` | 匿名 | Data 削除(404) |
| PUT | `/api/device/{terminalId}` | 匿名 | 端末からの登録(本文は名前)。未登録なら登録して 201、登録済みなら名前を更新して 200。端末 ID は英数字・`-`・`_` の 64 文字以内(違反は 400) |
| POST | `/api/push` | 匿名 | 端末への通知の送信(本文は宛先の `terminalId`(省略で全端末)・`title`・`body`)。送った件数 `count` を返す。宛先の端末が無い(未登録か無効)なら 404、検証 400 |
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
| SignalR | `/hubs/push?terminalId=` | 匿名 | 端末への通知(下記) |

## 📱 モバイル契約の要点

- JSON は **camelCase**(クライアント Rester の既定と同じ)、null プロパティは省略
- 要求の JSON は厳しく読む: 未知のプロパティ・同じプロパティの重複・文字列で書いた数値は 400(要求に項目を足すときはサーバーを先に更新する)
- DateTime は `yyyy-MM-ddTHH:mm:ss.fffZ`(UTC 変換)で出力
- `Content-Encoding: gzip` のリクエストボディはサーバー側で自動展開(RequestDecompression)
- 要認証 API(`/api/secret/message`)は未認証時に 401 を返す
- ファイル不在は 404 の意味論を維持
- ストレージのパスは `Storage:Root` の下だけ(外を指すものと、ルートの下のシンボリックリンクやジャンクションを経由するものは 400)
