// 首页动画编排（GSAP 从 ./vendor/gsap.min.js 本地加载，不走 CDN）。
//
// 分工：结构、状态与可访问性都在 Razor 侧；这里只做两件事——
//   1. Hero 的两个孩子（文案 / 实时样例）依次入场，统计数字滚到终值；
//   2. 组件墙的卡片滚进视口时逐个揭示。
//
// 悬停浮起交给 CSS（translate + 阴影），不再用 GSAP 写 transform：两套 transform
// 同时改会互相覆盖，倾斜（rotateX/rotateY）也超出了「仪器拨到位」的手感。
//
// 降级：没有 window.gsap（脚本被拦、离线裁剪过的构建）或系统开启
// prefers-reduced-motion 时，直接把数字写成终值、不做任何位移，
// 页面结构与交互完全不受影响。init 可重复调用，内部先 dispose 再重新绑定。

let observer = null;
let context = null;

export function init(root) {
    dispose();

    if (!root) {
        return;
    }

    const gsap = window.gsap;
    const counters = [...root.querySelectorAll('[data-home-count]')];
    const cards = [...root.querySelectorAll('[data-home-card]')];
    const hero = root.querySelector('[data-home-hero]');
    const reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    // 数字先落到终值：后面如果动画跑起来，会从 0 再滚一遍。
    const settleCounters = () => counters.forEach(element => {
        element.textContent = element.dataset.homeCount ?? element.textContent;
    });

    if (!gsap || reduce) {
        settleCounters();
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

        counters.forEach(element => {
            const target = Number(element.dataset.homeCount ?? 0);
            const proxy = { value: 0 };
            gsap.to(proxy, {
                value: target,
                duration: .9,
                ease: 'power1.out',
                onUpdate: () => {
                    element.textContent = String(Math.round(proxy.value));
                }
            });
        });
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
