using System.Text.Json;
using Microsoft.JSInterop;

namespace FluentNext.Frontend.Services;

/// <summary>客户端设置：存浏览器 localStorage（不同观看者各有各的配置）</summary>
public class ClientSettings
{
    private readonly IJSRuntime _js;
    private const string Key = "fluentnext-settings";

    public Settings Data { get; private set; } = new();
    public event Action? Changed;

    public ClientSettings(IJSRuntime js) => _js = js;

    public async Task InitAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", Key);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var parsed = JsonSerializer.Deserialize<Settings>(json);
                if (parsed is not null) Data = parsed;
            }
        }
        catch
        {
            // localStorage 不可用时静默回退到默认
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", Key, JsonSerializer.Serialize(Data));
        }
        catch { /* ignore */ }
        Changed?.Invoke();
    }

    public void SetLayout(string layout) => Data.Layout = layout;
    public void SetTheme(string theme) => Data.Theme = theme;
    public void SetFontSize(int size) => Data.FontSize = size;
}

public class Settings
{
    public string Layout { get; set; } = "magazine"; // list | magazine
    public string Theme { get; set; } = "System";     // Light | Dark | System
    public int FontSize { get; set; } = 16;           // 正文 px
}
