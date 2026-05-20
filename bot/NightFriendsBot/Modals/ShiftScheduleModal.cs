using Discord.Interactions;

namespace NightFriendsBot.Modals;

public class ShiftScheduleModal : IModal
{
    public string Title => "シフトスケジュール設定";
    
    [InputLabel("イベント名")]
    [ModalTextInput("EventName", placeholder: "名前を入力...")]
    public string EventName { get; set; }
    
    [InputLabel("イベントタイプ")]
    [ModalSelectMenu("eventtype")]
    [ModalSelectMenuOption("定期開催", "NormalOpen", "通常通り開催")]
    [ModalSelectMenuOption("定期特別開催" , "SpeciallyNormalOpen", "日時や時間以外で特別なことがある(例: 1stAnni)")]
    [ModalSelectMenuOption("特別開催", "SpeciallyOpen", "日時や時間で特別な開催(例: 追加で1日開催)")]
    [ModalSelectMenuOption("テスト(β)", "TestOpen", "テスト開催、OBT含む")]
    public string[] TextSelectMenu { get; set; }

    [InputLabel("イベント日付")]
    [ModalTextInput("EventDate", placeholder: "YYYY/MM/DD")]
    public string EventDate { get; set; }

    [InputLabel("イベント時間")]
    [ModalTextInput("EventTime", placeholder: "HH:MM")]
    public string EventTime { get; set; }
    
    [InputLabel("期限日付")]
    [ModalTextInput("DeadLineDate", placeholder: "YYYY/MM/DD")]
    public string DeadLineDate { get; set; }
}
