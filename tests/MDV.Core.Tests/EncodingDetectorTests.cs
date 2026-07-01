using System.Linq;
using System.Text;
using MDV.Core.Services;
using Xunit;

public class EncodingDetectorTests
{
    public EncodingDetectorTests() => EncodingDetector.RegisterProviders();

    [Fact] public void Utf8_bom()
    {
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes("あ")).ToArray();
        var r = EncodingDetector.Detect(bytes);
        Assert.Equal("UTF-8 (BOM)", r.DisplayName);
        Assert.Equal("あ", r.Encoding.GetString(StripBom(bytes, r)));
    }

    [Fact] public void Utf8_no_bom()
    {
        var bytes = Encoding.UTF8.GetBytes("Hello あ");
        var r = EncodingDetector.Detect(bytes);
        Assert.Equal("UTF-8", r.DisplayName);
    }

    [Fact] public void Utf16_le_bom()
    {
        var bytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("あ")).ToArray();
        Assert.Equal("UTF-16 LE", EncodingDetector.Detect(bytes).DisplayName);
    }

    [Fact] public void ShiftJis_fallback()
    {
        EncodingDetector.RegisterProviders();
        var sjis = Encoding.GetEncoding(932);
        var bytes = sjis.GetBytes("日本語テスト");
        var r = EncodingDetector.Detect(bytes);
        Assert.Equal("Shift_JIS", r.DisplayName);
        Assert.Equal("日本語テスト", r.Encoding.GetString(bytes));
    }

    [Fact] public void Pure_ascii_is_utf8()
    {
        var r = EncodingDetector.Detect(Encoding.ASCII.GetBytes("plain text"));
        Assert.Equal("UTF-8", r.DisplayName);
    }

    private static byte[] StripBom(byte[] b, MDV.Core.Models.TextEncodingInfo r)
    {
        var pre = r.Encoding.GetPreamble();
        return b.Length >= pre.Length && pre.Length > 0 ? b[pre.Length..] : b;
    }
}
