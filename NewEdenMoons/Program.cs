using System.Text;

Console.ForegroundColor = ConsoleColor.White;
var client = new HttpClient();
var response = await client.GetAsync($"https://docs.google.com/spreadsheets/d/1pwQ3V_mTEvUgaa-FoCeiI2TWtst4NzTsApZQCjDuoU0/export?format=csv&id=1pwQ3V_mTEvUgaa-FoCeiI2TWtst4NzTsApZQCjDuoU0&gid=1581013005");
var content = await response.Content.ReadAsStringAsync();

var moons = GetMoons(content).ToList();
Console.WriteLine("========== New Eden Moons ==========");
Console.WriteLine();

static IEnumerable<Moon> GetOrderedMoonPops(IEnumerable<Moon> moons)
{
    var currentDay = DateOnly.FromDateTime(DateTime.UtcNow - TimeSpan.FromDays(1));
    while (true)
    {
        var list = new List<Moon>();
        foreach (var moon in moons)
        {
            while (moon.PopDay < currentDay)
            {
                moon.Pop();
            }

            if (moon.PopDay == currentDay)
                list.Add(moon);
        }

        foreach (var moon in list.OrderBy(x => x.PopTimeUtc).ThenBy(x => x.SystemName).ThenBy(x => x.AthanorName))
            yield return moon;

        currentDay = currentDay.AddDays(1);
    }
}

DateOnly lastDate;
foreach (var moon in GetOrderedMoonPops(moons))
{
    if (lastDate != moon.PopDay)
    {
        Console.ReadLine();
        lastDate = moon.PopDay;
    }

    if (moon.SystemName == "Slays")
        Console.ForegroundColor = ConsoleColor.DarkRed;
    if (moon.SystemName == "Tarta")
        Console.ForegroundColor = ConsoleColor.Yellow;
    if (moon.SystemName == "Inder")
        Console.ForegroundColor = ConsoleColor.Green;

    Console.Write(moon.SystemName.PadRight(15));
    Console.ForegroundColor = ConsoleColor.White;

    Console.Write($"{moon.AthanorName.PadRight(25)}");

    if (moon.PeriodWeeks == 4)
        Console.ForegroundColor = ConsoleColor.Green;
    if (moon.PeriodWeeks == 8)
        Console.ForegroundColor = ConsoleColor.Red;

    Console.Write($"{GetPeriod(moon.PeriodWeeks).PadRight(25)}");
    Console.ForegroundColor = ConsoleColor.White;

    Console.WriteLine($"{GetTime(moon.PopTimeUtc, moon.NotLaterThan)}");
}

Console.ReadLine();

static string GetTime(DateTime time, string notLaterThan)
{
    var sb = new StringBuilder();
    sb.Append(time.ToString("MMMM dd"));

    if (time.Hour == 0 && time.Minute == 0 && time.Second == 0)
    {
        if (notLaterThan != string.Empty)
            sb.Append($" (not later than {notLaterThan})");
    }
    else
    {
        sb.Append(", ");
        sb.Append(time.ToString("H:mm:ss"));
    }

    return sb.ToString();
}

static string GetPeriod(int periodWeeks)
    => periodWeeks switch
    {
        1 => "Puny (1 week)",
        2 => "Normal (2 weeks)",
        4 => "FAT (4 weeks)",
        8 => "EXTRA FAT (8 weeks)",
        _ => throw new NotSupportedException("Unknown moon period.")
    };

static IEnumerable<Moon> GetMoons(string content)
{
    var header = 0;
    foreach (var row in content.Split('\n'))
    {
        if (header < 3)
        {
            header++;
            continue;
        }

        var columns = row.Trim('\r').Split(',');
        if (columns.All(x => x == string.Empty))
            continue;

        if (columns[3] == string.Empty || columns[4].Contains('?') || columns[4] == string.Empty)
        {
            Console.WriteLine($"Unknown time for {columns[1]} - {columns[2]} moon.");
            continue;
        }

        yield return new Moon(columns[1], columns[2], Convert.ToInt32(columns[4]), columns[6])
        {
            PopTimeUtc = ParsePopTime(columns[3], columns[5])
        };
    }
}

static DateTime ParsePopTime(string date, string time)
{
    var dateParts = date.Split(' ');
    var dayPart = Convert.ToInt32(dateParts[0]);
    var monthPart = dateParts[1];

    var dateTime = new DateTime(2023, ParseMonth(monthPart), dayPart, time == string.Empty ? 0 : ParseHour(time), 0, 0, DateTimeKind.Utc);

    return dateTime;

    int ParseMonth(string monthPart)
    {
        return monthPart switch
        {
            "Jan" => 1,
            "January" => 1,
            "Feb" => 2,
            "February" => 2,
            "Mar" => 3,
            "March" => 3,
            "Apr" => 4,
            "April" => 4,
            "May" => 5,
            "Jun" => 6,
            "June" => 6,
            "Jul" => 7,
            "July" => 7,
            "Aug" => 8,
            "August" => 8,
            "Sep" => 9,
            "Sept" => 9,
            "September" => 9,
            "Oct" => 10,
            "October" => 10,
            "Nov" => 11,
            "November" => 11,
            "Dec" => 12,
            "December" => 12,
            _ => throw new NotSupportedException("Not supported month value.")
        };
    }

    int ParseHour(string hour)
    {
        return Convert.ToInt32($"{hour[0]}{hour[1]}");
    }
}

public sealed record Moon(
    string SystemName,
    string AthanorName,
    int PeriodWeeks,
    string NotLaterThan)
{
    public DateTime PopTimeUtc { get; set; }

    public DateOnly PopDay => DateOnly.FromDateTime(PopTimeUtc);

    /// <summary>
    /// Pops the moon, i.e. advances its pop time by the pop period.
    /// </summary>
    public void Pop()
    {
        PopTimeUtc = PopTimeUtc + TimeSpan.FromDays(PeriodWeeks * 7);
    }
}
