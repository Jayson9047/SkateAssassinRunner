# Localization typography and runtime repair

The existing localization implementation was repaired in place. Previous uncommitted work was preserved; Git was used only to read authored UI baselines. No remote Git operation was performed.

The tested TMP typography and locale-refresh states pass the live review. This is **not a complete localized-art sign-off**: the existing claimed-reward stamp, Revive emblem, and some phase badges still contain English pixels. Those exact assets are listed in `BakedTextAudit.md`.

## Diagnosis and fixes

| Issue | Evidence and repair |
|---|---|
| Global shrinking | The old policy applied autosizing broadly with very low floors, rebuilt text from TMP's text-changed callback, and had already serialized reduced sizes/maxima into the scene. Values such as an authored 42 becoming 21 were present before this pass. Editor authoring used the contaminated size/base as a new ceiling. Prefab restoration alone also missed scene-added fitters on inherited gameplay objects. |
| Authored-size preservation | Restored targeted typography from the pre-localization Git/LFS content. Each fitter now stores an immutable authored string and maximum, measures at that maximum first, accounts for the authored footprint where LayerLabs deliberately used undersized text rectangles, and reduces only as needed. The lower bound is 80% of authored size. Fitting runs on changed text/rects in LateUpdate; there is no recursive mesh rebuilding or OnValidate mutation of TMP. Returning to English restores the same scale without learning a new baseline. |
| Settings disappearing labels | The three table references and callbacks were valid during inspection. The shared fitter had an unsafe text-changed → ForceMeshUpdate re-entry path, which was removed. The original persistent disappearance was not independently reproduced well enough to prove it had no other cause. After repair all three rows remained visible/localized throughout reopening and repeated locale cycles. No missing entries were invented to explain a rendering symptom. |
| Daily Spin corruption | The plain SDF title was falling back to styled bitmap glyphs. In addition, LayerLabs bitmap font atlas arrays referenced the embedded generation atlas while the material used an external styled PNG. TMP-generated fallback materials could therefore use the wrong texture/style. Repaired the bitmap atlas/material contract and assigned a matching plain Lilita SDF fallback to the plain primary font. Live German ä and all six Daily Spin titles now render correctly. |
| Mixed accents | Plain SDF text, including French PAR DÉFAUT, received accented characters from an outlined bitmap fallback. The new SDF fallback uses the real Lilita One source and the matching shader family. Outlined labels retain their LayerLabs bitmap font/material appearance and matching Extended ASCII atlases. |
| Dutch Magic | The key, Dutch value Magie, binding, and internal Magic ID were already valid. The card is in the scrollable second row; viewport culling must not be mistaken for an empty translation. Removed the unsafe shared fitting behavior and verified the visible card after a fresh Dutch start, locale switches, and reopening. The original reported empty state was not conclusively isolated beyond that shared rendering defect. Inventory locale callbacks now also refresh the Equip/Equipped presentation in all three controllers. |
| English gameplay missions | MissionUIBinder's late subscription rebuilt its initial text directly from mission.Description in English, bypassing MissionSystem's localized formatter. It now requests MissionSystem.RefreshMissionPresentation(), using the real slot mapping and the same localized formatting as assignment/progress updates. Pending subscriptions are cancelled when disabled. No second locale toggle is needed. |
| Competing gameplay writers | Removed redundant scene/prefab LocalizeStringEvents and the static localization binding on PhaseTimerText, which is a numeric timer intentionally cleared by gameplay. The death heading now has one consistent YOU DIED key, matching its runtime presenter. A dormant Bonus label with a null font/material now uses its sibling's matching Lilita font. |

## Targeted layout and content

- Restored Home navigation and level typography, gameplay HUD, Pause, Shop, Inventory, Rewards, and existing popup baselines. Home No Ads / Free Cash / Spin labels now occupy the available artwork area at readable sizes; Play has more safe label width. Bottom navigation remains inside its artwork.
- Language-menu choices use a consistent readable size. Exactly six native labels remain: English, Español, Nederlands, Français, Português (Brasil), Deutsch.
- Daily mission titles, descriptions, progress, rewards, timers, and claim labels were enlarged coherently. Free Cash reward/action text was also increased.
- Shop cost groups use preferred text width with preserved icon widths so owned states do not wrap inside a numeric-price box. Purchase logic and placeholder USD prices were preserved.
- Widened the death heading and result Continue label to accommodate German and Portuguese. No artwork was stretched.
- Layout-only wording changes: Spanish Shop category **PAQUETE DE MONEDAS → MONEDAS**; French reset timer **RÉINITIALISATION UTC → RÉINIT. UTC**.
- Eight gameplay mission templates now receive numeric Smart String arguments. Singular/plural forms were corrected where needed; localized number formatting remains in the table. Checked all eight templates with 0, 1, 2, and 1,000 in six locales (192 formatting cases).

## Fonts

