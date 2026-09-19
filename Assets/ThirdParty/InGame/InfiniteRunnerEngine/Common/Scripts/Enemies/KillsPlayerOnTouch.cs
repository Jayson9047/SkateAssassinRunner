using UnityEngine;
using System.Collections;
using MoreMountains.Tools;

namespace MoreMountains.InfiniteRunnerEngine
{
	/// <summary>
	/// Add this class to a trigger boxCollider and it'll kill all playable characters that collide with it.
	/// </summary>
	public class KillsPlayerOnTouch : MonoBehaviour
	{
        // Project integration: Transform-driven dashes and moving obstacles can
        // cross a thin trigger entirely between physics samples. Check their
        // relative motion as well as retaining Unity's normal trigger callbacks.
        private BoxCollider[] _sweptBoxes;
        private readonly System.Collections.Generic.List<SweepSample> _sweepSamples =
            new System.Collections.Generic.List<SweepSample>();

        private sealed class SweepSample
        {
            public BoxCollider Hazard;
            public BoxCollider Body;
            public Vector3 PreviousCenter;
            public bool Valid;
        }

        protected virtual void Awake()
        {
            _sweptBoxes = GetComponents<BoxCollider>();
        }

        protected virtual void OnEnable()
        {
            _sweepSamples.Clear();
        }

        protected virtual void OnDisable()
        {
            _sweepSamples.Clear();
        }

        protected virtual void LateUpdate()
        {
            var manager = LevelManager.Instance;
            if (manager == null || manager.CurrentPlayableCharacters == null || _sweptBoxes == null)
                return;
            var players = manager.CurrentPlayableCharacters;
            for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
            {
                var character = players[playerIndex];
                if (character == null || !character.isActiveAndEnabled)
                    continue;
                var body = character.GetComponent<BoxCollider>();
                if (body == null || !body.enabled)
                    continue;
                foreach (var hazard in _sweptBoxes)
                {
                    if (hazard == null) continue;
                    SweepSample sample = null;
                    foreach (var candidate in _sweepSamples)
                        if (candidate.Hazard == hazard && candidate.Body == body) { sample = candidate; break; }
                    if (sample == null)
                    {
                        sample = new SweepSample { Hazard = hazard, Body = body };
                        _sweepSamples.Add(sample);
                    }
                    if (!hazard.enabled || !hazard.isTrigger ||
                        Physics.GetIgnoreLayerCollision(hazard.gameObject.layer, body.gameObject.layer) ||
                        Physics.GetIgnoreCollision(hazard, body))
                    {
                        sample.Valid = false;
                        continue;
                    }
                    var relative = hazard.transform.worldToLocalMatrix * body.transform.localToWorldMatrix;
                    Vector3 center = relative.MultiplyPoint3x4(body.center) - hazard.center;
                    Vector3 half = body.size * 0.5f;
                    Vector3 bx = relative.MultiplyVector(Vector3.right * half.x);
                    Vector3 by = relative.MultiplyVector(Vector3.up * half.y);
                    Vector3 bz = relative.MultiplyVector(Vector3.forward * half.z);
                    Vector3 from = sample.Valid ? sample.PreviousCenter : center;
                    sample.PreviousCenter = center;
                    sample.Valid = true;
                    if (SweptBoxIntersection.Intersects(from, center, hazard.size * 0.5f, bx, by, bz))
                    {
                        // Virtual dispatch preserves slide/down-attack exceptions
                        // and the existing invincibility rules.
                        TriggerEnter(body.gameObject);
                        if (character == null || !character.isActiveAndEnabled) break;
                    }
                }
            }
        }

		/// <summary>
		/// Handles the collision if we're in 2D mode
		/// </summary>
		/// <param name="other">the Collider2D that collides with our object</param>
		protected virtual void OnTriggerEnter2D(Collider2D other)
		{
			TriggerEnter(other.gameObject);
		}

		/// <summary>
		/// Handles the collision if we're in 3D mode
		/// </summary>
		/// <param name="other">the Collider that collides with our object</param>
		protected virtual void OnTriggerEnter(Collider other)
		{
			TriggerEnter(other.gameObject);
		}

		/// <summary>
		/// Triggered when we collide with either a 2D or 3D collider
		/// </summary>
		/// <param name="collidingObject">Colliding object.</param>
		protected virtual void TriggerEnter(GameObject collidingObject)
		{
			// we verify that the colliding object is a PlayableCharacter with the Player tag. If not, we do nothing.			
			if (collidingObject == null || !collidingObject.activeInHierarchy || collidingObject.tag != "Player")
			{
				return;
			}

			PlayableCharacter player = collidingObject.GetComponent<PlayableCharacter>();
			if (player == null)
			{
				return;
			}

			if (player.Invincible)
			{
				return;
			}

			// we ask the LevelManager to kill the character
			LevelManager.Instance.KillCharacter(player);
		}
	}
}
