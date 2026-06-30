using MDV.Core.Models;

namespace MDV.Core.Services;

/// <summary>テキスト中の改行コードを判定する</summary>
public static class LineEndingDetector
{
    public static LineEndingKind Detect(string text)
    {
        int crlf = 0, lf = 0, cr = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n') { crlf++; i++; }
                else cr++;
            }
            else if (text[i] == '\n') lf++;
        }
        int kinds = (crlf > 0 ? 1 : 0) + (lf > 0 ? 1 : 0) + (cr > 0 ? 1 : 0);
        if (kinds == 0) return LineEndingKind.None;
        if (kinds > 1) return LineEndingKind.Mixed;
        if (crlf > 0) return LineEndingKind.Crlf;
        if (lf > 0) return LineEndingKind.Lf;
        return LineEndingKind.Cr;
    }

    public static string ToDisplay(LineEndingKind kind) => kind switch
    {
        LineEndingKind.Crlf => "CRLF",
        LineEndingKind.Lf => "LF",
        LineEndingKind.Cr => "CR",
        LineEndingKind.Mixed => "混在",
        _ => ""
    };
}
