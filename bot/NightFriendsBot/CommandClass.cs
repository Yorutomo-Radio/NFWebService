using Discord;
using Discord.Commands;
using Discord.Interactions;
using NightFriendsBot.Modals;

namespace NightFriendsBot;

public class CommandClass : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("hi", "hi")]
    public async Task Reply()
    {
        await ReplyAsync("hello");
    }

    [SlashCommand("schedule", "スケジュール設定開始")]
    public async Task StartSettingSchedule()
    {
        await RespondWithModalAsync<ShiftScheduleModal>("ShiftScheduleSetting");
        Program.isReception = true;
        // ShiftScheduler.StartSetSchedule();
    }

    [SlashCommand("managemember", "メンバー操作")]
    public async Task ManageMember()
    {
        var selectMenu = new SelectMenuBuilder()
            .WithCustomId("manage_member_action")
            .WithPlaceholder("操作を選択してください")
            .AddOption("確定メンバー決定", "DecidedMember")
            .AddOption("メンバーを含めないように", "IgnoreMember")
            .AddOption("リストを確認", "CheckMemberStatus")
            .AddOption("強制終了", "ForcedFinish")
            .AddOption("再度シフトを作成", "RerollShift")
            .AddOption("全てをリセット", "AllReset");

        var components = new ComponentBuilder()
            .WithSelectMenu(selectMenu)
            .Build();

        await RespondAsync("操作を選択してください", components: components, ephemeral: true);
    }
}