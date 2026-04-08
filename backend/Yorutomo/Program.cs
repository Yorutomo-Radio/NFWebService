using System.Text.Json;
using Microsoft.Data.Sqlite;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using Discord;
using Discord.Webhook;

var version = "beta-v1.00";
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
            "https://yorutomo-radio.com",
            "http://100.98.216.74:35500",
            "http://localhost:35500"
        )
        .WithMethods("GET", "POST")
        .WithHeaders("Content-Type");
    });
});

var app = builder.Build();
app.UseCors();


const bool isDebug = false; // デバッグモードフラグ

// お便り受信API [POST /letter]
app.MapPost("/letter", (LetterRequest req) =>
{
    InsertLetter(req.Name, req.Type, req.Message);
    sendDiscordWebhook(req.Name, req.Type, req.Message);
    return Results.Ok(new { success = true });
});

// 放送予定取得エンドポイント [GET /program]
app.MapGet("/program", () =>
{
    string data = readJsonFile("program.json");
    // 文字列を JSON オブジェクトとしてパース
    var jsonObject = JsonSerializer.Deserialize<object>(data);
    return Results.Ok(jsonObject);
});

// パーソナリティーエンドポイント [GET /hosts]
app.MapGet("/hosts", () =>
{
    string data = readJsonFile("hosts.json");
    // 文字列を JSON オブジェクトとしてパース
    var jsonObject = JsonSerializer.Deserialize<object>(data);
    return Results.Ok(jsonObject);
});

// 現在の放送情報を取得 [GET /islive]
app.MapGet("/islive", () =>
{
    string data = readJsonFile("islive.json");
    // 文字列を JSON オブジェクトとしてパース
    var jsonObject = JsonSerializer.Deserialize<object>(data);
    return Results.Ok(jsonObject);
});

// バージョン取得エンドポイント [GET /version]
app.MapGet("/version", () => new { message = version });


/// <summary>
/// Jsonファイルを読み込み、内容を文字列として返す
/// </summary>
/// <param name="fileName">ファイル名</param>
/// <returns>内容</returns>
/// <exception cref="FileNotFoundException">ファイルが見つからない</exception>
static string readJsonFile(string fileName)
{
    string filePath = Path.Combine("/app/database", fileName);

    if (File.Exists(filePath))
    {
        return File.ReadAllText(filePath);
    }

    throw new FileNotFoundException(
        $"[Error] File not found: {filePath}", filePath);
}

/// お便りをSQLiteデータベースに保存する
static void InsertLetter(string name, string type, string message)
{
    string dbPath = Environment.GetEnvironmentVariable("DB_PATH") ?? "/data/yorutomo.db";

    using var connection = new SqliteConnection($"Data Source={dbPath}");
    connection.Open();

    // テーブルがなければ作成
    var createCmd = connection.CreateCommand();
    createCmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS letters (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT,
            type TEXT,
            message TEXT,
            created_at TEXT
        )";
    createCmd.ExecuteNonQuery();

    // 挿入
    var insertCmd = connection.CreateCommand();
    insertCmd.CommandText = @"
        INSERT INTO letters (name, type, message, created_at)
        VALUES ($name, $type, $message, $createdAt)";
    // ブレースホルダーを使用してSQLiを防止
    insertCmd.Parameters.AddWithValue("$name", name);
    insertCmd.Parameters.AddWithValue("$type", type);
    insertCmd.Parameters.AddWithValue("$message", message);
    insertCmd.Parameters.AddWithValue("$createdAt",
        TimeZoneInfo.ConvertTime(DateTime.Now,
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo"))
        .ToString("yyyy-MM-dd HH:mm:ss"));
    insertCmd.ExecuteNonQuery();
}

static async Task sendDiscordWebhook(string name, string type, string message)
{
    string WebhookUrl = "";
    
    if (!isDebug)
    {
        // 本番環境のWebhook URL
        WebhookUrl = Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL");
    }
    else
    {
        WebhookUrl = Environment.GetEnvironmentVariable("TEST_DISCORD_WEBHOOK_URL");
    }

    if (string.IsNullOrEmpty(WebhookUrl))
    {
        Console.WriteLine("[Error] Discord Webhook URL is not set.");
        return;
    }

    // URLからIDとTokenを取り出す
    var parts = WebhookUrl.Split('/');
    ulong webhookId = ulong.Parse(parts[^2]);
    string webhookToken = parts[^1];

    using var client = new DiscordWebhookClient(webhookId, webhookToken);

    var embed = new EmbedBuilder()
    .WithTitle($"ラジオネーム: {name} さん")
    .WithDescription($"種類: {type} \n 内容: {message}")
    .WithColor(new Color(5814783))
    .Build();

    await client.SendMessageAsync(
        text: "📩 **新しいお便りが届きました！**",
        embeds: new[] { embed }
    );
}

app.Run();

// お便り受信APIのリクエストモデル
record LetterRequest(string Name, string Type, string Message);


