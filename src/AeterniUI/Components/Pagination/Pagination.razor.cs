using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
namespace AeterniUI.Components.Pagination;

public partial class Pagination : AeterniComponent
{
    [Parameter] public int CurrentPage { get; set; } = 1;
    [Parameter] public int TotalPages { get; set; } = 1;
    [Parameter] public EventCallback<int> CurrentPageChanged { get; set; }
    [Parameter] public string? AriaLabel { get; set; }
    [Parameter] public string? PreviousLabel { get; set; }
    [Parameter] public string? NextLabel { get; set; }
    [Parameter] public string? PageLabelFormat { get; set; }
    [Parameter] public int SiblingCount { get; set; } = 1;

    private string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel) ? UiText.PaginationLabel : AriaLabel.Trim();
    private string EffectivePreviousLabel => string.IsNullOrWhiteSpace(PreviousLabel) ? UiText.PaginationPreviousLabel : PreviousLabel.Trim();
    private string EffectiveNextLabel => string.IsNullOrWhiteSpace(NextLabel) ? UiText.PaginationNextLabel : NextLabel.Trim();
    private string EffectivePageLabelFormat => string.IsNullOrWhiteSpace(PageLabelFormat) ? UiText.PaginationPageLabelFormat : PageLabelFormat.Trim();

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

    private string PageLabel(int page) => string.Format(CultureInfo.CurrentCulture, EffectivePageLabelFormat, page);

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (TotalPages < 1) throw new ArgumentOutOfRangeException(nameof(TotalPages));
        if (SiblingCount < 0) throw new ArgumentOutOfRangeException(nameof(SiblingCount));
        if (CurrentPage < 1 || CurrentPage > TotalPages) throw new ArgumentOutOfRangeException(nameof(CurrentPage));
        try
        {
            var parsed = CompositeFormat.Parse(EffectivePageLabelFormat);
            if (parsed.MinimumArgumentCount != 1)
            {
                throw new FormatException("The format must reference page number placeholder {0} and no higher argument index.");
            }

            _ = string.Format(CultureInfo.CurrentCulture, parsed, CurrentPage);
        }
        catch (FormatException exception)
        {
            throw new ArgumentException("PageLabelFormat must be a valid composite format that accepts the page number as {0}.", nameof(PageLabelFormat), exception);
        }
    }
}
