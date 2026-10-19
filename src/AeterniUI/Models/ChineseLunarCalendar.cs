using System.Globalization;

namespace AeterniUI.Models;

/// <summary>
/// Converts a Gregorian day to its Chinese lunisolar (农历) representation for the
/// calendar's optional lunar line. <see cref="ChineseLunisolarCalendar" /> is the
/// BCL's own table so we do not ship a duplicated 1900–2100 data table; its
/// supported range is exposed and dates outside it simply render without a lunar
/// line.
/// </summary>
internal static class ChineseLunarCalendar
{
    /// <summary>First Gregorian day covered by <see cref="ChineseLunisolarCalendar" /> (1901-02-19).</summary>
    public static DateOnly MinDate { get; } = DateOnly.FromDateTime(new ChineseLunisolarCalendar().MinSupportedDateTime);

    /// <summary>Last Gregorian day covered by <see cref="ChineseLunisolarCalendar" /> (2101-01-28).</summary>
    public static DateOnly MaxDate { get; } = DateOnly.FromDateTime(new ChineseLunisolarCalendar().MaxSupportedDateTime);

    public static bool TryGetLunarDate(DateOnly date, out ChineseLunarDate lunar)
    {
        lunar = default;
        if (date < MinDate || date > MaxDate)
        {
            return false;
        }

        // A calendar instance is cheap and stateless for reads; creating one per
        // call avoids sharing mutable-looking state across Blazor Server circuits.
        var calendar = new ChineseLunisolarCalendar();
        var value = date.ToDateTime(TimeOnly.MinValue);
        var year = calendar.GetYear(value);
        var rawMonth = calendar.GetMonth(value);
        var day = calendar.GetDayOfMonth(value);
        var leapMonth = calendar.GetLeapMonth(year);

        // ChineseLunisolarCalendar numbers months 1–13 once a leap month exists:
        // the leap month takes the number of the month it doubles, and every
        // later month shifts up by one. Fold both back into 1–12 + IsLeapMonth.
        var month = leapMonth != 0 && rawMonth >= leapMonth ? rawMonth - 1 : rawMonth;
        var isLeapMonth = leapMonth != 0 && rawMonth == leapMonth;

        lunar = new ChineseLunarDate(year, month, day, isLeapMonth);
        return true;
    }
}
