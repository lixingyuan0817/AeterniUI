using System.Linq.Expressions;
using AeterniUI.Attributes;
using AeterniUI.Components.FormField;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace AeterniUI.Components.Transfer;

/// <summary>
/// 双列表迁移：源列常驻展示候选，目标列常驻展示已选，迁移按钮在两侧之间搬运勾选项。
/// </summary>
/// <remarks>
/// 数据归宿主：<see cref="Items" /> 是候选全集并决定源列顺序，<see cref="SelectedValues" />
/// 决定目标列顺序。勾选只是「待迁移」的瞬态状态，不会写回宿主；两列的键盘焦点都留在各自的
/// listbox 容器上，活动项用 <c>aria-activedescendant</c> 表达，条目本身不进入 Tab 序。
/// JS 只负责把容器上的导航/勾选键转发给 C# 并把活动项滚入列视口，业务状态全部留在 C#。
/// </remarks>
[JsModule("Components/Transfer/Transfer.razor.js", Name = "transfer", Interactive = true)]
public partial class Transfer<TItem> : AeterniComponent where TItem : class
{
    [CascadingParameter]
    private FormFieldContext? FormField { get; set; }

    [CascadingParameter]
    private EditContext? CascadedEditContext { get; set; }

    private readonly EditContextSubscription _validation;

    public Transfer() =>
        _validation = new EditContextSubscription(RequestStateHasChanged);

    /// <summary>两列按 DOM 顺序渲染；迁移按钮插在源列之后、目标列之前。</summary>
    private static readonly TransferSide[] Sides = [TransferSide.Source, TransferSide.Target];

    /// <summary>源列全部条目（<see cref="Items" /> 去掉已选），顺序由宿主给出的候选顺序决定。</summary>
    private readonly List<TItem> _source = [];

    /// <summary>目标列全部条目，顺序即 <see cref="SelectedValues" /> 的顺序。</summary>
    private readonly List<TItem> _target = [];

    private readonly List<TItem> _visibleSource = [];
    private readonly List<TItem> _visibleTarget = [];

    /// <summary>瞬态的「待迁移」勾选集合，按值比较；过滤变化时按列清空。</summary>
    private readonly HashSet<TItem> _checkedSource = new(EqualityComparer<TItem>.Default);
    private readonly HashSet<TItem> _checkedTarget = new(EqualityComparer<TItem>.Default);

    private TItem? _activeSource;
    private TItem? _activeTarget;
    private string _sourceFilter = string.Empty;
    private string _targetFilter = string.Empty;

    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;

    /// <summary>候选全集，同时决定源列的渲染顺序。</summary>
    [Parameter]
    public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>已选项，受控值，同时决定目标列的渲染顺序。</summary>
    [Parameter]
    public IReadOnlyList<TItem> SelectedValues { get; set; } = [];

    /// <summary>已选项变化回调；迁移、剔除不在候选集里的值都会触发。</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<TItem>> SelectedValuesChanged { get; set; }

    /// <summary>已选项的绑定表达式，供表单校验推导字段标识。</summary>
    [Parameter]
    public Expression<Func<IReadOnlyList<TItem>>>? SelectedValuesExpression { get; set; }

    /// <summary>取条目文案；缺省用 <c>ToString()</c>。</summary>
    [Parameter]
    public Func<TItem, string>? TextSelector { get; set; }

    /// <summary>判定条目是否不可迁移；不可迁移的条目仍会渲染，但不能勾选或迁移。</summary>
    [Parameter]
    public Func<TItem, bool>? DisabledSelector { get; set; }

    /// <summary>条目内容模板；缺省渲染 <see cref="TextSelector" /> 给出的文案。</summary>
    [Parameter]
    public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>源列标题，同时作为源 listbox 的可访问名；缺省取 <c>UiText.TransferSourceTitle</c>。</summary>
    [Parameter]
    public string? SourceTitle { get; set; }

    /// <summary>目标列标题，同时作为目标 listbox 的可访问名；缺省取 <c>UiText.TransferTargetTitle</c>。</summary>
    [Parameter]
    public string? TargetTitle { get; set; }

