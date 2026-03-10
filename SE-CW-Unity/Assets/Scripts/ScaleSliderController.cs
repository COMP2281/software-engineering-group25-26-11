using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScaleSliderController : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The parent GameObject whose scale will be adjusted")]
    public Transform parentTransform;

    [Header("Width Slider (X Scale)")]
    [Tooltip("Slider for adjusting X scale")]
    public Slider widthSlider;
    [Tooltip("Text component showing width value (1-100)")]
    public TextMeshProUGUI widthText;

    [Header("Height Slider (Y Scale)")]
    [Tooltip("Slider for adjusting Y scale")]
    public Slider heightSlider;
    [Tooltip("Text component showing height value (1-100)")]
    public TextMeshProUGUI heightText;

    [Header("Initial Values")]
    [Tooltip("Initial slider value for width (default: 80 = scale 8)")]
    public float initialWidthValue = 80f;
    [Tooltip("Initial slider value for height (default: 30 = scale 3)")]
    public float initialHeightValue = 30f;

    [Header("Ripple System")]
    [Tooltip("RippleEffect component on the WaterCube. Uses RippleEffect.Instance if not assigned.")]
    public RippleEffect rippleEffect;

    void Awake()
    {
        // Apply the initial scale to the transform RIGHT NOW, before any Start()
        // runs anywhere in the scene.  Unity guarantees all Awake() calls finish
        // before the first Start() call, so RippleEffect.Start() will read the
        // correct lossyScale when it creates its RenderTextures.
        if (parentTransform != null)
        {
            float initW = initialWidthValue / 10f;
            float initH = initialHeightValue / 10f;
            Vector3 s = parentTransform.localScale;
            parentTransform.localScale = new Vector3(initW, initH, s.z);
        }
    }

    void Start()
    {
        // Configure sliders
        if (widthSlider != null)
        {
            widthSlider.minValue = 1f;
            widthSlider.maxValue = 100f;
            widthSlider.value = initialWidthValue;
            widthSlider.onValueChanged.AddListener(OnWidthSliderChanged);
        }

        if (heightSlider != null)
        {
            heightSlider.minValue = 1f;
            heightSlider.maxValue = 100f;
            heightSlider.value = initialHeightValue;
            heightSlider.onValueChanged.AddListener(OnHeightSliderChanged);
        }

        // Sync text displays and notify ripple effect with the now-confirmed scale.
        UpdateWidthScale(initialWidthValue);
        UpdateHeightScale(initialHeightValue);
    }

    /// <summary>
    /// Called when the width slider value changes
    /// </summary>
    public void OnWidthSliderChanged(float value)
    {
        UpdateWidthScale(value);
    }

    /// <summary>
    /// Called when the height slider value changes
    /// </summary>
    public void OnHeightSliderChanged(float value)
    {
        UpdateHeightScale(value);
    }

    /// <summary>
    /// Updates the X scale based on slider value (1-100 maps to 0.1-10)
    /// </summary>
    private void UpdateWidthScale(float sliderValue)
    {
        if (parentTransform == null)
        {
            Debug.LogWarning("Parent transform is not assigned!");
            return;
        }

        // Convert slider value (1-100) to scale (0.1-10)
        float newScale = sliderValue / 10f;

        // Update parent's X scale
        Vector3 currentScale = parentTransform.localScale;
        parentTransform.localScale = new Vector3(newScale, currentScale.y, currentScale.z);

        // Update text display
        if (widthText != null)
        {
            widthText.text = sliderValue.ToString("F0");
        }

        NotifyRippleEffect();
        Debug.Log($"Width scale updated to {newScale} (slider value: {sliderValue})");
    }

    /// <summary>
    /// Updates the Y scale based on slider value (1-100 maps to 0.1-10).
    /// The water surface is vertical (X-Y plane), so visual height is localScale.y.
    /// UV V now derives from world-space bounds (b.size.y), so Y must drive height.
    /// </summary>
    private void UpdateHeightScale(float sliderValue)
    {
        if (parentTransform == null)
        {
            Debug.LogWarning("Parent transform is not assigned!");
            return;
        }

        // Convert slider value (1-100) to scale (0.1-10)
        float newScale = sliderValue / 10f;

        // Update parent's Y scale (visual height of the vertical surface)
        Vector3 currentScale = parentTransform.localScale;
        parentTransform.localScale = new Vector3(currentScale.x, newScale, currentScale.z);

        // Update text display
        if (heightText != null)
        {
            heightText.text = sliderValue.ToString("F0");
        }

        NotifyRippleEffect();
        Debug.Log($"Height scale updated to {newScale} (slider value: {sliderValue})");
    }

    /// <summary>
    /// Tells the RippleEffect about the current world-space X/Z dimensions of the
    /// WaterCube so it can rebuild its RenderTextures at the correct aspect ratio.
    /// </summary>
    private void NotifyRippleEffect()
    {
        RippleEffect effect = rippleEffect != null ? rippleEffect : RippleEffect.Instance;
        if (effect == null) return;

        // UpdateWaterScale reads directly from the collider bounds — no need to
        // pass dimensions here, removing any mismatch between parentTransform and
        // the RippleEffect's own transform.
        effect.UpdateWaterScale();
    }

    /// <summary>
    /// Optional: Reset to initial values
    /// </summary>
    public void ResetToDefaults()
    {
        if (widthSlider != null)
        {
            widthSlider.value = initialWidthValue;
        }
        if (heightSlider != null)
        {
            heightSlider.value = initialHeightValue;
        }
    }
}
