using UnityEngine;
using StableFluids.Marbling;

public class BrushModeManager : MonoBehaviour
{
    [Header("Fluid References")]
    public MarblingController fluidController;

    [Header("Clear Brush Settings")]
    public Color clearColor = Color.white;
    public float giantBrushSize = 0f;

    // These variables silently remember what the user was doing
    private float _savedFalloff;
    private Color _savedColor;
    private bool _isEraserEquipped = false;

    // --- THE TOGGLE LOGIC ---
    public void ToggleClearBrush()
    {
        if (fluidController == null) return;

        if (!_isEraserEquipped)
        {
            // 1. Save current settings
            _savedFalloff = fluidController.PointFalloff;
            _savedColor = fluidController.paintColor;
            _isEraserEquipped = true;

            // 2. Equip the giant clear brush
            fluidController.PointFalloff = giantBrushSize;
            
            // Force alpha to 1 so the white paint is completely solid
            Color solidClear = clearColor;
            solidClear.a = 1f;
            fluidController.paintColor = solidClear;
            
            Debug.Log("Giant Clear Brush Equipped!");
        }
        else
        {
            // 3. Restore the user's exact brush, color, and transparency
            fluidController.PointFalloff = _savedFalloff;
            fluidController.paintColor = _savedColor;
            
            _isEraserEquipped = false;
            
            Debug.Log("Previous Brush Settings Restored!");
        }
    }
}