namespace MDV.Core.Models;

/// <summary>ファイル読込結果: 本文・エンコード表示・改行種別・パス</summary>
public record DocumentLoadResult(string Text, string EncodingDisplay, LineEndingKind LineEnding, string FilePath);
