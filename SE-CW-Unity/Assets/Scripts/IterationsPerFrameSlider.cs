using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Seb.Fluid2D.Simulation;

public class IterationsPerFrameSlider : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The slider controlling iterations per frame (range 1-100)")]
    public Slider iterationsSlider;
    
    [Tooltip("Text component displaying the slider value")]
    public TextMeshProUGUI valueText;
    
    [Tooltip("The FluidSim2D component to update")]
    public FluidSim2D fluidSimulation;

    [Header("Settings")]
    [Tooltip("Initial slider value (default: 100 = 50 iterations)")]
    public float initialSliderValue = 30f;

    void Start()
    {
        // Configure slider
        if (iterationsSlider != null)
        {
            iterationsSlider.minValue = 1f;
            iterationsSlider.maxValue = 100f;
            iterationsSlider.value = initialSliderValue;
            iterationsSlider.onValueChanged.AddListener(OnSliderChanged);
        }

        // Initialize with default value
        UpdateIterationsPerFrame(initialSliderValue);
    }

    /// <summary>
    /// Called when the slider value changes
    /// </summary>
    public void OnSliderChanged(float sliderValue)
    {
        UpdateIterationsPerFrame(sliderValue);
    }

    /// <summary>
    /// Updates the iterations per frame based on slider value
    /// Slider range: 1-100
    /// Iterations range: 1-50
    /// Formula: iterations = ceil(sliderValue / 2)
    /// </summary>
    private void UpdateIterationsPerFrame(float sliderValue)
    {
        // Convert slider value (1-100) to iterations per frame (1-50)
        int iterations = Mathf.CeilToInt(sliderValue);

        // Update FluidSim2D iterations per frame
        if (fluidSimulation != null)
        {
            fluidSimulation.iterationsPerFrame = iterations;
        }
        else
        {
            Debug.LogWarning("FluidSimulation reference is not assigned!");
        }

        // Update text display (show slider value, not iterations)
        if (valueText != null)
        {
            valueText.text = sliderValue.ToString("F0");
        }

        Debug.Log($"Iterations per frame updated: slider={sliderValue}, iterations={iterations}");
    }

    /// <summary>
    /// Optional: Reset to initial value
    /// </summary>
    public void ResetToDefault()
    {
        if (iterationsSlider != null)
        {
            iterationsSlider.value = initialSliderValue;
        }
    }

    /// <summary>
    /// Optional: Set slider value directly
    /// </summary>
    public void SetSliderValue(float value)
    {
        if (iterationsSlider != null && value >= 1f && value <= 100f)
        {
            iterationsSlider.value = value;
        }
    }
}
