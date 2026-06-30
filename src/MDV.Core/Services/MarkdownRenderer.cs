using System.Text.RegularExpressions;
using Markdig;
using MDV.Core.Models;

namespace MDV.Core.Services;

/// <summary>Markdownを変換しHTMLテンプレートに組み立てる</summary>
public static class MarkdownRenderer
{
    // GFM(表/タスクリスト等)+数式 を有効化したパイプライン
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()   // 表・タスクリスト・自動リンク等
        .UseMathematics()          // $...$ / $$...$$ を保持
        .Build();

    public static string RenderBody(string markdown)
    {
        var html = Markdown.ToHtml(markdown ?? string.Empty, Pipeline);
        return ConvertMermaid(html);
    }

    // <pre><code class="language-mermaid">...</code></pre> を <div class="mermaid">...</div> に変換
    private static string ConvertMermaid(string html)
    {
        var pattern = "<pre><code class=\"language-mermaid\">(?<body>.*?)</code></pre>";
        return Regex.Replace(html, pattern,
            m => "<div class=\"mermaid\">" + System.Net.WebUtility.HtmlDecode(m.Groups["body"].Value) + "</div>",
            RegexOptions.Singleline);
    }

    public static string BuildHtml(string markdown, ThemeMode theme, string template, string baseDirUri)
    {
        var themeClass = theme switch
        {
            ThemeMode.Light => "theme-light",
            ThemeMode.Dark => "theme-dark",
            _ => "theme-system"
        };
        return template
            .Replace("{{THEME_CLASS}}", themeClass)
            .Replace("{{BASE_HREF}}", baseDirUri)
            .Replace("{{BODY}}", RenderBody(markdown));
    }
}
