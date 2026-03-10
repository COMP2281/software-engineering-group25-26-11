using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Seb.Fluid2D.Simulation;

public class SmoothingRadiusSlider : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The slider controlling smoothing radius (range 1-100)")]
    public Slider radiusSlider;
    
    [Tooltip("Text component displaying the slider value")]
    public TextMeshProUGUI valueText;
    
    [Tooltip("The FluidSim2D component to update")]
    public FluidSim2D fluidSimulation;

    [Header("Settings")]
    [Tooltip("Initial slider value (default: 50 = radius 0.275)")]
    public float initialSliderValue = 50f;

    void Start()
    {
        // Configure slider
        if (radiusSlider != null)
        {
            radiusSlider.minValue = 1f;
            radiusSlider.maxValue = 100f;
            radiusSlider.value = initialSliderValue;
            radiusSlider.onValueChanged.AddListener(OnSliderChanged);
        }

        // Initialize with default value
        UpdateSmoothingRadius(initialSliderValue);
    }

    /// <summary>
    /// Called when the slider value changes
    /// </summary>
    public void OnSliderChanged(float sliderValue)
    {
        UpdateSmoothingRadius(sliderValue);
    }

    /// <summary>
    /// Updates the smoothing radius based on slider value
    /// Slider range: 1-100
    /// Radius range: 0.05-0.5
    /// Linear interpolation between min and max radius
    /// </summary>
    private void UpdateSmoothingRadius(float sliderValue)
    {
        // Convert slider value (1-100) to smoothing radius (0.05-0.5)
        float radius = Mathf.Lerp(0.05f, 0.5f, (sliderValue - 1f) / 99f);

        // Update FluidSim2D smoothing radius
        if (fluidSimulation != null)
        {
            fluidSimulation.smoothingRadius = radius;
        }
        else
        {
            Debug.LogWarning("FluidSimulation reference is not assigned!");
        }

        // Update text display (show slider value, not radius)
        if (valueText != null)
        {
            valueText.text = sliderValue.ToString("F0");
        }

        Debug.Log($"Smoothing radius updated: slider={sliderValue}, radius={radius:F3}");
    }

    /// <summary>
    /// Optional: Reset to initial value
    /// </summary>
    public void ResetToDefault()
    {
        if (radiusSlider != null)
        {
            radiusSlider.value = initialSliderValue;
        }
    }

    /// <summary>
    /// Optional: Set slider value directly
    /// </summary>
    public void SetSliderValue(float value)
    {
        if (radiusSlider != null && value >= 1f && value <= 100f)
        {
            radiusSlider.value = value;
        }
    }

    /// <summary>
    /// Optional: Set radius directly and update slider
    /// </summary>
    public void SetRadius(float radius)
    {
        // Convert radius (0.05-0.5) back to slider value (1-100)
        float sliderValue = Mathf.Lerp(1f, 100f, (radius - 0.05f) / 0.45f);
        SetSliderValue(sliderValue);
    }
}
