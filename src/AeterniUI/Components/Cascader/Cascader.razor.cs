using System.Linq.Expressions;
using AeterniUI.Attributes;
using AeterniUI.Components.FormField;
using AeterniUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace AeterniUI.Components.Cascader;

/// <summary>
/// 层级单选：触发器 + 「一列一级」的浮层面板。宿主给出根节点、取子节点与取文案的方式，
/// 组件负责展开、键盘导航与路径展示；<see cref="Value" /> 绑定到叶子节点，祖先链由组件反查。
/// </summary>
/// <remarks>
/// 与 <c>ComboBox</c> 同构：焦点始终留在触发器（单一 Tab 停留点），浮层里用
/// <c>aria-activedescendant</c> 指向活动项；面板、定位、外部点击与滚动关闭全部复用
/// <c>PopupHost</c> + <c>Popover</c>。业务状态留在 C#，JS 只做「活动项滚入视口」与
/// 导航键的 <c>preventDefault</c>。
/// </remarks>
[JsModule("Components/Cascader/Cascader.razor.js", Name = "cascader", Interactive = true)]
public partial class Cascader<TItem> : AeterniComponent where TItem : class
{
    [CascadingParameter]
    private FormFieldContext? FormField { get; set; }

    [CascadingParameter]
    private EditContext? CascadedEditContext { get; set; }

    private readonly EditContextSubscription _validation;

    public Cascader() =>
        _validation = new EditContextSubscription(RequestStateHasChanged);

    [Parameter]
    public TItem? Value { get; set; }

    [Parameter]
    public EventCallback<TItem?> ValueChanged { get; set; }

    [Parameter]
    public Expression<Func<TItem?>>? ValueExpression { get; set; }

    /// <summary>根节点。为空时触发器只显示占位文案。</summary>
    [Parameter]
    public IReadOnlyList<TItem>? Items { get; set; }

    /// <summary>
    /// 取子节点。返回空序列或 <c>null</c> 即叶子；为 <c>null</c> 时整棵树按全叶子处理
    /// （等价于平铺，仍然是合法用法）。
    /// </summary>
    [Parameter]
    public Func<TItem, IEnumerable<TItem>?>? ChildrenSelector { get; set; }

    /// <summary>取显示文案；缺省用 <c>ToString()</c>。</summary>
    [Parameter]
    public Func<TItem, string>? TextSelector { get; set; }

    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>连接路径各层的分隔符；缺省取 <c>UiText.CascaderPathSeparator</c>。</summary>
    [Parameter]
    public string? Separator { get; set; }

    [Parameter]
    public bool Invalid { get; set; }

    [Parameter]
    public bool Required { get; set; }

    /// <summary>控件档位，与 <c>ComboBox</c>／<c>Input</c> 共用同一套高度别名。</summary>
    [Parameter]
    public Size Size { get; set; } = Size.Default;

    [Parameter]
    public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>某一列没有选项时显示的内容。</summary>
    [Parameter]
    public RenderFragment? EmptyContent { get; set; }

    [Parameter]
    public string? AriaLabel { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    [Parameter]
    public bool FullWidth { get; set; }

    /// <summary>确认一个叶子后触发，晚于 <see cref="ValueChanged" />。</summary>
    [Parameter]
    public EventCallback<TItem?> OnItemSelected { get; set; }

    private bool _open;
    private readonly List<TItem> _expanded = [];
    private int _activeColumn;
    private int _activeIndex = -1;
    private readonly List<List<TItem>> _columns = [];
    private ElementReference _triggerElement;

    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;

    private string PanelId => $"{ElementId}-panel";

    private string? FormFieldInputId => FormField?.InputId;
    private string? FormFieldLabelId => FormField?.LabelId;

    private string? EffectiveDescribedBy =>
        string.Join(" ", new[] { AriaDescribedBy, FormField?.DescribedBy }
            .Where(value => !string.IsNullOrWhiteSpace(value))) is { Length: > 0 } merged ? merged : null;

    private string PlaceholderText => string.IsNullOrWhiteSpace(Placeholder) ? UiText.CascaderPlaceholder : Placeholder;

    private string PathSeparator => string.IsNullOrWhiteSpace(Separator) ? UiText.CascaderPathSeparator : Separator;

    private string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel) ? UiText.CascaderLabel : AriaLabel.Trim();

    private string ListLabel => string.IsNullOrWhiteSpace(AriaLabel) ? UiText.CascaderColumnLabel : AriaLabel.Trim();

    private bool IsInvalid =>
        Invalid ||
        (FormField?.Invalid ?? false) ||
        (_hasFieldIdentifier && _validation.HasValidationMessages(_fieldIdentifier));

    private bool IsDisabled => Disabled || (FormField?.Disabled ?? false);

    private bool IsRequired => Required || (FormField?.Required ?? false);

    /// <summary>活动项的 id，供触发器的 <c>aria-activedescendant</c> 使用。</summary>
    private string? ActiveOptionId => _open && IsActiveValid
        ? $"{PanelId}-{OptionId(_activeColumn, _activeIndex)}"
        : null;

