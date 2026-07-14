using UnityEngine;

public enum SwordId
{
    Katana = 0,
    Bloodreaver = 1,
    Emberguard = 2,
    GlacierCipher = 3,
    Gravebreaker = 4,
    HellForge = 5,
    Sunspire = 6,
    Wyrmshade = 7
}

[CreateAssetMenu(menuName = "Elroi/Weapons/Sword Definition", fileName = "Sword_")]
public sealed class SwordDefinition : ScriptableObject
{
    [Header("Identity")]
    public SwordId swordId = SwordId.Katana;
    public GameObject swordPrefab;
    public string weaponId = "Katana_Default";

    [Header("Socket Transform")]
    public Vector3 localPosition;
    public Vector3 localEulerAngles;
    public Vector3 localScale = Vector3.one;
}
