using UnityEngine;
using System.Collections.Generic;
using Seb.Fluid2D.Simulation;

public class ColorSelectionManager : MonoBehaviour
{
    [Header("Settings")]
    public int maxSelectableColors = 5;
    public GameObject ballPrefab;
    
    [Header("Spawn Points for 5 Buttons")]
    [Tooltip("Spawn point for Button 1")]
    public Transform spawnPoint1;
    [Tooltip("Spawn point for Button 2")]
    public Transform spawnPoint2;
    [Tooltip("Spawn point for Button 3")]
    public Transform spawnPoint3;
    [Tooltip("Spawn point for Button 4")]
    public Transform spawnPoint4;
    [Tooltip("Spawn point for Button 5")]
    public Transform spawnPoint5;

    [Header("Button UI References")]
    [Tooltip("Button 1 GameObject (to change its color)")]
    public GameObject button1;
    [Tooltip("Button 2 GameObject (to change its color)")]
    public GameObject button2;
    [Tooltip("Button 3 GameObject (to change its color)")]
    public GameObject button3;
    [Tooltip("Button 4 GameObject (to change its color)")]
    public GameObject button4;
    [Tooltip("Button 5 GameObject (to change its color)")]
    public GameObject button5;

    [Header("UI References")]
    [Tooltip("The color selection panel that opens when a button is clicked")]
    public GameObject colorSelectionPanel;

    [Header("Water")]
    public Transform waterSurface;   // assign WaterCube from the scene in Inspector

    [Header("Simulation Control")]
    [Tooltip("The FluidSim2D to pause when selecting colors")]
    public FluidSim2D fluidSimulation;
    [Tooltip("Parent GameObject containing ripple effects (disabled when selecting colors)")]
    public GameObject rippleEffectsParent;

    private int totalBallCount = 0;
    private int pendingButtonIndex = -1; // Tracks which button was clicked
    private Color pendingColor;
    private bool wasSimulationPausedBefore = false;

    // Track which paintball GameObject belongs to which button
    public static Dictionary<int, GameObject> buttonToPaintball = new Dictionary<int, GameObject>();

    // Track the confirmed color for each button (for respawning with correct color)
    private Dictionary<int, Color> buttonConfirmedColor = new Dictionary<int, Color>();

    // Track used colors
    private HashSet<Color32> usedColors = new HashSet<Color32>();

    // Store original positions for respawn
    public static Dictionary<Color32, Queue<Vector3>> colorToSpawnQueue =
        new Dictionary<Color32, Queue<Vector3>>();

    void Start()
    {
        // Validate setup
        Debug.Log("=== ColorSelectionManager Setup ===");
        Debug.Log($"Spawn Point 1: {(spawnPoint1 != null ? spawnPoint1.name : "NOT ASSIGNED")}");
        Debug.Log($"Spawn Point 2: {(spawnPoint2 != null ? spawnPoint2.name : "NOT ASSIGNED")}");
        Debug.Log($"Spawn Point 3: {(spawnPoint3 != null ? spawnPoint3.name : "NOT ASSIGNED")}");
        Debug.Log($"Spawn Point 4: {(spawnPoint4 != null ? spawnPoint4.name : "NOT ASSIGNED")}");
        Debug.Log($"Spawn Point 5: {(spawnPoint5 != null ? spawnPoint5.name : "NOT ASSIGNED")}");
        Debug.Log($"Button 1: {(button1 != null ? button1.name : "NOT ASSIGNED")}");
        Debug.Log($"Button 2: {(button2 != null ? button2.name : "NOT ASSIGNED")}");
        Debug.Log($"Button 3: {(button3 != null ? button3.name : "NOT ASSIGNED")}");
        Debug.Log($"Button 4: {(button4 != null ? button4.name : "NOT ASSIGNED")}");
        Debug.Log($"Button 5: {(button5 != null ? button5.name : "NOT ASSIGNED")}");
        Debug.Log($"Color Panel: {(colorSelectionPanel != null ? colorSelectionPanel.name : "NOT ASSIGNED")}");
        Debug.Log($"Ball Prefab: {(ballPrefab != null ? ballPrefab.name : "NOT ASSIGNED")}");
        Debug.Log("===================================");

        // Spawn initial white paintballs at all 5 positions
        SpawnInitialWhiteBalls();
    }

