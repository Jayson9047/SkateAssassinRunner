using UnityEngine;

[CreateAssetMenu(menuName = "Elroi/VFX/Character Power Definition", fileName = "CP_")]
public class CharacterPowerDefinition : ScriptableObject
{
    [Header("ID")]
    public string powerId; // "Fire", "Ice", etc. optional but useful
    public CharacterPowerId characterPowerId = CharacterPowerId.None;

    [Header("Character Aura (looping, attached to character)")]
    public GameObject characterAuraPrefab;

    [Header("Dash Trail (looping, attached to dash trail anchor; enabled during dash)")]
    public GameObject dashTrailPrefab;

    [Header("Optional tuning - Aura")]
    public Vector3 auraLocalPositionOffset;
    public Vector3 auraLocalRotationOffset;
    public Vector3 auraLocalScale = Vector3.one;

    [Header("Optional tuning - Dash Trail")]
    public Vector3 dashTrailLocalPositionOffset;
    public Vector3 dashTrailLocalRotationOffset;
    public Vector3 dashTrailLocalScale = Vector3.one;
}

public enum CharacterPowerId
{
    None = 0,
    Ice = 1,
    Fire = 2,
    Electricity = 3,
    Poison = 4,
    Magic = 5,
    // add more later
}