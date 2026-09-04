////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Martin Bustos @FronkonGames <fronkongames@gmail.com>. All rights reserved.
//
// THIS FILE CAN NOT BE HOSTED IN PUBLIC REPOSITORIES.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace FronkonGames.SpiceUp.Slash
{
  ///------------------------------------------------------------------------------------------------------------------
  /// <summary> Slash controller. </summary>
  /// <remarks> Manages slash animations via coroutines. </remarks>
  ///------------------------------------------------------------------------------------------------------------------
  public class SlashController : MonoBehaviour
  {
    /// <summary> Is the slash currently playing? </summary>
    /// <returns> True / false </returns>
    public bool IsPlaying => slashCoroutine != null;

    [Tooltip("Duration of the slash effect in seconds.")]
    public float duration = 1.0f;

    [Tooltip("The volume profile to use for the effect. Must contain a SlashVolume component.")]
    public VolumeProfile volumeProfile;

    [Tooltip("Event called when the slash is started.")]
    public UnityEvent onStart;

    [Tooltip("Event called when the slash is updated.")]
    public UnityEvent<float> onProgress;

    [Tooltip("Event called when the slash is stopped.")]
    public UnityEvent onStop;

    private SlashVolume volume;

    private Coroutine slashCoroutine;

    private void Awake()
    {
      volume = volumeProfile != null && volumeProfile.TryGet(out SlashVolume vol) ? vol : null;
      if (volume == null)
      {
        Log.Warning($"SlashVolume component not found in volume profile '{volumeProfile.name}'. Using the active stack volume.");
        volume = VolumeManager.instance.stack.GetComponent<SlashVolume>();
      }

      if (volume == null)
      {
        Log.Warning($"SlashVolume component not found in volume profile '{volumeProfile.name}' and the active stack volume is also null. The effect will not work.");
        enabled = false;
      }
    }

    /// <summary> Play a slash. Cancels any current slash. </summary>
    public void Play()
    {
      Stop();

      if (volume != null)
        slashCoroutine = StartCoroutine(PlayCoroutine());
    }

    /// <summary> Cancel the current slash. </summary>
    public void Stop()
    {
      if (slashCoroutine != null)
      {
        StopCoroutine(slashCoroutine);
        onStop?.Invoke();
        slashCoroutine = null;
      }

      if (volume != null)
        volume.progress.value = 0.0f;
    }

    private IEnumerator PlayCoroutine()
    {
      Stop();

      onStart?.Invoke();
      volume.intensity.value = 1.0f;

      float elapsed = 0.0f;
      while (elapsed < duration)
      {
        float t = elapsed / duration;

        volume.progress.value = t;

        onProgress?.Invoke(t);

        elapsed += Time.deltaTime;

        yield return null;
      }

      volume.progress.value = 1.0f;
      volume.intensity.value = 0.0f;
      onProgress?.Invoke(1.0f);
      onStop?.Invoke();
      slashCoroutine = null;
    }

    private void OnDisable() => Stop();
  }
}
