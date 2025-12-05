using UnityEngine;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class URPShadowPresetSwitcher : MonoBehaviour
{
    [Header("Assign URP assets you prepared in Project Settings")]
    public UniversalRenderPipelineAsset performance; // small atlas, fewer shadows
    public UniversalRenderPipelineAsset balanced;    // good default
    public UniversalRenderPipelineAsset cinematic;   // big atlas, highest quality

    [Header("Optional: quick hotkeys")]
    public bool enableHotkeys = true;
    public KeyCode performanceKey = KeyCode.F1;
    public KeyCode balancedKey    = KeyCode.F2;
    public KeyCode cinematicKey   = KeyCode.F3;

    void Start()
    {
        // Default to balanced if present
        if (balanced != null) Apply(balanced);
    }

    void Update()
    {
        if (!enableHotkeys) return;
        if (performance != null && Input.GetKeyDown(performanceKey)) Apply(performance);
        if (balanced    != null && Input.GetKeyDown(balancedKey))    Apply(balanced);
        if (cinematic   != null && Input.GetKeyDown(cinematicKey))   Apply(cinematic);
    }

    public void ApplyPerformance() { if (performance != null) Apply(performance); }
    public void ApplyBalanced()    { if (balanced    != null) Apply(balanced);    }
    public void ApplyCinematic()   { if (cinematic   != null) Apply(cinematic);   }

    public void Apply(UniversalRenderPipelineAsset asset)
    {
        if (asset == null) return;

        // Apply at runtime
        GraphicsSettings.defaultRenderPipeline = asset;
        QualitySettings.renderPipeline       = asset;

        // If you use quality levels with different URP assets, also update the active level’s asset:
        var currentLevel = QualitySettings.GetQualityLevel();
        QualitySettings.SetQualityLevel(currentLevel, applyExpensiveChanges: true);

        Debug.Log($"[URPShadowPresetSwitcher] Applied: {asset.name}");
    }
}
