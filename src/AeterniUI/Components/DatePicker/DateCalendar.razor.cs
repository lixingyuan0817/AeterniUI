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

    private CultureInfo Culture => CultureInfo.CurrentCulture;
    private DateOnly? FocusedDate { get; set; }
    private string MonthTitle => DisplayMonth.ToDateTime(TimeOnly.MinValue).ToString("yyyy MMMM", Culture);
    private IReadOnlyList<string> Weekdays => Enumerable.Range(0, 7)
        .Select(index => Culture.DateTimeFormat.GetAbbreviatedDayName((DayOfWeek)((index + 1) % 7)))
        .ToArray();
    private IEnumerable<DateOnly> Days
    {
        get
        {
            var first = new DateOnly(DisplayMonth.Year, DisplayMonth.Month, 1);
            var offset = ((int)first.DayOfWeek + 6) % 7;
            var start = first.AddDays(-offset);
            return Enumerable.Range(0, 42).Select(start.AddDays);
        }
    }

    private bool IsDisabled(DateOnly day) => DisabledDate?.Invoke(day) == true;
    private bool IsSelected(DateOnly day) => SelectedDate == day ||
        (RangeStart.HasValue && RangeEnd.HasValue && day >= RangeStart && day <= RangeEnd);

    protected override void OnParametersSet()
    {
        FocusedDate ??= SelectedDate ?? RangeStart ?? DateOnly.FromDateTime(DateTime.Today);
        if (FocusedDate.Value.Year != DisplayMonth.Year || FocusedDate.Value.Month != DisplayMonth.Month)
            FocusedDate = new DateOnly(DisplayMonth.Year, DisplayMonth.Month, 1);
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
            case "Home": target = day.AddDays(-(int)day.DayOfWeek + 1); break;
            case "End": target = day.AddDays(7 - (int)day.DayOfWeek); break;
            case "PageUp": target = day.AddMonths(-1); break;
            case "PageDown": target = day.AddMonths(1); break;
            case "Enter":
            case " ": await SelectDayAsync(day); return;
            default: return;
        }
        if (!IsDisabled(target))
        {
            FocusedDate = target;
            if (target.Month != DisplayMonth.Month || target.Year != DisplayMonth.Year)
                await MonthChanged.InvokeAsync(new DateOnly(target.Year, target.Month, 1));
            await InvokeAsync(StateHasChanged);
        }
    }

    private Task PreviousMonthAsync() => MonthChanged.InvokeAsync(DisplayMonth.AddMonths(-1));
    private Task NextMonthAsync() => MonthChanged.InvokeAsync(DisplayMonth.AddMonths(1));
}
