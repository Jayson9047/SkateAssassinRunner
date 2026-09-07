# Production Localization Workflow

Unity Localization is the single authoritative framework.

1. Run **Tools → Skate Runner → Localization → Build or Update Production Localization** after adding semantic catalog entries or production static TMP.
2. Export review files with **Export CSV**. Files are written to `Assets/Localization/Exports/` and can be imported with Unity Localization's supported CSV import UI.
3. Run **Audit Production Localization**, **Validate Missing Entries**, **Validate Font Glyphs**, and **Validate TMP Fit** before a release candidate.
4. Use **Test Locale** menu entries to inspect the six launch locales (`en`, `es`, `nl`, `fr`, `pt-BR`, `de`) and the hidden +35% pseudo locale. The pseudo locale is intentionally absent from the player Settings menu.

## Adding a locale

Add its code to `SkateLocalizationCatalog.ProductionLocaleCodes`, provide every catalog value, add its native display name in `SkateLocalization` and the editor builder, then run the builder. Configure an English `FallbackLocale` and add the new native-name row to Settings. Do not add pseudo locales to `LocalizationLanguageMenu`.

## Font policy

All launch locales retain the authored Lilita One/LayerLabs TMP font and material setup. Matching authored Lilita One Extended ASCII atlases provide the accented Latin glyphs; generic fonts such as Liberation Sans are not production fallbacks.

The original Lilita One `.ttf`/`.otf` is not present in this project—the legacy TMP assets reference an unresolved source GUID—so glyph validation is performed against the actual shipped static TMP atlases and their Lilita-only fallback chains. Re-import the licensed source font before attempting any future atlas regeneration.

The previously generated `zh-Hans` locale and tables are intentionally dormant for possible future use. They are not registered in Supported Locales, do not appear in the player language menu, and are rejected by the runtime production-locale allow-list.
