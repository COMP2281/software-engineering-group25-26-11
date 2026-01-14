using UnityEngine;

public class DropPaintBall : MonoBehaviour
{
    [Header("Prefab Settings")]
    [Tooltip("The default paintball prefab to spawn (assign from Assets/Prefabs/Default Paintball)")]
    public GameObject defaultPaintballPrefab;

    [Header("References")]
    [Tooltip("Reference to the ColorSelectionManager to get water surface")]
    public ColorSelectionManager colorSelectionManager;

    [Header("Spawn Settings")]
    [Tooltip("Height above the camera/player to spawn the ball")]
    public float spawnHeightAbovePlayer = 0.0f;

    [Tooltip("Forward distance from camera")]
    public float forwardDistance = 0.7f;

    /// <summary>
    /// Called by UI Button OnClick event to drop a default paintball
    /// </summary>
    public void DropDefaultPaintball()
    {
        Debug.Log("DropDefaultPaintball called!");
        
        if (defaultPaintballPrefab == null)
        {
            Debug.LogError("DropPaintBall: Default Paintball Prefab is not assigned!");
            return;
        }

        Debug.Log("Prefab is assigned");

        // Find the main camera (player's view)
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("DropPaintBall: Main Camera not found!");
            return;
        }

        Debug.Log($"Camera found at: {mainCamera.transform.position}");

        // Spawn above and in front of the player's view
        Vector3 spawnPosition = mainCamera.transform.position 
            + Vector3.up * spawnHeightAbovePlayer 
            + mainCamera.transform.forward * forwardDistance;

        Debug.Log($"Spawning at: {spawnPosition}");

        // Instantiate the default paintball
        GameObject newPaintball = Instantiate(defaultPaintballPrefab, spawnPosition, Quaternion.identity);
        newPaintball.name = "Dropped Paintball";

        // Make the ball MUCH bigger so we can definitely see it
        newPaintball.transform.localScale = Vector3.one * 1.0f;

        // Force the renderer on and create a bright material
        MeshRenderer ballRenderer = newPaintball.GetComponent<MeshRenderer>();
        if (ballRenderer != null)
        {
            ballRenderer.enabled = true;
            
            // Try Unlit/Color shader which should always be visible
            Material newMaterial = new Material(Shader.Find("Unlit/Color"));
            if (newMaterial.shader == null)
            {
                // Fallback to Standard
                newMaterial = new Material(Shader.Find("Standard"));
            }
            
            newMaterial.color = new Color(1f, 0f, 0f, 1f); // Bright red, fully opaque
            ballRenderer.material = newMaterial;
            ballRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            
            Debug.Log($"Ball renderer enabled: {ballRenderer.enabled}, Shader: {newMaterial.shader.name}");
        }
        else
        {
            Debug.LogError("No MeshRenderer found on paintball!");
        }

        Debug.Log($"Paintball instantiated: {newPaintball != null}, Scale: {newPaintball.transform.localScale}");

        // Assign water surface reference from the ColorSelectionManager
        if (colorSelectionManager != null && colorSelectionManager.waterSurface != null)
        {
            var collision = newPaintball.GetComponent<PaintballCollision>();
            if (collision != null)
            {
                collision.waterSurface = colorSelectionManager.waterSurface;
            }
        }

        Debug.Log($"Dropped default paintball at {spawnPosition}");
    }
}
