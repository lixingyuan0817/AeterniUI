namespace AeterniUI.Sample.Components.Showcase;

/// <summary>
/// 一页里各段的登记表：demo 卡把自己登记进来，页骨架据此渲染右栏目录。
/// </summary>
/// <remarks>
/// 采用"子组件登记、父组件渲染"的形态，与库内 <c>Tabs</c>/<c>Tab</c> 一致：目录的数据源因此
/// 只有一份——卡片自己的锚点与标题，不需要页面再手写一遍目录，也不会出现"目录与卡片对不上"。
/// 登记顺序即阅读顺序。
/// </remarks>
public sealed class ShowcaseSectionRegistry
{
    private readonly List<ShowcaseSection> _sections = [];
    private Action? _onChanged;

    /// <summary>已登记的段，按登记（也就是渲染）顺序。</summary>
    public IReadOnlyList<ShowcaseSection> Sections => _sections;

    public void OnChanged(Action onChanged) => _onChanged = onChanged;

    public void Register(ShowcaseSection section)
    {
        if (_sections.Any(existing => string.Equals(existing.Id, section.Id, StringComparison.Ordinal)))
        {
            return;
        }

        _sections.Add(section);
        _onChanged?.Invoke();
    }

    public void Unregister(string id)
    {
        var index = _sections.FindIndex(section => string.Equals(section.Id, id, StringComparison.Ordinal));
        if (index < 0)
        {
            return;
        }

        _sections.RemoveAt(index);
        _onChanged?.Invoke();
    }
}

/// <summary>目录里的一条：锚点与显示标题。</summary>
public sealed record ShowcaseSection(string Id, string Title);
