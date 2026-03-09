using UnityEngine;
using Seb.Fluid2D.Simulation;

/// <summary>
/// Button script to clear all paint from the screen by removing all particles from the fluid simulation.
/// Attach this to a UI button and assign the FluidSim2D reference in the Inspector.
/// Connect the OnClick event to the ClearPaint() method.
/// </summary>
public class ClearPaintButton : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the FluidSim2D component (usually tagged 'FLUIDSIM')")]
    public FluidSim2D fluidSim;

    [Header("Settings")]
    [Tooltip("Should we automatically find the FluidSim2D if not assigned?")]
    public bool autoFindFluidSim = true;

    void Start()
    {
        // Auto-find FluidSim2D if not assigned
        if (fluidSim == null && autoFindFluidSim)
        {
            GameObject fluidSimObj = GameObject.FindWithTag("FLUIDSIM");
            if (fluidSimObj != null)
            {
                fluidSim = fluidSimObj.GetComponent<FluidSim2D>();
                if (fluidSim != null)
                {
                    Debug.Log("[ClearPaintButton] Auto-found FluidSim2D.");
                }
            }

            // Fallback: search scene
            if (fluidSim == null)
            {
                fluidSim = FindObjectOfType<FluidSim2D>();
                if (fluidSim != null)
                {
                    Debug.Log("[ClearPaintButton] Found FluidSim2D via scene search.");
                }
            }

            if (fluidSim == null)
            {
                Debug.LogError("[ClearPaintButton] Could not find FluidSim2D! Please assign it manually in the Inspector.");
            }
        }
    }

    /// <summary>
    /// Clears all paint particles from the screen.
    /// Call this method from a UI Button's OnClick event.
    /// </summary>
    public void ClearPaint()
    {
        if (fluidSim == null)
        {
            Debug.LogError("[ClearPaintButton] FluidSim2D is not assigned! Cannot clear paint.");
            return;
        }

        fluidSim.ClearAllParticles();
        Debug.Log("[ClearPaintButton] Paint cleared!");
    }
}
