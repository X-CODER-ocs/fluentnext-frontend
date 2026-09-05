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

    // 以下为「完整主题」自定义项：强调色 / 背景 / 文字 / 字体。
    // 背景 / 文字留空字符串表示「跟随主题（不覆盖）」，由抽屉里的颜色选择器显示主题默认值但不写入覆盖。
    public void SetAccent(string color) => Data.Accent = color;
    public void SetBackground(string? color) => Data.Background = color ?? "";
    public void SetForeground(string? color) => Data.Foreground = color ?? "";
    public void SetFontFamily(string font) => Data.FontFamily = font;
}

public class Settings
{
    public string Layout { get; set; } = "magazine"; // list | magazine
    public string Theme { get; set; } = "System";     // Light | Dark | System
    public int FontSize { get; set; } = 16;           // 正文 px

    /// <summary>强调色（自定义主色），默认品牌绿。空时回退到配置里的 BrandColor</summary>
    public string Accent { get; set; } = "#4AA26F";

    /// <summary>背景色（完整主题），空 = 跟随 Fluent 主题</summary>
    public string Background { get; set; } = "";

    /// <summary>文字色（完整主题），空 = 跟随 Fluent 主题</summary>
    public string Foreground { get; set; } = "";

    /// <summary>正文字体栈 key：system | sans | serif | mono</summary>
    public string FontFamily { get; set; } = "system";
}
