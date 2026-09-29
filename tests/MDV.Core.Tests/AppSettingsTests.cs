using MDV.Core.Models;
using Xunit;

public class AppSettingsTests
{
    [Fact] public void Defaults_are_system_1_true()
    {
        var s = new AppSettings();
        Assert.Equal(ThemeMode.System, s.Theme);
        Assert.Equal(1.0, s.ZoomFactor);
        Assert.True(s.ShowStatusBar);
    }

    [Fact] public void Roundtrips_through_json()
    {
        var s = new AppSettings { Theme = ThemeMode.Dark, ZoomFactor = 1.5, ShowStatusBar = false };
        var back = AppSettings.FromJson(s.ToJson());
        Assert.Equal(ThemeMode.Dark, back.Theme);
        Assert.Equal(1.5, back.ZoomFactor);
        Assert.False(back.ShowStatusBar);
    }

    [Fact] public void Null_or_garbage_returns_defaults()
    {
        Assert.Equal(ThemeMode.System, AppSettings.FromJson(null).Theme);
        Assert.Equal(ThemeMode.System, AppSettings.FromJson("not json {{{").Theme);
    }
}

public class AppSettingsLanguageTests
{
    [Fact] public void Language_defaults_to_system()
    {
        Assert.Equal("", new AppSettings().Language);
    }

    [Fact] public void Language_roundtrips_through_json()
    {
        var back = AppSettings.FromJson(new AppSettings { Language = "ko-KR" }.ToJson());
        Assert.Equal("ko-KR", back.Language);
    }

    [Fact] public void Settings_saved_by_older_versions_use_system_language()
    {
        // Language が無い旧バージョンの保存値でも読み込めること
        var back = AppSettings.FromJson("{\"Theme\":1,\"ZoomFactor\":1.2,\"ShowStatusBar\":true}");
        Assert.Equal("", back.Language);
        Assert.Equal(ThemeMode.Dark, back.Theme);
    }
}
