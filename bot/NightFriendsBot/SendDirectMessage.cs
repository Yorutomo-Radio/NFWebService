using System.Drawing;
using Discord;
using Discord.WebSocket;

namespace NightFriendsBot;

public class SendDirectMessage
{
    public static async Task SendDirectMessageEveryone()
    {
        var guild = Program.Client.GetGuild(Program.GuildId);

        await guild.DownloadUsersAsync();
        var users = guild.Users;

        int successCount = 0;
        int failCount = 0;

        foreach (var user in users)
        {
            // botアカウントはスキップ
            if (user.IsBot) continue;
            Console.WriteLine(user + user.Mention);

            try
            {
                var dmChannel = await user.CreateDMChannelAsync();
                var componentBuilder = new ComponentBuilder()
                    .WithButton("〇参加可能", "isJoined_join", ButtonStyle.Success)
                    .WithButton("×参加不可", "isJoined_leave", ButtonStyle.Danger);
                var embedBuilder = new EmbedBuilder()
                    .WithTitle($"イベントシフトが作成されました!: {ShiftScheduler.EventName}")
                    .WithDescription($"イベントタイプ: {ShiftScheduler.TextSelectMenu}")
                    .AddField("イベントの日時", $"{ShiftScheduler.EventDate} / {ShiftScheduler.EventTime}")
                    .AddField("イベントシフトの期限", $"{ShiftScheduler.DeadLineDate}")
                    .WithColor(Discord.Color.Red);

                await dmChannel.SendMessageAsync("", false, embedBuilder.Build(), components: componentBuilder.Build());
                Console.WriteLine($"✅ DMを送信しました: {user.Username}");
                successCount++;

                // レート制限対策：1送信ごとに少し待つ
                await Task.Delay(1000);
            }
            catch (Discord.Net.HttpException ex)
            {
                // 50007: DMを受け取らない設定のユーザー
                Console.WriteLine($"❌ DM送信失敗: {user.Username} - コード:{ex.HttpCode} {ex.Message}");
                failCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 予期しないエラー: {user.Username} - {ex.Message}");
                failCount++;
            }
        }

        Console.WriteLine($"\n=== 送信完了 成功:{successCount} 失敗:{failCount} ===");
    }
}