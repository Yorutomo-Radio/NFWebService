using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using NightFriendsBot.Modals;

namespace NightFriendsBot;

public class EventHandlers : InteractionModuleBase<SocketInteractionContext>
{
    public static async Task ButtonEventHandler(SocketMessageComponent component)
    {
        if (Program.isReception)
        {
            switch (component.Data.CustomId)
            {
                // 参加可能
                case "isJoined_join":
                    await component.RespondAsync($"Success: [USER:{component.User.Username}] 参加可能としてマークしました");
                    ShiftScheduler.IgnoreMember.Remove(component.User.Username);
                    break;

                // 参加不可
                case "isJoined_leave":
                    await component.RespondAsync($"Success: [USER:{component.User.Username}] 参加不可としてマークしました。");
                    ShiftScheduler.IgnoreMember.Add(component.User.Username);
                    break;
            }
        }
        else
        {
            await component.RespondAsync("エラー: このシフト募集は既に終了しています。");
        }
    }

    [ModalInteraction("ShiftScheduleSetting")]
    public async Task ShiftSettingEventHandler(ShiftScheduleModal modal)
    {
        await RespondAsync(
            $"イベント名: {modal.EventName}\n種別: {modal.TextSelectMenu[0]}\n日付: {modal.EventDate}\n時間: {modal.EventTime}\n期限: {modal.DeadLineDate}");
        ShiftScheduler.UpdateEventData(modal.EventName, modal.TextSelectMenu, modal.EventDate, modal.EventTime,
            modal.DeadLineDate);
        await SendDirectMessage.SendDirectMessageEveryone();
    }


    [ComponentInteraction("manage_member_action")]
    public async Task HandleMemberAction(string[] selections)
    {
        
        string action = selections[0];
        

        if (action == "ForcedFinish")
        {
            // 処理
            await RespondAsync("シフト受付を強制終了しました", ephemeral: false);
            Program.isReception = false;
            ShiftScheduler.StartSetSchedule();
        }
        else if (action == "RerollShift")
        {
            // 再度作成
            await RespondAsync("シフトを再度作成しています...", ephemeral: false);
            ShiftScheduler.StartSetSchedule();
        }
        else
        {
            var component = new ComponentBuilder()
                .WithSelectMenu(
                    customId: $"user_select_{action}",
                    options: new List<SelectMenuOptionBuilder>(), // 空リスト
                    placeholder: "ユーザーを選択",
                    minValues: 1,
                    maxValues: 1,
                    disabled: false,
                    row: 0,
                    type: ComponentType.UserSelect // UserSelect を指定
                )
                .Build();

            await RespondAsync("ユーザーを選択してください", components: component, ephemeral: true);
        }
    }

    [ComponentInteraction("user_select_*")]
    public async Task HandleUserSelection()
    {
        // SocketMessageComponentから直接データを取得
        var interaction = Context.Interaction as SocketMessageComponent;
        if (interaction == null) return;

        // 選択されたユーザーIDを取得
        var userIds = interaction.Data.Values.Select(ulong.Parse).ToArray();
        var userId = userIds[0];

        // CustomIdから操作タイプを取得
        string customId = interaction.Data.CustomId;
        string actionType = customId.Replace("user_select_", "");

        var user = await Context.Client.GetUserAsync(userId);
        string username = user.Username;

        switch (actionType)
        {
            case "DecidedMember":
                if (ShiftScheduler.DecidedMember.Contains(username))
                {
                    ShiftScheduler.DecidedMember.Remove(username);
                    await RespondAsync($":key::outbox_tray: ユーザー {username} を決定メンバーから削除しました", ephemeral: false);
                }
                else
                {
                    ShiftScheduler.DecidedMember.Add(username);
                    await RespondAsync($":key::inbox_tray: ユーザー {username} を決定メンバーに追加しました", ephemeral: false);
                }

                break;

            case "IgnoreMember":
                if (ShiftScheduler.IgnoreMember.Contains(username))
                {
                    ShiftScheduler.IgnoreMember.Remove(username);
                    await RespondAsync($":no_entry_sign::outbox_tray: ユーザー {username} を不可メンバーから削除しました",
                        ephemeral: false);
                }
                else
                {
                    ShiftScheduler.IgnoreMember.Add(username);
                    await RespondAsync($":no_entry_sign::inbox_tray: ユーザー {username} を不可メンバーに追加しました", ephemeral: false);
                }

                break;

            case "CheckMemberStatus":
                string description;
                Color color;
                if (ShiftScheduler.IgnoreMember.Contains(username))
                {
                    description = "無視リストに入っています";
                    color = Color.DarkRed;
                }
                else if (ShiftScheduler.DecidedMember.Contains(username))
                {
                    description = "決定リストに入っています";
                    color = Color.DarkBlue;
                }
                else
                {
                    description = "リストには入っていないか、ユーザーが存在しません";
                    color = Color.Default;
                }

                var embed = new EmbedBuilder()
                    .WithTitle($"ユーザーネーム: {username} の詳細")
                    .WithDescription(description)
                    .WithColor(color);

                await RespondAsync("", embed: embed.Build(), ephemeral: false);
                break;

            default:
                await RespondAsync($"不明な操作: {actionType}", ephemeral: true);
                break;
        }
    }
}