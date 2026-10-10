using System.ComponentModel;
using System.Globalization;
using AeterniUI.Attributes;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AeterniUI.Components.TimePicker;

/// <summary>Internal time selection surface shared by TimePicker and DateTimePicker.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[JsModule("Components/TimePicker/TimeOptionList.razor.js", Name = "time-option-list", Interactive = true)]
public partial class TimeOptionList : AeterniComponent
{
    private const string ModuleName = "time-option-list";
    private static readonly TimeUnit[] Units = [TimeUnit.Hour, TimeUnit.Minute, TimeUnit.Second];

    [Parameter] public TimeOnly? Value { get; set; }
    [Parameter] public EventCallback<TimeOnly> ValueSelected { get; set; }
    [Parameter] public EventCallback<TimeOnly> DraftChanged { get; set; }
    [Parameter] public EventCallback CancelRequested { get; set; }
    [Parameter] public TimeSpan Step { get; set; } = TimeSpan.FromSeconds(1);
    [Parameter] public TimeOnly? MinTime { get; set; }
    [Parameter] public TimeOnly? MaxTime { get; set; }
    [Parameter] public Func<TimeOnly, bool>? DisabledTime { get; set; }
    [Parameter] public TimeFormat TimeFormat { get; set; } = TimeFormat.TwentyFourHour;
    // Defaults are empty on purpose: a hard-coded English default would shadow
    // the matching AeterniUITextOptions entry and make the component ignore the
    // host's localisation. Leaving a parameter unset now falls back to the table.
    [Parameter] public string? AriaLabel { get; set; }
    [Parameter] public string? EmptyText { get; set; }
    [Parameter] public string? CancelText { get; set; }
    [Parameter] public string? ConfirmText { get; set; }

