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
    /// Slider 100 = fluid radius 5, ripple radius 0.4
    /// Slider 1 = fluid radius 0.05, ripple radius 0.05
    /// This ensures all values have a visible effect
    /// </summary>
    private void UpdateInteractionRadius(float sliderValue)
    {
        // Convert slider value (1-100) to interaction radius (0.05-5)
        // Using linear interpolation: min=0.05 at slider=1, max=5 at slider=100
        float fluidRadius = Mathf.Lerp(0.05f, 5f, (sliderValue - 1f) / 99f);
        
        // Convert slider value to ripple radius (0.05-0.4)
        float rippleRadius = Mathf.Lerp(0.05f, 0.4f, (sliderValue - 1f) / 99f);

        // Update FluidSim2D interaction radius
        if (fluidSimulation != null)
        {
            fluidSimulation.interactionRadius = fluidRadius;
        }
        else
        {
            Debug.LogWarning("FluidSimulation reference is not assigned!");
        }
        
        // Update RippleEffect ripple radius
        if (RippleEffect.Instance != null)
        {
            RippleEffect.Instance.rippleRadius = rippleRadius;
        }

        // Update text display (show slider value, not radius)
        if (valueText != null)
        {
            valueText.text = sliderValue.ToString("F0");
        }

        Debug.Log($"Radius updated: slider={sliderValue}, fluidRadius={fluidRadius:F2}, rippleRadius={rippleRadius:F3}");
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
