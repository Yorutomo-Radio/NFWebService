# 🌙 夜友Radio [NightFriend] — NFWebService 技術ドキュメント

> リポジトリ: [github.com/Yorutomo-Radio/NFWebService](https://github.com/Yorutomo-Radio/NFWebService)  
> 作成日: 2026年4月22日

---

## 1. プロジェクト概要

NFWebService（NightFriend Web Service）は、インターネットラジオ「夜友Radio」の公式ウェブサイトを提供するウェブサービスです。Docker コンテナを基盤とした構成で、フロントエンド・バックエンド・データベースを分離して運用します。

### 技術スタック

| レイヤー | 技術 | 詳細 |
|---|---|---|
| フロントエンド | HTML / CSS / JavaScript | Nginx で静的ファイルを配信 |
| バックエンド | C# (.NET) | REST API サーバー |
| データベース | SQLite | ファイルベース DB (yorutomo.db) |
| リバースプロキシ | Nginx | 静的ファイル配信 & ルーティング |
| コンテナ基盤 | Docker / Docker Compose | マルチコンテナ構成 |

---

## 2. リポジトリ構成

```
NFWebService/
├── backend/          # C# バックエンド API
├── database/         # SQLite データベースファイル・JSON データ
├── frontend/
│   └── main/         # フロントエンド静的ファイル (HTML/CSS/JS)
├── nginx/            # Nginx 設定ファイル
├── .gitignore
├── docker-compose.yml
├── readme.md
└── yorutomo_radio.sln  # Visual Studio ソリューションファイル
```

### 各ディレクトリの役割

| ディレクトリ | 説明 |
|---|---|
| `backend/` | C# (.NET) で実装されたバックエンド API。Dockerfile を含み、Docker ビルド対象。 |
| `database/` | SQLite データベース (yorutomo.db) と API が読み込む JSON ファイルを格納。コンテナのボリュームとしてマウントされる。 |
| `frontend/main/` | HTML・CSS・JavaScript で構成された静的ウェブページ。Nginx コンテナを通じて配信される。 |
| `nginx/` | Nginx の設定ファイル (nginx.conf) を格納。フロントエンドコンテナの設定として読み込まれる。 |

---

## 3. Docker 構成

本サービスは Docker Compose によるマルチコンテナ構成で動作します。

### コンテナ一覧

| コンテナ名 | ベースイメージ | ポート | 役割 |
|---|---|---|---|
| `frontend_main` | nginx:alpine | 35500 → 80 | 静的ファイル配信 (HTML/CSS/JS) |
| `backend` | カスタム (./backend) | 35501 → 8080 | REST API サーバー (C#) |

### 環境変数

| 変数名 | 例 | 説明 |
|---|---|---|
| `DB_PATH` | `/app/database/yorutomo.db` | SQLite データベースのパス |
| `DISCORD_WEBHOOK_URL` | `https://discord.com/api/webhooks/...` | Discord 通知用の Webhook URL |

> [!WARNING]
> `DISCORD_WEBHOOK_URL` は `.env` ファイルに記載し、リポジトリに含めないよう `.gitignore` で除外してください。

### ボリューム構成

| ホストパス | コンテナパス | 用途 |
|---|---|---|
| `./nginx/nginx.conf` | `/etc/nginx/nginx.conf` | Nginx 設定 (読み取り専用) |
| `./frontend/main` | `/usr/share/nginx/html` | フロントエンド静的ファイル |
| `./frontend/voice` | `/usr/share/nginx/html/voice` | 音声ファイル |
| `./database` | `/app/database` | SQLite DB・JSON ファイルの永続化 |

---

## 4. セットアップ・起動手順
>
> [!IMPORTANT]
> Docker および Docker Compose がインストールされている必要があります。

### 前提条件

- Docker Engine (最新版推奨)
- Docker Compose (v2 以上)
- Git

### 初回セットアップ

1. リポジトリをクローンする

```bash
git clone https://github.com/Yorutomo-Radio/NFWebService.git
cd NFWebService
```

1. 環境変数ファイルを作成する

```bash
echo "DISCORD_WEBHOOK_URL=https://discord.com/api/webhooks/..." > .env
```

1. コンテナをビルドして起動する

```bash
docker compose up -d --build
```

### 運用コマンド

| 操作 | コマンド |
|---|---|
| 起動 (再ビルドあり) | `docker compose up -d --build` |
| 停止 | `docker compose down` |
| 再起動 | `docker compose restart` |
| ログ確認 (全コンテナ) | `docker compose logs -f` |
| ログ確認 (フロントエンド) | `docker compose logs -f frontend_main` |
| ログ確認 (バックエンド) | `docker compose logs -f backend` |
| コンテナ状態確認 | `docker compose ps` |

---

## 5. アクセス情報

| サービス | URL | 説明 |
|---|---|---|
| フロントエンド | <http://localhost:35500> | 夜友Radio ウェブサイト |
| バックエンド API | <http://localhost:35501> | C# REST API エンドポイント |

---

## 6. API エンドポイント

ベース URL: `http://localhost:35501`

CORS の許可オリジン: `https://yorutomo-radio.com` / `http://localhost:35500`

---

### GET `/version`

バックエンドのバージョン情報を返します。

**レスポンス例**

```json
{
  "message": "beta-v1.00"
}
```

---

### GET `/program`

放送予定を返します。内容は `database/program.json` から読み込まれます。

**レスポンス例**

```json
{
    "programs": [
        {
            "id": 4, // ID
            "date": "04/22", // 日付
            "time": "22:00", // 時間
            "title": "夜友ラジオ β版 第1回", // タイトル
            "description": "夜友ラジオが遂に始動準備へ、初のOBT第1回目", // 説明欄
            "host": "<未定>" // ホスト(パーソナリティー)
        }
    ]
}
```

---

### GET `/hosts`

パーソナリティー情報を返します。内容は `database/hosts.json` から読み込まれます。

**レスポンス例**

```json
{
    "hosts": [
        {
            "name": "めだころ", // 名前
            "description": "夜友ラジオのメインパーソナリティー。ここに説明が来る", // 説明
            "image": "../assets/images/profiles/medakoro", // フロント側の画像パス
            "twitter": "https://x.com/medakoro0321" // TwitterURL
        },
        // 略...
    ]
}
```

---

### GET `/islive`

現在の放送状況を返します。内容は `database/islive.json` から読み込まれます。

**レスポンス例**

```json
{
    "islive": false, // ライブ中か? 外部GUIソフトから操作可能
    "title": "夜友ラジオ β版 第1回", // タイトル
    "start": "2026-04-17T22:00:00", // 開始時刻
    "url": "" // 配信URL
}

```

---

### POST `/letter`

お便りを受信し、SQLite に保存したうえで Discord Webhook に通知します。

**リクエストボディ** (JSON)

| フィールド | 型 | 説明 |
|---|---|---|
| `Name` | string | 投稿者のラジオネーム |
| `Type` | string | お便りの種類 (例: リクエスト、近況など) |
| `Message` | string | お便りの本文 |

**リクエスト例**

```json
{
  "name": "めだころ", // 名前
  "type": "request", // お便りの種類、種類は以下の通り
  "message": "次のラジオでこの音楽を流してほしいです!" // 本文
}
```

**お便りの種類**

| タイプ(ID) | クライアント側 | 説明 |
|---|---|---|
| `normal` | 夜風便り | 一般的なお便り種別 |
| `favorite` | すきって言わせて | ハマっているものや推し |
| `request` | リクエスト | (省略) |
| `advice` | 夜のお悩み放送局 | 相談やモヤモヤ等 |
| `vrchat` | 今週のVRC通信 | VRChat上での話 |
| `voice` | ボイス | シチュボ依頼 |

**レスポンス例**

```json
{
  "success": true
}
```

**処理の流れ**

1. リクエストボディを受け取る
2. SQLite の `letters` テーブルに保存する（テーブルが存在しない場合は自動作成）
3. Discord Webhook に Embed メッセージで通知する

**`letters` テーブル構造**

| カラム | 型 | 説明 |
|---|---|---|
| `id` | INTEGER | 主キー (自動採番) |
| `name` | TEXT | ラジオネーム |
| `type` | TEXT | お便りの種類 |
| `message` | TEXT | 本文 |
| `created_at` | TEXT | 受信日時 (JST, `yyyy-MM-dd HH:mm:ss`) |

> [!NOTE]
> このドキュメントはClaudeによる自動生成です
