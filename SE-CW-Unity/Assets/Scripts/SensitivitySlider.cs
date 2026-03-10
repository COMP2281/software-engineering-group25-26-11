using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Seb.Fluid2D.Simulation;

public class SensitivitySlider : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The slider controlling sensitivity (range 1-100)")]
    public Slider sensitivitySlider;
    
    [Tooltip("Text component displaying the slider value")]
    public TextMeshProUGUI valueText;
    
    [Tooltip("The FluidSim2D component to update")]
    public FluidSim2D fluidSimulation;

    [Header("Settings")]
    [Tooltip("Initial slider value (default: 50 = medium sensitivity)")]
    public float initialSliderValue = 50f;

    void Start()
    {
        // Configure slider
        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = 1f;
            sensitivitySlider.maxValue = 100f;
            sensitivitySlider.value = initialSliderValue;
            sensitivitySlider.onValueChanged.AddListener(OnSliderChanged);
        }

        // Initialize with default value
        UpdateSensitivity(initialSliderValue);
    }

    /// <summary>
    /// Called when the slider value changes
    /// </summary>
    public void OnSliderChanged(float sliderValue)
    {
        UpdateSensitivity(sliderValue);
    }

    /// <summary>
    /// Updates the pressure sensitivity based on slider value
    /// Slider range: 1-100
    /// Converts to t (0-1) and interpolates pressure multiplier between 50 and 500
    /// Near pressure multiplier is automatically set to pressureMultiplier / 50
    /// </summary>
    private void UpdateSensitivity(float sliderValue)
    {
        // Convert slider value (1-100) to normalized t (0.01-1.0)
        float t = sliderValue / 100f;

        // Calculate pressure multiplier (50-500 range)
        float pressureMultiplier = Mathf.Lerp(50f, 500f, t);
        float nearPressureMultiplier = pressureMultiplier / 50f;

        // Update FluidSim2D pressure settings
        if (fluidSimulation != null)
        {
            fluidSimulation.pressureMultiplier = pressureMultiplier;
            fluidSimulation.nearPressureMultiplier = nearPressureMultiplier;
        }
        else
        {
            Debug.LogWarning("FluidSimulation reference is not assigned!");
        }

        // Update text display (show slider value)
        if (valueText != null)
        {
            valueText.text = sliderValue.ToString("F0");
        }

        Debug.Log($"Sensitivity updated: slider={sliderValue}, pressureMultiplier={pressureMultiplier:F1}, nearPressureMultiplier={nearPressureMultiplier:F2}");
    }

    /// <summary>
    /// Optional: Reset to initial value
    /// </summary>
    public void ResetToDefault()
    {
        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = initialSliderValue;
        }
    }

    /// <summary>
    /// Optional: Set slider value directly
    /// </summary>
    public void SetSliderValue(float value)
    {
        if (sensitivitySlider != null && value >= 1f && value <= 100f)
        {
            sensitivitySlider.value = value;
        }
    }

    /// <summary>
    /// Set sensitivity using normalized value (0-1) - matches the original API
    /// </summary>
    public void SetSensitivity(float t)
    {
        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = t * 100f;
        }
        else
        {
            // Direct update if slider not assigned
            UpdateSensitivity(t * 100f);
        }
    }
}
