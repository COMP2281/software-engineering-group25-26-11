using UnityEngine;
using StableFluids.Marbling;

public class FluidColorBridge : MonoBehaviour
{
    public MarblingController fluidController;
    public ColorUIBinder uiBinder;

    // This will be called by the UI Binder whenever the color changes
    public void UpdateFluidColor(Color newColor)
    {
        if (fluidController != null)
        {
            fluidController.paintColor = newColor;
        }
    }
}