using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MDV.Core.Tests;

/// <summary>
/// 各言語の Resources.resw と翻訳テンプレートが、既定言語(ja-JP)と同じキー・プレースホルダを持つことを検査する。
/// キーの追加漏れ・綴り違いは実行時に「キー名がそのまま表示される」だけで気付きにくいため、テストで防ぐ。
/// </summary>
public class LocalizationResourceTests
{
    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly string StringsDir = Path.Combine(RepoRoot, "src", "MDV.App", "Strings");
    private static readonly string DefaultFile = Path.Combine(StringsDir, "ja-JP", "Resources.resw");
    private static readonly string TemplateFile = Path.Combine(RepoRoot, "docs", "translations", "Resources.template.resw");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "MDV.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("MDV.sln が見つかりません");
    }

    private static Dictionary<string, string> Load(string path) =>
        XDocument.Load(path).Root!.Elements("data")
            .ToDictionary(d => (string)d.Attribute("name")!, d => (string?)d.Element("value") ?? "");

    private static string[] Placeholders(string value) =>
        Regex.Matches(value, @"\{\d+\}").Select(m => m.Value).Distinct().Order().ToArray();

    // 既定言語以外の全言語ファイル + 翻訳テンプレート
    public static IEnumerable<object[]> OtherFiles() =>
        Directory.GetFiles(StringsDir, "Resources.resw", SearchOption.AllDirectories)
            .Where(p => !string.Equals(Path.GetFullPath(p), Path.GetFullPath(DefaultFile), StringComparison.OrdinalIgnoreCase))
            .Append(TemplateFile)
            .Select(p => new object[] { Path.GetRelativePath(RepoRoot, p) });

    [Fact]
    public void DefaultLanguageAndTemplateExist()
    {
        Assert.True(File.Exists(DefaultFile), DefaultFile);
        Assert.True(File.Exists(TemplateFile), TemplateFile);
    }

    [Theory]
    [MemberData(nameof(OtherFiles))]
    public void HasSameKeysAsDefault(string relativePath)
    {
        var expected = Load(DefaultFile).Keys.Order().ToArray();
        var actual = Load(Path.Combine(RepoRoot, relativePath)).Keys.Order().ToArray();
        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(OtherFiles))]
    public void KeepsPlaceholdersAndHasNoEmptyValues(string relativePath)
    {
        var def = Load(DefaultFile);
        foreach (var (key, value) in Load(Path.Combine(RepoRoot, relativePath)))
        {
            Assert.False(string.IsNullOrWhiteSpace(value), $"{key} が空です");
            if (def.TryGetValue(key, out var defValue))
                Assert.True(Placeholders(defValue).SequenceEqual(Placeholders(value)), $"{key} のプレースホルダが既定言語と異なります");
        }
    }

    [Fact]
    public void WebViewLanguageMatchesFolderName()
    {
        foreach (var dir in Directory.GetDirectories(StringsDir))
        {
            var file = Path.Combine(dir, "Resources.resw");
            if (!File.Exists(file)) continue;
            Assert.Equal(Path.GetFileName(dir), Load(file)["WebViewLanguage"], ignoreCase: true);
        }
    }
}
