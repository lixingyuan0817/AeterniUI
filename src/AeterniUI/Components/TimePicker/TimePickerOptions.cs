using System.Globalization;
using AeterniUI.Enums;

namespace AeterniUI.Components.TimePicker;

internal static class TimePickerOptions
{
    internal static string ResolveFormat(TimeFormat format, CultureInfo culture) => format switch
    {
        TimeFormat.TwelveHour => "h:mm tt",
        TimeFormat.TwentyFourHour => "HH:mm",
        _ => culture.DateTimeFormat.ShortTimePattern
    };

    internal static IReadOnlyList<TimeOnly> Build(
        TimeSpan step,
        TimeOnly? minTime,
        TimeOnly? maxTime,
        Func<TimeOnly, bool>? disabledTime = null)
    {
        Validate(step, minTime, maxTime);
        var values = new List<TimeOnly>();
        var stepTicks = step.Ticks;
        var dayTicks = TimeSpan.TicksPerDay;

        for (var ticks = 0L; ticks < dayTicks; ticks += stepTicks)
        {
            var value = TimeOnly.FromTimeSpan(TimeSpan.FromTicks(ticks));
            if ((!minTime.HasValue || value >= minTime.Value) &&
                (!maxTime.HasValue || value <= maxTime.Value) &&
                disabledTime?.Invoke(value) != true)
            {
                values.Add(value);
            }
        }

        return values;
    }

    internal static void Validate(TimeSpan step, TimeOnly? minTime, TimeOnly? maxTime)
    {
        if (step <= TimeSpan.Zero || step >= TimeSpan.FromDays(1))
            throw new ArgumentOutOfRangeException(nameof(step), step, "The time step must be greater than zero and shorter than one day.");
        if (minTime.HasValue && maxTime.HasValue && minTime.Value > maxTime.Value)
            throw new ArgumentException("The minimum time cannot be later than the maximum time.");
    }

    internal static TimeOnly? FindAdjacent(IReadOnlyList<TimeOnly> values, TimeOnly? current, int offset)
    {
        if (values.Count == 0) return null;
        if (!current.HasValue) return offset < 0 ? values[^1] : values[0];

        var index = -1;
        for (var i = 0; i < values.Count; i++)
        {
            if (values[i] == current.Value)
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            for (var i = 0; i < values.Count; i++)
            {
                if (values[i] > current.Value)
                {
                    index = offset < 0 ? Math.Max(0, i - 1) : i;
                    break;
                }
            }
            if (index < 0) index = values.Count - 1;
            return values[index];
        }

        return values[Math.Clamp(index + offset, 0, values.Count - 1)];
    }
}
