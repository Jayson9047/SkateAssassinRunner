# Production Localization Audit

Generated: 2026-09-07 02:01:26 UTC

## Scope

- `Assets/Scenes/ElroiBootSplash.unity`
- `Assets/Scenes/SkateRunnerLoadingScreen.unity`
- `Assets/Scenes/SkateRunnerStartScreen.unity`
- `Assets/Scenes/SkateRunner.unity`
- `Assets/Prefabs/Characters/UICamera.prefab`

Debug Tools, third-party demos, internal IDs, save keys, scene names, and branding are excluded by policy.

## Summary

- Production TMP components inspected: 432
- Static TMP with LocalizeStringEvent: 186
- Intentional/dynamic/unmapped TMP: 2
- Missing/empty translations: 0
- Broken localized references: 0
- Latin-table glyph warnings: 0
- Production Latin font chain: authored Lilita One/LayerLabs assets only; Extended ASCII Lilita atlases supply required accents.
- Lilita One source is included under OFL in Fonts; plain SDF uses a matching Lilita SDF fallback. Bitmap accents use matching styled LayerLabs atlases.
- Static checks do not certify appearance or fit. Live six-locale Game-view captures and drift observations are required; see ProductionPolishReport.md.
- Static typography policy warnings: 0
- Likely hardcoded user-string warnings: 0
- Known unresolved baked-text art: see `BakedTextAudit.md`.

## Unbound TMP review list

- Assets/Scenes/SkateRunner.unity :: UICamera/Canvas/PhaseTimerText :: `Timer`
- Assets/Prefabs/Characters/UICamera.prefab :: UICamera/Canvas/PhaseTimerText :: `Timer`

## Missing translations

None.

## Broken references

None.

## Font warnings

None.

## Fit warnings

None.

## Likely hardcoded strings

None.

