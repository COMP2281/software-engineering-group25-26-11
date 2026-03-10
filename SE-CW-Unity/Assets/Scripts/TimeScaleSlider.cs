using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Seb.Fluid2D.Simulation;

public class TimeScaleSlider : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The slider controlling the time scale (range 1-100)")]
    public Slider timeScaleSlider;
    
    [Tooltip("Text component displaying the slider value")]
    public TextMeshProUGUI valueText;
    
    [Tooltip("The FluidSim2D component to update")]
    public FluidSim2D fluidSimulation;

    [Header("Settings")]
    [Tooltip("Initial slider value (default: 100 = time scale 1)")]
    public float initialSliderValue = 100f;

    void Start()
    {
        // Configure slider
        if (timeScaleSlider != null)
        {
            timeScaleSlider.minValue = 1f;
            timeScaleSlider.maxValue = 100f;
            timeScaleSlider.value = initialSliderValue;
            timeScaleSlider.onValueChanged.AddListener(OnSliderChanged);
        }

        // Initialize with default value
        UpdateTimeScale(initialSliderValue);
    }

    /// <summary>
    /// Called when the slider value changes
    /// </summary>
    public void OnSliderChanged(float sliderValue)
    {
        UpdateTimeScale(sliderValue);
    }

    /// <summary>
    /// Updates the time scale based on slider value
    /// Slider range: 1-100
    /// Slider 100 = time scale 1
    /// Formula: timeScale = sliderValue / 100
    /// </summary>
    private void UpdateTimeScale(float sliderValue)
    {
        // Convert slider value (1-100) to time scale (0.01-1)
        float timeScale = sliderValue / 100f;

        // Update FluidSim2D time scale
        if (fluidSimulation != null)
        {
            fluidSimulation.timeScale = timeScale;
        }
        else
        {
            Debug.LogWarning("FluidSimulation reference is not assigned!");
        }

        // Update text display (show slider value, not time scale)
        if (valueText != null)
        {
            valueText.text = sliderValue.ToString("F0");
        }

        Debug.Log($"Time scale updated: slider={sliderValue}, timeScale={timeScale:F2}");
    }

    /// <summary>
    /// Optional: Reset to initial value
    /// </summary>
    public void ResetToDefault()
    {
        if (timeScaleSlider != null)
        {
            timeScaleSlider.value = initialSliderValue;
        }
    }

    /// <summary>
    /// Optional: Set slider value directly
    /// </summary>
    public void SetSliderValue(float value)
    {
        if (timeScaleSlider != null && value >= 1f && value <= 100f)
        {
            timeScaleSlider.value = value;
        }
    }
}