    private bool IsActiveValid =>
        _activeColumn >= 0 && _activeColumn < _columns.Count &&
        _activeIndex >= 0 && _activeIndex < _columns[_activeColumn].Count;

    private static string OptionId(int column, int index) => $"c{column}-o{index}";

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        UpdateEditContextSubscription();
        RefreshColumns();

        if (IsDisabled || !Visible)
        {
            _open = false;
        }

        if (!_open)
        {
            _activeIndex = -1;
        }
        else
        {
            ClampActive();
        }

        if (!Enum.IsDefined(Size))
        {
            throw new ArgumentOutOfRangeException(nameof(Size), Size, "Unknown cascader size.");
        }
    }

    protected override Task OnComponentAfterRenderAsync(bool firstRender) =>
        JsModuleManager.InvokeModuleVoidAsync("cascader", "sync", InstanceId, _triggerElement);

    protected override ClassBuilder BuildClass() => base.BuildClass()
        .Add("aeterni-cascader")
        .Add("is-full-width", FullWidth)
        .Add(SizeClass)
        .Add("is-open", _open)
        .Add("is-invalid", IsInvalid)
        .Add("is-disabled", IsDisabled);

    protected override IReadOnlyDictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>(
            base.BuildAttributes(),
            StringComparer.OrdinalIgnoreCase);

        if (IsDisabled)
        {
            attributes["aria-disabled"] = "true";
        }

        return attributes;
    }

    private string? SizeClass => ComponentClass.ForSize("aeterni-cascader", Size);

    // ---------------------------------------------------------------- 数据

    private IReadOnlyList<TItem> ChildrenOf(TItem node) =>
        ChildrenSelector?.Invoke(node)?.ToList() ?? [];

    private bool HasChildren(TItem node) => ChildrenOf(node).Count > 0;

    private void RefreshColumns()
    {
        _columns.Clear();
        _columns.Add((Items ?? []).ToList());
        foreach (var node in _expanded)
        {
            _columns.Add(ChildrenOf(node).ToList());
        }
    }

    private void ClampActive()
    {
        if (_columns.Count == 0)
        {
            _activeColumn = 0;
            _activeIndex = -1;
            return;
        }

        _activeColumn = Math.Clamp(_activeColumn, 0, _columns.Count - 1);
        var count = _columns[_activeColumn].Count;
        _activeIndex = count == 0 ? -1 : Math.Clamp(_activeIndex, 0, count - 1);
    }

    /// <summary>从根到目标叶子的祖先链；找不到时返回空。</summary>
    private List<TItem> FindPath(TItem target)
    {
        var path = new List<TItem>();
        return Walk(Items ?? [], target) ? path : [];

        bool Walk(IReadOnlyList<TItem> level, TItem wanted)
        {
            foreach (var node in level)
            {
                path.Add(node);
                if (EqualityComparer<TItem>.Default.Equals(node, wanted) || Walk(ChildrenOf(node), wanted))
                {
                    return true;
                }

                path.RemoveAt(path.Count - 1);
            }

            return false;
        }
    }

    private string DisplayText(TItem? item) => item is null
        ? string.Empty
        : TextSelector?.Invoke(item) ?? item.ToString() ?? string.Empty;

    /// <summary>触发器里展示的完整路径文案。</summary>
    private string PathText => string.Join(
        $" {PathSeparator} ",
        FindPath(Value!).Select(DisplayText));

    private string ColumnLabel(int index) => index <= 0 || index - 1 >= _expanded.Count
        ? UiText.CascaderColumnLabel
        : DisplayText(_expanded[index - 1]);

    // ---------------------------------------------------------------- 交互

    private async Task HandleTriggerClickAsync(MouseEventArgs args)
    {
        if (IsDisabled || !Visible)
        {
            return;
        }

        if (_open)
        {
            Close();
        }
        else
        {
            Open();
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task HandleTriggerKeyDownAsync(KeyboardEventArgs args)
    {
        if (IsDisabled || !Visible || args.AltKey || args.CtrlKey || args.MetaKey || args.ShiftKey)
        {
            return;
        }

        var handled = false;

        switch (args.Key)
        {
            case "ArrowDown":
                handled = true;
                if (!_open)
                {
                    Open();
                }
                else
                {
                    MoveWithin(1);
                }

                break;

            case "ArrowUp":
                handled = true;
                if (!_open)
                {
                    Open();
                }
                else
                {
                    MoveWithin(-1);
                }

                break;

            case "ArrowRight":
                if (_open)
                {
                    handled = true;
                    ExpandOrAdvance();
                }

                break;

            case "ArrowLeft":
                if (_open)
                {
                    handled = true;
                    Retreat();
                }

                break;

            case "Home":
                if (_open && IsActiveValid)
                {
                    handled = true;
                    _activeIndex = 0;
                }

                break;

            case "End":
                if (_open && IsActiveValid)
                {
                    handled = true;
                    _activeIndex = _columns[_activeColumn].Count - 1;
                }

                break;

            case "Enter":
            case " ":
                if (!_open)
                {
                    handled = true;
                    Open();
                }
                else if (IsActiveValid)
                {
                    handled = true;
                    var item = _columns[_activeColumn][_activeIndex];
                    if (HasChildren(item))
                    {
                        Expand(item);
                    }
                    else
                    {
                        await ChooseAsync(item);
                    }
                }

                break;

            case "Escape":
                if (_open)
                {
                    handled = true;
                    Close();
                }

                break;
        }

        if (handled)
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    private void Open()
    {
        if (IsDisabled || !Visible)
        {
            return;
        }

        _open = true;
        _expanded.Clear();
        _activeColumn = 0;
        _activeIndex = -1;

        // 有值时按当前值的路径预展开，并把活动项落到叶子所在列；否则活动项落在首列首项。
        if (Value is not null)
        {
            var path = FindPath(Value);
            if (path.Count > 0)
            {
                // 路径最后一个是叶子，它之前的每一层就是展开链。
                _expanded.AddRange(path.Take(path.Count - 1));
                RefreshColumns();
                _activeColumn = _columns.Count - 1;
                _activeIndex = Math.Max(0, _columns[_activeColumn].FindIndex(item => ValuesEqual(item, Value)));
                return;
            }
        }

        RefreshColumns();
        _activeIndex = _columns.Count > 0 && _columns[0].Count > 0 ? 0 : -1;
    }

    private void Close()
    {
        _open = false;
        _expanded.Clear();
        _activeColumn = 0;
        _activeIndex = -1;
        RefreshColumns();
    }

    private Task HandlePopoverOpenChangedAsync(bool open)
    {
        if (open)
        {
            Open();
        }
        else
        {
            Close();
        }

        return InvokeAsync(StateHasChanged);
    }

    private void MoveWithin(int direction)
    {
        if (!IsActiveValid)
        {
            return;
        }

        var count = _columns[_activeColumn].Count;
        _activeIndex = (_activeIndex + direction + count) % count;
    }

    /// <summary>活动项有子节点就展开下一列；否则若下一列已存在就移进去。</summary>
    private void ExpandOrAdvance()
    {
        if (!IsActiveValid)
        {
            return;
        }

        var item = _columns[_activeColumn][_activeIndex];
        if (HasChildren(item))
        {
            Expand(item);
            return;
        }

        if (_activeColumn < _expanded.Count)
        {
            _activeColumn += 1;
            _activeIndex = Math.Max(0, _columns[_activeColumn].FindIndex(node => ValuesEqual(node, _expanded[_activeColumn - 1])));
        }
    }

    private void Expand(TItem item)
    {
        // 展开一条新分支：丢掉这条分支之后的列，再补上当前节点。
        if (_activeColumn < _expanded.Count)
        {
            _expanded.RemoveRange(_activeColumn, _expanded.Count - _activeColumn);
        }

        _expanded.Add(item);
        RefreshColumns();
        _activeColumn = _expanded.Count;
        _activeIndex = _columns[_activeColumn].Count > 0 ? 0 : -1;
    }

    private void Retreat()
    {
        if (_activeColumn <= 0)
        {
            return;
        }

        _activeColumn -= 1;
        _activeIndex = Math.Max(0, _columns[_activeColumn].FindIndex(node => ValuesEqual(node, _expanded[_activeColumn])));
    }

    private async Task ChooseAsync(TItem item)
    {
        if (IsDisabled)
        {
            return;
        }

        Value = item;
        await ValueChanged.InvokeAsync(item);
        await OnItemSelected.InvokeAsync(item);
        NotifyFieldChanged();
        Close();
    }

    /// <summary>鼠标点击：父节点展开下一列，叶子确认并关闭。</summary>
    private async Task HandleOptionClickAsync(int column, int index)
    {
        if (IsDisabled || column < 0 || column >= _columns.Count ||
            index < 0 || index >= _columns[column].Count)
        {
            return;
        }

        _activeColumn = column;
        _activeIndex = index;
        var item = _columns[column][index];

        if (HasChildren(item))
        {
            Expand(item);
            await InvokeAsync(StateHasChanged);
            return;
        }

        await ChooseAsync(item);
    }

    private static bool ValuesEqual(TItem? left, TItem? right) =>
        EqualityComparer<TItem>.Default.Equals(left!, right!);

    // ---------------------------------------------------------------- 校验

    protected override ValueTask OnComponentDisposeAsync()
    {
        _open = false;
        _validation.Detach();
        return ValueTask.CompletedTask;
    }

    private void UpdateEditContextSubscription()
    {
        _validation.Attach(ValueExpression is null ? null : CascadedEditContext);

        if (ValueExpression is not null)
        {
            _fieldIdentifier = FieldIdentifier.Create(ValueExpression);
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
}
