using UnityEngine;

public class BarrierVisibilityController : MonoBehaviour
{
    public Material transparentMat;   // "transparent mat"
    public Material opaqueMat;        // opaque copy

    public Renderer[] barrierRenderers;  

    bool isOpaque = false;

    public void ToggleBarrierOpacity()
    {
        Debug.Log("ToggleBarrierOpacity called. isOpaque before = " + isOpaque);

        if (transparentMat == null || opaqueMat == null)
        {
            Debug.LogWarning("BarrierVisibilityController: materials not assigned.");
            return;
        }

        if (barrierRenderers == null || barrierRenderers.Length == 0)
        {
            Debug.LogWarning("BarrierVisibilityController: no barrierRenderers assigned.");
            return;
        }

        isOpaque = !isOpaque;
        var targetMat = isOpaque ? opaqueMat : transparentMat;

        foreach (var r in barrierRenderers)
        {
            if (r == null) continue;
            r.sharedMaterial = targetMat;
        }
    }
}


