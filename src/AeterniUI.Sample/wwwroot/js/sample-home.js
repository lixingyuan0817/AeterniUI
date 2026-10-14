// 首页动画编排（GSAP 从 ./vendor/gsap.min.js 本地加载，不走 CDN）。
//
// 分工：结构、状态与可访问性都在 Razor 侧；这里只做三件事——
//   1. Hero 元素入场与统计数字滚动；
//   2. 卡片滚进视口时逐个揭示（用 IntersectionObserver 而不是 ScrollTrigger，
//      少引一个插件，也不需要额外的滚动监听）；
//   3. 卡片悬停浮起 / 指针微倾，以及 Hero 光斑跟随指针。
//
// 降级：没有 window.gsap（脚本被拦、离线裁剪过的构建）或系统开启
// prefers-reduced-motion 时，直接把数字写成终值、不做任何位移，
// 页面结构与交互完全不受影响。init 可重复调用，内部先 dispose 再重新绑定。

let observer = null;
let context = null;
const teardown = [];

export function init(root) {
    dispose();

    if (!root) {
        return;
    }

    const gsap = window.gsap;
    const counters = [...root.querySelectorAll('[data-home-count]')];
    const cards = [...root.querySelectorAll('[data-home-card]')];
    const hero = root.querySelector('[data-home-hero]');
    const spot = root.querySelector('[data-home-spot]');
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
        const heroItems = hero
            ? [...hero.children].filter(child => child !== spot)
            : [];

        if (heroItems.length > 0) {
            gsap.from(heroItems, {
                y: 20,
                opacity: 0,
                duration: .65,
                stagger: .07,
                ease: 'power2.out'
            });
        }

        counters.forEach(element => {
            const target = Number(element.dataset.homeCount ?? 0);
            const proxy = { value: 0 };
            gsap.to(proxy, {
                value: target,
                duration: 1.1,
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
            gsap.from(card, { y: 26, opacity: 0, duration: .55, ease: 'power2.out' });
        });
    }, { rootMargin: '0px 0px -8% 0px', threshold: .12 });

    cards.forEach(card => observer.observe(card));

    cards.forEach(card => {
        const enter = () => gsap.to(card, { y: -6, duration: .28, ease: 'power2.out' });
        const leave = () => gsap.to(card, { y: 0, rotateX: 0, rotateY: 0, duration: .32, ease: 'power2.out' });
        const move = event => {
            const rect = card.getBoundingClientRect();
            if (rect.width === 0 || rect.height === 0) {
                return;
            }

            const dx = (event.clientX - rect.left) / rect.width - .5;
            const dy = (event.clientY - rect.top) / rect.height - .5;
            gsap.to(card, {
                rotateY: dx * 6,
                rotateX: -dy * 6,
                transformPerspective: 900,
                duration: .35,
                ease: 'power2.out'
            });
        };

        card.addEventListener('pointerenter', enter);
        card.addEventListener('pointerleave', leave);
        card.addEventListener('pointermove', move);
        teardown.push(() => {
            card.removeEventListener('pointerenter', enter);
            card.removeEventListener('pointerleave', leave);
            card.removeEventListener('pointermove', move);
        });
    });

    if (spot && hero) {
        const toX = gsap.quickTo(spot, 'x', { duration: .6, ease: 'power3.out' });
        const toY = gsap.quickTo(spot, 'y', { duration: .6, ease: 'power3.out' });
        const track = event => {
            const rect = hero.getBoundingClientRect();
            toX(event.clientX - rect.left - 160);
            toY(event.clientY - rect.top - 160);
        };

        hero.addEventListener('pointermove', track);
        teardown.push(() => hero.removeEventListener('pointermove', track));
    }

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

    while (teardown.length > 0) {
        teardown.pop()();
    }
}
