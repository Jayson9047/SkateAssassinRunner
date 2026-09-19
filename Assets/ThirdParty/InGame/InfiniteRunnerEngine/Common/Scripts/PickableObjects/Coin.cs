using UnityEngine;
using System.Collections;

namespace MoreMountains.InfiniteRunnerEngine
{
	/// <summary>
	/// Add this class to an object and it'll add points when collected.
	/// Note that you'll need a trigger boxcollider on it
	/// </summary>
	public class Coin : PickableObject
	{
		/// The amount of points to add when collected
		[Tooltip("The amount of points to add when collected")]
		public int CashToAdd = 10;

        private BoxCollider _pickupBox;
        private bool _collected;
        private readonly System.Collections.Generic.Dictionary<BoxCollider, Vector3> _previousCenters =
            new System.Collections.Generic.Dictionary<BoxCollider, Vector3>();

        protected virtual void Awake()
        {
            _pickupBox = GetComponent<BoxCollider>();
        }

        protected virtual void OnEnable()
        {
            _collected = false;
            _previousCenters.Clear();
        }

        protected virtual void OnDisable()
        {
            _previousCenters.Clear();
        }

        // Transform-driven dashes can cross an entire pickup between physics ticks.
        // Use the same relative box sweep as lethal obstacles, including world motion.
        protected virtual void LateUpdate()
        {
            if (_collected || _pickupBox == null || !_pickupBox.enabled || !_pickupBox.isTrigger)
            {
                _previousCenters.Clear();
                return;
            }
            var manager = LevelManager.Instance;
            if (manager == null || manager.CurrentPlayableCharacters == null) return;
            foreach (var character in manager.CurrentPlayableCharacters)
            {
                if (character == null) continue;
                var body = character.GetComponent<BoxCollider>();
                if (body == null) continue;
                if (!character.isActiveAndEnabled || !body.enabled ||
                    Physics.GetIgnoreLayerCollision(gameObject.layer, body.gameObject.layer) ||
                    Physics.GetIgnoreCollision(_pickupBox, body))
                {
                    _previousCenters.Remove(body);
                    continue;
                }
                var relative = _pickupBox.transform.worldToLocalMatrix * body.transform.localToWorldMatrix;
                Vector3 center = relative.MultiplyPoint3x4(body.center) - _pickupBox.center;
                Vector3 previous;
                if (!_previousCenters.TryGetValue(body, out previous)) previous = center;
                _previousCenters[body] = center;
                Vector3 half = body.size * 0.5f;
                if (SweptBoxIntersection.Intersects(previous, center, _pickupBox.size * 0.5f,
                    relative.MultiplyVector(Vector3.right * half.x),
                    relative.MultiplyVector(Vector3.up * half.y),
                    relative.MultiplyVector(Vector3.forward * half.z)))
                {
                    TriggerEnter(body.gameObject);
                    if (_collected) return;
                }
            }
        }

        protected override void TriggerEnter(GameObject collidingObject)
        {
            // Native triggers and the sweep share one claim. Pool reactivation resets it.
            if (_collected || !isActiveAndEnabled || collidingObject == null ||
                !collidingObject.activeInHierarchy || collidingObject.GetComponent<PlayableCharacter>() == null)
                return;
            _collected = true;
            base.TriggerEnter(collidingObject);
        }

		protected override void ObjectPicked()
		{
			SkateRunnerAudioManager.PlayCashPickup();
			// We pass the specified amount of points to the game manager
			SkateRunnerGameManager.SkateRunnerGameManagerAccessor.AddCash(CashToAdd);
		}
	}
}
