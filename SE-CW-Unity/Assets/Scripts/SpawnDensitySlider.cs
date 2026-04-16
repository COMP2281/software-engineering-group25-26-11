using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpawnDensitySlider : MonoBehaviour
{
    const float SliderMin = 1f;
    const float SliderMax = 100f;
    const float DensityMin = 50f;
    const float DensityMax = 1000f;
    const float ClumpScaleAtSliderMin = 0.5f;
    const float ClumpScaleAtSlider30 = 1.2f;
    const float ClumpScaleAtSliderMax = 2f;

    [Header("References")]
    [Tooltip("The slider controlling spawn density (range 1-100)")]
    public Slider densitySlider;
    
    [Tooltip("Text component displaying the slider value")]
    public TextMeshProUGUI valueText;
    
    [Tooltip("The Spawner2D component to update")]
    public Spawner2D spawner;

    [Header("Settings")]
    [Tooltip("Initial slider value (default: 50 = density 525)")]
    public float initialSliderValue = 50f;

    void Start()
    {
        // Configure slider
        if (densitySlider != null)
        {
            densitySlider.minValue = SliderMin;
            densitySlider.maxValue = SliderMax;
            densitySlider.value = initialSliderValue;
            densitySlider.onValueChanged.AddListener(OnSliderChanged);
        }

        // Initialize with default value
        UpdateSpawnDensity(initialSliderValue);
    }

    /// <summary>
    /// Called when the slider value changes
    /// </summary>
    public void OnSliderChanged(float sliderValue)
    {
        UpdateSpawnDensity(sliderValue);
    }

    /// <summary>
    /// Updates the spawn density based on slider value
    /// Slider range: 1-100
    /// Density range: 50-1000
    /// Clump scale: 0.4 at slider 1, 1.15 at slider 30, 2.0 at slider 100
    /// </summary>
    private void UpdateSpawnDensity(float sliderValue)
    {
        // Convert slider value (1-100) to spawn density (50-1000)
        float densityT = Mathf.InverseLerp(SliderMin, SliderMax, sliderValue);
        float density = Mathf.Lerp(DensityMin, DensityMax, densityT);

        // Full-range clump mapping while preserving requested anchor points at slider 1 and 30.
        float clumpScale;
        if (sliderValue <= 30f)
        {
            float clumpT = Mathf.InverseLerp(SliderMin, 30f, sliderValue);
            clumpScale = Mathf.Lerp(ClumpScaleAtSliderMin, ClumpScaleAtSlider30, clumpT);
        }
        else
        {
            float clumpT = Mathf.InverseLerp(30f, SliderMax, sliderValue);
            clumpScale = Mathf.Lerp(ClumpScaleAtSlider30, ClumpScaleAtSliderMax, clumpT);
        }

        // Update Spawner2D spawn density
        if (spawner != null)
        {
            spawner.spawnDensity = density;
            spawner.clumpScale = clumpScale;
        }
        else
        {
            Debug.LogWarning("Spawner2D reference is not assigned!");
        }

        // Update text display (show slider value, not density)
        if (valueText != null)
        {
            valueText.text = sliderValue.ToString("F0");
        }

        Debug.Log($"Spawn settings updated: slider={sliderValue}, density={density:F1}, clumpScale={clumpScale:F2}");
    }

    /// <summary>
    /// Optional: Reset to initial value
    /// </summary>
    public void ResetToDefault()
    {
        if (densitySlider != null)
        {
            densitySlider.value = initialSliderValue;
        }
    }

    /// <summary>
    /// Optional: Set slider value directly
    /// </summary>
    public void SetSliderValue(float value)
    {
        if (densitySlider != null && value >= SliderMin && value <= SliderMax)
        {
            densitySlider.value = value;
        }
    }

    /// <summary>
    /// Optional: Set density directly and update slider
    /// </summary>
    public void SetDensity(float density)
    {
        // Convert density (50-1000) back to slider value (1-100)
        float sliderValue = Mathf.Lerp(SliderMin, SliderMax, (density - DensityMin) / (DensityMax - DensityMin));
        SetSliderValue(sliderValue);
    }
}
