// FluentNext 用户自定义主题：把强调色 / 背景 / 文字 / 字体写成 CSS 变量，覆盖 Fluent 默认设计令牌。
// 由 App.razor 在初始化与设置变化时调用。
window.fluentNextApplyUserTheme = function (s) {
    if (!s) return;
    var style = document.getElementById('fn-user-theme');
    if (!style) {
        style = document.createElement('style');
        style.id = 'fn-user-theme';
        document.head.appendChild(style);
    }

    var rules = ':root{';

    // 强调色：Fluent 的 accent 令牌由 FluentDesignTheme(CustomColor) 接管，
    // 这里只补一个 --fn-accent 供自定义样式（链接 / 标题左边框 / 阅读更多）直接引用。
    if (s.accent) {
        rules += '--fn-accent:' + s.accent + ';';
    }

    // 背景色：覆盖 neutral layer 令牌；layer-2/3 用 color-mix 做轻微明暗差，制造层级感。
    if (s.background) {
        rules += '--neutral-layer-1:' + s.background + ';'
            + '--neutral-layer-2:color-mix(in srgb,' + s.background + ' 94%, #000);'
            + '--neutral-layer-3:color-mix(in srgb,' + s.background + ' 88%, #000);'
            + '--fn-bg:' + s.background + ';';
    }

    // 文字色：前景 + 提示色（提示色降透明度，保证层级但不刺眼）。
    if (s.foreground) {
        rules += '--neutral-foreground-rest:' + s.foreground + ';'
            + '--neutral-foreground-hint:color-mix(in srgb,' + s.foreground + ' 65%, transparent);'
            + '--fn-fg:' + s.foreground + ';';
    }

    // 字体栈
    if (s.fontFamily) {
        rules += '--fn-font:' + s.fontFamily + ';';
    }

    rules += '}';

    if (s.fontFamily) {
        rules += 'html,body{font-family:var(--fn-font);}';
    }

    style.textContent = rules;
};