    /// <summary>
    /// Spawns white placeholder paintballs at all 5 spawn points at game start
    /// </summary>
    private void SpawnInitialWhiteBalls()
    {
        if (ballPrefab == null)
        {
            Debug.LogWarning("Cannot spawn initial white balls - ballPrefab is not assigned");
            return;
        }

        for (int buttonIndex = 1; buttonIndex <= 5; buttonIndex++)
        {
            Transform spawnPoint = GetSpawnPointForButton(buttonIndex);
            if (spawnPoint == null)
            {
                Debug.LogWarning($"Cannot spawn initial ball for Button {buttonIndex} - spawn point not assigned");
                continue;
            }

            // Use the unified SpawnPlaceholder method with white color
            SpawnPlaceholder(buttonIndex, Color.white);
        }

        Debug.Log("Initial white paintballs spawned at all 5 positions");
    }

    public bool CanSpawn()
    {
        return totalBallCount < maxSelectableColors;
    }

    /// <summary>
    /// Called by Button 1 - Opens color selection panel
    /// </summary>
    public void OnButton1Clicked()
    {
        OpenColorPanel(1);
    }

    /// <summary>
    /// Called by Button 2 - Opens color selection panel
    /// </summary>
    public void OnButton2Clicked()
    {
        OpenColorPanel(2);
    }

    /// <summary>
    /// Called by Button 3 - Opens color selection panel
    /// </summary>
    public void OnButton3Clicked()
    {
        OpenColorPanel(3);
    }

    /// <summary>
    /// Called by Button 4 - Opens color selection panel
    /// </summary>
    public void OnButton4Clicked()
    {
        OpenColorPanel(4);
    }

    /// <summary>
    /// Called by Button 5 - Opens color selection panel
    /// </summary>
    public void OnButton5Clicked()
    {
        OpenColorPanel(5);
    }

    /// <summary>
    /// Opens the color selection panel and stores which button was clicked
    /// </summary>
    private void OpenColorPanel(int buttonIndex)
    {
        // Use the confirmed color for this button (or white if never confirmed)
        Color placeholderColor = buttonConfirmedColor.GetValueOrDefault(buttonIndex, Color.white);

        // Check if this button already has a paintball tracked - if so, destroy it
        if (buttonToPaintball.ContainsKey(buttonIndex) && buttonToPaintball[buttonIndex] != null)
        {
            GameObject oldBall = buttonToPaintball[buttonIndex];
            
            // Get the old color for cleanup
            Renderer oldRenderer = oldBall.GetComponent<Renderer>();
            if (oldRenderer != null)
            {
                Color oldColor = oldRenderer.material.color;
                Color32 oldColorKey = (Color32)oldColor;
                
                // Remove from used colors only if not white
                if (oldColor != Color.white)
                {
                    usedColors.Remove(oldColorKey);
                    
                    // Only decrement for colored balls
                    if (totalBallCount > 0)
                    {
                        totalBallCount--;
                    }
                }
            }
            
            DestroyImmediate(oldBall);
            buttonToPaintball.Remove(buttonIndex);
        }

        // Spawn a placeholder ball at this position with the confirmed color
        SpawnPlaceholder(buttonIndex, placeholderColor);
        
        // Reset button color to match the confirmed color
        UpdateButtonColor(buttonIndex, placeholderColor);

        if (!CanSpawn())
        {
            Debug.Log("Max colors selected. Cannot spawn more paintballs.");
            return;
        }

        pendingButtonIndex = buttonIndex;
        
        // Pause simulation and disable ripple effects when opening color panel
        if (fluidSimulation != null)
        {
            wasSimulationPausedBefore = fluidSimulation.IsPaused;
            fluidSimulation.SetPaused(true);
        }
        
        if (rippleEffectsParent != null)
        {
            rippleEffectsParent.SetActive(false);
        }
        
        if (colorSelectionPanel != null)
        {
            colorSelectionPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("Color selection panel is not assigned in the Inspector!");
        }
    }

