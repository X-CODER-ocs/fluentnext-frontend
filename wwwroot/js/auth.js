// FluentNext 统一 GitHub 登录（驱动 Gitalk 评论）。
// 关键：token 写入 Gitalk 使用的 localStorage 键 "gitalk:{clientID}"，
// 这样侧边栏登录一次，文章页 Gitalk 即可识别已登录态 —— 真正的「统一登录」。
window.fluentNextGitHub = {
    // 当前部署基址（含 /blog/ 子路径），用作 OAuth redirect_uri
    baseUri: function () { return document.baseURI; },

    // 发起 OAuth：记住登录前页面，跳转 GitHub 授权页
    login: function (clientId, redirectUri) {
        try { sessionStorage.setItem('fn_gh_return', location.pathname + location.search); } catch (e) {}
        var state = Date.now().toString(36) + Math.random().toString(36).slice(2);
        try { sessionStorage.setItem('fn_gh_oauth_state', state); } catch (e) {}
        var url = 'https://github.com/login/oauth/authorize'
            + '?client_id=' + encodeURIComponent(clientId)
            + '&redirect_uri=' + encodeURIComponent(redirectUri)
            + '&state=' + encodeURIComponent(state)
            + '&scope=' + encodeURIComponent('public_repo');
        location.href = url;
    },

    // 回跳收尾：校验 state，用 code 换 token，落到 Gitalk 的 localStorage 键，并清理 URL
    complete: async function (clientId, clientSecret, redirectUri) {
        var params = new URLSearchParams(location.search);
        var code = params.get('code');
        var state = params.get('state');
        if (!code) return { ok: false, reason: 'no_code' };
        var saved = null;
        try { saved = sessionStorage.getItem('fn_gh_oauth_state'); } catch (e) {}
        if (state !== saved) return { ok: false, reason: 'state_mismatch' };
        try {
            var resp = await fetch('https://github.com/login/oauth/access_token', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
                body: JSON.stringify({ client_id: clientId, client_secret: clientSecret, code: code, redirect_uri: redirectUri })
            });
            var data = await resp.json();
            if (!data.access_token) return { ok: false, reason: 'no_token', raw: data };
            // 写入 Gitalk 使用的 localStorage 键，实现「统一登录」
            try { localStorage.setItem('gitalk:' + clientId, data.access_token); } catch (e) {}
            // 清掉 URL 里的 code/state，避免刷新重复兑换
            try {
                var u = new URL(location.href);
                u.searchParams.delete('code');
                u.searchParams.delete('state');
                history.replaceState({}, '', u.pathname + u.search);
            } catch (e) {}
            return { ok: true, token: data.access_token };
        } catch (e) { return { ok: false, reason: 'exception', msg: String(e) }; }
    },

    // 用 token 拉取当前账户（头像/用户名），用于侧边栏预览
    getUser: async function (token) {
        try {
            var resp = await fetch('https://api.github.com/user', {
                headers: { 'Authorization': 'token ' + token, 'Accept': 'application/json' }
            });
            if (!resp.ok) return null;
            var u = await resp.json();
            return { login: u.login, name: u.name || u.login, avatar_url: u.avatar_url, html_url: u.html_url };
        } catch (e) { return null; }
    },

    getToken: function (clientId) {
        try { return localStorage.getItem('gitalk:' + clientId); } catch (e) { return null; }
    },

    clear: function (clientId) {
        try {
            localStorage.removeItem('gitalk:' + clientId);
            localStorage.removeItem('fluentnext:github-user');
            sessionStorage.removeItem('fn_gh_return');
        } catch (e) {}
    },

    getUserCache: function () {
        try { var s = localStorage.getItem('fluentnext:github-user'); return s || ''; } catch (e) { return ''; }
    },

    setUserCache: function (u) {
        try { localStorage.setItem('fluentnext:github-user', u); } catch (e) {}
    },

    getReturn: function () {
        try { return sessionStorage.getItem('fn_gh_return') || ''; } catch (e) { return ''; }
    },

    // 唤起 Gitalk 原生登录：Gitalk 用 github.com 同域 iframe 换 token（不踩 CORS），
    // 登录后把 token 写入 gitalk:{clientID}，本服务轮询到即同步侧边栏账户。
    // 在文章页的 #fn-comments 容器内找到 Gitalk 的登录链接并点击；非文章页（无评论区）则滚动提示。
    triggerGitalkLogin: function () {
        var box = document.getElementById('fn-comments');
        if (!box) {
            // 非文章页没有评论区：滚动到评论入口（若有）并提示
            var hint = document.querySelector('.fn-comments-hint');
            if (hint) hint.scrollIntoView({ behavior: 'smooth' });
            return;
        }
        // Gitalk 的登录入口是跳转到 github.com 授权的 <a>
        var links = box.querySelectorAll('a[href*="github.com/login/oauth/authorize"]');
        if (links.length) { links[0].click(); return; }
        var gl = box.querySelector('.gt-login, .gt-header .gt-login, a.gt-login');
        if (gl) { gl.click(); return; }
        box.scrollIntoView({ behavior: 'smooth' });
    }
};
