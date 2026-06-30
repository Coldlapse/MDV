using System.Text.Json;

namespace MDV.Core.Models;

/// <summary>永続化するアプリ設定: テーマ/文字倍率/ステータスバー表示</summary>
public class AppSettings
{
    public ThemeMode Theme { get; set; } = ThemeMode.System;
    public double ZoomFactor { get; set; } = 1.0;
    public bool ShowStatusBar { get; set; } = true;

    public string ToJson() => JsonSerializer.Serialize(this);

    public static AppSettings FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new AppSettings();
        try { return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings(); }
        catch (JsonException) { return new AppSettings(); }
    }
}
