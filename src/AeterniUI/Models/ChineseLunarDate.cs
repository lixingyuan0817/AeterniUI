namespace AeterniUI.Models;

/// <summary>
/// One day on the Chinese lunisolar (农历) calendar, used only for the optional
/// lunar line the date calendar renders under the Gregorian day number. The
/// pickers never bind to it and it carries no time-zone or solar-term data.
/// </summary>
internal readonly struct ChineseLunarDate
{
    private static readonly string[] MonthNames =
        ["正月", "二月", "三月", "四月", "五月", "六月", "七月", "八月", "九月", "十月", "冬月", "腊月"];

    // 初一..初十, 十一..十九, 二十, 廿一..廿九, 三十. Written out instead of
    // composed from digits so the cells read like a printed calendar.
    private static readonly string[] DayNames =
    [
        "初一", "初二", "初三", "初四", "初五", "初六", "初七", "初八", "初九", "初十",
        "十一", "十二", "十三", "十四", "十五", "十六", "十七", "十八", "十九", "二十",
        "廿一", "廿二", "廿三", "廿四", "廿五", "廿六", "廿七", "廿八", "廿九", "三十"
    ];

    public ChineseLunarDate(int year, int month, int day, bool isLeapMonth)
    {
        if (month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month), month, "Lunar month must be between 1 and 12.");
        if (day is < 1 or > 30) throw new ArgumentOutOfRangeException(nameof(day), day, "Lunar day must be between 1 and 30.");
        Year = year;
        Month = month;
        Day = day;
        IsLeapMonth = isLeapMonth;
    }

    public int Year { get; }

    /// <summary>Lunar month 1–12 with the leap marker folded into <see cref="IsLeapMonth" />.</summary>
    public int Month { get; }

    /// <summary>Lunar day 1–30.</summary>
    public int Day { get; }

    public bool IsLeapMonth { get; }

    /// <summary>Month name without the day, e.g. <c>九月</c> or <c>闰六月</c>.</summary>
    public string MonthText => (IsLeapMonth ? "闰" : string.Empty) + MonthNames[Month - 1];

    /// <summary>Day name, e.g. <c>初十</c>.</summary>
    public string DayText => DayNames[Day - 1];

    /// <summary>
    /// Line shown inside a calendar cell: trailing Chinese calendars label the
    /// first day of a lunar month with the month name and the rest with the day.
    /// </summary>
    public string ShortText => Day == 1 ? MonthText : DayText;

    /// <summary>Full name used in accessible labels, e.g. <c>九月初十</c>.</summary>
    public string FullText => MonthText + DayText;
}
