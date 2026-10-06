# Owned Powerslam assets

The five Explotion_Cone variants are authored from Piloto Studio's Explotion_Cone
using the mobile base in ../PowerslamConeTrial. Materials are copied per element;
textures and the neutral dark impact are shared. An owned two-sample unlit shader
colors the flame and bright ground layers; the soot sorts behind the plume.
Source vendor assets remain intact. Each cone has a 45-particle capacity.

EarthAOE_Mobile is derived from PixPlays Elemental AOE 1.0.0's
EarthSlamSpikesAoeVFX. Its fractured ground pose is sampled from
Animation Clip_GroundShatter_040 at 0.4 seconds, aligned to the source's road plane
and combined into one owned mesh with a 4.5-unit radius. All three source spike
rings are sampled at 0.4 seconds and reduced to 18 spikes in a second combined
mesh. StoneBurst, Cracks, GroundFlashLines and Flash are capped at 15 particles
in total. Ground is 3,781 triangles; combined geometry is below 18,000 triangles.
The copied dirt/mask textures and burst meshes are under Earth/. The owned URP
shader replaces vendor shaders; no vendor controllers, Timeline or scripts are
needed by the shipped prefab. The original PixPlays import is ignored by Git.

Rebuild in Unity: ELROI > VFX > Rebuild Elemental Powerslams.
Rebuilding Earth needs the original ignored import present. Playing/building the
saved prefabs does not. Rebuilding enables the presentation checkbox on the player.
To rebuild only Earth without touching accepted ability cones or player settings,
use ELROI > VFX > Rebuild Mobile Earth Powerslam.

The real player prefab is Assets/Prefabs/Characters/S_01_Male.prefab.
PowerslamConeFx > Enable Powerslam Effects controls charged air/ground VFX,
distortion and kill flash. Gameplay damage, charge consumption, normal Ground Slam
and camera/haptic feedback remain controlled by the existing gameplay systems.
Element selection reads that player's WeaponPowerEquipper, not a Home preview.

Charged impact placement queries the Ground layer and ignores triggers. Active
effects detach from the player and follow the road's measured translation. If
the road chunk is retired or missing, the captured scroll rate continues using
the current level speed; static surfaces use the road multiplier of 0.5. Recycled
chunks cannot teleport the effect. The same anchoring applies to Earth and cones.

Earth is presentation only: no Colliders, Rigidbodies, Joints, particle collision,
trigger forces or subemitters. The builder strips physics components and the
asset validator rejects them. Enemy damage stays in SwipeDownDetector's existing
overlap/damage pass. Geometry sinks and all cached emitters clear on pool return.

Editor checks do not establish phone thermals. Profile identical sustained gameplay
on Galaxy S10 and S24 Ultra with this checkbox on/off before accepting the budget.
