using Microsoft.Windows.ApplicationModel.Resources;
using Microsoft.Windows.Globalization;

namespace MDV.App.Services;

/// <summary>
/// コードから使うUI文字列を Strings/&lt;言語&gt;/Resources.resw から取得する。
/// 言語は [表示]→[言語] の指定、未指定ならWindowsの表示言語で決まり、
/// 該当する翻訳が無ければ既定言語(ja-JP)になる。
/// XAML側の文字列は x:Uid で同じリソースから読み込まれる。
/// </summary>
public static partial class Localizer
{
    private static ResourceLoader? _loader;

    public static string Get(string key)
    {
        try
        {
            _loader ??= new ResourceLoader();
            var value = _loader.GetString(key);
            // キーが見つからない場合も画面が空欄にならないよう、キー名をそのまま返す
            return string.IsNullOrEmpty(value) ? key : value;
        }
        catch
        {
            return key;
        }
    }

    /// <summary>
    /// 翻訳が用意されている言語(BCP-47タグ)の一覧。Strings/ 以下のフォルダ名から
    /// ビルド時に生成される(MDV.App.csproj の MdvGenerateLanguageList)ため、言語を追加してもコードの変更は不要。
    /// </summary>
    public static IReadOnlyList<string> AvailableLanguages() => BuiltInLanguages;

    /// <summary>
    /// 表示言語を指定する(空文字はWindowsの表示言語に従う)。ウィンドウ生成前に呼ぶこと。
    /// 用意されていない言語が保存されていた場合は無視してWindowsの表示言語に従う。
    /// </summary>
    public static void ApplyLanguageOverride(string? language)
    {
        try
        {
            var available = AvailableLanguages();
            var tag = !string.IsNullOrEmpty(language) &&
                      available.Any(l => string.Equals(l, language, StringComparison.OrdinalIgnoreCase))
                ? language
                : "";
            ApplicationLanguages.PrimaryLanguageOverride = tag;
        }
        catch
        {
            // 指定できない環境では既定の言語選択に任せる
        }
    }

    /// <summary>言語タグをその言語自身の名称で返す(例: ko-KR → 한국어)。</summary>
    public static string NativeName(string tag)
    {
        try { return new Windows.Globalization.Language(tag).NativeName; }
        catch { return tag; }
    }
}
