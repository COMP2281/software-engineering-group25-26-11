using UnityEngine;

/// <summary>
/// Simple blur post-process to make particles blend into a fluid appearance.
/// Attach this to your Main Camera.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class FluidBlurEffect : MonoBehaviour
{
    [Header("Blur Settings")]
    [Range(0, 10)]
    public int blurIterations = 3;
    
    [Range(0.5f, 5f)]
    public float blurSpread = 1.5f;
    
    [Range(0f, 1f)]
    public float blurStrength = 0.8f;

    private Material blurMaterial;

    void OnEnable()
    {
        CreateMaterial();
    }

    void CreateMaterial()
    {
        if (blurMaterial == null)
        {
            Shader shader = Shader.Find("Hidden/FluidBlur");
            
            if (shader != null && shader.isSupported)
            {
                blurMaterial = new Material(shader);
                blurMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
            else
            {
                Debug.LogError("FluidBlurEffect: Could not find shader 'Hidden/FluidBlur'. Make sure FluidBlur.shader exists.");
            }
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (blurMaterial == null || blurIterations == 0 || blurStrength == 0)
        {
            Graphics.Blit(source, destination);
            return;
        }

        // Create temporary render textures
        RenderTexture temp1 = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
        RenderTexture temp2 = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);

        Graphics.Blit(source, temp1);

        // Apply blur iterations
        for (int i = 0; i < blurIterations; i++)
        {
            float iterSpread = blurSpread * (i + 1) * 0.5f;
            blurMaterial.SetFloat("_BlurSize", iterSpread);
            
            // Horizontal blur
            Graphics.Blit(temp1, temp2, blurMaterial, 0);
            // Vertical blur
            Graphics.Blit(temp2, temp1, blurMaterial, 1);
        }

        // Blend original with blurred
        blurMaterial.SetTexture("_BlurTex", temp1);
        blurMaterial.SetFloat("_BlurStrength", blurStrength);
        Graphics.Blit(source, destination, blurMaterial, 2);

        RenderTexture.ReleaseTemporary(temp1);
        RenderTexture.ReleaseTemporary(temp2);
    }

    void OnDisable()
    {
        if (blurMaterial != null)
        {
            DestroyImmediate(blurMaterial);
            blurMaterial = null;
        }
    }
}
