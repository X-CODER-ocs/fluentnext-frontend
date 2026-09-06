using System.Net.Http.Json;
using System.Text.Json;

namespace FluentNext.Frontend.Services;

/// <summary>消费 Hexo 后端产出的 content.json（带进程内缓存，多个组件共享一次拉取）</summary>
public class ContentService
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private ContentData? _cache;
    private Task<ContentData>? _inflight;

    public ContentService(HttpClient http) => _http = http;

    public Task<ContentData> GetContentAsync(string apiPath = "api/content.json")
    {
        if (_cache is not null) return Task.FromResult(_cache);
        if (_inflight is not null) return _inflight;
        _inflight = LoadAsync(apiPath);
        return _inflight;
    }

    private async Task<ContentData> LoadAsync(string apiPath)
    {
        try
        {
            // 绕过浏览器 HTTP 缓存：content.json 在每次部署时变化（新增/修改文章），
            // 若不破除缓存，首页会一直显示旧的文章列表。用唯一查询参数保证每次拉取都是最新。
            var sep = apiPath.Contains('?') ? '&' : '?';
            var url = $"{apiPath}{sep}_={DateTime.UtcNow.Ticks}";
            var data = await _http.GetFromJsonAsync<ContentData>(url, _jsonOptions);
            _cache = data ?? new ContentData();
        }
        catch
        {
            _cache = new ContentData();
        }
        return _cache;
    }
}