    /// <summary>
    /// Spawns a placeholder paintball at the specified button's spawn point with the given color
    /// </summary>
    private void SpawnPlaceholder(int buttonIndex, Color color)
    {
        if (ballPrefab == null)
        {
            Debug.LogWarning("Cannot spawn placeholder - ballPrefab is not assigned");
            return;
        }

        Transform spawnPoint = GetSpawnPointForButton(buttonIndex);
        if (spawnPoint == null)
        {
            Debug.LogWarning($"Cannot spawn placeholder for Button {buttonIndex} - spawn point not assigned");
            return;
        }

        Vector3 spawnPosition = spawnPoint.position;

        // Create paintball with specified color
        GameObject newBall = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
        newBall.name = $"Paintball_Placeholder_Button{buttonIndex}";

        // Set color
        Renderer ballRenderer = newBall.GetComponent<Renderer>();
        if (ballRenderer != null)
        {
            ballRenderer.material.color = color;
        }

        // Make it kinematic (anchored)
        Rigidbody ballRb = newBall.GetComponent<Rigidbody>();
        if (ballRb != null)
        {
            ballRb.isKinematic = true;
            ballRb.useGravity = true;
        }

        // Inject WaterCube reference
        var collision = newBall.GetComponent<PaintballCollision>();
        if (collision != null)
        {
            collision.waterSurface = waterSurface;
        }

        var spawnOnContact = newBall.GetComponent<SpawnOnContact>();
        if (spawnOnContact != null)
        {
            spawnOnContact.paintballPrefab = ballPrefab;
        }

        // Add spawn position to respawn queue (so placeholder can respawn after water contact)
        Color32 colorKey = (Color32)color;
        if (!colorToSpawnQueue.ContainsKey(colorKey))
        {
            colorToSpawnQueue[colorKey] = new Queue<Vector3>();
        }
        colorToSpawnQueue[colorKey].Enqueue(spawnPosition);

        // Track this paintball
        buttonToPaintball[buttonIndex] = newBall;

        Debug.Log($"Spawned placeholder ({color}) at Button {buttonIndex} spawn point: {spawnPosition}");
    }

    /// <summary>
    /// Called by the color picker UI when a color is selected (but not yet confirmed)
    /// </summary>
    public void OnColorPicked(Color color)
    {
        pendingColor = color;
        Debug.Log($"Color picked: {color}, pending button: {pendingButtonIndex}");
    }

    /// <summary>
    /// Called by the Confirm button in the color selection panel
    /// </summary>
    public void OnColorConfirmed()
    {
        Debug.Log($"OnColorConfirmed called - pendingButtonIndex: {pendingButtonIndex}, pendingColor: {pendingColor}");
        
        if (pendingButtonIndex == -1)
        {
            Debug.LogWarning("No button was clicked before confirming color!");
            return;
        }

        HandleColorSelection(pendingColor, pendingButtonIndex);
        
        // Close the panel
        if (colorSelectionPanel != null)
        {
            colorSelectionPanel.SetActive(false);
        }

        // Resume simulation and ripple effects if they weren't paused before
        if (fluidSimulation != null && !wasSimulationPausedBefore)
        {
            fluidSimulation.SetPaused(false);
        }
        
        if (rippleEffectsParent != null && !wasSimulationPausedBefore)
        {
            rippleEffectsParent.SetActive(true);
        }

        // Reset pending state
        pendingButtonIndex = -1;
    }

