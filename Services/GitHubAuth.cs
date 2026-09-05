using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using System.Text.Json;

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
/// 统一 GitHub 登录（驱动 Gitalk 评论）。
/// 流程：侧边栏「通过 GitHub 登录」→ 跳转 GitHub OAuth（authorization code）→
/// 回跳后用 code + clientSecret 换 access_token → 写入 Gitalk 的 localStorage 键
/// （gitalk:{clientID}）→ 拉取账户信息作为预览。登出即清除该键。
/// 因为 token 与 Gitalk 共用同一 localStorage 键，登录一次即可同时驱动侧边栏预览与文章页评论。
/// </summary>
public class GitHubAuth
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

    public GitHubAuth(IJSRuntime js, NavigationManager nav, IConfiguration config)
    {
        _js = js;
        _nav = nav;
        _cfg = config.GetSection("FluentNext:Comments").Get<CommentsConfig>() ?? new();
    }

    /// <summary>应用启动：恢复缓存的账户预览，并完成可能挂起的 OAuth 回调</summary>
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
    }

    /// <summary>GitHub 回跳后（URL 带 ?code=&state=）在此完成 token 交换与账户拉取</summary>
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
                // 回到登录前的页面（SPA 内导航，不整页刷新）
                var ret = await _js.InvokeAsync<string?>("fluentNextGitHub.getReturn");
                if (!string.IsNullOrEmpty(ret) && ret.Trim('/') != _nav.ToBaseRelativePath(_nav.Uri).Trim('/'))
                    _nav.NavigateTo(ret, forceLoad: false);
                Changed?.Invoke();
            }
        }
    }

    /// <summary>发起 GitHub OAuth 登录（整页跳转，回调后由 HandleCallback 收尾）</summary>
    public async Task LoginAsync()
    {
        if (!IsConfigured)
        {
            await _js.InvokeVoidAsync("alert",
                "评论系统尚未配置 GitHub OAuth App。请在 wwwroot/appsettings.json 的 FluentNext.Comments.Gitalk 填写 clientId / clientSecret。");
            return;
        }
        var redirectUri = await _js.InvokeAsync<string>("fluentNextGitHub.baseUri");
        await _js.InvokeVoidAsync("fluentNextGitHub.login", ClientId, redirectUri);
    }

    /// <summary>登出：清除 token 与缓存的账户信息</summary>
    public async Task LogoutAsync()
    {
        await _js.InvokeVoidAsync("fluentNextGitHub.clear", ClientId);
        User = null;
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
}
