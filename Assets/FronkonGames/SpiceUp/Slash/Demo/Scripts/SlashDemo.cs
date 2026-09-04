using System;
using UnityEngine;
using UnityEngine.Rendering;
using FronkonGames.SpiceUp.Slash;

/// <summary> Spice Up: Slash demo. </summary>
/// <remarks>
/// This code is designed for a simple demo, not for production environments.
/// </remarks>
public class SlashDemo : MonoBehaviour
{
  [Header("This code is only for the demo, not for production environments.")]

  [Space(20.0f), SerializeField]
  private VolumeProfile volumeProfile;

  [SerializeField]
  private SlashController controller;

  [SerializeField]
  private AudioClip slashClip;

  [SerializeField, Range(0.0f, 1.0f)]
  private float slashClipVolume = 1.0f;

  [SerializeField, Range(0.0f, 1.0f)]
  private float slashClipDelay = 0.1f;

  [SerializeField, Range(0.0f, 1.0f)]
  private float slashClipPitchOffset = 0.1f;

  private SlashVolume volume;
  private AudioSource audioSource;

  private GUIStyle styleTitle;
  private GUIStyle styleLabel;
  private GUIStyle styleButton;

  private void Awake()
  {
    styleTitle = styleLabel = styleButton = null;

    if (Slash.IsInRenderFeatures() == false)
    {
      Debug.LogWarning($"Effect '{Constants.Asset.Name}' not found. You must add it as a Render Feature.");
#if UNITY_EDITOR
      if (UnityEditor.EditorUtility.DisplayDialog($"Effect '{Constants.Asset.Name}' not found", $"You must add '{Constants.Asset.Name}' as a Render Feature.", "Quit") == true)
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    volume = volumeProfile != null && volumeProfile.TryGet(out SlashVolume vol) ? vol : null;
    this.enabled = Slash.IsInRenderFeatures() && volume != null;
  }

  private void Start()
  {
    ResetDemo();

    if (slashClip != null)
    {
      audioSource = this.gameObject.AddComponent<AudioSource>();
      audioSource.clip = slashClip;
      audioSource.volume = slashClipVolume;

      controller.duration = slashClip.length * 0.9f;
    }
  }

  private void OnGUI()
  {
    if (volume == null)
      return;

    styleTitle ??= new GUIStyle(GUI.skin.label)
    {
      alignment = TextAnchor.LowerCenter,
      fontSize = 32,
      fontStyle = FontStyle.Bold
    };

    styleLabel ??= new GUIStyle(GUI.skin.label)
    {
      alignment = TextAnchor.UpperLeft,
      fontSize = 24
    };

    styleButton ??= new GUIStyle(GUI.skin.button)
    {
      fontSize = 24
    };

    GUILayout.BeginHorizontal();
    {
      GUILayout.BeginVertical("box", GUILayout.Width(300.0f), GUILayout.Height(Screen.height));
      {
        const float space = 10.0f;

        GUILayout.Space(space);

        GUILayout.Label("SLASH DEMO", styleTitle);

        GUILayout.Space(space);

        if (controller != null)
        {
          GUILayout.Space(space * 2.0f);

          GUI.enabled = !controller.IsPlaying;

          if (GUILayout.Button("SLASH", styleButton) == true)
          {
            volume.angle.value = UnityEngine.Random.Range(0.0f, 360.0f);
            if (audioSource != null)
            {
              audioSource.pitch = UnityEngine.Random.Range(1.0f - slashClipPitchOffset, 1.0f + slashClipPitchOffset);
              controller.duration = slashClip.length * audioSource.pitch;
              audioSource.PlayDelayed(slashClipDelay);
            }
            controller.Play();
          }

          GUI.enabled = true;
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("RESET", styleButton) == true)
        {
          volume.Reset();
          volume.intensity.overrideState = true;
          volume.intensity.value = 1.0f;
        }

        GUILayout.Space(4.0f);

        if (GUILayout.Button("ONLINE DOCUMENTATION", styleButton) == true)
          Application.OpenURL(Constants.Support.Documentation);

        GUILayout.Space(4.0f);

        if (GUILayout.Button("❤️ LEAVE A REVIEW ❤️", styleButton) == true)
          Application.OpenURL(Constants.Support.Store);
      }
      GUILayout.EndVertical();

      GUILayout.FlexibleSpace();
    }
    GUILayout.EndHorizontal();
  }

  private void OnDestroy() => volume?.Reset();

  private void ResetDemo()
  {
    if (volume == null)
      return;

    volume.Reset();
    volume.intensity.overrideState = true;
    volume.intensity.value = 1.0f;
  }

  private bool Toggle(string label, bool value)
  {
    GUILayout.BeginHorizontal();
    {
      GUILayout.Label(label, styleLabel);

      value = GUILayout.Toggle(value, string.Empty);
    }
    GUILayout.EndHorizontal();

    return value;
  }

  private float Slider(string label, float value, float min = 0.0f, float max = 1.0f)
  {
    GUILayout.BeginHorizontal();
    {
      GUILayout.Label(label, styleLabel);

      value = GUILayout.HorizontalSlider(value, min, max);
    }
    GUILayout.EndHorizontal();

    return value;
  }

  private int Slider(string label, int value, int min, int max)
  {
    GUILayout.BeginHorizontal();
    {
      GUILayout.Label(label, styleLabel);

      value = (int)GUILayout.HorizontalSlider(value, min, max);
    }
    GUILayout.EndHorizontal();

    return value;
  }

  private Color Color(string label, Color value, bool alpha = true)
  {
    GUILayout.BeginHorizontal();
    {
      GUILayout.Label(label, styleLabel);

      float originalAlpha = value.a;

      UnityEngine.Color.RGBToHSV(value, out float h, out float s, out float v);
      h = GUILayout.HorizontalSlider(h, 0.0f, 1.0f);
      value = UnityEngine.Color.HSVToRGB(h, s, v);

      if (alpha == false)
        value.a = originalAlpha;
    }
    GUILayout.EndHorizontal();

    return value;
  }

  private Vector3 Vector3(string label, Vector3 value, string x = "X", string y = "Y", string z = "Z", float min = 0.0f, float max = 1.0f)
  {
    GUILayout.Label(label, styleLabel);

    value.x = Slider($"   {x}", value.x, min, max);
    value.y = Slider($"   {y}", value.y, min, max);
    value.z = Slider($"   {z}", value.z, min, max);

    return value;
  }

  private T Enum<T>(string label, T value) where T : Enum
  {
    string[] names = System.Enum.GetNames(typeof(T));
    Array values = System.Enum.GetValues(typeof(T));
    int index = Array.IndexOf(values, value);

    GUILayout.BeginHorizontal();
    {
      GUILayout.Label(label, styleLabel);

      if (GUILayout.Button("<", styleButton) == true)
        index = index > 0 ? index - 1 : values.Length - 1;

      GUILayout.Label(names[index], styleLabel);

      if (GUILayout.Button(">", styleButton) == true)
        index = index < values.Length - 1 ? index + 1 : 0;
    }
    GUILayout.EndHorizontal();

    return (T)(object)index;
  }
}