    /// <summary>
    /// Called by the Cancel button to close the panel without spawning
    /// </summary>
    public void OnColorCancelled()
    {
        if (colorSelectionPanel != null)
        {
            colorSelectionPanel.SetActive(false);
        }
        
        // Resume simulation and ripple effects if they weren't paused before
        if (fluidSimulation != null && !wasSimulationPausedBefore)
        {
            fluidSimulation.SetPaused(false);
        }
        
        if (rippleEffectsParent != null && !wasSimulationPausedBefore)
        {
            rippleEffectsParent.SetActive(true);
        }
        
        pendingButtonIndex = -1;
        Debug.Log("Color selection cancelled.");
    }

    private void HandleColorSelection(Color color, int buttonIndex)
    {
        Color32 colorKey = (Color32)color;

        // Destroy the placeholder if it exists in tracking
        if (buttonToPaintball.ContainsKey(buttonIndex) && buttonToPaintball[buttonIndex] != null)
        {
            DestroyImmediate(buttonToPaintball[buttonIndex]);
            buttonToPaintball.Remove(buttonIndex);
        }

        // Check for duplicates with other buttons
        if (usedColors.Contains(colorKey))
        {
            Debug.LogWarning($"Color {colorKey} is already used by another button. Cannot select the same color again.");
            return;
        }

        // Check total count
        if (!CanSpawn())
        {
            Debug.Log("Max colors selected.");
            return;
        }

        // Get the spawn point for this button
        Transform spawnPoint = GetSpawnPointForButton(buttonIndex);
        if (spawnPoint == null)
        {
            Debug.LogError($"Spawn point for Button {buttonIndex} is not assigned! Check Inspector.");
            return;
        }

        Debug.Log($"Using spawn point: {spawnPoint.name} at position: {spawnPoint.position}");
        
        Vector3 spawnPosition = spawnPoint.position;

        // Create and color ball
        GameObject newBall = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
        newBall.name = $"Paintball_{colorKey}_Button{buttonIndex}";

        Renderer ballRenderer = newBall.GetComponent<Renderer>();
        if (ballRenderer != null)
        {
            ballRenderer.material.color = color;
        }
        else
        {
            Debug.LogWarning("Spawned ball missing Renderer component.");
        }

        // Make the paintball kinematic (anchored) until grabbed
        Rigidbody ballRb = newBall.GetComponent<Rigidbody>();
        if (ballRb != null)
        {
            ballRb.isKinematic = true;
            ballRb.useGravity = true;
            Debug.Log($"ColorSelectionManager: Set {newBall.name} Rigidbody to kinematic. isKinematic={ballRb.isKinematic}");
        }
        else
        {
            Debug.LogError($"ColorSelectionManager: {newBall.name} is missing Rigidbody component!");
        }

        // Check for XRGrabInteractable
        var grabInteractable = newBall.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable>();
        if (grabInteractable == null)
        {
            Debug.LogError($"ColorSelectionManager: {newBall.name} is missing XRGrabInteractable component! Add it to the prefab.");
        }
        else
        {
            Debug.Log($"ColorSelectionManager: {newBall.name} has XRGrabInteractable component");
        }

        // Inject WaterCube into the new ball
        var collision = newBall.GetComponent<PaintballCollision>();
        if (collision != null)
        {
            collision.waterSurface = waterSurface;
        }
        else
        {
            Debug.LogWarning("Spawned ball missing PaintballCollision component.");
        }

        // Save position for respawn
        if (!colorToSpawnQueue.ContainsKey(colorKey))
        {
            colorToSpawnQueue[colorKey] = new Queue<Vector3>();
        }
        colorToSpawnQueue[colorKey].Enqueue(spawnPosition);

        // Track this paintball for this button
        buttonToPaintball[buttonIndex] = newBall;

        // Add color to used set
        usedColors.Add(colorKey);
        totalBallCount++;

        // Update the button's visual color to match the paintball
        UpdateButtonColor(buttonIndex, color);

        // Store this as the confirmed color for this button (for respawning)
        buttonConfirmedColor[buttonIndex] = color;

        Debug.Log($"Spawned paintball ({colorKey}) at Button {buttonIndex} spawn point: {spawnPosition}, confirmed color saved");
    }

