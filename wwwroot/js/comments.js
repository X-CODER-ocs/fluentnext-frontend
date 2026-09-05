// 切换文章时清空评论区并解除初始化守卫，使下一篇能重新加载评论
window.fluentNextResetComments = function () {
    // 仅清空评论挂载容器，保留标题 / 提示语（它们属于 Razor 静态渲染，不归 JS 管）。
    var container = document.getElementById('fn-comments-mount');
    if (container) container.innerHTML = '';
    window.__fnCommentsInited = false;
};

// FluentNext 评论注入：当前仅支持 Utterances（基于 GitHub Issues，配置驱动）。
// cfg: { provider, repo, issueTerm, label, theme, slug, title }
window.fluentNextInitComments = function (cfg) {
    var container = document.getElementById('fn-comments-mount');
    if (!container || window.__fnCommentsInited) return;
    window.__fnCommentsInited = true;

    if (cfg.provider === 'utterances') {
        if (!cfg.repo) return;
        var s = document.createElement('script');
        s.src = 'https://utteranc.es/client.js';
        s.setAttribute('repo', cfg.repo);
        s.setAttribute('issue-term', cfg.issueTerm || 'pathname');
        if (cfg.label) s.setAttribute('label', cfg.label);
        // theme 由调用方按用户主题传入：github-light / github-dark / preferred-color-scheme
        s.setAttribute('theme', cfg.theme || 'preferred-color-scheme');
        s.crossOrigin = 'anonymous';
        s.async = true;
        container.appendChild(s);
    }
};

// 侧边栏文章目录（TOC）点击：平滑滚动到对应标题。
// id 为 "__top__" 时回到顶部（滚动主区 .fn-main）。
window.fluentNextScrollTo = function (id) {
    var main = document.querySelector('.fn-main');
    if (id === '__top__') {
        if (main) main.scrollTo({ top: 0, behavior: 'smooth' });
        else window.scrollTo({ top: 0, behavior: 'smooth' });
        return;
    }
    var el = document.getElementById(id);
    if (el) {
        if (main) {
            // 相对主区计算偏移，避开被 sticky 顶栏遮挡
            var top = el.offsetTop - main.offsetTop - 12;
            main.scrollTo({ top: top, behavior: 'smooth' });
        } else {
            el.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    }
};
