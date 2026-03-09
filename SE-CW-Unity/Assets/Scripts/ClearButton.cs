using UnityEngine;
using Seb.Fluid2D.Simulation;

/// <summary>
/// Handles the clear button to reset the FluidSim2D paint screen
/// </summary>
public class ClearButton : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The FluidSim2D component to control")]
    public FluidSim2D fluidSimulation;

    void Start()
    {
        // Validate references
        if (fluidSimulation == null)
        {
            Debug.LogError("ClearButton: FluidSim2D reference not assigned!");
        }
    }

    /// <summary>
    /// Call this from the button's OnClick event in the Inspector
    /// </summary>
    public void OnButtonClicked()
    {
        if (fluidSimulation != null)
        {
            fluidSimulation.ClearAllParticles();
            Debug.Log("[ClearButton] Paint screen cleared");
        }
    }
}
