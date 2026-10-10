using System.Collections;
using System.Globalization;
using AeterniUI.Enums;

namespace AeterniUI.Components.TimePicker;

internal static class TimePickerOptions
{
    internal const int SecondsPerDay = 24 * 60 * 60;

    internal static bool IncludesSeconds(TimeSpan step) =>
        step.Ticks % TimeSpan.TicksPerMinute != 0;

    internal static string ResolveFormat(TimeFormat format, CultureInfo culture, bool includeSeconds) => format switch
    {
        TimeFormat.TwelveHour => includeSeconds ? "h:mm:ss tt" : "h:mm tt",
        TimeFormat.TwentyFourHour => includeSeconds ? "HH:mm:ss" : "HH:mm",
        _ => includeSeconds ? culture.DateTimeFormat.LongTimePattern : culture.DateTimeFormat.ShortTimePattern
    };

    internal static bool UsesTwelveHourClock(TimeFormat format, CultureInfo culture) => format switch
    {
        TimeFormat.TwelveHour => true,
        TimeFormat.TwentyFourHour => false,
        _ => culture.DateTimeFormat.ShortTimePattern.Contains('h')
    };

    internal static TimeSelectionMap CreateMap(
        TimeSpan step,
        TimeOnly? minTime,
        TimeOnly? maxTime,
        Func<TimeOnly, bool>? disabledTime = null,
        TimeSelectionMap? previous = null)
    {
        Validate(step, minTime, maxTime);
        // Delegates can close over mutable state: never reuse their results.
        if (disabledTime is null && previous?.Matches(step, minTime, maxTime) == true) return previous;
        return new TimeSelectionMap(step, minTime, maxTime, disabledTime);
    }

    internal static bool HasAvailable(
        TimeSpan step,
        TimeOnly? minTime,
        TimeOnly? maxTime,
        Func<TimeOnly, bool>? disabledTime = null)
    {
        Validate(step, minTime, maxTime);
        var stepSeconds = checked((int)(step.Ticks / TimeSpan.TicksPerSecond));
        var start = FirstStepAtOrAfter(minTime, stepSeconds);
        var end = LastStepAtOrBefore(maxTime, stepSeconds);
        for (var secondOfDay = start; secondOfDay <= end; secondOfDay += stepSeconds)
        {
            var value = new TimeOnly(secondOfDay / 3600, (secondOfDay / 60) % 60, secondOfDay % 60);
            if ((!minTime.HasValue || value >= minTime.Value) &&
                (!maxTime.HasValue || value <= maxTime.Value) &&
                disabledTime?.Invoke(value) != true)
            {
                return true;
            }
        }

        return false;
    }

    internal static void Validate(TimeSpan step, TimeOnly? minTime, TimeOnly? maxTime)
    {
        if (step < TimeSpan.FromSeconds(1) || step >= TimeSpan.FromDays(1) || step.Ticks % TimeSpan.TicksPerSecond != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(step),
                step,
                "The time step must be a whole number of seconds between one second and one day.");
        }

        if (minTime.HasValue && maxTime.HasValue && minTime.Value > maxTime.Value)
        {
            throw new ArgumentException("The minimum time cannot be later than the maximum time.");
        }
    }

    internal static int FirstStepAtOrAfter(TimeOnly? minTime, int stepSeconds)
    {
        var minimum = minTime.HasValue
            ? (int)((minTime.Value.Ticks + TimeSpan.TicksPerSecond - 1) / TimeSpan.TicksPerSecond) : 0;
        return Math.Min(SecondsPerDay, ((minimum + stepSeconds - 1) / stepSeconds) * stepSeconds);
    }

    internal static int LastStepAtOrBefore(TimeOnly? maxTime, int stepSeconds) =>
        Math.Min(SecondsPerDay - 1, maxTime.HasValue ? ToSecondOfDay(maxTime.Value) / stepSeconds * stepSeconds : SecondsPerDay - 1);

    private static int ToSecondOfDay(TimeOnly value) =>
        checked((int)(value.Ticks / TimeSpan.TicksPerSecond));
}

internal sealed class TimeSelectionMap
{
    private readonly BitArray _available = new(TimePickerOptions.SecondsPerDay);
    private readonly (TimeSpan Step, TimeOnly? Min, TimeOnly? Max, bool Cacheable) _configuration;

