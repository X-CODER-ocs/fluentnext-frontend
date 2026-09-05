using FluentNext.Frontend;
using FluentNext.Frontend.Services;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Options;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Fluent UI Blazor 官方组件库（Microsoft.FluentUI.AspNetCore.Components）
builder.Services.AddFluentUIComponents();

// 后端数据（Hexo 生成的 content.json）+ 客户端设置（localStorage）
builder.Services.AddScoped<ContentService>();
builder.Services.AddScoped<ClientSettings>();
// 跨组件 UI 状态（侧边栏设置按钮 → 主布局抽屉）
builder.Services.AddScoped<AppState>();

// NexT 式主题配置（wwwroot/appsettings.json 的 FluentNext 段）
builder.Services.Configure<FluentNextConfig>(builder.Configuration.GetSection("FluentNext"));

await builder.Build().RunAsync();
