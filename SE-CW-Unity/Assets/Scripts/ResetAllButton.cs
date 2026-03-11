using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Resets all UI sliders to their default values when the button is clicked.
/// Attach this to a Reset Button in the UI.
/// </summary>
public class ResetAllButton : MonoBehaviour
{
    [Header("Sliders")]
    [Tooltip("Interaction Radius Slider (Brush Width)")]
    public Slider brushWidthSlider;
    
    [Tooltip("Spawn Density Slider (Paintball Density)")]
    public Slider paintballDensitySlider;
    
    [Tooltip("Smoothing Radius Slider (Fluidity)")]
    public Slider fluiditySlider;
    
    [Tooltip("Time Scale Slider (Paint Speed)")]
    public Slider paintSpeedSlider;
    
    [Tooltip("Sensitivity Slider")]
    public Slider sensitivitySlider;
    
    [Tooltip("Height Slider")]
    public Slider heightSlider;
    
    [Tooltip("Width Slider")]
    public Slider widthSlider;

    [Header("Default Values")]
    [Tooltip("Default Brush Width (1-100)")]
    public float defaultBrushWidth = 10f;
    
    [Tooltip("Default Paintball Density (1-100)")]
    public float defaultPaintballDensity = 25f;
    
    [Tooltip("Default Fluidity (1-100)")]
    public float defaultFluidity = 90f;
    
    [Tooltip("Default Paint Speed (1-100)")]
    public float defaultPaintSpeed = 50f;
    
    [Tooltip("Default Sensitivity (1-100)")]
    public float defaultSensitivity = 60f;
    
    [Tooltip("Default Height (1-100)")]
    public float defaultHeight = 35f;
    
    [Tooltip("Default Width (1-100)")]
    public float defaultWidth = 80f;

    [Header("Button")]
    [Tooltip("The reset button (optional - auto-finds if not assigned)")]
    public Button resetButton;

    void Start()
    {
        // Auto-find button on this GameObject if not assigned
        if (resetButton == null)
        {
            resetButton = GetComponent<Button>();
        }

        // Add click listener
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetAllToDefaults);
        }
        else
        {
            Debug.LogWarning("ResetAllButton: No Button component found!");
        }
    }

    /// <summary>
    /// Resets all sliders to their default values
    /// </summary>
    public void ResetAllToDefaults()
    {
        Debug.Log("Resetting all sliders to default values...");

        // Reset Brush Width (Interaction Radius)
        if (brushWidthSlider != null)
        {
            brushWidthSlider.value = defaultBrushWidth;
        }
        else
        {
            Debug.LogWarning("Brush Width Slider not assigned!");
        }

        // Reset Paintball Density (Spawn Density)
        if (paintballDensitySlider != null)
        {
            paintballDensitySlider.value = defaultPaintballDensity;
        }
        else
        {
            Debug.LogWarning("Paintball Density Slider not assigned!");
        }

        // Reset Fluidity (Smoothing Radius)
        if (fluiditySlider != null)
        {
            fluiditySlider.value = defaultFluidity;
        }
        else
        {
            Debug.LogWarning("Fluidity Slider not assigned!");
        }

        // Reset Paint Speed (Time Scale)
        if (paintSpeedSlider != null)
        {
            paintSpeedSlider.value = defaultPaintSpeed;
        }
        else
        {
            Debug.LogWarning("Paint Speed Slider not assigned!");
        }

        // Reset Sensitivity
        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = defaultSensitivity;
        }
        else
        {
            Debug.LogWarning("Sensitivity Slider not assigned!");
        }

        // Reset Height
        if (heightSlider != null)
        {
            heightSlider.value = defaultHeight;
        }
        else
        {
            Debug.LogWarning("Height Slider not assigned!");
        }

        // Reset Width
        if (widthSlider != null)
        {
            widthSlider.value = defaultWidth;
        }
        else
        {
            Debug.LogWarning("Width Slider not assigned!");
        }

        Debug.Log($"Reset complete: BrushWidth={defaultBrushWidth}, Density={defaultPaintballDensity}, " +
                  $"Fluidity={defaultFluidity}, Speed={defaultPaintSpeed}, Sensitivity={defaultSensitivity}, " +
                  $"Height={defaultHeight}, Width={defaultWidth}");
    }

    /// <summary>
    /// Optional: Call this method directly from a Unity Button's OnClick event
    /// </summary>
    public void OnResetButtonClick()
    {
        ResetAllToDefaults();
    }
}