    /// <summary>是否为每列渲染本地搜索框；过滤只影响可见行，不改变已选项。</summary>
    [Parameter]
    public bool Searchable { get; set; }

    /// <summary>搜索框占位文案；缺省取 <c>UiText.TransferSearchPlaceholder</c>。</summary>
    [Parameter]
    public string? SearchPlaceholder { get; set; }

    /// <summary>控件档位，行高与字号按档位收敛。</summary>
    [Parameter]
    public Size Size { get; set; } = Size.Default;

    /// <summary>是否呈现校验失败状态。</summary>
    [Parameter]
    public bool Invalid { get; set; }

    /// <summary>是否呈现必填状态。</summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>某一列没有可见条目时显示的内容；缺省取 <c>UiText.TransferEmptyText</c>。</summary>
    [Parameter]
    public RenderFragment? EmptyContent { get; set; }

    /// <summary>根 group 的可访问名；缺省取 <c>UiText.TransferLabel</c>。</summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>根 group 的补充说明 id，会与表单字段给出的说明合并。</summary>
    [Parameter]
    public string? AriaDescribedBy { get; set; }

    private bool IsDisabled => Disabled || (FormField?.Disabled ?? false);

    private bool IsInvalid => Invalid ||
        (FormField?.Invalid ?? false) ||
        (_hasFieldIdentifier && _validation.HasValidationMessages(_fieldIdentifier));

    private bool IsRequired => Required || (FormField?.Required ?? false);

    private string? FormFieldLabelId => FormField?.LabelId;

    private string? EffectiveDescribedBy => string.Join(" ", new[] { AriaDescribedBy, FormField?.DescribedBy }
        .Where(value => !string.IsNullOrWhiteSpace(value))) is { Length: > 0 } merged ? merged : null;

