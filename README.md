# MDV

Windows 11 ネイティブの**閲覧専用 Markdown ビューア**です。日本語UIで、GitHub風のレンダリングを行います。

## 概要

MDV（Markdown Viewer）は、Markdown ファイルを開いて閲覧することに特化したデスクトップアプリです。編集機能は持たず、GitHub の Markdown プレビューに近い見た目・体験で `.md` ファイルを表示します。

## 対応機能

- 表（テーブル）
- コードブロックのシンタックスハイライト
- タスクリスト（チェックボックス）
- 画像表示
- リンク（外部リンクは既定のブラウザで開く）
- LaTeX 数式（KaTeX によるレンダリング）
- Mermaid 図
- テーマ切替（ライト / ダーク / システム連動）
- 文字サイズの拡大・縮小・リセット
- ステータスバー（左：文字エンコード、右：改行コード）
- 設定の永続化（テーマ・文字サイズ・ステータスバー表示状態などを次回起動時に復元）
- 3 つの開き方
  - メニューからファイルを選択
  - ファイルのドラッグ＆ドロップ
  - ファイル関連付け（エクスプローラーから `.md` 等をダブルクリック）

## 技術スタック

- C# / .NET 9
- WinUI 3（Windows App SDK）
- WebView2 + Markdig（Markdown → HTML 変換）
- フロントエンド資産（KaTeX / Mermaid / highlight.js / github-markdown-css）はすべてローカルに同梱（オフラインで動作）

## ビルド方法

```powershell
dotnet build MDV.sln
```

実行する場合は、Visual Studio 2022 / 2026 で `MDV.App` をスタートアッププロジェクトに設定して起動してください（単一プロジェクト MSIX 構成）。

### Core 層のテスト

```powershell
dotnet test tests/MDV.Core.Tests
```

## 対応拡張子

- `.md`
- `.markdown`
- `.mdown`
- `.mkd`

## 対象OS

Windows 11 のみ

## 既知の制限

1. ファイルが外部で更新されても自動再読み込みは行いません（手動の「再読み込み」操作が必要です）。
2. テーマを動的に切り替えても、すでにレンダリング済みの Mermaid 図は再テーマされません（反映するには再読み込みが必要です）。
3. 文字サイズの拡大・縮小は WebView2 の CSS zoom によって実装しています（WinUI 3 の WebView2 はネイティブの `ZoomFactor` が公開されていないため）。

## サンプル文書

`docs/sample.md` に、表・コード・タスクリスト・数式・Mermaid 図・外部リンクを網羅した検証用サンプルがあります。
