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

    [Header("Settings")]
    [Tooltip("Should the menu start closed?")]
    public bool startClosed = true;

    private bool isMenuOpen = false;

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
        // Sync with actual panel state before toggling (in case closed/opened externally)
        if (menuPanel != null)
        {
            isMenuOpen = menuPanel.activeSelf;
        }

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

        // Sync the internal flag with actual panel state (in case it was closed externally)
        isMenuOpen = menuPanel.activeSelf;

        // Check if menu is already open
        if (isMenuOpen)
        {
            Debug.Log("MenuPanelController: Menu is already open");
            return;
        }

        Debug.Log("MenuPanelController: Opening menu");

        // Show the menu panel
        menuPanel.SetActive(true);
        isMenuOpen = true;

        // Pause the simulation and all effects
        PauseSimulation();
    }

    /// <summary>
    /// Closes the menu panel (does NOT resume simulation - user must manually unpause)
    /// </summary>
    public void CloseMenu()
    {
        if (menuPanel == null)
        {
            Debug.LogWarning("MenuPanelController: Cannot close menu - menuPanel is not assigned");
            return;
        }

        // Sync the internal flag with actual panel state (in case it was opened externally)
        isMenuOpen = menuPanel.activeSelf;

        // Check if menu is already closed
        if (!isMenuOpen)
        {
            Debug.Log("MenuPanelController: Menu is already closed");
            return;
        }

        Debug.Log("MenuPanelController: Closing menu (simulation remains paused)");
        
        // Hide the menu panel
        menuPanel.SetActive(false);
        isMenuOpen = false;
        
        // Do NOT resume - let the user manually unpause using the pause/play button
    }

    /// <summary>
    /// Pauses the fluid simulation, ripple effects, and all animations/particle systems
    /// </summary>
    private void PauseSimulation()
    {
        if (fluidSimulation != null)
        {
            fluidSimulation.SetPaused(true);
            Debug.Log("MenuPanelController: Fluid simulation paused");
        }

        if (RippleEffect.Instance != null)
        {
            RippleEffect.Instance.SetPaused(true);
            Debug.Log("MenuPanelController: Ripple effects paused (includes animations and particle systems)");
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
