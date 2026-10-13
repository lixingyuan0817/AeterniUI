namespace AeterniUI.Sample.Components.Showcase;

/// <summary>
/// 搜索索引里的一条命中：一个页面本身，或页面里的一个段。
/// </summary>
/// <remarks>
/// 数据由 <c>scripts/generate-search-index.mjs</c> 生成（<see cref="ShowcaseSearchIndex"/>），
/// 这里只给出渲染与导航需要的形状。锚点与页面上的真实 id 一致——页面条目指向页面自己的落地
/// 元素（组件页是 <c>AnchorId</c>），段条目指向 <c>ComponentPage</c> / <c>DemoCard</c> 渲染出来
/// 的 id，所以每条结果都能落到一个存在的元素上。
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
