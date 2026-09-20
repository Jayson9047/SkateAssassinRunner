using UnityEngine;

namespace IndieKit
{
    /// <summary>Two authored meshes, moved cosmetically around their own centers. No debris physics.</summary>
    public sealed class Enemy1SlicePresentation : MonoBehaviour
    {
        [Header("Existing halves")]
        public Transform upper;
        public Transform lower;
        public ParticleSystem blood;
        public Vector3 seam = new Vector3(0.035f, 1.25f, 0.03f);
        [Header("World units, relative to scrolling road")]
        public Vector3 upperVelocity = new Vector3(7.8f, 1.35f, 0.18f);
        public Vector3 lowerVelocity = new Vector3(2.2f, -0.35f, -0.12f);
        public Vector3 opening = new Vector3(0.085f, 0.045f, 0f);
        public float upperSpin = -430f;
        public float lowerSpin = 265f;
        public float gravity = 18f;
        public float airDrag = 1.4f;
        public float settleDegreesPerSecond = 420f;
        public float lifetime = 1.15f;

        struct Piece
        {
            public Transform transform;
            public Vector3 homePosition, homeScale, meshCenter, meshExtents, velocity;
            public Quaternion homeRotation, restRotation;
            public float spin;
            public bool grounded;
        }

        Piece _upper, _lower;
        float _age, _floor;
        bool _hasFloor, _cached;
        public float Age => _age;
        public Vector3 UpperCenter => _upper.transform.TransformPoint(_upper.meshCenter);
        public Vector3 LowerCenter => _lower.transform.TransformPoint(_lower.meshCenter);

        public void Cache()
        {
            if (_cached) return;
            _upper = MakePiece(upper);
            _lower = MakePiece(lower);
            _cached = true;
        }

        static Piece MakePiece(Transform part)
        {
            Bounds bounds = part.GetComponent<MeshFilter>().sharedMesh.bounds;
            return new Piece { transform = part, homePosition = part.localPosition,
                homeRotation = part.localRotation, homeScale = part.localScale,
                meshCenter = bounds.center, meshExtents = bounds.extents };
        }

        public void Begin(Vector3 position, Quaternion rotation, Vector3 scale, KillCause cause,
            bool playBlood, bool hasFloor, float floor)
        {
            Cache();
            ResetPiece(ref _upper);
            ResetPiece(ref _lower);
            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = scale;
            _age = 0f;
            _hasFloor = hasFloor;
            _floor = floor;
            bool downward = cause == KillCause.DownAttack || cause == KillCause.PowerSlam;
            Vector3 gap = opening;
            _upper.velocity = upperVelocity;
            _lower.velocity = lowerVelocity;
            if (downward)
            {
                // A downward strike spreads the halves to different sides without an upward explosion.
                _upper.velocity = new Vector3(5.5f, -1.5f, upperVelocity.z);
                _lower.velocity = new Vector3(-2.8f, -2f, lowerVelocity.z);
            }
            _upper.spin = upperSpin;
            _lower.spin = lowerSpin;
            _upper.restRotation = Quaternion.AngleAxis(-90f, Vector3.forward) * upper.rotation;
            _lower.restRotation = Quaternion.AngleAxis(90f, Vector3.forward) * lower.rotation;
            upper.position += gap;
            lower.position -= gap;
            blood.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            blood.transform.localPosition = seam;
            blood.transform.rotation = Quaternion.LookRotation(downward ? new Vector3(0.8f, -0.6f, 0f) : Vector3.right);
            gameObject.SetActive(true);
            if (playBlood) blood.Play(true);
        }

        static void ResetPiece(ref Piece p)
        {
            p.transform.localPosition = p.homePosition;
            p.transform.localRotation = p.homeRotation;
            p.transform.localScale = p.homeScale;
            p.velocity = Vector3.zero;
            p.spin = 0f;
            p.grounded = false;
        }

        public bool Tick(float dt, float scroll)
        {
            _age += dt;
            transform.position += Vector3.left * scroll;
            MovePiece(ref _upper, dt);
            MovePiece(ref _lower, dt);
            if (_age < lifetime) return true;
            Release();
            return false;
        }

        void MovePiece(ref Piece p, float dt)
        {
            Vector3 center = p.transform.TransformPoint(p.meshCenter);
            float drag = p.grounded ? 12f : airDrag;
            float decay = Mathf.Exp(-drag * dt);
            float travel = (1f - decay) / drag;
            center.x += p.velocity.x * travel;
            center.z += p.velocity.z * travel;
            p.velocity.x *= decay;
            p.velocity.z *= decay;
            if (!p.grounded)
            {
                center.y += p.velocity.y * dt - 0.5f * gravity * dt * dt;
                p.velocity.y -= gravity * dt;
                p.transform.rotation = Quaternion.AngleAxis(p.spin * dt, Vector3.forward) * p.transform.rotation;
            }
            else
            {
                // Continue falling onto a side after first floor contact instead of freezing upright.
                p.transform.rotation = Quaternion.RotateTowards(p.transform.rotation, p.restRotation, settleDegreesPerSecond * dt);
            }
            // Rotated mesh bounds keep the cosmetic pieces above the sampled floor, without colliders.
            Vector3 ex = p.transform.TransformVector(new Vector3(p.meshExtents.x, 0, 0));
            Vector3 ey = p.transform.TransformVector(new Vector3(0, p.meshExtents.y, 0));
            Vector3 ez = p.transform.TransformVector(new Vector3(0, 0, p.meshExtents.z));
            float extentY = Mathf.Abs(ex.y) + Mathf.Abs(ey.y) + Mathf.Abs(ez.y);
            if (_hasFloor && (p.grounded || center.y - extentY <= _floor))
            {
                center.y = _floor + extentY + 0.008f;
                p.velocity.y = 0f;
                p.grounded = true;
            }
            p.transform.position = center - p.transform.TransformVector(p.meshCenter);
        }

        public void Release()
        {
            blood.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            gameObject.SetActive(false);
        }
    }
}
