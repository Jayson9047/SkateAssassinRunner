using MoreMountains.InfiniteRunnerEngine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ShadowDebug : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Quality Level: " + QualitySettings.names[QualitySettings.GetQualityLevel()]);
        Debug.Log("GraphicsDeviceType: " + SystemInfo.graphicsDeviceType);

        var rp = GraphicsSettings.currentRenderPipeline;
        Debug.Log("RP Asset: " + (rp ? rp.name : "NULL"));
        var skateRunnerGuiManager = SkateRunnerGUIManager.SkateRunnerGUIManagerAccessor;
        if (skateRunnerGuiManager.CashText == null)
            return;

        var urp = rp as UniversalRenderPipelineAsset;
        if (urp != null)
        {
            Debug.Log("URP Shadow Distance: " + urp.shadowDistance);
            Debug.Log("URP Main Light Shadow Supported: " + urp.supportsMainLightShadows);
        }
        skateRunnerGuiManager.CashText.text = "Quality Level: " + QualitySettings.names[QualitySettings.GetQualityLevel()] + "\n" +
                                              "Quality Shadow: " + QualitySettings.shadows.ToString() + "\n" +
                                              "GraphicsDeviceType: " + SystemInfo.graphicsDeviceType + "\n" +
                                                "RenderPipelineAsset: " + (rp ? rp.name : "NULL") + "\n" +
                                                "URP supports main shadows: " + urp.supportsMainLightShadows + "\n" +
                                                "URP shadow distance: " + urp.shadowDistance;
    }
}
