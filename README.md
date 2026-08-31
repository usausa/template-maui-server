# Template project for Mobile Server

template-maui 系(maui / maui-blazor)の通信処理の対向となるサーバーテンプレート。
template-blazor-server をベースに、モバイル契約 API(JWT)と管理画面(Cookie 認証)を提供する。

## 機能一覧

- モバイル契約 API(Minimal API、JWT Bearer 認証、PascalCase JSON)
- ファイルストレージ API(簡易 FTP: 一覧 / ダウンロード / アップロード / 削除)
- gRPC チャット(双方向ストリーミング、JWT Bearer 認証、ポート 8084)
- 管理画面(Blazor Server + MudBlazor、Cookie 認証)
- OpenAPI(開発時 `/swagger` / `/redoc`)、ヘルスチェック(`/health` / `/alive`)
- Serilog / OpenTelemetry / FeatureManagement / Aspire AppHost

## API 一覧

| メソッド | パス | 認証 | 内容 |
|---|---|---|---|
| GET | `/api/server/time` | 匿名 | サーバー時刻 |
| POST | `/api/account/login` | 匿名 | Id のみで JWT 発行(Account テーブル照合なし) |
| GET | `/api/secret/message` | JWT | 認証確認用メッセージ |
| GET | `/api/data/list` | 匿名 | Data 一覧(モバイル契約) |
| GET | `/api/data/{id}` | JWT | Data 取得 |
| POST | `/api/data` | JWT | Data 作成(重複 409 / 検証 400) |
| PUT | `/api/data/{id}` | JWT | Data 更新(404 / 409) |
| DELETE | `/api/data/{id}` | JWT | Data 削除(404) |
| GET | `/api/storage/{**path}` | 匿名 | 末尾 `/` または空 = 一覧(名前/種別/サイズ/更新日)、それ以外 = ダウンロード |
| POST | `/api/storage/{**path}` | 匿名 | 生ボディ保存(親ディレクトリ自動作成、gzip 展開対応) |
| DELETE | `/api/storage/{**path}` | 匿名 | ファイル / ディレクトリ(再帰)削除 |
| GET | `/api/test/time` | 匿名 | テスト用時刻 |
| GET | `/api/test/error/{code}` | 匿名 | テスト用エラー(400/403/404/例外) |
| GET | `/api/test/delay/{timeout}` | 匿名 | テスト用遅延(ms) |
| POST | `/auth/login` `/auth/logout` | フォーム / Cookie | 管理画面用ログイン / ログアウト |
| GET | `/health` `/alive` | 匿名 | ヘルスチェック |
| gRPC | `chat.ChatRoom/Connect`(8084) | JWT | チャット双方向ストリーミング(metadata `authorization: Bearer ...`) |

## 管理画面一覧

| 画面 | ルート | 認証 | 内容 |
|---|---|---|---|
| ログイン | `/login` | 匿名 | 静的 SSR フォーム |
| ホーム | `/` | Cookie | 簡易ステータス(サーバー時刻 / ストレージ使用量 / Data 件数) |
| データ | `/data` | Cookie | MudDataGrid による CRUD(削除は Administrator ロール) |
| ファイル | `/files/{*path}` | Cookie | ストレージブラウザ(階層ブラウズ / アップロード / フォルダ作成 / 削除) |
| チャット | `/chat` | Cookie | チャット(gRPC クライアントとプロセス内ハブを共有、リアルタイム表示) |
| QR | `/qr` | Cookie | 設定 QR コード表示(template-maui の設定読取フォーマット互換) |

## 起動方法

```
dotnet run --project Template.MobileServer.Web
```

- ポート構成(`appsettings.json` の `Kestrel:Endpoints`): **8081 = Web / API(HTTP/1.1)**、**8084 = gRPC(HTTP/2 h2c)**
  (gRPC の平文 h2c は HTTP/1.1 と同居できないためポートを分離)
- Aspire を使う場合: `dotnet run --project Template.MobileServer.AppHost`
- データベース(SQLite)とストレージディレクトリは起動時に自動作成

## 初期アカウント(管理画面)

- ID: `admin` / パスワード: `Auth:InitialPassword`(既定 `admin`)
- **運用時は `Auth:InitialPassword` を必ず変更すること**(初回起動時のアカウント作成に使用)

## モバイル契約の要点

