# 第三方：GSAP

本目录下的 `gsap.min.js` 是示例站的**本地 vendored** 副本，不走 CDN——示例站要能在断网、
内网与 GitHub Pages 三种环境下表现一致，首页动画不能依赖第三方请求是否可达。

- 来源包：`gsap@3.15.0`（npm），文件取自包内 `dist/gsap.min.js`
- 上游主页：<https://gsap.com>
- 许可：Standard "no charge" license —— <https://gsap.com/standard-license>
  （包内 `package.json` 的 `license` 字段原文：`Standard 'no charge' license: https://gsap.com/standard-license.`）

更新方式（保持上面这条来源可追溯）：

```bash
npm pack gsap@3.15.0
tar -xzf gsap-3.15.0.tgz
cp package/dist/gsap.min.js src/AeterniUI.Sample/wwwroot/js/vendor/gsap.min.js
```

只用到核心运行时（Tween / Timeline / `gsap.context` / `gsap.quickTo`）；插件（ScrollTrigger、
Draggable 等）没有被引入——首页的"滚进视口再揭示"由 `IntersectionObserver` 触发，见
`../sample-home.js`。
