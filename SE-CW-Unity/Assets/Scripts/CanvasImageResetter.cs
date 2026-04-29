using UnityEngine;
using StableFluids.Marbling; 

public class CanvasImageResetter : MonoBehaviour
{
    [Header("Canvas References")]
    [Tooltip("Drag your Canvas (Custom Render Texture) here")]
    public CustomRenderTexture fluidCanvas;

    [Tooltip("Drag your original PNG image here")]
    public Texture2D originalImage;

    [Header("Physics Reset")]
    [Tooltip("Drag your FluidManager here")]
    public MarblingFluidSimulator fluidSimulator;

    // Call this method from your UI Button!
    public void ResetToImage()
    {
        if (fluidCanvas == null || fluidSimulator == null) 
        {
            Debug.LogWarning("CanvasImageResetter: Missing references in the Inspector!");
            return;
        }

        // 1. Reload the image on the canvas
        fluidCanvas.initializationSource = CustomRenderTextureInitializationSource.TextureAndColor;
        fluidCanvas.initializationTexture = originalImage;
        fluidCanvas.initializationColor = Color.white;

        // 2. Double-wipe to clear the visual buffers
        fluidCanvas.Initialize();
        fluidCanvas.Update();
        fluidCanvas.Initialize();

        // 3. Trigger the factory reset on the physics engine
        fluidSimulator.ResetSimulation();
        
        Debug.Log("Canvas Image Restored and Physics Nuked!");
    }
}