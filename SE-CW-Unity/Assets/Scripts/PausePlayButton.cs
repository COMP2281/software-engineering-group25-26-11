using UnityEngine;
using UnityEngine.UI;
using Seb.Fluid2D.Simulation;

/// <summary>
/// Handles the pause/play button UI and toggles the FluidSim2D simulation
/// </summary>
public class PausePlayButton : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The FluidSim2D component to control")]
    public FluidSim2D fluidSimulation;
    
    [Tooltip("The Image component on the button (will swap sprites)")]
    public Image buttonImage;
    
    [Tooltip("Parent GameObject containing render camera (disabled when paused to hide ripple effects)")]
    public GameObject rippleEffectsParent;
    
    [Header("Sprites")]
    [Tooltip("Sprite to show when simulation is playing (shows pause icon)")]
    public Sprite pauseSprite;
    
    [Tooltip("Sprite to show when simulation is paused (shows play icon)")]
    public Sprite playSprite;

    void Start()
    {
        // Validate references
        if (fluidSimulation == null)
        {
            Debug.LogError("PausePlayButton: FluidSim2D reference not assigned!");
        }
        
        if (buttonImage == null)
        {
            Debug.LogError("PausePlayButton: Button Image reference not assigned!");
        }
        
        if (pauseSprite == null)
        {
            Debug.LogError("PausePlayButton: Pause Sprite not assigned!");
        }
        
        if (playSprite == null)
        {
            Debug.LogError("PausePlayButton: Play Sprite not assigned!");
        }
        
        // Set initial sprite based on current state
        UpdateButtonSprite();
        Debug.Log($"PausePlayButton initialized. Current sprite: {(buttonImage != null ? buttonImage.sprite?.name : "null")}");
    }

    /// <summary>
    /// Call this from the button's OnClick event in the Inspector
    /// </summary>
    public void OnButtonClicked()
    {
        if (fluidSimulation != null)
        {
            Debug.Log($"PausePlayButton clicked. Before toggle: IsPaused={fluidSimulation.IsPaused}");
            fluidSimulation.TogglePause();
            
            // Also toggle RippleEffect pause state
            if (RippleEffect.Instance != null)
            {
                RippleEffect.Instance.SetPaused(fluidSimulation.IsPaused);
                Debug.Log($"RippleEffect paused: {fluidSimulation.IsPaused}");
            }
            
            Debug.Log($"After toggle: IsPaused={fluidSimulation.IsPaused}");
            UpdateButtonSprite();
            UpdateRippleEffects();
        }
        else
        {
            Debug.LogError("PausePlayButton: Cannot toggle - fluidSimulation is null!");
        }
    }

    /// <summary>
    /// Updates the button sprite based on the current simulation state
    /// </summary>
    void UpdateButtonSprite()
    {
        if (buttonImage == null || pauseSprite == null || playSprite == null || fluidSimulation == null)
        {
            return;
        }

        // If simulation is paused, show play icon (to resume)
        // If simulation is playing, show pause icon (to pause)
        Sprite newSprite = fluidSimulation.IsPaused ? playSprite : pauseSprite;
        
        if (buttonImage.sprite != newSprite)
        {
            buttonImage.sprite = newSprite;
            Debug.Log($"PausePlayButton: Sprite changed to {newSprite.name} (IsPaused={fluidSimulation.IsPaused})");
        }
    }

    /// <summary>
    /// Toggles the ripple effects parent based on pause state
    /// </summary>
    void UpdateRippleEffects()
    {
        if (rippleEffectsParent != null && fluidSimulation != null)
        {
            // Disable ripple effects when paused, enable when playing
            rippleEffectsParent.SetActive(!fluidSimulation.IsPaused);
        }
    }

    void Update()
    {
        // Update sprite every frame in case pause state changes from keyboard input
        UpdateButtonSprite();
        UpdateRippleEffects();
    }
}
