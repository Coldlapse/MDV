# RESUME — MDV (Markdown Viewer) 開発再開メモ

> 作成日: 2026-07-01
> 目的: フォルダ改名(`pyMDV`→`MDV`)のためセッションを一度終了する。再開時にこのファイルを読ませて続きから始める。

---

## 🔁 再開手順

1. このClaude Codeセッションを終了する
2. エクスプローラー or 別ターミナルで親フォルダを改名:
   - `C:\Users\kajiy\Documents\git-personal\pyMDV` → `C:\Users\kajiy\Documents\git-personal\MDV`
   - PowerShell例（フォルダの外＝git-personalで実行）:
     ```powershell
     Rename-Item "C:\Users\kajiy\Documents\git-personal\pyMDV" -NewName "MDV"
     ```
3. `MDV` フォルダで Claude Code を開き直す
4. 最初のメッセージで「`RESUME.md` を読んで続きから再開して」と伝える

> ⚠️ 改名できなかった理由: セッション自身が `pyMDV` を作業ディレクトリとして使用中でロックされていたため。セッション終了後なら改名できる。

---

## 🎯 作るもの

- **MDV** = Win11ネイティブの **Markdown表示専用** デスクトップアプリ
- **日本語UI** / **編集モードなし（閲覧専用）**

---

## ✅ 確定した要件（ブレストで合意済み）

| 項目 | 決定内容 |
|---|---|
| Markdown対応範囲 | **GitHub風フル対応**（表・コードシンタックスハイライト・チェックリスト・画像・リンク・**LaTeX数式**・**Mermaid図**） |
| ファイルの開き方 | **3つすべて**: ①メニューから開く ②ドラッグ&ドロップ ③ファイル関連付け/右クリック(コマンドライン引数受け取り) |
| 上部メニュー | **[ファイル]** と **[表示]**（Win11ネイティブなメニューバー） |
| 「表示」メニューの中身 | **4つすべて**: テーマ切替 / 文字サイズ拡大縮小・リセット / 再読み込み / ステータスバー表示切替 |
| テーマ切替 | 右上アイコン(🌙/☀️)で ライト/ダーク 切替（「システムに合わせる」も含む） |
| ステータスバー | 左下=**エンコード** / 右下=**改行コード** |
| 配布形態 | **MSIXパッケージ化**（マニフェストでファイル関連付けを宣言できてクリーン） |

---

## 🏗️ 提示済みの設計案（ユーザーレビュー途中）

> 状態: セクション1〜5を提示し、ユーザーの最終承認待ちだった段階。承認後に仕様書(spec)を書く予定。

### 技術スタックと描画方式（推奨案）
- **C# / .NET 9 / WinUI 3（Windows App SDK）**
- **描画方式 = WebView2 + Markdig**（採用理由: 数式・Mermaid・表を満たせるのはこの方式だけ。CommunityToolkit MarkdownTextBlockはフル要件を満たせず不採用）
- Markdown変換: **Markdig**（MD→HTML）
- 見た目: **github-markdown-css** + **highlight.js**(コード色付け) + **KaTeX**(数式) + **Mermaid**(図) をHTMLテンプレートに同梱

### 画面レイアウト
```
┌─────────────────────────────────────────────┐
│ [ファイル] [表示]              タイトル   🌙/☀️ │ ← メニューバー + テーマ切替アイコン
├─────────────────────────────────────────────┤
│         WebView2（Markdownレンダリング）      │
├─────────────────────────────────────────────┤
│ UTF-8                                  CRLF │ ← ステータスバー(左:エンコード / 右:改行)
└─────────────────────────────────────────────┘
```

### コンポーネント構成
- **MainWindow** — レイアウト組み立て
- **MarkdownRenderer** — Markdig変換 + HTMLテンプレート組み立て
- **FileService** — ファイル読込 / エンコード判定 / 改行コード判定
- **ThemeService** — ライト/ダーク/システム連動の管理と適用（WebView2のCSSも切替）
- **SettingsService** — テーマ・文字倍率・ステータスバー表示状態を保存（次回起動時復元）
- **App起動処理** — ファイル関連付け起動・コマンドライン引数・D&D の受け取り

### データフロー（ファイルを開く）
1. ファイル指定（メニュー/D&D/関連付け）
2. バイト読込 → **エンコード判定**（BOM優先 → UTF-8試行 → 失敗ならShift_JIS。`System.Text.Encoding.CodePagesEncodingProvider`が必要）
3. **改行コード判定**（CRLF / LF / CR、混在は「混在」表示）
4. Markdig変換 → HTMLテンプレートに流し込み → WebView2へ
5. ステータスバー更新（左:エンコード, 右:改行コード）

### エラー処理・補足
- ファイルが開けない/非テキスト → 日本語エラーメッセージ表示
- 文字サイズ変更は WebView2 の ZoomFactor で実現
- エンコード表示例: `UTF-8` / `UTF-8 (BOM)` / `Shift_JIS` / `UTF-16 LE`

---

## 🧰 環境メモ（確認済み）

- `.NET 9` インストール済み（`dotnet --version` → 9.0.315）
- **WinUI3テンプレートは未インストール**（`dotnet new list` に出ない）→ 実装前にWindows App SDK用テンプレート導入が必要
  - 例: `dotnet new install Microsoft.WindowsAppSDK.ProjectTemplates`（または Visual Studio の「.NET デスクトップ開発」+「Windows App SDK」ワークロード）
- MSIXパッケージ化のため、単一プロジェクトMSIX(Single-project MSIX)構成を想定

---

## ⏭️ 次のステップ（再開後）

1. 上記設計の**最終承認**をユーザーから得る（変更あれば反映）
2. **仕様書**を `docs/superpowers/specs/2026-07-01-mdv-design.md` に作成・コミット
3. 仕様書のセルフレビュー → ユーザーレビュー
4. **writing-plans** スキルで実装計画を作成
5. 実装開始

> プロセス: superpowers の brainstorming → writing-plans → 実装 の流れで進行中。
