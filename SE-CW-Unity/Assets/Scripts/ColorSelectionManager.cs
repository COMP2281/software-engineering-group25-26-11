using UnityEngine;
using System.Collections.Generic;

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

    [Header("UI References")]
    [Tooltip("The color selection panel that opens when a button is clicked")]
    public GameObject colorSelectionPanel;

    [Header("Water")]
    public Transform waterSurface;   // assign WaterCube from the scene in Inspector

    private int totalBallCount = 0;
    private int pendingButtonIndex = -1; // Tracks which button was clicked
    private Color pendingColor;

    // Track which paintball GameObject belongs to which button
    private Dictionary<int, GameObject> buttonToPaintball = new Dictionary<int, GameObject>();

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
        Debug.Log($"Color Panel: {(colorSelectionPanel != null ? colorSelectionPanel.name : "NOT ASSIGNED")}");
        Debug.Log($"Ball Prefab: {(ballPrefab != null ? ballPrefab.name : "NOT ASSIGNED")}");
        Debug.Log("===================================");
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
        if (!CanSpawn())
        {
            Debug.Log("Max colors selected. Cannot spawn more paintballs.");
            return;
        }

        pendingButtonIndex = buttonIndex;
        
        if (colorSelectionPanel != null)
        {
            colorSelectionPanel.SetActive(true);
            Debug.Log($"Color panel opened for Button {buttonIndex}");
        }
        else
        {
            Debug.LogError("Color selection panel is not assigned in the Inspector!");
        }
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
        
        pendingButtonIndex = -1;
        Debug.Log("Color selection cancelled.");
    }

    private void HandleColorSelection(Color color, int buttonIndex)
    {
        Debug.Log($"HandleColorSelection called with buttonIndex: {buttonIndex}, color: {color}");
        
        Color32 colorKey = (Color32)color;

        // Check if this button already has a paintball - if so, destroy it
        if (buttonToPaintball.ContainsKey(buttonIndex) && buttonToPaintball[buttonIndex] != null)
        {
            GameObject oldBall = buttonToPaintball[buttonIndex];
            
            // Get the old color and remove it from used colors
            Renderer oldRenderer = oldBall.GetComponent<Renderer>();
            if (oldRenderer != null)
            {
                Color32 oldColorKey = (Color32)oldRenderer.material.color;
                usedColors.Remove(oldColorKey);
                
                // Remove from colorToSpawnQueue if it exists
                if (colorToSpawnQueue.ContainsKey(oldColorKey))
                {
                    colorToSpawnQueue.Remove(oldColorKey);
                }
            }
            
            Destroy(oldBall);
            totalBallCount--;
            Debug.Log($"Destroyed old paintball at Button {buttonIndex}");
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

        Debug.Log($"Spawned paintball ({colorKey}) at Button {buttonIndex} spawn point: {spawnPosition}");
    }

    /// <summary>
    /// Returns the spawn point Transform for the specified button index
    /// </summary>
    private Transform GetSpawnPointForButton(int buttonIndex)
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

        Debug.Log("All paintballs reset.");
    }
}
