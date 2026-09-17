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
    private DateOnly? FocusedDate { get; set; }
    private DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
    private IReadOnlyList<DateOnly> Months => Enumerable.Range(0, VisibleMonths).Select(DisplayMonth.AddMonths).ToArray();
    private string VisibleMonthTitle => VisibleMonths == 1
        ? MonthTitle(DisplayMonth)
        : $"{MonthTitle(DisplayMonth)} – {MonthTitle(DisplayMonth.AddMonths(VisibleMonths - 1))}";
    private IReadOnlyList<string> Weekdays => Enumerable.Range(0, 7)
        .Select(index => Culture.DateTimeFormat.GetAbbreviatedDayName((DayOfWeek)((index + 1) % 7)))
        .ToArray();

    private string MonthTitle(DateOnly month) => month.ToDateTime(TimeOnly.MinValue).ToString("yyyy MMMM", Culture);

    private static IEnumerable<DateOnly> Days(DateOnly month)
    {
        var first = new DateOnly(month.Year, month.Month, 1);
        var offset = ((int)first.DayOfWeek + 6) % 7;
        var start = first.AddDays(-offset);
        return Enumerable.Range(0, 42).Select(start.AddDays);
    }

    private bool IsDisabled(DateOnly day) => DisabledDate?.Invoke(day) == true;
    private bool IsSelected(DateOnly day) => SelectedDate == day ||
        (RangeStart.HasValue && RangeEnd.HasValue && day >= RangeStart && day <= RangeEnd);
    private bool IsRangePreview(DateOnly day) => RangeStart.HasValue && !RangeEnd.HasValue && RangePreviewEnd.HasValue &&
        ((day >= RangeStart.Value && day <= RangePreviewEnd.Value) || (day >= RangePreviewEnd.Value && day <= RangeStart.Value));

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (VisibleMonths is < 1 or > 3)
            throw new ArgumentOutOfRangeException(nameof(VisibleMonths), VisibleMonths, "Visible months must be between 1 and 3.");
        FocusedDate ??= SelectedDate ?? RangeStart ?? Today;
        var lastMonth = DisplayMonth.AddMonths(VisibleMonths - 1);
        var focusedMonth = new DateOnly(FocusedDate.Value.Year, FocusedDate.Value.Month, 1);
        if (focusedMonth < DisplayMonth || focusedMonth > lastMonth)
            FocusedDate = DisplayMonth;
    }

    private async Task SelectDayAsync(DateOnly day)
    {
        if (IsDisabled(day)) return;
        FocusedDate = day;
        await DateSelected.InvokeAsync(day);
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs e, DateOnly day)
    {
        var target = day;
        switch (e.Key)
        {
            case "ArrowLeft": target = day.AddDays(-1); break;
            case "ArrowRight": target = day.AddDays(1); break;
            case "ArrowUp": target = day.AddDays(-7); break;
            case "ArrowDown": target = day.AddDays(7); break;
            case "Home": target = day.AddDays(-WeekdayOffset(day)); break;
            case "End": target = day.AddDays(6 - WeekdayOffset(day)); break;
            case "PageUp": target = day.AddMonths(-1); break;
            case "PageDown": target = day.AddMonths(1); break;
            case "Enter":
            case " ": await SelectDayAsync(day); return;
            default: return;
        }
        if (!IsDisabled(target))
        {
            FocusedDate = target;
            var targetMonth = new DateOnly(target.Year, target.Month, 1);
            var lastMonth = DisplayMonth.AddMonths(VisibleMonths - 1);
            if (targetMonth < DisplayMonth)
                await MonthChanged.InvokeAsync(targetMonth);
            else if (targetMonth > lastMonth)
                await MonthChanged.InvokeAsync(targetMonth.AddMonths(1 - VisibleMonths));
            await InvokeAsync(StateHasChanged);
        }
    }

    private static int WeekdayOffset(DateOnly day) => ((int)day.DayOfWeek + 6) % 7;

    private Task PreviousMonthAsync() => MonthChanged.InvokeAsync(DisplayMonth.AddMonths(-1));
    private Task NextMonthAsync() => MonthChanged.InvokeAsync(DisplayMonth.AddMonths(1));
}
