using System.ComponentModel;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Components.DatePicker;

/// <summary>
/// Internal calendar rendering surface used by <see cref="DatePicker"/> and
/// <see cref="DateRangePicker"/>. This type is not a stable public API.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class DateCalendar : AeterniComponent
{
    [Parameter] public DateOnly DisplayMonth { get; set; }
    [Parameter] public DateOnly? SelectedDate { get; set; }
    [Parameter] public DateOnly? RangeStart { get; set; }
    [Parameter] public DateOnly? RangeEnd { get; set; }
    [Parameter] public DateOnly? RangePreviewEnd { get; set; }
    [Parameter] public Func<DateOnly, bool>? DisabledDate { get; set; }
    [Parameter] public EventCallback<DateOnly> DateSelected { get; set; }
    [Parameter] public EventCallback<DateOnly> MonthChanged { get; set; }
    [Parameter] public EventCallback<DateOnly> DateHovered { get; set; }
    [Parameter] public string AriaLabel { get; set; } = "Calendar";
    [Parameter] public int VisibleMonths { get; set; } = 1;

    private CultureInfo Culture => CultureInfo.CurrentCulture;
    private readonly Dictionary<CalendarDayKey, ElementReference> _dayElements = [];
    private DateOnly? _pendingFocusDate;
    private DateOnly? FocusedDate { get; set; }
    private DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
    private int RenderedMonthCount => Math.Min(VisibleMonths, MonthDistanceToMax(DisplayMonth) + 1);
    private IReadOnlyList<DateOnly> Months => Enumerable.Range(0, RenderedMonthCount).Select(DisplayMonth.AddMonths).ToArray();
    private string VisibleMonthTitle => RenderedMonthCount == 1
        ? MonthTitle(DisplayMonth)
        : $"{MonthTitle(DisplayMonth)} – {MonthTitle(DisplayMonth.AddMonths(RenderedMonthCount - 1))}";
    private IReadOnlyList<string> Weekdays => Enumerable.Range(0, 7)
        .Select(index => Culture.DateTimeFormat.GetAbbreviatedDayName((DayOfWeek)((index + 1) % 7)))
        .ToArray();

    private string MonthTitle(DateOnly month) => month.ToDateTime(TimeOnly.MinValue).ToString("yyyy MMMM", Culture);

    private string DayLabel(DateOnly day) => day.ToDateTime(TimeOnly.MinValue).ToString("D", Culture);

    private static IEnumerable<DateOnly?> Days(DateOnly month)
    {
        var first = new DateOnly(month.Year, month.Month, 1);
        var offset = ((int)first.DayOfWeek + 6) % 7;
        var start = first.DayNumber - offset;
        return Enumerable.Range(start, 42).Select(day =>
            day >= DateOnly.MinValue.DayNumber && day <= DateOnly.MaxValue.DayNumber
                ? (DateOnly?)DateOnly.FromDayNumber(day) : null);
    }

    private static IEnumerable<IReadOnlyList<DateOnly?>> Weeks(DateOnly month) =>
        Days(month).Chunk(7).Select(week => (IReadOnlyList<DateOnly?>)week);

    private bool IsDisabled(DateOnly day) => DisabledDate?.Invoke(day) == true;
    private bool IsSelected(DateOnly day) => SelectedDate == day ||
        (RangeStart.HasValue && RangeEnd.HasValue && day >= RangeStart && day <= RangeEnd);
    private bool IsRangePreview(DateOnly day) => RangeStart.HasValue && !RangeEnd.HasValue && RangePreviewEnd.HasValue &&
        ((day >= RangeStart.Value && day <= RangePreviewEnd.Value) || (day >= RangePreviewEnd.Value && day <= RangeStart.Value));

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        _dayElements.Clear();
        if (VisibleMonths is < 1 or > 3)
            throw new ArgumentOutOfRangeException(nameof(VisibleMonths), VisibleMonths, "Visible months must be between 1 and 3.");
        DisplayMonth = new DateOnly(DisplayMonth.Year, DisplayMonth.Month, 1);
        FocusedDate ??= SelectedDate ?? RangeStart ?? Today;
        var lastMonth = DisplayMonth.AddMonths(RenderedMonthCount - 1);
        var focusedMonth = new DateOnly(FocusedDate.Value.Year, FocusedDate.Value.Month, 1);
        if (focusedMonth < DisplayMonth || focusedMonth > lastMonth || IsDisabled(FocusedDate.Value))
        {
            FocusedDate = FirstEnabledDate(DisplayMonth, VisibleMonths);
        }
    }

    private async Task SelectDayAsync(DateOnly day)
    {
        if (IsDisabled(day)) return;
        FocusedDate = day;
        await DateSelected.InvokeAsync(day);
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs e, DateOnly day)
    {
        if (e.AltKey || e.CtrlKey || e.MetaKey || e.ShiftKey) return;
        var target = day;
        switch (e.Key)
        {
            case "ArrowLeft": target = AddDaysClamped(day, -1); break;
            case "ArrowRight": target = AddDaysClamped(day, 1); break;
            case "ArrowUp": target = AddDaysClamped(day, -7); break;
            case "ArrowDown": target = AddDaysClamped(day, 7); break;
            case "Home": target = AddDaysClamped(day, -WeekdayOffset(day)); break;
            case "End": target = AddDaysClamped(day, 6 - WeekdayOffset(day)); break;
            case "PageUp": target = day.Year == 1 && day.Month == 1 ? day : day.AddMonths(-1); break;
            case "PageDown": target = day.Year == 9999 && day.Month == 12 ? day : day.AddMonths(1); break;
            case "Enter":
            case " ": await SelectDayAsync(day); return;
            default: return;
        }
        target = FindEnabledTarget(day, target, e.Key);
        if (!IsDisabled(target))
        {
            FocusedDate = target;
            _pendingFocusDate = target;
            var targetMonth = new DateOnly(target.Year, target.Month, 1);
            var lastMonth = DisplayMonth.AddMonths(RenderedMonthCount - 1);
            if (targetMonth < DisplayMonth)
                await MonthChanged.InvokeAsync(targetMonth);
            else if (targetMonth > lastMonth)
                await MonthChanged.InvokeAsync(targetMonth.AddMonths(1 - VisibleMonths));
            await InvokeAsync(StateHasChanged);
        }
    }

    protected override async Task OnComponentAfterRenderAsync(bool firstRender)
    {
        if (!_pendingFocusDate.HasValue)
        {
            return;
        }

        var target = _pendingFocusDate.Value;
        var targetMonth = new DateOnly(target.Year, target.Month, 1);
        var match = _dayElements.FirstOrDefault(pair => pair.Key.Month == targetMonth && pair.Key.Day == target);
        if (string.IsNullOrEmpty(match.Value.Id))
        {
            return;
        }

        _pendingFocusDate = null;
        await match.Value.FocusAsync(preventScroll: true);
    }

    private static int WeekdayOffset(DateOnly day) => ((int)day.DayOfWeek + 6) % 7;

    private DateOnly FindEnabledTarget(DateOnly origin, DateOnly target, string key)
    {
        if (!IsDisabled(target))
        {
            return target;
        }

        var step = key switch
        {
            "ArrowLeft" or "ArrowUp" or "End" => -1,
            _ => 1
        };
        var limit = key is "Home" or "End" ? 6 : 366;
        for (var offset = 1; offset <= limit; offset++)
        {
            if ((step < 0 && target < DateOnly.MinValue.AddDays(offset)) ||
                (step > 0 && target > DateOnly.MaxValue.AddDays(-offset)))
            {
                break;
            }
            var candidate = target.AddDays(step * offset);
            if (!IsDisabled(candidate))
            {
                return candidate;
            }
        }

        return origin;
    }

    private DateOnly FirstEnabledDate(DateOnly firstMonth, int visibleMonths)
    {
        var first = new DateOnly(firstMonth.Year, firstMonth.Month, 1);
        var lastMonth = first.AddMonths(Math.Min(visibleMonths - 1, MonthDistanceToMax(first)));
        var lastDayNumber = new DateOnly(lastMonth.Year, lastMonth.Month,
            DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month)).DayNumber;
        for (var dayNumber = first.DayNumber; dayNumber <= lastDayNumber; dayNumber++)
        {
            var date = DateOnly.FromDayNumber(dayNumber);
            if (!IsDisabled(date))
            {
                return date;
            }
        }

        return first;
    }

    private Task PreviousMonthAsync() => DisplayMonth <= new DateOnly(1, 1, 1)
        ? Task.CompletedTask
        : MonthChanged.InvokeAsync(DisplayMonth.AddMonths(-1));
    private Task NextMonthAsync() => DisplayMonth >= new DateOnly(9999, 12, 1)
        ? Task.CompletedTask
        : MonthChanged.InvokeAsync(DisplayMonth.AddMonths(1));

    private static int MonthDistanceToMax(DateOnly month) =>
        (9999 - month.Year) * 12 + (12 - month.Month);

    private static DateOnly AddDaysClamped(DateOnly day, int delta) =>
        DateOnly.FromDayNumber(Math.Clamp(day.DayNumber + delta, DateOnly.MinValue.DayNumber, DateOnly.MaxValue.DayNumber));

    private readonly record struct CalendarDayKey(DateOnly Month, DateOnly Day);
}
