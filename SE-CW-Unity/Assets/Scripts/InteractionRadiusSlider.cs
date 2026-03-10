using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Seb.Fluid2D.Simulation;

public class InteractionRadiusSlider : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The slider controlling the interaction radius (range 1-100)")]
    public Slider radiusSlider;
    
    [Tooltip("Text component displaying the slider value")]
    public TextMeshProUGUI valueText;
    
    [Tooltip("The FluidSim2D component to update")]
    public FluidSim2D fluidSimulation;

    [Header("Settings")]
    [Tooltip("Initial slider value (default: 100 = radius 5)")]
    public float initialSliderValue = 100f;

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
        UpdateInteractionRadius(initialSliderValue);
    }

    /// <summary>
    /// Called when the slider value changes
    /// </summary>
    public void OnSliderChanged(float sliderValue)
    {
        UpdateInteractionRadius(sliderValue);
    }

    /// <summary>
    /// Updates the interaction radius based on slider value
    /// Slider range: 1-100
    /// Slider 100 = radius 5
    /// Slider 1 = radius 0.05
    /// Formula: radius = (sliderValue * 0.05) + 0.00005
    /// This ensures all values have a visible effect
    /// </summary>
    private void UpdateInteractionRadius(float sliderValue)
    {
        // Convert slider value (1-100) to interaction radius (0.05-5)
        // Using linear interpolation: min=0.05 at slider=1, max=5 at slider=100
        float radius = Mathf.Lerp(0.05f, 5f, (sliderValue - 1f) / 99f);

        // Update FluidSim2D interaction radius
        if (fluidSimulation != null)
        {
            fluidSimulation.interactionRadius = radius;
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

        Debug.Log($"Interaction radius updated: slider={sliderValue}, radius={radius:F2}");
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
}
