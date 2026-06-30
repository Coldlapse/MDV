using System.Text;
using MDV.Core.Models;
using MDV.Core.Services;
using Xunit;

public class FileServiceTests
{
    [Fact]
    public void Loads_utf8_crlf_file()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "# 見出し\r\n本文\r\n", new UTF8Encoding(false));
        try
        {
            var r = FileService.Load(path);
            Assert.Equal("UTF-8", r.EncodingDisplay);
            Assert.Equal(LineEndingKind.Crlf, r.LineEnding);
            Assert.Contains("見出し", r.Text);
            Assert.Equal(path, r.FilePath);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Loads_shiftjis_lf_file()
    {
        EncodingDetector.RegisterProviders();
        var path = Path.GetTempFileName();
        File.WriteAllBytes(path, Encoding.GetEncoding(932).GetBytes("日本語\n"));
        try
        {
            var r = FileService.Load(path);
            Assert.Equal("Shift_JIS", r.EncodingDisplay);
            Assert.Equal(LineEndingKind.Lf, r.LineEnding);
            Assert.Contains("日本語", r.Text);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Missing_file_throws()
    {
        Assert.Throws<FileNotFoundException>(() => FileService.Load(@"C:\no\such\file.md"));
    }
}