    private string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel) ? UiText.TimePickerOptionsLabel : AriaLabel.Trim();
    private string EffectiveEmptyText => string.IsNullOrWhiteSpace(EmptyText) ? UiText.TimePickerEmptyText : EmptyText.Trim();
    private string EffectiveCancelText => string.IsNullOrWhiteSpace(CancelText) ? UiText.TimePickerCancelText : CancelText.Trim();
    private string EffectiveConfirmText => string.IsNullOrWhiteSpace(ConfirmText) ? UiText.TimePickerConfirmText : ConfirmText.Trim();

    private TimeSelectionMap _map = default!;
    private TimeOnly? _draftValue;
    private TimeOnly? _lastParameterValue;
    private bool _parametersInitialized;
    private int _activeUnitIndex;
    private bool _scrollActiveIntoView;
    private bool _animateActiveIntoView;
    private long _alignmentRevision;
    private CultureInfo Culture => CultureInfo.CurrentCulture;
    private TimeUnit ActiveUnit => Units[_activeUnitIndex];
    private string? ActiveOptionId => _draftValue.HasValue
        ? OptionId(ActiveUnit, ValueForUnit(ActiveUnit, _draftValue.Value))
        : null;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!Enum.IsDefined(TimeFormat))
        {
            throw new ArgumentOutOfRangeException(nameof(TimeFormat), TimeFormat, "Unknown time option list format.");
        }

        _map = TimePickerOptions.CreateMap(Step, MinTime, MaxTime, DisabledTime, _map);
        var preferred = !_parametersInitialized || Value != _lastParameterValue ? Value : _draftValue;
        _lastParameterValue = Value;
        var nextDraft = preferred.HasValue && _map.Contains(preferred.Value)
            ? preferred
            : _map.FindClosest(preferred);
        if (_draftValue != nextDraft)
        {
            _draftValue = nextDraft;
            _activeUnitIndex = Units.Length - 1;
            _animateActiveIntoView = _parametersInitialized;
            _scrollActiveIntoView = true;
        }
        _parametersInitialized = true;
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-time-option-list")
        .Add("is-empty", _map.Count == 0);

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(base.BuildAttributes(), StringComparer.OrdinalIgnoreCase)
        {
            ["role"] = "group",
            ["aria-label"] = EffectiveAriaLabel,
            ["tabindex"] = _map.Count == 0 ? "-1" : "0"
        };

        if (ActiveOptionId is not null)
        {
            attributes["aria-activedescendant"] = ActiveOptionId;
        }

        return attributes;
    }

    protected override async Task OnComponentAfterRenderAsync(bool firstRender)
    {
        var shouldAlign = _scrollActiveIntoView || firstRender;
        var animateAlignment = _animateActiveIntoView && !firstRender;
        _scrollActiveIntoView = false;
        _animateActiveIntoView = false;
        await JsModuleManager.InvokeModuleVoidAsync(
            ModuleName,
            "sync",
            InstanceId,
            RootElement,
            ActiveOptionId,
            shouldAlign,
            animateAlignment,
            _alignmentRevision);
    }

    private IReadOnlyList<int> ValuesFor(TimeUnit unit)
    {
        if (!_draftValue.HasValue)
        {
            return [];
        }

        return unit switch
        {
            TimeUnit.Hour => _map.Hours(),
            TimeUnit.Minute => _map.Minutes(_draftValue.Value.Hour),
            TimeUnit.Second => _map.Seconds(_draftValue.Value.Hour, _draftValue.Value.Minute),
            _ => []
        };
    }

    private string OptionId(TimeUnit unit, int value) => $"{ElementId}-{unit.ToString().ToLowerInvariant()}-{value}";

    private string ColumnLabel(TimeUnit unit) => unit switch
    {
        TimeUnit.Hour => UiText.TimePickerHourLabel,
        TimeUnit.Minute => UiText.TimePickerMinuteLabel,
        TimeUnit.Second => UiText.TimePickerSecondLabel,
        _ => string.Empty
    };

    private string OptionLabel(TimeUnit unit, int value) => unit switch
    {
        TimeUnit.Hour when TimePickerOptions.UsesTwelveHourClock(TimeFormat, Culture) =>
            new TimeOnly(value, 0).ToString("h tt", Culture),
        TimeUnit.Hour => value.ToString("00", Culture),
        _ => value.ToString("00", Culture)
    };

    private bool IsSelected(TimeUnit unit, int value) =>
        _draftValue.HasValue && ValueForUnit(unit, _draftValue.Value) == value;

    private bool IsActive(TimeUnit unit, int value) =>
        unit == ActiveUnit && IsSelected(unit, value);

    private async Task SelectUnitAsync(TimeUnit unit, int value, bool animateAlignment = true)
    {
        var candidate = ResolveCandidate(unit, value);
        if (!candidate.HasValue)
        {
            return;
        }

        var changed = _draftValue != candidate;
        _draftValue = candidate;
        _activeUnitIndex = unit switch
        {
            TimeUnit.Hour => 0,
            TimeUnit.Minute => 1,
            TimeUnit.Second => 2,
            _ => 0
        };
        if (animateAlignment)
        {
            _alignmentRevision++;
            _scrollActiveIntoView = true;
            _animateActiveIntoView = true;
        }
        if (changed) await DraftChanged.InvokeAsync(candidate.Value);
    }

    private async Task ConfirmAsync()
    {
        // A click or Enter can arrive before the scroll debounce has published its draft.
        var pending = await JsModuleManager.InvokeModuleAsync<Dictionary<string, int>>(
            ModuleName, "pendingValues", InstanceId);
        foreach (var unit in Units)
        {
            if (pending is not null &&
                pending.TryGetValue(unit.ToString().ToLowerInvariant(), out var value) &&
                ValuesFor(unit).Contains(value))
            {
                _draftValue = ResolveCandidate(unit, value);
            }
        }

        if (_draftValue.HasValue)
        {
            await ValueSelected.InvokeAsync(_draftValue.Value);
        }
    }

    private Task CancelAsync() => CancelRequested.InvokeAsync();

    [JSInvokable]
    public async Task OnWheelChangedAsync(string unitName, int value)
    {
        // The wheel module reports through a ResizeObserver callback and a
        // reduced-motion change listener, so a queued call can still arrive after
        // the component was released.
        if (IsDisposed ||
            !Enum.TryParse<TimeUnit>(unitName, ignoreCase: true, out var unit) ||
            !Units.Contains(unit) ||
            !ValuesFor(unit).Contains(value))
        {
            return;
        }

        await SelectUnitAsync(unit, value, animateAlignment: false);
        await InvokeAsync(StateHasChanged);
    }

    private TimeOnly? ResolveCandidate(TimeUnit unit, int value)
    {
        if (!_draftValue.HasValue)
        {
            return null;
        }

        var current = _draftValue.Value;
        var preferred = unit switch
        {
            TimeUnit.Hour => new TimeOnly(value, current.Minute, current.Second),
            TimeUnit.Minute => new TimeOnly(current.Hour, value, current.Second),
            TimeUnit.Second => new TimeOnly(current.Hour, current.Minute, value),
            _ => current
        };
        if (_map.Contains(preferred))
        {
            return preferred;
        }

        return unit switch
        {
            TimeUnit.Hour => _map.FindClosest(preferred, hour: value),
            TimeUnit.Minute => _map.FindClosest(preferred, current.Hour, value),
            TimeUnit.Second => null,
            _ => null
        };
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (!_draftValue.HasValue)
        {
            return;
        }

        if (args.Key is "ArrowLeft" or "ArrowRight")
        {
            var delta = args.Key == "ArrowLeft" ? -1 : 1;
            _activeUnitIndex = Math.Clamp(_activeUnitIndex + delta, 0, Units.Length - 1);
            _scrollActiveIntoView = true;
            return;
        }

        if (args.Key is "Enter" or " ")
        {
            await ConfirmAsync();
            return;
        }

        var values = ValuesFor(ActiveUnit);
        if (values.Count == 0)
        {
            return;
        }

        var current = ValueForUnit(ActiveUnit, _draftValue.Value);
        var index = IndexOf(values, current);
        var target = args.Key switch
        {
            "ArrowDown" => Math.Min(index + 1, values.Count - 1),
            "ArrowUp" => Math.Max(index - 1, 0),
            "Home" => 0,
            "End" => values.Count - 1,
            _ => -1
        };

        if (target >= 0)
        {
            await SelectUnitAsync(ActiveUnit, values[target]);
        }
    }

    private static int ValueForUnit(TimeUnit unit, TimeOnly value) => unit switch
    {
        TimeUnit.Hour => value.Hour,
        TimeUnit.Minute => value.Minute,
        TimeUnit.Second => value.Second,
        _ => 0
    };

    private static int IndexOf(IReadOnlyList<int> values, int value)
    {
        for (var index = 0; index < values.Count; index++)
        {
            if (values[index] == value)
            {
                return index;
            }
        }

        return 0;
    }

    private enum TimeUnit
    {
        Hour,
        Minute,
        Second
    }
}
