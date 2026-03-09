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
        
        // Set initial sprite based on current state
        UpdateButtonSprite();
    }

    /// <summary>
    /// Call this from the button's OnClick event in the Inspector
    /// </summary>
    public void OnButtonClicked()
    {
        if (fluidSimulation != null)
        {
            fluidSimulation.TogglePause();
            UpdateButtonSprite();
        }
    }

    /// <summary>
    /// Updates the button sprite based on the current simulation state
    /// </summary>
    void UpdateButtonSprite()
    {
        if (buttonImage == null || pauseSprite == null || playSprite == null)
        {
            return;
        }

        // If simulation is paused, show play icon (to resume)
        // If simulation is playing, show pause icon (to pause)
        buttonImage.sprite = fluidSimulation.IsPaused ? playSprite : pauseSprite;
    }

    void Update()
    {
        // Update sprite every frame in case pause state changes from keyboard input
        UpdateButtonSprite();
    }
}
