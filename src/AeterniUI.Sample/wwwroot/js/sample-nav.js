// 侧栏当前项可见性：SPA 导航后布局不会重建，菜单的滚动位置会停在上一页，
// 当前页可能落在折叠线以下。这里只做一件事：把选中的菜单项滚进侧栏的
// 可视范围（block:'nearest' 不会带动整页滚动）。
//
// 第二个参数给筛选路径用：查询命中后当前页可能不在结果里，这时退回到首个命中项，
// 让读者看到结果真的从列表顶端开始，而不是对着一个没有选中项的过滤结果。

const prefersReducedMotion = () =>
    typeof window.matchMedia === 'function'
        && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

export function revealSelected(preferFirstMatch = false) {
    const nav = document.querySelector('.showcase-nav');

    if (!nav) {
        return;
    }

    const selected = nav.querySelector('.aeterni-menu__item.is-selected[aria-current="page"]');
    const firstMatch = preferFirstMatch ? nav.querySelector('.aeterni-menu__item[href]') : null;
    const target = firstMatch ?? selected;

    // 窄屏目录折叠时元素没有布局盒，滚动它没有意义（也不需要）。
    if (!target || target.getClientRects().length === 0) {
        return;
    }

    const navRect = nav.getBoundingClientRect();
    const itemRect = target.getBoundingClientRect();

    if (itemRect.top < navRect.top || itemRect.bottom > navRect.bottom) {
        target.scrollIntoView({
            block: 'nearest',
            behavior: prefersReducedMotion() ? 'instant' : 'smooth'
        });
    }
}
