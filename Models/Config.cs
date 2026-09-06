namespace FluentNext.Frontend;

/// <summary>NexT 式主题配置，对应 wwwroot/appsettings.json 的 "FluentNext" 段</summary>
public class FluentNextConfig
{
    /// <summary>强调色（Fluent UI 的 Accent base color），默认绿 #4AA26F</summary>
    public string BrandColor { get; set; } = "#4AA26F";

    /// <summary>默认布局：list（仅标题） / magazine（含内容）</summary>
    public string DefaultLayout { get; set; } = "magazine";

    /// <summary>默认主题：Light / Dark / System</summary>
    public string DefaultTheme { get; set; } = "System";

    /// <summary>导航菜单</summary>
    public List<MenuItem> Menu { get; set; } = new();

    /// <summary>RSS / Atom 订阅源地址；相对路径会随 base href(/blog/) 解析为 /blog/atom.xml</summary>
    public string FeedUrl { get; set; } = "atom.xml";

    /// <summary>评论配置：utterances / none</summary>
    public CommentsConfig Comments { get; set; } = new();
}

public class CommentsConfig
{
    public string Provider { get; set; } = "utterances"; // "utterances" | "none"
    public UtterancesConfig Utterances { get; set; } = new();
}

public class UtterancesConfig
{
    public string Repo { get; set; } = "";        // 例如 "X-CODER-ocs/blog"
    public string IssueTerm { get; set; } = "pathname";
    public string Label { get; set; } = "";
}
