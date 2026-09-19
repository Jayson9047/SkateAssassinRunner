# Scenario design experiment — 2026-09-19

Built through UnityMCP in SkateAssassinRunner_UnityMCP, Unity 6000.0.67f1. Both are standalone review prefabs, absent from the active ObstacleSpawner and SkateRunner scene dependencies. No runtime scripts were created or edited.

## ScenarioA_DuckThenPop

Prefab: ./Assets/Prefabs/MicroScenarios/ScenarioA_DuckThenPop.prefab

A maintenance pipe rack covers a slide barrier, followed by an open-sky food-cart obstacle. Read the low barrier, slide, recover beyond the rack, then jump/double jump over the cart. Double jumping into the rack contacts its lethal pipe geometry; jumping becomes useful only after the roof ends.

Local X progression: warning at -39; roof -33 to -20; barrier root -28.5 (copied collider offset retained); ground cash -16; cart -6. Roof underside Y=-0.95, or 3.15 m above the prop-ground datum Y=-4.10. The 14 m roof-to-cart spacing provides a recovery and takeoff area. All existing gameplay object Y/Z, rotations, scales, scripts and collider values are copied from the references below; only X and names change.

Reused: Scenario1 slide barrier GameObject (BarrierStick 1/2); Scenario3 Hot_Dog cart; Scenario2 Cash (1); Synty PolygonCity straight pipe and warning sign; PolygonGeneric beam. Roof trigger behavior copies Scenario3's KillsPlayerOnTouch. Scenario1 supplies the root movement, pool, recycling and reset configuration.

Vehicle: no. Required missing model: none. Optional visual improvement: a purpose-built maintenance pipe gantry about 13 m long, 4.5 m wide, with 3.15 m underside clearance and a separate striped sliding gate.

## ScenarioB_PipeRackBreach

Prefab: ./Assets/Prefabs/MicroScenarios/ScenarioB_PipeRackBreach.prefab

Two destructible barrels obstruct a low pipe rack. Stay grounded and dash through the pair; continue beneath the pipes to the exit reward. The roof clears the standing player but provides insufficient room to jump over a barrel. Sliding leaves the barrels intact, so it does not solve the barricade.

Local X progression: warning -40; roof -34 to -10; barrels -25 and -22.5; ground cash -7. Roof underside Y=-1.20, or 2.90 m above the prop-ground datum. The pair occupies about 3.69 m including colliders, inside the current approximately 11.11 m dash reach. Barrel health, debris, layer, collision and reward settings are unchanged.

Reused: Scenario1 barrel_01_destructible twice, including project-owned destruction and cash reward components; Scenario2 Cash (1); the same Synty pipe, beam and warning-sign models. Scenario1 supplies the root configuration, Scenario3 the roof trigger behavior.

Vehicle: no. Required missing model: none. Optional visual improvement: a dedicated 24 m industrial pipe rack with about 4.5 m width and 2.90 m underside clearance.

## Verification and limits

- 59 Editor structural/geometric checks passed. Root components match Scenario1; slide-barrier collider and barrel components match their references; no missing scripts, materials or component references.
- A: 1,338 mesh triangles, 15 renderers, 7 colliders. B: 2,970 triangles, 14 renderers, 7 colliders. These are asset counts, not device performance measurements.
- Sampled axis-aligned collider overlap against the actual standing box at world speeds 7.5, 10, 15 and 20. Grounded paths clear the roofs; a representative blind double jump at each roof entrance overlaps a lethal pipe.
- A representative timed double jump after A's roof clears the cart at all four sampled speeds. Calculation uses current gravity -50, JumpForce 800, mass 1, fixed timestep 0.02, and second jump 0.14 seconds after the first. Takeoff is 0.55 seconds before the cart center reaches the player. These are geometric trajectory samples, not a dynamic playthrough or an exhaustive input search.
- Gate traversal fits the existing 0.8 second slide even before its speed boost. Existing SlideAware immunity is retained; the player collider does not need to shrink.
- No elevated running surfaces were added. Roofs are lethal triggers, not platforms; the player stays at the normal street datum. No death-bound edits were needed.
- Visually reviewed both prefab layouts in Unity Scene View. Warning boards retain native proportions, with separate support posts.
- Live dash destruction, trigger timing, camera warning duration, pool reuse and mobile feel still require a playtest before activation. Deliberately early or carefully timed aerial approaches may find alternatives; the intent is to defeat blind jumping, not remove player agency.
- Console inspection contained three Unity Editor stale-inspector SerializedObjectNotCreatableException entries (Transform/Renderer/Collider inspectors). No compiler errors were reported. Final saved assets passed reference validation.

## Files and untouched scope

Created the two prefab files above and their .meta files, plus this report. Review captures are temporary files under Temp/ScenarioReview.

No existing project asset was edited by this experiment. Scenario2's initial and final SHA-256 is 7101297AF28ABBC3EC8CF9A051BBD96F97384911CF8082893FA9048ACC59492A.

Background buildings, player and abilities, spawner architecture and active pool, UI, audio, monetization, tutorials, Phase 2, progression, existing scenarios and gameplay scene were left untouched. No third scenario was created.

Per the standing ELROI source-archive instruction, the existing IndieKit README and metadata were copied unchanged to the canonical vault's Vendor_Docs/Crates and Barrels/snapshots/1.1.0-2026-09-19, with SOURCE.md and generated source indexes updated and validated. No bundled manuals were found in the relevant Synty PolygonCity/PolygonGeneric or Urban City model folders. No verified workflow/implementation knowledge was promoted.

