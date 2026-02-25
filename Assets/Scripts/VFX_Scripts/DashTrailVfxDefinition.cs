using UnityEngine;
using UnityEngine.VFX;

[CreateAssetMenu(menuName = "Elroi/VFX/Dash Trail VFX Definition", fileName = "DT_")]
public class DashTrailVfxDefinition : ScriptableObject
{
    public DashTrailId id = DashTrailId.None;

    [Header("VFX Graph Asset (.vfx)")]
    public VisualEffectAsset vfxAsset;

    [Header("Binding property names (match this graph's Blackboard exposed names)")]
    public string skinnedMeshRendererProperty = "SkinnedMeshRenderer";
    public string canDrawTrailBool = "CanDrawTrail";

    [Header("Optional")]
    public bool setCanDrawTrailOnEquip = false;
}
public enum DashTrailId
{
    None = 0,
    Ice = 1,
    Fire = 2,
    Poison = 3,
    Electricity = 4,
}