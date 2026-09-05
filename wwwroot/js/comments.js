// 切换文章时清空评论区并解除初始化守卫，使下一篇能重新加载评论
window.fluentNextResetComments = function () {
    var container = document.getElementById('fn-comments');
    if (container) container.innerHTML = '';
    window.__fnCommentsInited = false;
};

// FluentNext 评论注入：根据配置加载 Gitalk 或 Utterances（配置驱动，支持两种）
window.fluentNextInitComments = function (cfg) {
    var container = document.getElementById('fn-comments');
    if (!container || window.__fnCommentsInited) return;
    window.__fnCommentsInited = true;

    if (cfg.provider === 'utterances') {
        if (!cfg.repo) return;
        var s = document.createElement('script');
        s.src = 'https://utteranc.es/client.js';
        s.setAttribute('repo', cfg.repo);
        s.setAttribute('issue-term', cfg.issueTerm || 'pathname');
        if (cfg.label) s.setAttribute('label', cfg.label);
        s.setAttribute('theme', 'preferred-color-scheme');
        s.crossOrigin = 'anonymous';
        s.async = true;
        container.appendChild(s);
    } else if (cfg.provider === 'gitalk') {
        if (!cfg.gitalk || !cfg.gitalk.clientID) return;
        var link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = 'https://cdn.jsdelivr.net/npm/gitalk/dist/gitalk.css';
        document.head.appendChild(link);

        var gs = document.createElement('script');
        gs.src = 'https://cdn.jsdelivr.net/npm/gitalk/dist/gitalk.min.js';
        gs.onload = function () {
            // Gitalk 的 id 必须 ≤50 字符，slug 可能过长，做稳定哈希兜底
            var id = cfg.slug || location.pathname;
            if (id.length > 50) {
                var h = 0;
                for (var i = 0; i < id.length; i++) { h = (h << 5) - h + id.charCodeAt(i); h |= 0; }
                id = 'p' + (h >>> 0).toString(36);
            }
            // Gitalk 自动读取 localStorage 的 gitalk:{clientID} token，
            // 该 token 由侧边栏「通过 GitHub 登录」写入 —— 即统一登录。
            var g = new Gitalk({
                clientID: cfg.gitalk.clientID,
                clientSecret: cfg.gitalk.clientSecret,
                repo: cfg.gitalk.repo,
                owner: cfg.gitalk.owner,
                admin: cfg.gitalk.admin || [],
                id: id,
                title: cfg.title,
                distractionFreeMode: false
            });
            g.render('fn-comments');
        };
        document.body.appendChild(gs);
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
