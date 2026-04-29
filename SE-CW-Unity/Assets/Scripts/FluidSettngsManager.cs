using UnityEngine;
using UnityEngine.UI;
using TMPro; // Required for TextMeshPro!
using StableFluids.Marbling;

public class FluidSettingsManager : MonoBehaviour
{
    [Header("Core References")]
    public MarblingController fluidController;
    public MarblingFluidSimulator fluidSimulator;

    [Header("UI Text Readouts (Optional)")]
    public TextMeshProUGUI brushSizeText;
    public TextMeshProUGUI pushForceText;
    public TextMeshProUGUI inkOpacityText;
    public TextMeshProUGUI viscosityText;

    [Header("Settings State")]
    [Tooltip("Check this if your sliders go from 0 to 100 instead of 0 to 1")]
    public bool slidersAreZeroToOneHundred = false; // Left this unchecked based on our previous fix!

    private float GetPercentage(float sliderValue)
    {
        return slidersAreZeroToOneHundred ? Mathf.Clamp01(sliderValue / 100f) : Mathf.Clamp01(sliderValue);
    }

    public void OnBrushSizeChanged(float value)
    {
        if (fluidController == null) return;
        
        float percent = GetPercentage(value);
        fluidController.PointFalloff = Mathf.Lerp(500f, 50f, percent);

        // Update the UI Text if it is assigned
        if (brushSizeText != null) 
        {
            brushSizeText.text = Mathf.RoundToInt(percent * 100f).ToString();
        }
    }

    public void OnPushForceChanged(float value)
    {
        if (fluidController == null) return;

        float percent = GetPercentage(value);
        fluidController.PointForce = Mathf.Lerp(0f, 500f, percent);

        if (pushForceText != null) 
        {
            pushForceText.text = Mathf.RoundToInt(percent * 100f).ToString();
        }
    }

    public void OnInkOpacityChanged(float value)
    {
        if (fluidController == null) return;

        float percent = GetPercentage(value);
        Color currentColor = fluidController.paintColor;
        currentColor.a = percent; 
        fluidController.paintColor = currentColor;

        if (inkOpacityText != null) 
        {
            inkOpacityText.text = Mathf.RoundToInt(percent * 100f).ToString();
        }
    }

    public void OnViscosityChanged(float value)
    {
        if (fluidSimulator == null) return;

        float percent = GetPercentage(value);
        fluidSimulator.Viscosity = Mathf.Lerp(0f, 0.005f, percent);

        if (viscosityText != null) 
        {
            viscosityText.text = Mathf.RoundToInt(percent * 100f).ToString();
        }
    }
}