using UnityEngine;
using Seb.Fluid2D.Simulation;

/// <summary>
/// Controls opening/closing a menu panel and pauses the fluid simulation when menu is open
/// </summary>
public class MenuPanelController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The menu panel GameObject to show/hide")]
    public GameObject menuPanel;
    
    [Tooltip("The FluidSim2D component to pause when menu is open")]
    public FluidSim2D fluidSimulation;
    
    [Tooltip("Parent GameObject containing ripple effects (disabled when menu is open)")]
    public GameObject rippleEffectsParent;

    [Header("Settings")]
    [Tooltip("Should the menu start closed?")]
    public bool startClosed = true;

    private bool isMenuOpen = false;
    private bool wasSimulationPausedBefore = false;

    void Start()
    {
        // Validate references
        if (menuPanel == null)
        {
            Debug.LogError("MenuPanelController: Menu Panel not assigned!");
        }
        
        if (fluidSimulation == null)
        {
            Debug.LogError("MenuPanelController: FluidSim2D reference not assigned!");
        }

        // Set initial state
        if (startClosed && menuPanel != null)
        {
            menuPanel.SetActive(false);
            isMenuOpen = false;
        }
        else if (menuPanel != null)
        {
            menuPanel.SetActive(true);
            isMenuOpen = true;
            PauseSimulation();
        }
    }

    /// <summary>
    /// Toggles the menu panel open/closed. Call this from a button's OnClick event.
    /// </summary>
    public void ToggleMenu()
    {
        if (isMenuOpen)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    /// <summary>
    /// Opens the menu panel and pauses the simulation
    /// </summary>
    public void OpenMenu()
    {
        if (menuPanel == null)
        {
            Debug.LogWarning("MenuPanelController: Cannot open menu - menuPanel is not assigned");
            return;
        }

        Debug.Log("MenuPanelController: Opening menu");
        
        // Save the current pause state before we change it
        if (fluidSimulation != null)
        {
            wasSimulationPausedBefore = fluidSimulation.IsPaused;
        }

        // Show the menu panel
        menuPanel.SetActive(true);
        isMenuOpen = true;

        // Pause the simulation
        PauseSimulation();
    }

    /// <summary>
    /// Closes the menu panel and resumes the simulation (if it wasn't manually paused)
    /// </summary>
    public void CloseMenu()
    {
        if (menuPanel == null)
        {
            Debug.LogWarning("MenuPanelController: Cannot close menu - menuPanel is not assigned");
            return;
        }

        Debug.Log("MenuPanelController: Closing menu");
        
        // Hide the menu panel
        menuPanel.SetActive(false);
        isMenuOpen = false;

        // Resume the simulation (only if it wasn't already paused before opening menu)
        ResumeSimulation();
    }

    /// <summary>
    /// Pauses the fluid simulation and disables ripple effects
    /// </summary>
    private void PauseSimulation()
    {
        if (fluidSimulation != null)
        {
            fluidSimulation.SetPaused(true);
            Debug.Log("MenuPanelController: Simulation paused");
        }

        if (rippleEffectsParent != null)
        {
            rippleEffectsParent.SetActive(false);
            Debug.Log("MenuPanelController: Ripple effects disabled");
        }
    }

    /// <summary>
    /// Resumes the fluid simulation and enables ripple effects (respects previous pause state)
    /// </summary>
    private void ResumeSimulation()
    {
        // Only resume if the simulation wasn't manually paused before opening the menu
        if (fluidSimulation != null && !wasSimulationPausedBefore)
        {
            fluidSimulation.SetPaused(false);
            Debug.Log("MenuPanelController: Simulation resumed");
        }
        else if (wasSimulationPausedBefore)
        {
            Debug.Log("MenuPanelController: Simulation stays paused (was paused before menu opened)");
        }

        if (rippleEffectsParent != null && !wasSimulationPausedBefore)
        {
            rippleEffectsParent.SetActive(true);
            Debug.Log("MenuPanelController: Ripple effects enabled");
        }
    }

    /// <summary>
    /// Check if the menu is currently open
    /// </summary>
    public bool IsMenuOpen()
    {
        return isMenuOpen;
    }
}
