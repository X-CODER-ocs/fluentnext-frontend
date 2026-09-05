namespace FluentNext.Frontend;

/// <summary>后端（Hexo）产出的内容数据</summary>
public class ContentData
{
    public SiteInfo Site { get; set; } = new();
    public List<BlogPost> Posts { get; set; } = new();
    /// <summary>分类统计（name / slug / count）</summary>
    public List<CategoryInfo> Categories { get; set; } = new();
    /// <summary>标签统计（name / slug / count）</summary>
    public List<TagInfo> Tags { get; set; } = new();
    /// <summary>归档统计（按年 → 月）</summary>
    public List<ArchiveInfo> Archives { get; set; } = new();
}

public class SiteInfo
{
    public string Title { get; set; } = "X-CODER";
    public string Description { get; set; } = "";
    public string Url { get; set; } = "";
    public List<MenuItem> Menu { get; set; } = new();
}

public class BlogPost
{
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Permalink { get; set; } = "";
    public DateTime Date { get; set; }
    public DateTime Updated { get; set; }
    public string Excerpt { get; set; } = "";
    /// <summary>文章正文（HTML，由 Hexo 后端渲染后输出；标题已注入与 Toc 一致的 id）</summary>
    public string Content { get; set; } = "";
    public List<string> Categories { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    /// <summary>文章目录（h2~h4），id 与正文标题锚点一一对应</summary>
    public List<TocItem> Toc { get; set; } = new();
}

/// <summary>文章目录条目</summary>
public class TocItem
{
    public int Level { get; set; }
    public string Text { get; set; } = "";
    public string Id { get; set; } = "";
}

public class CategoryInfo
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public int Count { get; set; }
}

public class TagInfo
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public int Count { get; set; }
}

public class ArchiveInfo
{
    public int Year { get; set; }
    public int Count { get; set; }
    public List<ArchiveMonth> Months { get; set; } = new();
}

public class ArchiveMonth
{
    public int Month { get; set; }
    public int Count { get; set; }
}

public class MenuItem
{
    public string Text { get; set; } = "";
    public string Href { get; set; } = "";
    public string Icon { get; set; } = "";
}