    internal bool Matches(TimeSpan step, TimeOnly? min, TimeOnly? max) =>
        _configuration == (step, min, max, true);

    internal TimeSelectionMap(
        TimeSpan step,
        TimeOnly? minTime,
        TimeOnly? maxTime,
        Func<TimeOnly, bool>? disabledTime)
    {
        _configuration = (step, minTime, maxTime, disabledTime is null);
        var stepSeconds = checked((int)(step.Ticks / TimeSpan.TicksPerSecond));
        var start = TimePickerOptions.FirstStepAtOrAfter(minTime, stepSeconds);
        var end = TimePickerOptions.LastStepAtOrBefore(maxTime, stepSeconds);
        for (var secondOfDay = start; secondOfDay <= end; secondOfDay += stepSeconds)
        {
            var value = FromSecondOfDay(secondOfDay);
            if ((!minTime.HasValue || value >= minTime.Value) &&
                (!maxTime.HasValue || value <= maxTime.Value) &&
                disabledTime?.Invoke(value) != true)
            {
                _available[secondOfDay] = true;
                Count++;
            }
        }
    }

    internal int Count { get; }

    internal bool Contains(TimeOnly value)
    {
        if (value.Ticks % TimeSpan.TicksPerSecond != 0)
        {
            return false;
        }

        return _available[ToSecondOfDay(value)];
    }

    internal IReadOnlyList<int> Hours() => AvailableGroups(0, 24, 60 * 60);

    internal IReadOnlyList<int> Minutes(int hour) =>
        AvailableGroups(hour * 60 * 60, 60, 60);

    internal IReadOnlyList<int> Seconds(int hour, int minute)
    {
        var start = (hour * 60 * 60) + (minute * 60);
        var values = new List<int>();
        for (var second = 0; second < 60; second++)
        {
            if (_available[start + second])
            {
                values.Add(second);
            }
        }

        return values;
    }

    internal TimeOnly? FindFirst() => FindFrom(0, 1);

    internal TimeOnly? FindClosest(TimeOnly? preferred, int? hour = null, int? minute = null)
    {
        if (Count == 0)
        {
            return null;
        }

        var start = hour.HasValue ? hour.Value * 60 * 60 : 0;
        var end = hour.HasValue ? start + (60 * 60) : TimePickerOptions.SecondsPerDay;
        if (minute.HasValue)
        {
            start += minute.Value * 60;
            end = start + 60;
        }

        var target = preferred.HasValue ? ToSecondOfDay(preferred.Value) : start;
        var bestIndex = -1;
        var bestDistance = int.MaxValue;
        for (var index = start; index < end; index++)
        {
            if (!_available[index])
            {
                continue;
            }

            var distance = Math.Abs(index - target);
            if (distance < bestDistance)
            {
                bestIndex = index;
                bestDistance = distance;
            }
        }

        return bestIndex >= 0 ? FromSecondOfDay(bestIndex) : null;
    }

    internal TimeOnly? FindAdjacent(TimeOnly? current, int offset)
    {
        if (Count == 0)
        {
            return null;
        }

        if (!current.HasValue)
        {
            return offset < 0 ? FindFrom(TimePickerOptions.SecondsPerDay - 1, -1) : FindFirst();
        }

        var index = ToSecondOfDay(current.Value);
        var start = Contains(current.Value) ? index + Math.Sign(offset) : index;
        return FindFrom(start, Math.Sign(offset));
    }

    private IReadOnlyList<int> AvailableGroups(int start, int groupCount, int groupSize)
    {
        var values = new List<int>();
        for (var group = 0; group < groupCount; group++)
        {
            var groupStart = start + (group * groupSize);
            for (var index = groupStart; index < groupStart + groupSize; index++)
            {
                if (_available[index])
                {
                    values.Add(group);
                    break;
                }
            }
        }

        return values;
    }

    private TimeOnly? FindFrom(int start, int direction)
    {
        if (direction == 0)
        {
            return null;
        }

        for (var index = start; index >= 0 && index < TimePickerOptions.SecondsPerDay; index += direction)
        {
            if (_available[index])
            {
                return FromSecondOfDay(index);
            }
        }

        return null;
    }

    private static int ToSecondOfDay(TimeOnly value) =>
        (value.Hour * 60 * 60) + (value.Minute * 60) + value.Second;

    private static TimeOnly FromSecondOfDay(int value) =>
        new(value / 3600, (value / 60) % 60, value % 60);
}
