using System;
using UnityEngine;

public enum RollerbladeId
{
    Default = 0,
    UrbanRush = 1,
    NeonVelocity = 2,
    FrostbiteGlide = 3,
    InfernoDrift = 4,
    CelestialApex = 5
}

[Serializable]
public struct RollerbladeSideTransform
{
    public Vector3 localPosition;
    public Vector3 localEulerAngles;
    public Vector3 localScale;

    public static RollerbladeSideTransform Identity
    {
        get
        {
            return new RollerbladeSideTransform
            {
                localPosition = Vector3.zero,
                localEulerAngles = Vector3.zero,
                localScale = Vector3.one
            };
        }
    }
}

[CreateAssetMenu(menuName = "Elroi/Rollerblades/Rollerblade Definition", fileName = "Rollerblade_")]
public sealed class RollerbladeDefinition : ScriptableObject
{
    [Header("Identity")]
    public RollerbladeId rollerbladeId = RollerbladeId.Default;

    [Header("Left / Right Prefabs")]
    public GameObject leftPrefab;
    public GameObject rightPrefab;

    [Header("Socket Transforms")]
    public RollerbladeSideTransform leftTransform = new RollerbladeSideTransform
    {
        localScale = Vector3.one
    };

    public RollerbladeSideTransform rightTransform = new RollerbladeSideTransform
    {
        localScale = Vector3.one
    };
}
