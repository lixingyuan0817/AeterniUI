using Microsoft.AspNetCore.Components;
namespace AeterniUI.Components.Pagination;

public partial class Pagination : AeterniComponent
{
    [Parameter] public int CurrentPage { get; set; } = 1;
    [Parameter] public int TotalPages { get; set; } = 1;
    [Parameter] public EventCallback<int> CurrentPageChanged { get; set; }
    [Parameter] public string? AriaLabel { get; set; }
    [Parameter] public int SiblingCount { get; set; } = 1;

    protected override ClassBuilder BuildClass() => base.BuildClass().Add("aeterni-pagination");

    private IEnumerable<int> VisiblePages
    {
        get
        {
            var first = Math.Max(1, CurrentPage - SiblingCount);
            var last = Math.Min(TotalPages, CurrentPage + SiblingCount);
            var pages = new HashSet<int> { 1, TotalPages };
            for (var page = first; page <= last; page++) pages.Add(page);
            return pages.OrderBy(page => page);
        }
    }

    private async Task GoToAsync(int page)
    {
        page = Math.Clamp(page, 1, TotalPages);
        if (page == CurrentPage) return;
        await CurrentPageChanged.InvokeAsync(page);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (TotalPages < 1) throw new ArgumentOutOfRangeException(nameof(TotalPages));
        if (SiblingCount < 0) throw new ArgumentOutOfRangeException(nameof(SiblingCount));
        if (CurrentPage < 1 || CurrentPage > TotalPages) throw new ArgumentOutOfRangeException(nameof(CurrentPage));
    }
}
