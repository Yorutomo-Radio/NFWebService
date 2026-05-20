using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using Discord;
using NightFriendsBot;

namespace NightFriendsBot;

class ShiftScheduler
{
    // 全メンバーリスト
    private HashSet<string> _allMembers = new HashSet<string>
    {
        "suraimu3321", "kan_kan_kiri", "tamakichan801", "nanaki_01",
        "itika0531", "etta3774", "twister716", "airenyuna"
    };

    public static List<string> IgnoreMember = new List<string>();
    public static List<string> DecidedMember = new List<string>();
    public static string EventName;
    public static string TextSelectMenu;
    public static string EventDate;
    public static string EventTime;
    public static string DeadLineDate;
    private SQLiteConnection _conn;
    private Dictionary<string, int> _personCount = new Dictionary<string, int>();
    private List<string> _lastShift = new List<string>();
    private List<string> _secondLastShift = new List<string>();
    private List<string> _cameraMan = new List<string> { "itika0531" }; // 固定


    public ShiftScheduler()
    {
        string dbPath = Environment.GetEnvironmentVariable("DB_PATH") ?? "/data/shiftList.db";
        _conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
        _conn.Open();
    }

    public void Init()
    {
        string createTableSql = @"CREATE TABLE IF NOT EXISTS shiftList(
            id INTEGER, 
            first TEXT, 
            second TEXT, 
            third TEXT, 
            fourth TEXT
        )";

        using (var cmd = new SQLiteCommand(createTableSql, _conn))
        {
            cmd.ExecuteNonQuery();
        }

        // テストデータを追加（既にデータがある場合はスキップ）
        using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM shiftList", _conn))
        {
            long count = (long)cmd.ExecuteScalar();

            if (count == 0)
            {
                var testData = new List<(int, string, string, string, string)>
                {
                    (1, "suraimu3321", "itika0531", "kan_kan_kiri", null),
                    (2, "suraimu3321", "kan_kan_kiri", "tamakichan80", "nanaki_01"),
                    (3, "itika0531", "tamakichan80", "nanaki_01", "suraimu3321")
                };

                foreach (var data in testData)
                {
                    string insertSql = "INSERT INTO shiftList VALUES (@id, @first, @second, @third, @fourth)";
                    using (var insertCmd = new SQLiteCommand(insertSql, _conn))
                    {
                        insertCmd.Parameters.AddWithValue("@id", data.Item1);
                        insertCmd.Parameters.AddWithValue("@first", data.Item2);
                        insertCmd.Parameters.AddWithValue("@second", data.Item3);
                        insertCmd.Parameters.AddWithValue("@third", data.Item4);
                        insertCmd.Parameters.AddWithValue("@fourth", data.Item5 ?? (object)DBNull.Value);
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }

    public void GetMember()
    {
        // 1/2番目に大きいIDを取得
        List<int> top2Ids = new List<int>();
        using (var cmd = new SQLiteCommand("SELECT DISTINCT id FROM shiftList ORDER BY id DESC LIMIT 2", _conn))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                top2Ids.Add(reader.GetInt32(0));
            }
        }

        // lastShift取得
        _lastShift.Clear();
        if (top2Ids.Count >= 1)
        {
            int largestId = top2Ids[0];
            using (var cmd = new SQLiteCommand(
                       "SELECT first, second, third, fourth FROM shiftList WHERE id = @id", _conn))
            {
                cmd.Parameters.AddWithValue("@id", largestId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            if (!reader.IsDBNull(i))
                            {
                                string person = reader.GetString(i);
                                if (!string.IsNullOrEmpty(person))
                                {
                                    _lastShift.Add(person);
                                }
                            }
                        }
                    }
                }
            }
        }

