namespace FluentNext.Frontend.Services;

/// <summary>
/// 跨组件 UI 状态：侧边栏的设置按钮通过它请求 MainLayout 打开设置抽屉。
/// Blazor WebAssembly 中注册为 Scoped（等价于单例）即可在组件间共享。
/// </summary>
public class AppState
{
    public event Action? SettingsOpenRequested;

    /// <summary>由侧边栏「设置」按钮调用，通知 MainLayout 打开抽屉</summary>
    public void RequestOpenSettings() => SettingsOpenRequested?.Invoke();
}
