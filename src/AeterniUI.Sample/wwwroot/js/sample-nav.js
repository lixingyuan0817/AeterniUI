// 侧栏当前项可见性：SPA 导航后布局不会重建，菜单的滚动位置会停在上一页，
// 当前页可能落在折叠线以下。这里只做一件事：把选中的菜单项滚进侧栏的
// 可视范围（block:'nearest' 不会带动整页滚动）。
export function revealSelected() {
    const nav = document.querySelector('.showcase-nav');
    const selected = nav?.querySelector('.aeterni-menu__item.is-selected[aria-current="page"]');

    if (!nav || !selected) {
        return;
    }

    const navRect = nav.getBoundingClientRect();
    const itemRect = selected.getBoundingClientRect();

    if (itemRect.top < navRect.top || itemRect.bottom > navRect.bottom) {
        selected.scrollIntoView({ block: 'nearest' });
    }
}
