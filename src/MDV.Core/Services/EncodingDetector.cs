using System.Text;
using MDV.Core.Models;

namespace MDV.Core.Services;

/// <summary>バイト列からテキストエンコードを判定する</summary>
public static class EncodingDetector
{
    private static bool _registered;

    /// <summary>Shift_JIS(932)を使うためのプロバイダ登録。起動時に1回呼ぶ。</summary>
    public static void RegisterProviders()
    {
        if (_registered) return;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _registered = true;
    }

    public static TextEncodingInfo Detect(byte[] bytes)
    {
        // 1. BOM優先
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return new TextEncodingInfo(new UTF8Encoding(true), "UTF-8 (BOM)");
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return new TextEncodingInfo(Encoding.Unicode, "UTF-16 LE");
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return new TextEncodingInfo(Encoding.BigEndianUnicode, "UTF-16 BE");

        // 2. UTF-8として厳密デコードを試行
        if (IsValidUtf8(bytes))
            return new TextEncodingInfo(new UTF8Encoding(false), "UTF-8");

        // 3. 失敗ならShift_JIS
        RegisterProviders();
        return new TextEncodingInfo(Encoding.GetEncoding(932), "Shift_JIS");
    }

    private static bool IsValidUtf8(byte[] bytes)
    {
        try
        {
            var strict = new UTF8Encoding(false, throwOnInvalidBytes: true);
            strict.GetString(bytes);
            return true;
        }
        catch (DecoderFallbackException) { return false; }
    }
}
