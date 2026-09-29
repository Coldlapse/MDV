# Translating MDV

MDV picks its UI language from the Windows display language.
If there is no translation for that language, it falls back to Japanese.

| Language | File |
|---|---|
| Japanese (default) | [`src/MDV.App/Strings/ja-JP/Resources.resw`](../../src/MDV.App/Strings/ja-JP/Resources.resw) |

## Adding a language

1. Copy [`Resources.template.resw`](Resources.template.resw) to
   `src/MDV.App/Strings/<language-tag>/Resources.resw`.
   `<language-tag>` is a BCP-47 tag such as `fr-FR`, `de-DE`, `zh-Hans`, `pt-BR`.
2. Open the copy in any text editor and replace the text inside each `<value>…</value>`.
   - The English text is a reference. Each `<comment>` says where the text appears and gives the Japanese original.
   - Do **not** change `name="…"`.
   - Keep placeholders such as `{0}` and `{1}`.
   - Set `WebViewLanguage` to the same tag as the folder name.
3. Build the app. No code or manifest change is needed: the package picks up every folder under `Strings/`.
4. Run the tests (`dotnet test tests/MDV.Core.Tests`). They fail if a language file is missing a key,
   has an extra key, or drops a placeholder.

To try a translation without changing your Windows language, add the language in
**Settings → Time & language → Language & region** and move it to the top.

## Adding or changing a UI string (for developers)

1. Add the key to `Strings/ja-JP/Resources.resw` (the default language).
2. Add the same key to `docs/translations/Resources.template.resw` with an English reference value and a
   `<comment>` that explains the context and gives the Japanese original.
3. Add the key to every other `Strings/*/Resources.resw`. If you cannot translate it, copy the English
   text; the tests only check that the key exists.

Menu items use `x:Uid` in XAML (key = `<Uid>.<Property>`, e.g. `MenuOpen.Text`).
Code uses `Localizer.Get("Key")`.