The existing primary outlined Lilita assets and their matching Extended ASCII atlases retain the authored face, border, depth, and material styling. Bitmap atlas arrays now identify the actual texture used by the styled material. The plain primary uses `SkateRunner_LilitaLatinSDF.asset`, a static 1024×1024 SDF atlas generated at 70-point sampling with padding 5 from the included OFL-licensed Lilita One font. No generic Latin fallback was added. Source/license: https://github.com/google/fonts/tree/main/ofl/lilitaone.

A live eight-style specimen verifies the requested German, French, Portuguese, and Spanish accents, alongside ordinary Latin letters. It is included with the screenshots.

## Live QA and limits

All six real locales were visually reviewed on Home, Settings, language options, Missions, Rewards, all four Shop tabs, all three Inventory tabs (including scrolling to Magic), Free Cash, Daily/Lucky Spin, purchase confirmation, reward reveal, gameplay HUD/missions, Pause, death, and result UI.

The main pass visited English → German → French → Portuguese → Spanish → Dutch → English and repeated that sequence three more times in visible Settings. Repeated English captures showed **zero font-size drift**. Runtime observations reported no unexpected empty labels, excessive shrinking, unwanted single-line wrapping, or generic-font substitutions in the captured states. Native language choices remained unchanged across locales. Chinese selection is rejected. Requesting the pseudo code through the production API resolves to English rather than activating pseudo; pseudo remains available only through the development tooling.

Each production locale was tested with its saved preference on entry to Play Mode and the actual Home → Loading → Gameplay path. Mission text was recorded after the real late subscriber ran, before any test refresh. Return Home retained the selected locale. Spanish → return Home → French → another level was also exercised in one session. Dutch Magic was separately captured immediately after a fresh Dutch start.

Purchase confirmation and reward reveal were opened for presentation without confirming a purchase or granting the displayed reward. Death/result panels were opened through their existing presentation APIs; the test used an unscaled animation mode only on the live QA instance so death could be captured while gameplay was paused. These are presentation and initialization checks, not an end-to-end monetization, full-level gameplay, or device/aspect-ratio certification. Existing world signage, item proper names, branding, and Debug UI were not translated by this pass.

Static audit now checks baseline capture, effective/authored size ratio, floors, suspiciously small text, duplicate writers, unexpected fonts, and bitmap atlas/material mismatch. It no longer interprets a failed TMP measurement as zero-sized success. Static results explicitly require live visual review.

**Remaining artistic work:** localized artwork for the claimed reward stamp, Revive emblem, FASE phase badges, and the Mafia Board preview heading behind its localized Coming Soon overlay. Story Missions and Mafia Board currently retain their existing Coming Soon states. No other tested TMP control was left needing an artistic size adjustment. The existing scroll viewport intentionally clips cards outside its visible region.

## Evidence and final technical state

Review `Documentation/LocalizationQA/README.md` for the six-locale contact sheets, accent specimen, cold Dutch Magic capture, runtime observations, and immediate mission logs. Original screenshots are in `Temp/LocalizationQA/production`.

Verified after the final runtime pass: **Edit Mode**, `SkateRunnerStartScreen`, **English (en)** selected and saved, scene clean, no temporary QA objects, compilation complete (`isCompiling = false`), **0 console errors and 0 warnings**. No pseudo locale is active.

The final static audit inspected **432 TMP components / 186 statically localized TMP objects** and reported **0 missing translations, 0 broken references, 0 Latin glyph warnings, 0 typography-policy warnings, and 0 likely hardcoded-string warnings**. The two listed unbound PhaseTimerText instances are intentionally controlled by the numeric gameplay timer.

The final Home/settings cycle, mission-tab captures, and final English gameplay observations contained **0 unexpected empty labels, 0 suspicious shrink events, 0 unwanted single-line wrapping events, and 0 size-drift events**. The saved evidence includes all six immediate gameplay mission logs. Artwork exceptions remain explicitly listed above.


## Exact production files changed by this pass

The list below identifies this repair pass; the working tree also contains earlier-session changes that were preserved. Generated CSV exports were refreshed; only Common, Shop, and Missions content changed in this pass. Temporary diagnostic helpers and backups remain in ignored `Temp/LocalizationQA`.

