using MDV.Core.Models;

namespace MDV.Core.Services;

/// <summary>Markdownファイルを読み込み、本文・エンコード・改行種別を返す</summary>
public static class FileService
{
    public static DocumentLoadResult Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("ファイルが見つかりません。", path);

        var bytes = File.ReadAllBytes(path);
        var enc = EncodingDetector.Detect(bytes);

        // BOM分を除いてデコード
        var preamble = enc.Encoding.GetPreamble();
        var body = (preamble.Length > 0 && bytes.Length >= preamble.Length)
            ? bytes[preamble.Length..]
            : bytes;
        var text = enc.Encoding.GetString(body);

        var lineEnding = LineEndingDetector.Detect(text);
        return new DocumentLoadResult(text, enc.DisplayName, lineEnding, path);
    }
}
