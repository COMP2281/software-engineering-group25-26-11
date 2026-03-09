using UnityEngine;

/// <summary>
/// Attach this to individual color buttons in the color picker panel.
/// When clicked, it tells the ColorSelectionManager which color was picked.
/// </summary>
public class ColorButton : MonoBehaviour
{
    public Color buttonColor;
    public ColorSelectionManager selectionManager;  // Reference to the central manager

    /// <summary>
    /// Called when this color button is clicked in the panel
    /// </summary>
    public void OnColorButtonClicked()
    {
        if (selectionManager == null)
        {
            Debug.LogError("ColorSelectionManager not assigned.");
            return;
        }

        // Tell the manager this color was picked (not confirmed yet)
        selectionManager.OnColorPicked(buttonColor);
        Debug.Log($"Color picked: {buttonColor}");
    }
}
