using MDV.Core.Models;
using MDV.Core.Services;
using Xunit;

public class LineEndingDetectorTests
{
    [Fact] public void Crlf_only() =>
        Assert.Equal(LineEndingKind.Crlf, LineEndingDetector.Detect("a\r\nb\r\n"));

    [Fact] public void Lf_only() =>
        Assert.Equal(LineEndingKind.Lf, LineEndingDetector.Detect("a\nb\n"));

    [Fact] public void Cr_only() =>
        Assert.Equal(LineEndingKind.Cr, LineEndingDetector.Detect("a\rb\r"));

    [Fact] public void Mixed_crlf_and_lf() =>
        Assert.Equal(LineEndingKind.Mixed, LineEndingDetector.Detect("a\r\nb\nc"));

    [Fact] public void No_line_ending() =>
        Assert.Equal(LineEndingKind.None, LineEndingDetector.Detect("abc"));

    [Theory]
    [InlineData(LineEndingKind.Crlf, "CRLF")]
    [InlineData(LineEndingKind.Lf, "LF")]
    [InlineData(LineEndingKind.Cr, "CR")]
    [InlineData(LineEndingKind.Mixed, "混在")]
    [InlineData(LineEndingKind.None, "")]
    public void Display_strings(LineEndingKind kind, string expected) =>
        Assert.Equal(expected, LineEndingDetector.ToDisplay(kind));
}
