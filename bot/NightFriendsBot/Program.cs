using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace NightFriendsBot;

class Program
{
    /*
     * 1350845172078088272 : デバッグ鯖
     * 1468980843442868451 : 本番環境鯖
     * ! デバッグ / テスト時は切り替えを忘れないこと !
     */
    public const ulong GuildId = 1350845172078088272;
    public const ulong TextChannelId = 1471316538140135496;
    public static DiscordSocketClient Client;
    public static bool isReception;
    
    private const string Token = "";
    private CommandService Commands;
    private InteractionService Interactions; // 追加
    private ServiceProvider Services;


    static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

    public async Task MainAsync()
    {
        var config = new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Info,
            GatewayIntents = GatewayIntents.AllUnprivileged |
                             GatewayIntents.MessageContent |
                             GatewayIntents.Guilds |
                             GatewayIntents.GuildMembers |
                             GatewayIntents.DirectMessages
        };

        // クライアントの初期化
        Client = new DiscordSocketClient(config);

        // ログのイベントハンドラーの追加
        Client.Log += Log;
        Client.MessageReceived += MessageReceived;


        // Interaction用の設定
        Client.Ready += ClientReady;
        Client.InteractionCreated += InteractionCreated;      
        Client.ButtonExecuted += EventHandlers.ButtonEventHandler;

        // サービスの初期化
        Commands = new CommandService();
        Interactions = new InteractionService(Client); // 追加

        Services = new ServiceCollection()
            .AddSingleton(Client)
            .AddSingleton(Commands)
            .AddSingleton(Interactions)
            .BuildServiceProvider();

        // テキストコマンドとスラッシュコマンドの両方を登録
        await Commands.AddModulesAsync(Assembly.GetEntryAssembly(), Services);
        await Interactions.AddModulesAsync(Assembly.GetEntryAssembly(), Services);

        await Client.LoginAsync(TokenType.Bot, Token);
        await Client.StartAsync();
        await Task.Delay(-1);
    }

    // Clientが準備完了したときにスラッシュコマンドを登録
    private async Task ClientReady()
    {
        Console.WriteLine("Bot is ready!");

        // ギルドコマンドとして登録（即時反映）
        await Interactions.RegisterCommandsToGuildAsync(GuildId);

        // グローバルコマンドとして登録する場合（反映に最大1時間）
        // await Interactions.RegisterCommandsGloballyAsync();
    }

    // Interactionが実行されたとき
    private async Task InteractionCreated(SocketInteraction interaction)
    {
        var context = new SocketInteractionContext(Client, interaction);
        await Interactions.ExecuteCommandAsync(context, Services);
    }

    // ログ出力
    private Task Log(LogMessage message)
    {
        Console.WriteLine(message.ToString());
        return Task.CompletedTask;
    }

    // テキストコマンド用 (そのうち破棄)
    private async Task MessageReceived(SocketMessage messageParam)
    {
        var message = messageParam as SocketUserMessage;

        if (message == null)
        {
            return;
        }

        Console.WriteLine(message.Content);
        if (message.Author.IsBot)
        {
            return;
        }

        int argPos = 0;

        // コマンドかどうか判定
        if (!message.HasCharPrefix('!', ref argPos))
        {
            return;
        }

        var context = new CommandContext(Client, message);
        //コマンド実行
        var result = await Commands.ExecuteAsync(context, argPos, Services);

        if (!result.IsSuccess)
        {
            await context.Channel.SendMessageAsync(result.ErrorReason);
        }
    }
    
    // ヘルパー関数
    /// <summary>
    /// ユーザーネームからIDを検索
    /// </summary>
    /// <returns></returns>
    public static ulong? GetUserNameToDiscordId(string username)
    {
        var guild = Program.Client.GetGuild(Program.GuildId);
        
        // ギルド内のユーザーをusernameから検索
        var user = guild.Users.FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) ||
            u.GlobalName?.Equals(username, StringComparison.OrdinalIgnoreCase) == true);

        if (user == null)
        {
            Console.WriteLine($"ユーザー '{username}' が見つかりません");
            return null;
        }

        return user.Id;
    }
}


/*
 * シフト自動計算システム
 * 1. 管理者が次イベントの日程を登録
 * 2. 登録した時点で全員に参加可能/不可のDMが自動的に一斉送信
 * 3. 指定日程もしくは全員の回答がそろった時点で計算
 * 4-1. 足りた場合: そのまま決定した人にDMを自動送信 / 全体公開
 * 4-2. 足りない場合: 足りない旨を全体公開、再度計算
 */