    private string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel) ? UiText.TransferLabel : AriaLabel.Trim();

    private string SourceTitleText => string.IsNullOrWhiteSpace(SourceTitle) ? UiText.TransferSourceTitle : SourceTitle.Trim();

    private string TargetTitleText => string.IsNullOrWhiteSpace(TargetTitle) ? UiText.TransferTargetTitle : TargetTitle.Trim();

    private string SearchPlaceholderText => string.IsNullOrWhiteSpace(SearchPlaceholder)
        ? UiText.TransferSearchPlaceholder
        : SearchPlaceholder;

    private string MoveSelectedToTargetLabel => UiText.TransferMoveSelectedLabel;

    private string MoveSelectedToSourceLabel => UiText.TransferRemoveSelectedLabel;

    private string MoveAllToTargetLabel => UiText.TransferMoveAllLabel;

    private string MoveAllToSourceLabel => UiText.TransferRemoveAllLabel;

    private bool CanMoveCheckedToTarget =>
        !IsDisabled && _visibleSource.Any(item => _checkedSource.Contains(item) && !IsItemDisabled(item));

    private bool CanMoveCheckedToSource =>
        !IsDisabled && _visibleTarget.Any(item => _checkedTarget.Contains(item) && !IsItemDisabled(item));

    private bool CanMoveAllToTarget => !IsDisabled && _visibleSource.Any(item => !IsItemDisabled(item));

    private bool CanMoveAllToSource => !IsDisabled && _visibleTarget.Any(item => !IsItemDisabled(item));

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!Enum.IsDefined(Size))
        {
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "Unknown transfer size.");
        }

        ArgumentNullException.ThrowIfNull(Items);
        ArgumentNullException.ThrowIfNull(SelectedValues);
        UpdateEditContextSubscription();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        // 与 List 同一约定：SelectedValues 必须是 Items 的子集，参数同步时剔除越界值并
        // 通知宿主，避免目标列渲染出候选集之外、永远无法回到源列的条目。
        var retained = SelectedValues.Where(value => Items.Any(item => ValuesEqual(item, value))).ToArray();
        if (retained.Length != SelectedValues.Count)
        {
            SelectedValues = retained;
            await SelectedValuesChanged.InvokeAsync(retained);
        }

        RebuildLists();
        PruneChecked();
        EnsureActive();

        if (IsDisabled || !Visible)
        {
            _activeSource = null;
            _activeTarget = null;
        }
    }

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-transfer")
        .Add(SizeClass)
        .Add("is-invalid", IsInvalid)
        .Add("is-disabled", IsDisabled);

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(
            base.BuildAttributes(),
            StringComparer.OrdinalIgnoreCase);

        attributes["role"] = "group";

        // A surrounding FormField already owns the accessible name through its
        // label; only name the group ourselves when there is no such label.
        if (FormFieldLabelId is null)
        {
            attributes["aria-label"] = EffectiveAriaLabel;
        }
        else
        {
            attributes["aria-labelledby"] = FormFieldLabelId;
        }

        if (EffectiveDescribedBy is not null)
        {
            attributes["aria-describedby"] = EffectiveDescribedBy;
        }

        if (IsInvalid)
        {
            attributes["aria-invalid"] = "true";
        }

        if (IsDisabled)
        {
            attributes["aria-disabled"] = "true";
        }

        return attributes;
    }

    protected override Task OnComponentAfterRenderAsync(bool firstRender) =>
        JsModuleManager.InvokeModuleVoidAsync("transfer", "sync", InstanceId, RootElement);

    protected override ValueTask OnComponentDisposeAsync()
    {
        _validation.Detach();
        _source.Clear();
        _target.Clear();
        _visibleSource.Clear();
        _visibleTarget.Clear();
        _checkedSource.Clear();
        _checkedTarget.Clear();
        _activeSource = null;
        _activeTarget = null;
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Handles browser navigation in the focused column. The module only installs a
    /// listener on each listbox; the active item, the checked set and the selected
    /// values remain in C#.
    /// </summary>
    [JSInvokable]
    public Task HandleKeyFromBrowserAsync(string side, string key)
    {
        if (!TryParseSide(side, out var parsed))
        {
            return Task.CompletedTask;
        }

        return HandleKeyAsync(parsed, key);
    }

    // ---------------------------------------------------------------- 数据同步

    private string? SizeClass => ComponentClass.ForSize("aeterni-transfer", Size);

    private void RebuildLists()
    {
        _source.Clear();
        foreach (var item in Items)
        {
            if (!IsSelected(item))
            {
                _source.Add(item);
            }
        }

        _target.Clear();
        _target.AddRange(SelectedValues);

        ApplyFilters();
    }

    private void ApplyFilters()
    {
        FilterInto(_source, _sourceFilter, _visibleSource);
        FilterInto(_target, _targetFilter, _visibleTarget);
    }

    private void FilterInto(List<TItem> source, string filter, List<TItem> visible)
    {
        visible.Clear();
        foreach (var item in source)
        {
            if (filter.Length == 0 || DisplayText(item).Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                visible.Add(item);
            }
        }
    }

    private void PruneChecked()
    {
        _checkedSource.RemoveWhere(item => !_visibleSource.Any(visible => ValuesEqual(visible, item)));
        _checkedTarget.RemoveWhere(item => !_visibleTarget.Any(visible => ValuesEqual(visible, item)));
    }

    private void EnsureActive()
    {
        if (_activeSource is not null && !_visibleSource.Any(item => ValuesEqual(item, _activeSource)))
        {
            _activeSource = null;
        }

        if (_activeTarget is not null && !_visibleTarget.Any(item => ValuesEqual(item, _activeTarget)))
        {
            _activeTarget = null;
        }
    }

    // ---------------------------------------------------------------- 渲染辅助

    private List<TItem> VisibleFor(TransferSide side) => side == TransferSide.Source ? _visibleSource : _visibleTarget;

    private TItem? ActiveFor(TransferSide side) => side == TransferSide.Source ? _activeSource : _activeTarget;

    private void SetActive(TransferSide side, TItem? item)
    {
        if (side == TransferSide.Source)
        {
            _activeSource = item;
        }
        else
        {
            _activeTarget = item;
        }
    }

    private HashSet<TItem> CheckedFor(TransferSide side) =>
        side == TransferSide.Source ? _checkedSource : _checkedTarget;

    private string TitleFor(TransferSide side) =>
        side == TransferSide.Source ? SourceTitleText : TargetTitleText;

    private string ListIdFor(TransferSide side) => $"{ElementId}-{SideName(side)}";

    private string OptionId(TransferSide side, int index) => $"{ListIdFor(side)}-option-{index}";

    private string? ActiveOptionId(TransferSide side)
    {
        var active = ActiveFor(side);
        if (active is null)
        {
            return null;
        }

        var index = IndexOf(VisibleFor(side), active);
        return index < 0 ? null : OptionId(side, index);
    }

    private bool IsActive(TransferSide side, TItem item) => ValuesEqual(item, ActiveFor(side));

    private bool IsChecked(TransferSide side, TItem item) => CheckedFor(side).Contains(item);

    private bool IsItemDisabled(TItem item) => DisabledSelector?.Invoke(item) == true;

    private string DisplayText(TItem item) => TextSelector?.Invoke(item) ?? item.ToString() ?? string.Empty;

    private string FilterFor(TransferSide side) =>
        side == TransferSide.Source ? _sourceFilter : _targetFilter;

    private static string SideName(TransferSide side) => side == TransferSide.Source ? "source" : "target";

    private static bool TryParseSide(string? side, out TransferSide parsed)
    {
        switch (side)
        {
            case "source":
                parsed = TransferSide.Source;
                return true;
            case "target":
                parsed = TransferSide.Target;
                return true;
            default:
                parsed = TransferSide.Source;
                return false;
        }
    }

    // ---------------------------------------------------------------- 交互

    private async Task HandleFilterAsync(string side, ChangeEventArgs args)
    {
        if (IsDisabled || !Visible || !TryParseSide(side, out var parsed))
        {
            return;
        }

        var text = args.Value as string ?? string.Empty;
        if (parsed == TransferSide.Source)
        {
            _sourceFilter = text;
            _checkedSource.Clear();
            _activeSource = null;
        }
        else
        {
            _targetFilter = text;
            _checkedTarget.Clear();
            _activeTarget = null;
        }

        ApplyFilters();
        await InvokeAsync(StateHasChanged);
    }

    private async Task HandleOptionClickAsync(string side, int index)
    {
        if (IsDisabled || !Visible || !TryParseSide(side, out var parsed))
        {
            return;
        }

        var items = VisibleFor(parsed);
        if (index < 0 || index >= items.Count)
        {
            return;
        }

        var item = items[index];
        SetActive(parsed, item);
        ToggleChecked(parsed, item);
        await InvokeAsync(StateHasChanged);
    }

    private void ToggleChecked(TransferSide side, TItem item)
    {
        if (IsDisabled || !Visible || IsItemDisabled(item))
        {
            return;
        }

        var checkedItems = CheckedFor(side);
        if (!checkedItems.Remove(item))
        {
            checkedItems.Add(item);
        }
    }

    private async Task HandleKeyAsync(TransferSide side, string key)
    {
        if (IsDisposed || IsDisabled || !Visible)
        {
            return;
        }

        if (key is "ArrowDown" or "ArrowUp" or "Home" or "End")
        {
            MoveActive(side, key);
            await InvokeAsync(StateHasChanged);
            return;
        }

        if (key is " " or "Enter")
        {
            var active = ActiveFor(side);
            if (active is null)
            {
                active = FirstEnabled(VisibleFor(side));
                SetActive(side, active);
            }

            if (active is not null)
            {
                ToggleChecked(side, active);
            }

            await InvokeAsync(StateHasChanged);
        }
    }

    private void MoveActive(TransferSide side, string key)
    {
        var items = VisibleFor(side);
        var enabled = new List<int>();
        for (var index = 0; index < items.Count; index++)
        {
            if (!IsItemDisabled(items[index]))
            {
                enabled.Add(index);
            }
        }

        if (enabled.Count == 0)
        {
            SetActive(side, null);
            return;
        }

        var current = -1;
        if (ActiveFor(side) is { } active)
        {
            current = enabled.FindIndex(index => ValuesEqual(items[index], active));
        }

        var next = key switch
        {
            "Home" => 0,
            "End" => enabled.Count - 1,
            "ArrowDown" => current < 0 ? 0 : (current + 1) % enabled.Count,
            "ArrowUp" => current < 0 ? enabled.Count - 1 : (current - 1 + enabled.Count) % enabled.Count,
            _ => Math.Max(current, 0)
        };

        SetActive(side, items[enabled[next]]);
    }

    private TItem? FirstEnabled(IReadOnlyList<TItem> items)
    {
        for (var index = 0; index < items.Count; index++)
        {
            if (!IsItemDisabled(items[index]))
            {
                return items[index];
            }
        }

        return null;
    }

    private async Task MoveCheckedToTargetAsync()
    {
        if (IsDisabled || !Visible)
        {
            return;
        }

        var moving = _visibleSource
            .Where(item => _checkedSource.Contains(item) && !IsItemDisabled(item))
            .ToArray();
        if (moving.Length == 0)
        {
            return;
        }

        var next = SelectedValues.ToList();
        foreach (var item in moving)
        {
            if (!next.Any(value => ValuesEqual(value, item)))
            {
                next.Add(item);
            }
        }

        await CommitAsync(next);
    }

    private async Task MoveCheckedToSourceAsync()
    {
        if (IsDisabled || !Visible)
        {
            return;
        }

        var moving = _visibleTarget
            .Where(item => _checkedTarget.Contains(item) && !IsItemDisabled(item))
            .ToArray();
        if (moving.Length == 0)
        {
            return;
        }

        var next = SelectedValues
            .Where(value => !moving.Any(item => ValuesEqual(item, value)))
            .ToList();
        await CommitAsync(next);
    }

    private async Task MoveAllToTargetAsync()
    {
        if (IsDisabled || !Visible)
        {
            return;
        }

        var moving = _visibleSource.Where(item => !IsItemDisabled(item)).ToArray();
        if (moving.Length == 0)
        {
            return;
        }

        var next = SelectedValues.ToList();
        foreach (var item in moving)
        {
            if (!next.Any(value => ValuesEqual(value, item)))
            {
                next.Add(item);
            }
        }

        await CommitAsync(next);
    }

    private async Task MoveAllToSourceAsync()
    {
        if (IsDisabled || !Visible)
        {
            return;
        }

        var moving = _visibleTarget.Where(item => !IsItemDisabled(item)).ToArray();
        if (moving.Length == 0)
        {
            return;
        }

        var next = SelectedValues
            .Where(value => !moving.Any(item => ValuesEqual(item, value)))
            .ToList();
        await CommitAsync(next);
    }

    private async Task CommitAsync(IReadOnlyList<TItem> next)
    {
        SelectedValues = next;
        await SelectedValuesChanged.InvokeAsync(next);
        NotifyFieldChanged();

        // Checked state and the keyboard cursor describe the previous columns: a
        // move changes both sides, so neither survives it.
        _checkedSource.Clear();
        _checkedTarget.Clear();
        _activeSource = null;
        _activeTarget = null;
        RebuildLists();
        await InvokeAsync(StateHasChanged);
    }

    // ---------------------------------------------------------------- 表单

    private void UpdateEditContextSubscription()
    {
        _validation.Attach(SelectedValuesExpression is null ? null : CascadedEditContext);

        if (SelectedValuesExpression is not null)
        {
            _fieldIdentifier = FieldIdentifier.Create(SelectedValuesExpression);
            _hasFieldIdentifier = true;
        }
        else
        {
            _hasFieldIdentifier = false;
        }
    }

    private void NotifyFieldChanged()
    {
        if (_hasFieldIdentifier)
        {
            _validation.NotifyFieldChanged(_fieldIdentifier);
        }
    }

    // ---------------------------------------------------------------- 工具

    private bool IsSelected(TItem item) => SelectedValues.Any(value => ValuesEqual(value, item));

    private static int IndexOf(IReadOnlyList<TItem> items, TItem item)
    {
        for (var index = 0; index < items.Count; index++)
        {
            if (ValuesEqual(items[index], item))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool ValuesEqual(TItem? left, TItem? right) =>
        EqualityComparer<TItem>.Default.Equals(left!, right!);

    private enum TransferSide
    {
        Source,
        Target
    }
}
