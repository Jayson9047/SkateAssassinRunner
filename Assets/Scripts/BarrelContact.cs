using IndieKit;
using MoreMountains.InfiniteRunnerEngine;
using UnityEngine;

/// <summary>Barrel-only relative-motion detection. Does not change physical collision response.</summary>
[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
[RequireComponent(typeof(CapsuleCollider), typeof(SkateRunnerDestructibleObject))]
public sealed class BarrelContact : MonoBehaviour
{
    private static readonly System.Collections.Generic.List<BarrelContact> Active =
        new System.Collections.Generic.List<BarrelContact>();
    private CapsuleCollider barrel;
    private SkateRunnerDestructibleObject damageable;
    private BoxCollider body;
    private Jumper player;
    private SwipeRightAttackDetector attack;
    private PlayerBarrelStumble stumble;
    private SweptBoxIntersection.BoxPose previousBarrel, previousBody;
    private bool hasSample, contactConsumed;

    private void Awake()
    {
        barrel = GetComponent<CapsuleCollider>();
        damageable = GetComponent<SkateRunnerDestructibleObject>();
    }

    private void OnEnable() { hasSample = false; contactConsumed = false; Active.Add(this); }
    private void OnDisable() { hasSample = false; Active.Remove(this); }

    // The moving dash also samples the forward slash. This preserves reach when a
    // dash crosses out and back within one frame, without adding physics queries.
    public static void CheckDashSegment(SwipeRightAttackDetector attacker, Vector3 from, Vector3 to)
    {
        for (int i = Active.Count - 1; i >= 0; i--)
        {
            var target = Active[i];
            if (target == null || !target.barrel.enabled) continue;
            var pose = target.CaptureBarrel();
            if (attacker.SlashReachesBarrel(pose, pose, from, to))
                attacker.TryDamageSweptBarrel(target.damageable, target.barrel);
        }
    }

    private void LateUpdate()
    {
        var manager = LevelManager.Instance;
        if (player == null || !player.isActiveAndEnabled)
        {
            hasSample = false;
            if (manager == null || manager.CurrentPlayableCharacters == null || manager.CurrentPlayableCharacters.Count == 0) return;
            player = manager.CurrentPlayableCharacters[0] as Jumper;
            if (player == null) return;
            body = player.GetComponent<BoxCollider>();
            attack = player.GetComponent<SwipeRightAttackDetector>();
            stumble = player.GetComponent<PlayerBarrelStumble>();
        }
        if (body == null || !body.enabled || !barrel.enabled || contactConsumed ||
            GameManager.Instance == null || GameManager.Instance.Status != GameManager.GameStatus.GameInProgress ||
            Time.deltaTime <= 0f)
        { hasSample = false; return; }

        var currentBarrel = CaptureBarrel();
        var currentBody = SweptBoxIntersection.BoxPose.Capture(body);
        var fromBarrel = hasSample ? previousBarrel : currentBarrel;
        var fromBody = hasSample ? previousBody : currentBody;
        previousBarrel = currentBarrel;
        previousBody = currentBody;
        hasSample = true;

        // Same sampled-world-pose sweep as cash/barriers, including scrolling barrels.
        // A capsule's enclosing box is a conservative gameplay contact envelope only;
        // the existing capsule, Rigidbody, layers and collision matrix stay untouched.
        // LateUpdate runs after the dash's final sweep, including zero-duration dashes.
        // Successful destruction always wins over the non-destructive trip penalty.
        if (attack != null && attack.SlashReachesBarrel(fromBarrel, currentBarrel, fromBody.Position, currentBody.Position) &&
            attack.TryDamageSweptBarrel(damageable, barrel)) return;
        if (!SweptBoxIntersection.IntersectsMovingBoxes(fromBarrel, currentBarrel, fromBody, currentBody)) return;
        if (attack != null && attack.TryDamageSweptBarrel(damageable, barrel)) return;
        contactConsumed = true; // one penalty per intact barrel / pool activation
        if (stumble != null) stumble.TryStumble();
    }

    private SweptBoxIntersection.BoxPose CaptureBarrel()
    {
        Vector3 scale = barrel.transform.lossyScale;
        Vector3 half = Vector3.one * barrel.radius;
        half[barrel.direction] = Mathf.Max(barrel.radius, barrel.height * .5f);
        half = Vector3.Scale(half, scale);
        return new SweptBoxIntersection.BoxPose {
            Position = barrel.transform.position, Rotation = barrel.transform.rotation,
            CenterOffset = Vector3.Scale(barrel.center, scale),
            HalfSize = new Vector3(Mathf.Abs(half.x), Mathf.Abs(half.y), Mathf.Abs(half.z)) };
    }
}
