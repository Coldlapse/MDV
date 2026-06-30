using MDV.Core.Models;
using Windows.Storage;

namespace MDV.App.Services;

/// <summary>AppSettingsをローカル設定(ApplicationData)に保存・復元する</summary>
public static class SettingsService
{
    private const string Key = "mdv.settings";

    public static AppSettings Load()
    {
        try
        {
            var json = ApplicationData.Current.LocalSettings.Values[Key] as string;
            return AppSettings.FromJson(json);
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            ApplicationData.Current.LocalSettings.Values[Key] = settings.ToJson();
        }
        catch
        {
            // 非パッケージ実行などで保存に失敗した場合は無視する
        }
    }
}
