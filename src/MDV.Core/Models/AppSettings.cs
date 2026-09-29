using System.Text.Json;

namespace MDV.Core.Models;

/// <summary>永続化するアプリ設定: テーマ/文字倍率/ステータスバー表示/表示言語</summary>
public class AppSettings
{
    public ThemeMode Theme { get; set; } = ThemeMode.System;
    public double ZoomFactor { get; set; } = 1.0;
    public bool ShowStatusBar { get; set; } = true;
    /// <summary>UIの表示言語(BCP-47タグ)。空文字はWindowsの表示言語に合わせる</summary>
    public string Language { get; set; } = "";

    public string ToJson() => JsonSerializer.Serialize(this);

    public static AppSettings FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new AppSettings();
        try { return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings(); }
        catch (JsonException) { return new AppSettings(); }
    }
}
