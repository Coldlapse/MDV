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
