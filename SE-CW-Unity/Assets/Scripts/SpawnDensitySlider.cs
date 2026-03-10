using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpawnDensitySlider : MonoBehaviour
{
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
            densitySlider.minValue = 1f;
            densitySlider.maxValue = 100f;
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
    /// Linear interpolation between min and max density
    /// </summary>
    private void UpdateSpawnDensity(float sliderValue)
    {
        // Convert slider value (1-100) to spawn density (50-1000)
        float density = Mathf.Lerp(50f, 1000f, (sliderValue - 1f) / 99f);

        // Update Spawner2D spawn density
        if (spawner != null)
        {
            spawner.spawnDensity = density;
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

        Debug.Log($"Spawn density updated: slider={sliderValue}, density={density:F1}");
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
        if (densitySlider != null && value >= 1f && value <= 100f)
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
        float sliderValue = Mathf.Lerp(1f, 100f, (density - 50f) / 950f);
        SetSliderValue(sliderValue);
    }
}
