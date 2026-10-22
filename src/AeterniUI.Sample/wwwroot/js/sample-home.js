// 首页动画编排（GSAP 从 ./vendor/gsap.min.js 本地加载，不走 CDN）。
//
// 分工：结构、状态与可访问性都在 Razor 侧；这里只做两件事——
//   1. Hero 的两个孩子（文案 / 实时样例）依次入场；
//   2. 组件墙的卡片滚进视口时逐个揭示。
//
// 统计数字由 Razor 直接渲染终值，不再做「从 0 滚到终值」的计数动画：WASM 启动
// 慢时，首屏会在 0.9s 内先显示 0/中间值，而与同页静态的「全部 57 个组件」矛盾。
//
// 悬停浮起交给 CSS（translate + 阴影），不再用 GSAP 写 transform：两套 transform
// 同时改会互相覆盖，倾斜（rotateX/rotateY）也超出了「仪器拨到位」的手感。
//
// 降级：没有 window.gsap（脚本被拦、离线裁剪过的构建）或系统开启
// prefers-reduced-motion 时只标记 ready、不做位移，页面结构与交互完全不受影响。
// init 可重复调用，内部先 dispose 再重新绑定。

let observer = null;
let context = null;

export function init(root) {
    dispose();

    if (!root) {
        return;
    }

    const gsap = window.gsap;
    const cards = [...root.querySelectorAll('[data-home-card]')];
    const hero = root.querySelector('[data-home-hero]');
    const reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    if (!gsap || reduce) {
        root.dataset.homeReady = 'true';
        return;
    }

    context = gsap.context(() => {
        const heroItems = hero ? [...hero.children] : [];

        if (heroItems.length > 0) {
            gsap.from(heroItems, {
                y: 14,
                opacity: 0,
                duration: .5,
                stagger: .06,
                ease: 'power2.out'
            });
        }

    }, root);

    observer = new IntersectionObserver(entries => {
        entries.forEach(entry => {
            if (!entry.isIntersecting) {
                return;
            }

            const card = entry.target;
            observer.unobserve(card);
            gsap.from(card, { y: 18, opacity: 0, duration: .45, ease: 'power2.out' });
        });
    }, { rootMargin: '0px 0px -8% 0px', threshold: .12 });

    cards.forEach(card => observer.observe(card));

    root.dataset.homeReady = 'true';
}

export function dispose() {
    if (observer) {
        observer.disconnect();
        observer = null;
    }

    if (context) {
        context.revert();
        context = null;
    }
}
