# Production Baked-Text Audit

Scope is limited to enabled production scenes and their authored UI dependencies. Third-party demos and Debug Tools are excluded.

| Asset / production location | Visible text | Classification | Resolution |
|---|---|---|---|
| Start-screen title/logo artwork in `Assets/Scenes/SkateRunnerStartScreen.unity` | Skate Assassin Runner | A — intentional branding | Remains English by product-brand policy. |
| `Assets/Scenes/ElroiBootSplash.unity` logo artwork | ELROI / ELROI Creative Studios | A — intentional branding | Remains English by studio-brand policy. |
| `Assets/Scenes/SkateRunnerLoadingScreen.unity` | `0%` loading progress | C — locale-neutral runtime value | The live scene contains no authored `LOADING...` label; the sole TMP field is a numeric percentage and does not require a string-table binding. |
| Production button labels converted in Start Screen/Home/Shop/Inventory/Missions/Rewards/Settings | Ordinary navigation and action labels | B — ordinary UI | TMP is retained; `LocalizeStringEvent` is attached where the label is static. |

## Unresolved localized-art variants

The live Game-view review identified these ordinary-English sprites. The earlier static review missed them. They remain artwork localization work; passing string-table or font checks does not resolve them.

| Production location | Visible art text | Source | Remaining work |
|---|---|---|---|
| Rewards, already-claimed cards | DAILY / CLEAR / REWARD | LayerLabs `ResourcesData/Sprites/Components/IconMisc/Icon_ImageIcon_ClearStamp_l.png` and `_s.png` | Supply matching localized stamps or approve a language-neutral claimed emblem. Day labels and reward descriptions are localized TMP. |
| Gameplay death/revive action | REVIVE | `Assets/Prefabs/Images/ReviveLogo (1).png` | Supply matching localized emblem variants. The death heading and decline action are localized TMP. |
| Gameplay phase badge | PHASE 1 / PHASE 2 | `Assets/Prefabs/UI/Phase1.png`, `Phase2.png` | Supply FASE artwork for Spanish, Dutch, and Portuguese or convert the lettering to styled TMP. English, French, and German use PHASE. |
| Mafia Board preview behind the Coming Soon overlay | MAFIA BOARD | `Assets/Prefabs/UI/MafiaBoard.png` | Localize the preview heading before this feature launches. The visible Coming Soon overlay and navigation labels are already localized TMP. |

These sprites were preserved during the typography repair to retain their custom artwork. See `ProductionPolishReport.md` for the current pass, evidence, and limits of the visual sign-off.

## Review caveat

This report is an explicit visual/asset audit, not OCR over every texture in third-party packages. Re-run **Tools → Skate Runner → Localization → Report Baked Text** and the production scene review whenever UI art is replaced.
