using UnityEngine;
using System.Collections.Generic;

public class ColorSelectionManager : MonoBehaviour
{
    [Header("Settings")]
    public int maxSelectableColors = 3;
    public GameObject ballPrefab;
    public Transform spawnBasePoint;
    public float ballSpacing = 0.5f;

    private int totalBallCount = 0;

    // Track used colors
    private HashSet<Color32> usedColors = new HashSet<Color32>();

    // Store original positions for respawn
    public static Dictionary<Color32, Queue<Vector3>> colorToSpawnQueue = new Dictionary<Color32, Queue<Vector3>>();

    public bool CanSpawn()
    {
        return totalBallCount < maxSelectableColors;
    }

    public void HandleColorSelection(Color color)
    {
        Color32 colorKey = (Color32)color;

        //  Check for duplicates
        if (usedColors.Contains(colorKey))
        {
            Debug.LogWarning($"Color {colorKey} has already been selected. Cannot select the same color again.");
            // You can also trigger a UI popup here
            return;
        }

        // Check total count
        if (!CanSpawn())
        {
            Debug.Log("Max colors selected.");
            return;
        }

        // Spawn position
        Vector3 spawnOffset = new Vector3(totalBallCount * ballSpacing, 0.2f, 0);
        Vector3 spawnPosition = spawnBasePoint.position + spawnOffset;

        //  Create and color ball
        GameObject newBall = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
        newBall.name = $"Paintball_{colorKey}";

        Renderer ballRenderer = newBall.GetComponent<Renderer>();
        if (ballRenderer != null)
        {
            ballRenderer.material.color = color;
        }
        else
        {
            Debug.LogWarning("Spawned ball missing Renderer component.");
        }

        //  Save position for respawn
        if (!colorToSpawnQueue.ContainsKey(colorKey))
        {
            colorToSpawnQueue[colorKey] = new Queue<Vector3>();
        }
        colorToSpawnQueue[colorKey].Enqueue(spawnPosition);

        //  Add color to used set
        usedColors.Add(colorKey);
        totalBallCount++;

        Debug.Log($"Spawned new paintball ({colorKey}) at {spawnPosition}");
    }

    public void ResetBalls()
    {
        foreach (Transform child in spawnBasePoint)
        {
            Destroy(child.gameObject);
        }

        totalBallCount = 0;
        usedColors.Clear();
        colorToSpawnQueue.Clear();

        Debug.Log("All paintballs reset.");
    }
}