    /// <summary>
    /// Returns the spawn point Transform for the specified button index
    /// </summary>
    public Transform GetSpawnPointForButton(int buttonIndex)
    {
        switch (buttonIndex)
        {
            case 1: return spawnPoint1;
            case 2: return spawnPoint2;
            case 3: return spawnPoint3;
            case 4: return spawnPoint4;
            case 5: return spawnPoint5;
            default:
                Debug.LogError($"Invalid button index: {buttonIndex}");
                return null;
        }
    }

    /// <summary>
    /// Returns the spawn position for the specified button index
    /// </summary>
    public Vector3 GetSpawnPositionForButton(int buttonIndex)
    {
        Transform spawnPoint = GetSpawnPointForButton(buttonIndex);
        if (spawnPoint != null)
        {
            return spawnPoint.position;
        }
        return Vector3.zero;
    }

    /// <summary>
    /// Returns the button GameObject for the specified button index
    /// </summary>
    private GameObject GetButtonForIndex(int buttonIndex)
    {
        switch (buttonIndex)
        {
            case 1: return button1;
            case 2: return button2;
            case 3: return button3;
            case 4: return button4;
            case 5: return button5;
            default:
                Debug.LogError($"Invalid button index: {buttonIndex}");
                return null;
        }
    }

    /// <summary>
    /// Updates the button's visual color to match the paintball
    /// </summary>
    private void UpdateButtonColor(int buttonIndex, Color color)
    {
        GameObject button = GetButtonForIndex(buttonIndex);
        if (button == null)
        {
            Debug.LogWarning($"Button {buttonIndex} not assigned, cannot update color");
            return;
        }

        // Try to find Image component (UI)
        UnityEngine.UI.Image imageComponent = button.GetComponent<UnityEngine.UI.Image>();
        if (imageComponent != null)
        {
            imageComponent.color = color;
            Debug.Log($"Updated Button {buttonIndex} UI Image color to {color}");
            return;
        }

        // Try to find Renderer component (3D objects)
        Renderer renderer = button.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
            Debug.Log($"Updated Button {buttonIndex} Renderer color to {color}");
            return;
        }

        Debug.LogWarning($"Button {buttonIndex} has no Image or Renderer component to color");
    }

    /// <summary>
    /// Legacy method for backward compatibility.
    /// Spawns a ball at the first available spawn point.
    /// </summary>
    public void HandleColorSelection(Color color)
    {
        // Find first available spawn point
        int buttonIndex = 1;
        for (int i = 1; i <= 5; i++)
        {
            if (GetSpawnPointForButton(i) != null)
            {
                buttonIndex = i;
                break;
            }
        }
        
        HandleColorSelection(color, buttonIndex);
    }

    public void ResetBalls()
    {
        // Find all paintballs by name pattern or tag
        GameObject[] allPaintballs = GameObject.FindGameObjectsWithTag("Paintball");
        
        // If no tag is set, find by name pattern
        if (allPaintballs.Length == 0)
        {
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.StartsWith("Paintball_"))
                {
                    Destroy(obj);
                }
            }
        }
        else
        {
            foreach (GameObject paintball in allPaintballs)
            {
                Destroy(paintball);
            }
        }

        totalBallCount = 0;
        usedColors.Clear();
        colorToSpawnQueue.Clear();
        buttonToPaintball.Clear();
        buttonConfirmedColor.Clear(); // Clear confirmed colors so buttons reset to white

        // Reset accuracy tracking
        if (Accuracy.Instance != null)
        {
            Accuracy.Instance.ResetAccuracy();
        }
        
        // Reset inactivity timer
        if (InactivityWarning.Instance != null)
        {
            InactivityWarning.Instance.ResetActivityTimer();
        }

        // Reset all button colors to white (or default)
        for (int i = 1; i <= 5; i++)
        {
            UpdateButtonColor(i, Color.white);
        }

        Debug.Log("All paintballs reset.");

        // Respawn initial white paintballs
        SpawnInitialWhiteBalls();
    }
}
