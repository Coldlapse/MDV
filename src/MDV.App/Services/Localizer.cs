using Microsoft.Windows.ApplicationModel.Resources;

namespace MDV.App.Services;

/// <summary>
/// コードから使うUI文字列を Strings/&lt;言語&gt;/Resources.resw から取得する。
/// 言語はWindowsの表示言語で決まり、該当する翻訳が無ければ既定言語(ja-JP)になる。
/// XAML側の文字列は x:Uid で同じリソースから読み込まれる。
/// </summary>
public static class Localizer
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
}
