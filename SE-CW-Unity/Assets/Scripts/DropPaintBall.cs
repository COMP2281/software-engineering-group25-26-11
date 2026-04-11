using UnityEngine;

public class DropPaintBall : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the ColorSelectionManager to get the correct paintball prefab and water surface")]
    public ColorSelectionManager colorSelectionManager;

    [Header("Spawn Settings")]
    [Tooltip("The spawn point transform where paintballs will be dropped")]
    public Transform spawnPoint;

    [Header("Default Color")]
    [Tooltip("The default color for dropped paintballs")]
    public Color defaultColor = Color.red;

    /// <summary>
    /// Called by UI Button OnClick event to drop a default paintball
    /// </summary>
    public void DropDefaultPaintball()
    {
        // Get prefab from ColorSelectionManager (same one used by palette)
        if (colorSelectionManager == null)
        {
            Debug.LogError("DropPaintBall: ColorSelectionManager is not assigned!");
            return;
        }

        if (colorSelectionManager.ballPrefab == null)
        {
            Debug.LogError("DropPaintBall: ballPrefab is not assigned in ColorSelectionManager!");
            return;
        }

        // Determine spawn position
        Vector3 spawnPosition;
        
        if (spawnPoint != null)
        {
            spawnPosition = spawnPoint.position;
        }
        else
        {
            // Fallback: try to find PaintballRespawnPoint in the scene
            GameObject respawnObj = GameObject.Find("PaintballRespawnPoint");
            if (respawnObj != null)
            {
                spawnPosition = respawnObj.transform.position;
            }
            else
            {
                Debug.LogError("DropPaintBall: No spawn point assigned and PaintballRespawnPoint not found in scene!");
                return;
            }
        }

        // Instantiate using the same prefab as ColorSelectionManager
        GameObject newPaintball = Instantiate(colorSelectionManager.ballPrefab, spawnPosition, Quaternion.identity);
        newPaintball.name = "Dropped_Paintball";

        // Set the paintball color
        Renderer ballRenderer = newPaintball.GetComponent<Renderer>();
        if (ballRenderer != null)
        {
            ballRenderer.material.color = defaultColor;
        }

        // Assign water surface reference from ColorSelectionManager
        if (colorSelectionManager.waterSurface != null)
        {
            var collision = newPaintball.GetComponent<PaintballCollision>();
            if (collision != null)
            {
                collision.waterSurface = colorSelectionManager.waterSurface;
            }
        }

        // Enable physics so the ball drops
        Rigidbody rb = newPaintball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;
        }

        Collider col = newPaintball.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }

        Debug.Log($"Dropped default paintball at {spawnPosition}");
    }
}
