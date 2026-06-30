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

    // Markdig(UseAdvancedExtensions)のDiagrams拡張により、```mermaid フェンスは
    // 既に <pre class="mermaid">...</pre> として出力されるため、変換処理は不要
    // （テンプレート側 mermaid.run({querySelector:'.mermaid'}) がそのまま拾える）。
    public static string RenderBody(string markdown)
    {
        return Markdown.ToHtml(markdown ?? string.Empty, Pipeline);
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
