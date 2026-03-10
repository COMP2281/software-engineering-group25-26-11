using UnityEngine;
using Seb.Fluid2D.Simulation;

/// <summary>
/// Displays a warning panel if no particles have been spawned within a specified time period.
/// Attach this script to the warning panel GameObject.
/// </summary>
public class InactivityWarning : MonoBehaviour
{
    public static InactivityWarning Instance;

    [Header("Settings")]
    [Tooltip("Time in seconds before showing the warning (default: 180 = 3 minutes)")]
    public float inactivityThreshold = 180f;

    [Header("References")]
    [Tooltip("The warning panel to show/hide (leave empty to use the GameObject this script is attached to)")]
    public GameObject warningPanel;

    [Tooltip("Optional: FluidSim2D reference to subscribe to particle spawn events")]
    public FluidSim2D fluidSimulation;

    private float lastActivityTime;
    private bool isWarningVisible = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple InactivityWarning instances found. Only one should exist.");
        }
    }

    void Start()
    {
        // If no warning panel is specified, use the GameObject this script is attached to
        if (warningPanel == null)
        {
            warningPanel = gameObject;
        }

        // Start tracking time
        ResetActivityTimer();

        // Hide warning panel initially
        SetWarningVisible(false);

        // Subscribe to FluidSim2D particle spawn events if reference is provided
        if (fluidSimulation != null)
        {
            fluidSimulation.SimulationStepCompleted += OnSimulationStep;
        }
    }

    void Update()
    {
        // Check if inactivity threshold has been exceeded
        float timeSinceLastActivity = Time.time - lastActivityTime;

        if (timeSinceLastActivity >= inactivityThreshold)
        {
            // Show warning if not already visible
            if (!isWarningVisible)
            {
                SetWarningVisible(true);
            }
        }
        else
        {
            // Hide warning if it's visible
            if (isWarningVisible)
            {
                SetWarningVisible(false);
            }
        }
    }

    /// <summary>
    /// Call this method when particles are spawned to reset the inactivity timer
    /// </summary>
    public void RegisterActivity()
    {
        lastActivityTime = Time.time;

        // Hide warning immediately when activity is detected
        if (isWarningVisible)
        {
            SetWarningVisible(false);
        }
    }

    /// <summary>
    /// Resets the activity timer (e.g., on game start or after clearing)
    /// </summary>
    public void ResetActivityTimer()
    {
        lastActivityTime = Time.time;
    }

    /// <summary>
    /// Shows or hides the warning panel
    /// </summary>
    private void SetWarningVisible(bool visible)
    {
        if (warningPanel != null)
        {
            warningPanel.SetActive(visible);
            isWarningVisible = visible;

            if (visible)
            {
                Debug.Log("[InactivityWarning] Warning displayed - No activity detected for 3 minutes");
            }
            else
            {
                Debug.Log("[InactivityWarning] Warning hidden - Activity detected");
            }
        }
    }

    /// <summary>
    /// Called when FluidSim2D completes a simulation step (if subscribed)
    /// This is optional and depends on how particle spawning is tracked
    /// </summary>
    private void OnSimulationStep()
    {
        // This gets called every simulation step, not just when new particles spawn
        // So we don't use it for activity tracking - SpawnOnContact should call RegisterActivity instead
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (fluidSimulation != null)
        {
            fluidSimulation.SimulationStepCompleted -= OnSimulationStep;
        }
    }

    /// <summary>
    /// Public method to manually show/hide the warning (for testing or external control)
    /// </summary>
    public void SetWarning(bool show)
    {
        SetWarningVisible(show);
    }
}
