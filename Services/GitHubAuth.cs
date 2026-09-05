using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using System.Text.Json;
using System.Threading;

namespace FluentNext.Frontend.Services;

/// <summary>已登录的 GitHub 账户信息（用于侧边栏预览）</summary>
public class GitHubUser
{
    public string Login { get; set; } = "";
    public string Name { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
    public string HtmlUrl { get; set; } = "";
}

/// <summary>
/// 统一 GitHub 登录态管理（驱动侧边栏账户预览 + 文章页 Gitalk 评论）。
///
/// ⚠️ 关键约束：GitHub 的 token 端点（github.com/login/oauth/access_token）**不允许浏览器跨域（CORS）**，
/// 且 implicit grant 已下线，纯静态站点无法在浏览器内把 code 换成 token。因此「换 token」这一步不能由我们自己做。
/// 解法：复用 Gitalk 原生登录——Gitalk 用 github.com 同域 iframe 完成换 token，并把 token 写入
/// localStorage 的 `gitalk:{clientID}` 键。本服务轮询该键，一旦检测到就拉取账户信息显示到侧边栏，
/// 实现「登录一次、侧边栏预览 + 评论同时生效」的统一登录，且无需任何后端 / 代理。
/// </summary>
public class GitHubAuth : IDisposable
{
    private readonly IJSRuntime _js;
    private readonly NavigationManager _nav;
    private readonly CommentsConfig _cfg;

    /// <summary>JS 端返回的是 camelCase（login / avatar_url …），用大小写不敏感反序列化</summary>
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    /// <summary>登录态变化（登录 / 登出 / 账户刷新）时通知 UI 重绘</summary>
    public event Action? Changed;

    public GitHubUser? User { get; private set; }
    public bool IsLoggedIn => User is not null;

    /// <summary>评论系统是否配置为 gitalk（决定是否显示登录 UI）</summary>
    public bool IsGitalk => _cfg.Provider == "gitalk";

    /// <summary>是否已具备可用的 OAuth 凭据（clientId 非空）</summary>
    public bool IsConfigured => IsGitalk && !string.IsNullOrEmpty(ClientId);

    public string ClientId => _cfg.Gitalk.ClientID;
    public string ClientSecret => _cfg.Gitalk.ClientSecret;

    private Timer? _pollTimer;
    private string? _lastToken;
    private bool _syncing;

    public GitHubAuth(IJSRuntime js, NavigationManager nav, IConfiguration config)
    {
        _js = js;
        _nav = nav;
        _cfg = config.GetSection("FluentNext:Comments").Get<CommentsConfig>() ?? new();
    }

    /// <summary>应用启动：恢复缓存的账户预览，并完成可能挂起的自定义 OAuth 回调，再开始轮询 Gitalk 登录态</summary>
    public async Task InitAsync()
    {
        var cached = await _js.InvokeAsync<string?>("fluentNextGitHub.getUserCache");
        if (!string.IsNullOrEmpty(cached))
        {
            try
            {
                User = JsonSerializer.Deserialize<GitHubUser>(cached, JsonOpts);
                Changed?.Invoke();
            }
            catch { /* 缓存损坏则忽略 */ }
        }
        await HandleCallbackAsync();
        await SyncWithGitalkAsync();
        // 轮询捕获用户经 Gitalk 原生登录（写 gitalk:{clientID}）后的态，同步到侧边栏。
        _pollTimer = new Timer(_ => _ = SafeSync(), null, 1500, 1500);
    }

    private async Task SafeSync()
    {
        if (_syncing) return;
        _syncing = true;
        try { await SyncWithGitalkAsync(); }
        catch { /* 轮询中的瞬时错误忽略 */ }
        finally { _syncing = false; }
    }

    /// <summary>检测 Gitalk 原生登录写入的 token（localStorage 键 gitalk:{clientID}），拉取账户显示到侧边栏</summary>
    public async Task SyncWithGitalkAsync()
    {
        if (!IsConfigured) return;
        string? token = null;
        try { token = await _js.InvokeAsync<string?>("fluentNextGitHub.getToken", ClientId); } catch { }
        if (token == _lastToken) return;
        _lastToken = token;
        if (string.IsNullOrEmpty(token))
        {
            if (User is not null)
            {
                User = null;
                Changed?.Invoke();
            }
            return;
        }
        var user = await FetchUserAsync(token);
        if (user is not null)
        {
            User = user;
            try { await _js.InvokeVoidAsync("fluentNextGitHub.setUserCache", JsonSerializer.Serialize(user)); } catch { }
            Changed?.Invoke();
        }
    }

    /// <summary>GitHub 回跳后（URL 带 ?code=&state=）在此完成 token 交换与账户拉取。
    /// 注意：无 CORS 代理时此步会被浏览器拦截而静默失败，登录实际依赖 Gitalk 原生流程（见 SyncWithGitalkAsync）。</summary>
    public async Task HandleCallbackAsync()
    {
        if (!IsConfigured) return;
        var redirectUri = await _js.InvokeAsync<string>("fluentNextGitHub.baseUri");
        var res = await _js.InvokeAsync<JsonElement>("fluentNextGitHub.complete", ClientId, ClientSecret, redirectUri);
        if (res.TryGetProperty("ok", out var ok) && ok.GetBoolean())
        {
            var token = res.GetProperty("token").GetString() ?? "";
            var user = await FetchUserAsync(token);
            if (user is not null)
            {
                User = user;
                await _js.InvokeVoidAsync("fluentNextGitHub.setUserCache", JsonSerializer.Serialize(user));
                var ret = await _js.InvokeAsync<string?>("fluentNextGitHub.getReturn");
                if (!string.IsNullOrEmpty(ret) && ret.Trim('/') != _nav.ToBaseRelativePath(_nav.Uri).Trim('/'))
                    _nav.NavigateTo(ret, forceLoad: false);
                Changed?.Invoke();
            }
        }
    }

    /// <summary>侧边栏登录按钮：唤起 Gitalk 原生登录（在文章页），非文章页提示去文章页。
    /// 不再走自定义 OAuth（被 CORS 拦），委托 Gitalk 自身完成换 token。</summary>
    public async Task LoginAsync()
    {
        if (!IsConfigured)
        {
            await _js.InvokeVoidAsync("alert",
                "评论系统尚未配置 GitHub OAuth App。请在 wwwroot/appsettings.json 的 FluentNext.Comments.Gitalk 填写 clientId / clientSecret。");
            return;
        }
        await _js.InvokeVoidAsync("fluentNextGitHub.triggerGitalkLogin");
    }

    /// <summary>登出：清除 token 与缓存的账户信息</summary>
    public async Task LogoutAsync()
    {
        await _js.InvokeVoidAsync("fluentNextGitHub.clear", ClientId);
        User = null;
        _lastToken = null;
        Changed?.Invoke();
    }

    private async Task<GitHubUser?> FetchUserAsync(string token)
    {
        try
        {
            var raw = await _js.InvokeAsync<JsonElement?>("fluentNextGitHub.getUser", token);
            if (raw is null) return null;
            var je = raw.Value;
            if (je.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) return null;
            return JsonSerializer.Deserialize<GitHubUser>(je.GetRawText(), JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose() => _pollTimer?.Dispose();
}