- `Assets/Scripts/Localization/LocalizedTMPFitPolicy.cs`
- `Assets/Scripts/Localization/LocalizationVisualQA.cs`
- `Assets/Scripts/Localization/LocalizationVisualQA.cs.meta`
- `Assets/Scripts/Missions/MissionSystem.cs`
- `Assets/Scripts/Missions/UI/MissionUIBinder.cs`
- `Assets/Scripts/UI/Inventory/WeaponPowerInventoryController.cs`
- `Assets/Scripts/UI/Inventory/SwordInventoryController.cs`
- `Assets/Scripts/UI/Inventory/RollerbladeInventoryController.cs`
- `Assets/Editor/Localization/SkateLocalizationEditorTools.cs`
- `Assets/Editor/Localization/SkateLocalizationCatalog.cs`
- `Assets/Editor/Localization/SkateLocalizationGermanCatalog.cs`
- `Assets/Scenes/SkateRunnerStartScreen.unity`
- `Assets/Scenes/SkateRunner.unity`
- `Assets/Prefabs/Characters/UICamera.prefab`
- `Assets/ThirdParty/InGame/Layer Lab/GUI Pro-CasualGame/ResourcesData/Fonts/LilitaOne-Regular Outline 32 SDF.asset`
- `Assets/ThirdParty/InGame/Layer Lab/GUI Pro-CasualGame/ResourcesData/Fonts/LilitaOne-Regular Outline 40 SDF.asset`
- `Assets/ThirdParty/InGame/Layer Lab/GUI Pro-CasualGame/ResourcesData/Fonts/LilitaOne-Regular Outline 50 SDF.asset`
- `Assets/ThirdParty/InGame/Layer Lab/GUI Pro-CasualGame/ResourcesData/Fonts/LilitaOne-Regular Outline 54 SDF.asset`
- `Assets/ThirdParty/InGame/Layer Lab/GUI Pro-CasualGame/ResourcesData/Fonts/LilitaOne-Regular Outline 64 SDF.asset`
- `Assets/ThirdParty/InGame/Layer Lab/GUI Pro-CasualGame/ResourcesData/Fonts/LilitaOne-Regular Outline 72 SDF.asset`
- `Assets/ThirdParty/InGame/Layer Lab/GUI Pro-CasualGame/ResourcesData/Fonts/LilitaOne-Regular Outline 120 SDF.asset`
- `Assets/ThirdParty/InGame/Layer Lab/GUI Pro-CasualGame/ResourcesData/Fonts/LilitaOne-Regular Outline 210 SDF.asset`
- `Assets/ThirdParty/InGame/Layer Lab/GUI Pro-CasualGame/ResourcesData/Fonts/LilitaOne-Regular Outline_Extended ASCII_32 SDF.asset`
- `Assets/ThirdParty/InGame/Layer Lab/GUI Pro-CasualGame/ResourcesData/Fonts/LilitaOne-Regular Outline_Extended ASCII_40 SDF.asset`
- `Assets/ThirdParty/InGame/Layer Lab/GUI Pro-CasualGame/ResourcesData/Fonts/LilitaOne-Regular Outline_Extended ASCII_50 SDF.asset`
- `Assets/ThirdParty/InGame/Layer Lab/GUI Pro-CasualGame/ResourcesData/Fonts/LilitaOne-Regular Outline_Extended ASCII_54 SDF.asset`
- `Assets/ThirdParty/InGame/Layer Lab/GUI Pro-CasualGame/ResourcesData/Fonts/LilitaOne-Regular Outline_Extended ASCII_64 SDF.asset`
- `Assets/ThirdParty/InGame/Layer Lab/GUI Pro-CasualGame/ResourcesData/Fonts/LilitaOne-Regular Outline_Extended ASCII_72 SDF.asset`
- `Assets/ThirdParty/InGame/Layer Lab/GUI Pro-CasualGame/ResourcesData/Fonts/LilitaOne-Regular Outline_Extended ASCII_120 SDF.asset`
- `Assets/ThirdParty/InGame/Layer Lab/GUI Pro-CasualGame/ResourcesData/Fonts/LilitaOne-Regular SDF.asset`
- `Assets/Localization/Fonts/LilitaOne-Regular.ttf`
- `Assets/Localization/Fonts/LilitaOne-Regular.ttf.meta`
- `Assets/Localization/Fonts/OFL.txt`
- `Assets/Localization/Fonts/OFL.txt.meta`
- `Assets/Localization/Fonts/SkateRunner_LilitaLatinSDF.asset`
- `Assets/Localization/Fonts/SkateRunner_LilitaLatinSDF.asset.meta`
- `Assets/Localization/Tables/Missions/Missions_en.asset`
- `Assets/Localization/Tables/Missions/Missions_es.asset`
- `Assets/Localization/Tables/Missions/Missions_nl.asset`
- `Assets/Localization/Tables/Missions/Missions_fr.asset`
- `Assets/Localization/Tables/Missions/Missions_pt-BR.asset`
- `Assets/Localization/Tables/Missions/Missions_de.asset`
- `Assets/Localization/Tables/Shop/Shop_es.asset`
- `Assets/Localization/Tables/Common/Common_fr.asset`
- `Assets/Localization/Exports/Missions.csv`
- `Assets/Localization/Exports/Shop.csv`
- `Assets/Localization/Exports/Common.csv`
- `Assets/Localization/BakedTextAudit.md`
- `Assets/Localization/ProductionLocalizationAudit.md`
- `Assets/Localization/ProductionPolishReport.md`
- `Assets/Localization/ProductionPolishReport.md.meta`

Review artifacts are enumerated individually in `Documentation/LocalizationQA/changed-files.txt`.
