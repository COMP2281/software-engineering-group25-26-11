using UnityEngine;

/// <summary>
/// Integrates a color wheel or color picker with the ColorSelectionManager.
/// Attach this to your color wheel GameObject.
/// </summary>
public class ColorWheelIntegration : MonoBehaviour
{
    [Header("References")]
    public ColorSelectionManager selectionManager;

    [Header("Current Color")]
    [Tooltip("The currently selected color from the wheel")]
    public Color currentColor = Color.white;

    /// <summary>
    /// Call this method when the color wheel value changes.
    /// For example, from a slider's OnValueChanged event.
    /// </summary>
    public void OnColorChanged(Color newColor)
    {
        currentColor = newColor;
        
        if (selectionManager != null)
        {
            selectionManager.OnColorPicked(newColor);
        }
    }

    /// <summary>
    /// Alternative method if your color wheel uses separate RGB components
    /// </summary>
    public void OnColorChangedRGB(float r, float g, float b)
    {
        currentColor = new Color(r, g, b, 1f);
        
        if (selectionManager != null)
        {
            selectionManager.OnColorPicked(currentColor);
        }
    }
}
