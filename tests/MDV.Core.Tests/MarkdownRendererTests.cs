using MDV.Core.Models;
using MDV.Core.Services;
using Xunit;

public class MarkdownRendererTests
{
    [Fact] public void Table_is_rendered()
    {
        var html = MarkdownRenderer.RenderBody("| a | b |\n|---|---|\n| 1 | 2 |");
        Assert.Contains("<table", html);
    }

    [Fact] public void Task_list_is_rendered()
    {
        var html = MarkdownRenderer.RenderBody("- [x] done\n- [ ] todo");
        Assert.Contains("type=\"checkbox\"", html);
    }

    [Fact] public void Fenced_code_has_language_class()
    {
        var html = MarkdownRenderer.RenderBody("```csharp\nvar x = 1;\n```");
        Assert.Contains("language-csharp", html);
    }

    [Fact] public void Mermaid_block_has_mermaid_class()
    {
        // Markdig 1.3.2 (UseAdvancedExtensions の Diagrams拡張) は
        // ```mermaid フェンスを <pre class="mermaid">...</pre> として出力する
        // （<pre><code class="language-mermaid"> ではない）。
        var html = MarkdownRenderer.RenderBody("```mermaid\ngraph TD; A-->B;\n```");
        Assert.Contains("class=\"mermaid\"", html);
        Assert.Contains("graph TD; A-->B;", html);
        Assert.DoesNotContain("language-mermaid", html);
    }

    [Fact] public void Math_dollar_is_preserved_for_katex()
    {
        // KaTeXはクライアント側でレンダリングするため、数式の中身がHTMLに残ればよい
        var html = MarkdownRenderer.RenderBody("$E=mc^2$");
        Assert.Contains("E=mc^2", html);
    }

    [Fact] public void Math_renders_as_katex_span_with_backslash_paren_delimiter()
    {
        // Markdig 1.3.2 (UseMathematics) は $...$ を解釈し、$ をHTMLに残さず
        // <span class="math">\(...\)</span> 形式で出力する。
        // テンプレート側KaTeXの delimiters は \( \) を含む必要があり、この契約を固定する。
        var html = MarkdownRenderer.RenderBody("$E=mc^2$");
        Assert.Contains("class=\"math\"", html);
        Assert.Contains("\\(E=mc^2\\)", html);
    }

    [Fact] public void BuildHtml_substitutes_placeholders()
    {
        var template = "<html class=\"{{THEME_CLASS}}\"><base href=\"{{BASE_HREF}}\"><body>{{BODY}}</body></html>";
        var html = MarkdownRenderer.BuildHtml("# Hi", ThemeMode.Dark, template, "file:///C:/docs/");
        Assert.Contains("theme-dark", html);
        Assert.Contains("file:///C:/docs/", html);
        Assert.Contains("<h1", html);
        Assert.DoesNotContain("{{BODY}}", html);
        Assert.DoesNotContain("{{THEME_CLASS}}", html);
        Assert.DoesNotContain("{{BASE_HREF}}", html);
    }
}
