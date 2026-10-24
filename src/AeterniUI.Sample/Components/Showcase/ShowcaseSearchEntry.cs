namespace AeterniUI.Sample.Components.Showcase;

/// <summary>
/// 搜索索引里的一条命中：一个页面本身，或页面里的一个段。
/// </summary>
/// <remarks>
/// 数据由 <c>scripts/generate-search-index.mjs</c> 生成（<see cref="ShowcaseSearchIndex"/>），
/// 这里只给出渲染与导航需要的形状。锚点与页面上的真实 id 一致——页面条目指向页面自己的落地
/// 元素（组件页是 <c>AnchorId</c>），段条目指向 <c>ComponentPage</c> / <c>DemoCard</c> 渲染出来
/// 的 id，所以每条结果都能落到一个存在的元素上。
/// <para>
/// 生成物本身按页面分组（<see cref="ShowcaseSearchPage"/>），页面字段只存一次；这个扁平形状由
/// <see cref="ShowcaseSearchModel"/> 在消费端摊平，渲染与导航仍只面对一条条命中。
/// </para>
/// </remarks>
public sealed record ShowcaseSearchEntry(
    string PageId,
    string PageTitle,
    string Category,
    string SectionId,
    string SectionTitle)
{
    /// <summary>命中项的路由。安装页的路由是 <c>/components</c>，由目录统一给出。</summary>
    public string Href => $"{ShowcaseCatalog.RouteFor(PageId)}#{SectionId}";
}

/// <summary>
/// 搜索索引里的一个页面：页标题与分类只存一次，页内段只存锚点与标题。
/// </summary>
/// <remarks>
/// 索引里绝大多数条目是段：扁平结构会把页标题与分类在每个段上重复一遍，按页面分组后
/// 生成物小约三分之一，页与段的从属关系也不再靠重复字段表达。
/// </remarks>
public sealed record ShowcaseSearchPage(
    string PageId,
    string PageTitle,
    string Category,
    IReadOnlyList<ShowcaseSearchSection> Sections);

/// <summary>页面里的一个段；<see cref="SectionId"/> 是页面上真实存在的元素 id。</summary>
public sealed record ShowcaseSearchSection(string SectionId, string SectionTitle);
