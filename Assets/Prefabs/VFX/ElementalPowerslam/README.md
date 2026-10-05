# Owned Powerslam assets

The five Explotion_Cone variants are authored from Piloto Studio's Explotion_Cone
using the mobile base in ../PowerslamConeTrial. Materials are copied per element;
textures and the neutral dark impact are shared. An owned two-sample unlit shader
colors the flame and bright ground layers; the soot sorts behind the plume.
Source vendor assets remain intact. Each cone has a 45-particle capacity.

EarthAOE_Mobile is derived from PixPlays Elemental AOE 1.0.0's
EarthSlamSpikesAoeVFX. Its fractured ground pose is sampled from
Animation Clip_GroundShatter_040 at 0.4 seconds, normalized and combined into one
owned mesh. StoneBurst, Stones, GroundFlashLines and Flash are shortened and capped
at 26 particles in total. The combined ground is 3,781 triangles.
The copied dirt/mask textures and burst meshes are under Earth/. The owned URP
shader replaces vendor shaders; no vendor controllers, Timeline or scripts are
needed by the shipped prefab. The original PixPlays import is ignored by Git.

Rebuild in Unity: ELROI > VFX > Rebuild Elemental Powerslams.
Rebuilding Earth needs the original ignored import present. Playing/building the
saved prefabs does not. Rebuilding enables the presentation checkbox on the player.

The real player prefab is Assets/Prefabs/Characters/S_01_Male.prefab.
PowerslamConeFx > Enable Powerslam Effects controls charged air/ground VFX,
distortion and kill flash. Gameplay damage, charge consumption, normal Ground Slam
and camera/haptic feedback remain controlled by the existing gameplay systems.
Element selection reads that player's WeaponPowerEquipper, not a Home preview.

Editor checks do not establish phone thermals. Profile identical sustained gameplay
on Galaxy S10 and S24 Ultra with this checkbox on/off before accepting the budget.
