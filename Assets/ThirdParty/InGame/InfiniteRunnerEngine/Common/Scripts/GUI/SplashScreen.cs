using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

namespace MoreMountains.InfiniteRunnerEngine
{
	/// <summary>
	/// Simple start screen class.
	/// </summary>
	public class SplashScreen : MonoBehaviour
	{
		/// the name of the first level to load
		[Tooltip("the name of the first level to load")]
		public string FirstLevel;
		/// the duration in seconds after which to load the first level
		[Tooltip("the duration in seconds after which to load the first level")]
		public float AutoSkipDelay = 2f;

		protected float _delayAfterClick = 1f;

		/// <summary>
		/// Initialization
		/// </summary>
		protected virtual void Start()
		{
			GUIManager.Instance.FaderOn(false, 1f);

			if (AutoSkipDelay > 1f)
			{
				_delayAfterClick = AutoSkipDelay;
				StartCoroutine(LoadFirstLevel());
			}
		}

		protected virtual IEnumerator LoadFirstLevel()
		{
			yield return new WaitForSeconds(_delayAfterClick);
			GUIManager.Instance.FaderOn(true, 1f);
			yield return new WaitForSeconds(1f);
			SceneManager.LoadScene(FirstLevel);
		}
	}
}