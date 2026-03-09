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

        // Initialize values
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

        Debug.Log($"Width scale updated to {newScale} (slider value: {sliderValue})");
    }

    /// <summary>
    /// Updates the Y scale based on slider value (1-100 maps to 0.1-10)
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

        // Update parent's Y scale
        Vector3 currentScale = parentTransform.localScale;
        parentTransform.localScale = new Vector3(currentScale.x, newScale, currentScale.z);

        // Update text display
        if (heightText != null)
        {
            heightText.text = sliderValue.ToString("F0");
        }

        Debug.Log($"Height scale updated to {newScale} (slider value: {sliderValue})");
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
