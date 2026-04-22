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
      "title": "夜友Radio #1",
      "date": "2026-04-25",
      "time": "22:00"
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
      "name": "パーソナリティー名",
      "description": "紹介文"
    }
  ]
}
```

---

### GET `/islive`

現在の放送状況を返します。内容は `database/islive.json` から読み込まれます。

**レスポンス例**

```json
{
  "isLive": true,
  "title": "夜友Radio #1"
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
  "Name": "リスナーA",
  "Type": "リクエスト",
  "Message": "〇〇をかけてください！"
}
```

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
> このドキュメントはCLaudeによる自動生成です