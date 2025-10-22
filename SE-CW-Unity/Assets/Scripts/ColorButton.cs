using UnityEngine;

public class ColorButton : MonoBehaviour
{
    public Color buttonColor;
    public ColorSelectionManager selectionManager;  // Reference to the central manager

    public void SpawnBall()
    {
        if (selectionManager == null)
        {
            Debug.LogError("ColorSelectionManager not assigned.");
            return;
        }

        // Delegate the spawn to the manager
        selectionManager.HandleColorSelection(buttonColor);
    }
}