        // secondLastShift取得
        _secondLastShift.Clear();
        if (top2Ids.Count >= 2)
        {
            int secondLargestId = top2Ids[1];
            using (var cmd = new SQLiteCommand(
                       "SELECT first, second, third, fourth FROM shiftList WHERE id = @id", _conn))
            {
                cmd.Parameters.AddWithValue("@id", secondLargestId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            if (!reader.IsDBNull(i))
                            {
                                string person = reader.GetString(i);
                                if (!string.IsNullOrEmpty(person))
                                {
                                    _secondLastShift.Add(person);
                                }
                            }
                        }
                    }
                }
            }
        }

        // 1番目~10番目に大きいIDの中で各人が何回含まれているかカウント
        Dictionary<string, int> personCountTemp = new Dictionary<string, int>();

        using (var cmd = new SQLiteCommand(
                   "SELECT id, first, second, third, fourth FROM shiftList ORDER BY id DESC LIMIT 10", _conn))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                for (int i = 1; i < 5; i++)
                {
                    if (!reader.IsDBNull(i))
                    {
                        string person = reader.GetString(i);
                        if (!string.IsNullOrEmpty(person))
                        {
                            if (!personCountTemp.ContainsKey(person))
                            {
                                personCountTemp[person] = 0;
                            }

                            personCountTemp[person]++;
                        }
                    }
                }
            }
        }

        // 全メンバーをpersonCountに含める（カウントがない人は0）
        _personCount.Clear();
        foreach (var member in _allMembers)
        {
            _personCount[member] = personCountTemp.ContainsKey(member) ? personCountTemp[member] : 0;
        }
    }

    public List<string> Calc()
    {
        int k = 4; // 必要人数

        Dictionary<string, int> allMember = new Dictionary<string, int>(_personCount);
        Random random = new Random();

        // スコア計算関数
        double CalcScore(string member)
        {
            double point = 0;

            // 直近ペナルティ
            if (_lastShift.Contains(member))
                point += 5;
            if (_secondLastShift.Contains(member))
                point += 2;
            if (_cameraMan.Contains(member))
                point += 7;
            if (IgnoreMember.Contains(member))
                point += 99;
            if (DecidedMember.Contains(member))
                point -= 99;

            // 出勤回数
            point += allMember[member];

            // ランダム性
            point += random.NextDouble() * 3.0;

            return point;
        }

        // 前回参加していない人から優先的に選択
        var candidatesPhase1 = allMember.Keys
            .Where(m => !_lastShift.Contains(m))
            .ToList();

        // (KEY, VALUE) というタプルを作成
        var scoredPhase1 = candidatesPhase1
            .Select(m => new { Member = m, Score = CalcScore(m) })
            .OrderBy(x => x.Score)
            .ToList();

        // スコア順に並んだ上位k人の"名前だけ"を取り出す
        List<string> selected = scoredPhase1
            .Take(k)
            .Select(x => x.Member)
            .ToList();

        // 足りなければ全体から補充
        if (selected.Count < k)
        {
            int remainingSlots = k - selected.Count;

            var candidatesPhase2 = allMember.Keys
                .Where(m => !selected.Contains(m))
                .ToList();

            var scoredPhase2 = candidatesPhase2
                .Select(m => new { Member = m, Score = CalcScore(m) })
                .OrderBy(x => x.Score)
                .ToList();

            selected.AddRange(scoredPhase2
                .Take(remainingSlots)
                .Select(x => x.Member));
        }

        Console.WriteLine("Selected: [" + string.Join(", ", selected) + "]");

        // 状態更新
        foreach (var m in selected)
        {
            allMember[m]++;
        }

        Console.WriteLine("Updated counts: {" +
                          string.Join(", ", allMember.Select(kv => $"{kv.Key}: {kv.Value}")) + "}");

        return selected;
    }

    public void Close()
    {
        _conn?.Close();
    }

    public static void StartSetSchedule()
    {
        var scheduler = new ShiftScheduler();
        List<string> result = new List<string>();

        scheduler.Init();
        scheduler.GetMember();
        result = scheduler.Calc();
        scheduler.Close();

        Console.WriteLine(string.Join(", ", result));
        Console.WriteLine(result);

        var ShiftEmbed = new EmbedBuilder()
            .WithTitle($"今回のイベント [{EventName}] のシフト")
            .WithColor(Discord.Color.Red)
            .WithDescription($"イベントタイプ: {TextSelectMenu}")
            .AddField("1人目:", Program.GetUserNameToDiscordId(result[0]))
            .AddField("2人目:", Program.GetUserNameToDiscordId(result[1]))
            .AddField("3人目", Program.GetUserNameToDiscordId(result[2]))
            .AddField("4人目:", Program.GetUserNameToDiscordId(result[3]));

        Program.Client.GetGuild(Program.GuildId).GetTextChannel(Program.TextChannelId)
            .SendMessageAsync("今回のシフトは以下の通りです、変更がある場合は管理者へ個別にお問い合わせください", false, ShiftEmbed.Build());
    }

    public static void UpdateEventData(string eventName, string[] textSelectMenu, string eventDate, string eventTime,
        string deadLineDate)
    {
        EventName = eventName;
        TextSelectMenu = textSelectMenu[0];
        EventDate = eventDate;
        EventTime = eventTime;
        DeadLineDate = deadLineDate;
    }
}