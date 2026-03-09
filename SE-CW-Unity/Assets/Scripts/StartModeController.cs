using UnityEngine;
using System.Collections.Generic;

public class StartModeController : MonoBehaviour
{
    public UIManager uiManager;
    public PaintballManager paintballManager;

    public bool startInPaintingMode = false;

    // Default colours automatically added when skipping
    public List<ColorButton> defaultColorButtons;

   void Start()
    {
        if (uiManager == null || paintballManager == null)
        {
            Debug.LogWarning("StartModeController missing references.");
            return;
        }

        if (!startInPaintingMode)
        {
            uiManager.SetInitialState();
            return;
        }

        // Simulate spawning balls from color buttons
        foreach (var button in defaultColorButtons)
        {
            button.OnColorButtonClicked();
        }

        // Pretend like you pressed the submit button
        uiManager.OnSubmit();
    }


}
