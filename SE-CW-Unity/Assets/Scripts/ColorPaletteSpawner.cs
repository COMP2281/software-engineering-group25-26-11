using UnityEngine;

public class ColorPaletteSpawner : MonoBehaviour
{
    public ColorSelectionManager selectionManager;

    public void SpawnBallFromPalette(Color pickedColor)
    {
        if (selectionManager == null)
        {
            Debug.LogError("ColorSelectionManager not assigned on ColorPaletteSpawner.");
            return;
        }

        selectionManager.HandleColorSelection(pickedColor);
    }
}
