# Ability visual pipeline

## Rebuild the animated previews

Use **Tools > Skate Runner > Abilities > Rebuild Animated Previews and Bind Inventory**
in Edit Mode. This updates the existing builder; no external converter is required.

The preview now shows the real upright Katana with its corresponding animated
weapon aura. It turns gently from side to side around the vertical axis (±18°)
rather than doing a full revolution, so the thin blade never becomes edge-on.
Default uses the same motion with no aura. There are no slash effects in these previews.

Other commands in that menu:

- **Bind Existing Animated Previews** rebinds the generated clips without capture.
- **Cancel Capture** stops capture and returns from the isolated Play Mode session.

## Preserved content

The six existing `*PowerCard.png` files and their import settings are not regenerated
or modified. The builder checks their file fingerprints across a rebuild.
Card Images, slot mappings, card hierarchy, layout and gradient fixes are untouched.
No shop, ownership, equip, save or gameplay code is changed.

Inputs remain the six `WP_*.asset` definitions in
`Assets/Prefabs/VFX/WeaponPowerVFX/`, the real Katana prefab, and each definition's
`weaponAuraPrefab` with its existing `Katana_Default` aura tuning.
The baked-in Ice effect on the Katana is disabled on capture clones only.
Authored effects/colors are retained; there is no invented or recolored artwork.

## New output assets

Folder: `Assets/Prefabs/PreviewSprites/AbilitySprites/KatanaPreviews/`

For each prefix `Default`, `Fire`, `Ice`, `Electricity`, `Poison`, `Magic`:

- `<prefix>PowerPreview.png`: transparent 3072 × 2048 sprite sheet.
- `<prefix>PowerPreview.anim`: 24 frames, 6 columns × 4 rows, 12 FPS, two-second loop.
- Each individual frame is 512 × 512, ordered left-to-right then top-to-bottom.

`AbilityKatanaPreview.controller` supplies the default Ability state/template for
the existing shared `WeaponPowerPreviewPlayer`. Only the Ability preview clip
references and shared preview's default controller/template/state/initial sprite
are rebound. Sword and Rollerblade category overrides remain unchanged.

The earlier slash-only sheets, clips and controller remain in the parent folder
as unused legacy assets. They are no longer bound to the Ability inventory.
Existing card PNGs remain in that parent folder as before.

## Capture and checks

An isolated, temporary Play Mode scene is used because the VFX Graphs did not
advance reliably in the original Edit Mode capture tests. The real aura warms
for 1.5 simulated seconds, then is sampled at 60 Hz for the 12 FPS sprite animation.
The camera/framing is fixed across powers and frames. Capture-only lighting,
HDR tone mapping and dual-background compositing preserve transparency and glow.
The sway loops smoothly; the authored stochastic particle simulation restarts
when the sprite clip loops.

The builder restores the prior Play Mode start scene and quality setting and
removes its temporary capture scene. Cancel before editing scripts or changing
Play Mode manually. Script reloads cancel the session safely.

Before saving a dirty Start Screen, a recoverable local copy is placed in
`Library/AbilityPreviewBackups/`. Existing scene edits are preserved.
Rebuilds update assets in place and retain sprite IDs by name.

Isolated smoke tests exercise all six selections, actual Image/Animator playback,
Equip/Equipped labels, Sword/Rollerblade playback and restoration of Ability
playback. Tests use temporary available cards and do not purchase, equip or save.

No manual import, slicing or binding is needed. An on-device visual/gameplay check
is still recommended for mobile compression and the full owned-ability flow.
