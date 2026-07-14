using UnityEngine;

public class WeaponIdentity : MonoBehaviour
{
    [Tooltip("Unique ID for this weapon prefab (e.g., Katana_01, Sword_Default, Axe_Heavy).")]
    public string weaponId;

    [Tooltip("Serialized Sword-local anchor used for looping Weapon Power aura VFX.")]
    [SerializeField] private Transform auraAnchor;

    public Transform AuraAnchor => auraAnchor;
}