- JSON は **PascalCase**(`PropertyNamingPolicy = null`。クライアント Rester 既定との契約)、null プロパティは省略
- DateTime は `yyyy-MM-ddTHH:mm:ss.fffZ`(UTC 変換)で出力
- `Content-Encoding: gzip` のリクエストボディはサーバー側で自動展開(RequestDecompression)
- `/api` 配下は未認証時に 401/403 を返す(ログインページへのリダイレクトなし)
- ファイル不在は 404 の意味論を維持

## チャット(gRPC)

- proto: `Template.MobileServer.Web/Protos/chat.proto`(`chat.ChatRoom/Connect`、双方向ストリーミング)
- 認証: JWT Bearer(`/api/account/login` で取得したトークンを metadata `authorization: Bearer ...` で送信)
- `ChatMessage.user` はサーバー側で JWT のユーザー名に上書き、`timestamp` はサーバー時刻(Unix ミリ秒)
- 接続時にレスポンスヘッダーを即時送信(クライアントの接続確立検知用)、続いて直近 50 件の履歴を受信、以降は全参加者の発言をリアルタイム受信
- 管理画面 `/chat` は同じプロセス内ハブ(ChatService)に直結した**完全参加者**: gRPC クライアントの発言は `/chat` に表示され、`/chat` からの送信は全 gRPC クライアントへ配送される

## チャットクライアント(WPF サンプル)

`Template.MobileServer.ChatClient` は gRPC チャットの対向クライアントサンプル(MAUI への移植を想定した層構造)。

```
dotnet run --project Template.MobileServer.ChatClient
```

既定値: Server = `http://localhost:8081/`、gRPC = `http://localhost:8084/`、User = `user`。

### 層構造

| 層 | ファイル | 依存 |
|---|---|---|
| 通信コア | `Chat/ChatClient.cs` ほか `Chat/` 一式 | Grpc.Net.Client のみ(**WPF 参照なし**) |
| ViewModel | `MainWindowViewModel.cs` | Smart.Mvvm(WPF 型に非依存、UI マーシャリングは SynchronizationContext) |
| View | `MainWindow.xaml` / `App.xaml` | WPF のみ |

通信コア(`ChatClient`)の仕様:

- `ConnectAsync` = login(REST)で JWT 取得 → gRPC 双方向ストリーム接続(戻りの Task は初回接続の成否確定まで)
- 切断・接続失敗時は指数バックオフ(1→2→…→最大 30 秒)で自動再接続、再接続時は JWT を再取得
- 送信はキュー経由で直列化。**切断中の送信はキューに保持され、再接続後に自動配送**(モバイル回線の断続を想定)
- 受信・状態変化はイベント通知(バックグラウンドスレッドから発火)

### MAUI への移植手順

1. `Chat/` フォルダの 5 ファイルを**そのままコピー**(namespace の変更のみ。System.Windows 系の参照なし)
2. csproj に `Grpc.Net.Client` / `Google.Protobuf` / `Grpc.Tools` を追加し、proto を共有参照:
   `<Protobuf Include="..\Template.MobileServer.Web\Protos\chat.proto" GrpcServices="Client" />`
3. `MainWindowViewModel.cs` をコピーして ViewModel を移植。Smart.Mvvm の対応関係:
   - `ExtendViewModelBase` / `MakeAsyncCommand` / `MakeDelegateCommand`: WPF = `Usa.Smart.Windows.Extensions`(Smart.Windows.ViewModels)⇔ MAUI = `Usa.Smart.Maui.Extensions`(Smart.Maui.ViewModels)。**同名・同 API のため using の読み替えのみ**
   - `[ObservableProperty]` / `[ObservableGeneratorOption]`: 共通(`Usa.Smart.Mvvm`)
   - UI スレッドへのマーシャリングは SynchronizationContext 経由のため Dispatcher 依存なし(そのまま動作)
4. View(XAML)のみ MAUI で作り直す(CollectionView + Entry + Button 等。コードビハインドの自動スクロールも View 側で実装)
5. Android エミュレータからの接続先は `localhost` ではなく `10.0.2.2` を指定

## QR 設定フォーマット

`/qr` が生成する QR コードは行単位の `Key=Value` テキスト(template-maui の `SettingParser` 互換)。

```
ApiEndPoint=http://server:8081/
AIServiceEndPoint=...
AIServiceKey=...
```

- キー: `ApiEndPoint` / `MonitorEndPoint` / `AIServiceEndPoint` / `AIServiceKey`(空欄は出力しない、未知キーは端末側で無視)
- 既定値としてサーバー自身の URL を `ApiEndPoint` に設定
