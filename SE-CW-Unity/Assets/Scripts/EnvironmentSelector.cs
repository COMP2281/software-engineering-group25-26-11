using UnityEngine;
using TMPro;

public class EnvironmentSelector : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("The text label that displays the current environment name")]
    public TextMeshProUGUI environmentLabel;

    [Header("Cube Barrier")]
    [Tooltip("Reference to the BarrierVisibilityController")]
    public BarrierVisibilityController barrierController;
    
    [Header("Terrain Options")]
    [Tooltip("Add terrain GameObjects here. They will appear after 'None' and 'Cube' options")]
    public GameObject[] terrainObjects;

    private int currentIndex = 0;
    private int totalOptions;
    private GameObject currentActiveTerrain = null;

    void Start()
    {
        // Total options = None + Cube + (None + each terrain) + (Cube + each terrain)
        // = 2 + (2 * terrainCount)
        int terrainCount = (terrainObjects != null ? terrainObjects.Length : 0);
        totalOptions = 2 + (2 * terrainCount);

        // Deactivate all terrains at start
        DeactivateAllTerrains();

        // Ensure cube starts as transparent (overrides any other initialization)
        EnsureCubeTransparent();

        // Set to "None" option by default (index 0 = None)
        currentIndex = 0;
        UpdateEnvironment();
        
        Debug.Log("EnvironmentSelector initialized to 'None' - transparent cube, no terrain");
    }

    /// <summary>
    /// Called by the Back button - cycles to the previous environment
    /// </summary>
    public void OnBackButtonClicked()
    {
        currentIndex--;
        if (currentIndex < 0)
        {
            currentIndex = totalOptions - 1; // Wrap to last option
        }
        UpdateEnvironment();
    }

    /// <summary>
    /// Called by the Forward button - cycles to the next environment
    /// </summary>
    public void OnForwardButtonClicked()
    {
        currentIndex++;
        if (currentIndex >= totalOptions)
        {
            currentIndex = 0; // Wrap to first option
        }
        UpdateEnvironment();
    }

    /// <summary>
    /// Updates the environment based on current selection
    /// </summary>
    private void UpdateEnvironment()
    {
        // Deactivate previous terrain if one was active
        if (currentActiveTerrain != null)
        {
            currentActiveTerrain.SetActive(false);
            currentActiveTerrain = null;
        }

        int terrainCount = (terrainObjects != null ? terrainObjects.Length : 0);

        if (currentIndex == 0)
        {
            // None - transparent cube, no terrain
            SetEnvironmentNone();
        }
        else if (currentIndex == 1)
        {
            // Cube - opaque cube, no terrain
            SetEnvironmentCube();
        }
        else if (currentIndex >= 2 && currentIndex < 2 + terrainCount)
        {
            // None + Terrain - transparent cube with terrain
            int terrainIndex = currentIndex - 2;
            SetEnvironmentNoneAndTerrain(terrainIndex);
        }
        else if (currentIndex >= 2 + terrainCount && currentIndex < 2 + (2 * terrainCount))
        {
            // Cube + Terrain - opaque cube with terrain
            int terrainIndex = currentIndex - (2 + terrainCount);
            SetEnvironmentCubeAndTerrain(terrainIndex);
        }

        UpdateLabel();
        Debug.Log($"Environment changed to: {GetCurrentEnvironmentName()}");
    }

    /// <summary>
    /// Sets environment to "None" - transparent cube
    /// </summary>
    private void SetEnvironmentNone()
    {
        if (barrierController != null)
        {
            // Ensure cube is transparent
            if (barrierController.GetComponent<BarrierVisibilityController>() != null)
            {
                // Check current state and set to transparent if needed
                EnsureCubeTransparent();
            }
        }
    }

    /// <summary>
    /// Sets environment to "Cube" - opaque cube
    /// </summary>
    private void SetEnvironmentCube()
    {
        if (barrierController != null)
        {
            // Ensure cube is opaque
            EnsureCubeOpaque();
        }
    }

    /// <summary>
    /// Sets environment to "None + Terrain" - transparent cube with terrain
    /// </summary>
    private void SetEnvironmentNoneAndTerrain(int terrainIndex)
    {
        if (terrainObjects == null || terrainIndex < 0 || terrainIndex >= terrainObjects.Length)
        {
            Debug.LogWarning($"Terrain index {terrainIndex} is out of bounds.");
            return;
        }

        // Set cube to transparent
        if (barrierController != null)
        {
            EnsureCubeTransparent();
        }

        // Activate the selected terrain
        GameObject selectedTerrain = terrainObjects[terrainIndex];
        if (selectedTerrain != null)
        {
            selectedTerrain.SetActive(true);
            currentActiveTerrain = selectedTerrain;
        }
        else
        {
            Debug.LogWarning($"Terrain at index {terrainIndex} is null.");
        }
    }

    /// <summary>
    /// Sets environment to "Cube + Terrain" - opaque cube with terrain
    /// </summary>
    private void SetEnvironmentCubeAndTerrain(int terrainIndex)
    {
        if (terrainObjects == null || terrainIndex < 0 || terrainIndex >= terrainObjects.Length)
        {
            Debug.LogWarning($"Terrain index {terrainIndex} is out of bounds.");
            return;
        }

        // Set cube to opaque
        if (barrierController != null)
        {
            EnsureCubeOpaque();
        }

        // Activate the selected terrain
        GameObject selectedTerrain = terrainObjects[terrainIndex];
        if (selectedTerrain != null)
        {
            selectedTerrain.SetActive(true);
            currentActiveTerrain = selectedTerrain;
        }
        else
        {
            Debug.LogWarning($"Terrain at index {terrainIndex} is null.");
        }
    }

    /// <summary>
    /// Ensures the cube is in transparent state
    /// </summary>
    private void EnsureCubeTransparent()
    {
        if (barrierController == null) return;

        // Access private isOpaque field through reflection or use public method
        // Since we can't access isOpaque directly, we'll set materials directly
        if (barrierController.transparentMat != null && barrierController.barrierRenderers != null)
        {
            foreach (var r in barrierController.barrierRenderers)
            {
                if (r != null)
                {
                    r.sharedMaterial = barrierController.transparentMat;
                }
            }
        }
    }

    /// <summary>
    /// Ensures the cube is in opaque state
    /// </summary>
    private void EnsureCubeOpaque()
    {
        if (barrierController == null) return;

        if (barrierController.opaqueMat != null && barrierController.barrierRenderers != null)
        {
            foreach (var r in barrierController.barrierRenderers)
            {
                if (r != null)
                {
                    r.sharedMaterial = barrierController.opaqueMat;
                }
            }
        }
    }

    /// <summary>
    /// Deactivates all terrain objects
    /// </summary>
    private void DeactivateAllTerrains()
    {
        if (terrainObjects == null) return;

        foreach (GameObject terrain in terrainObjects)
        {
            if (terrain != null)
            {
                terrain.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Updates the UI label with the current environment name
    /// </summary>
    private void UpdateLabel()
    {
        if (environmentLabel == null) return;

        environmentLabel.text = GetCurrentEnvironmentName();
    }

    /// <summary>
    /// Gets the name of the current environment
    /// </summary>
    private string GetCurrentEnvironmentName()
    {
        int terrainCount = (terrainObjects != null ? terrainObjects.Length : 0);

        if (currentIndex == 0)
        {
            return "None";
        }
        else if (currentIndex == 1)
        {
            return "Cube";
        }
        else if (currentIndex >= 2 && currentIndex < 2 + terrainCount)
        {
            // None + Terrain
            int terrainIndex = currentIndex - 2;
            if (terrainObjects != null && terrainIndex >= 0 && terrainIndex < terrainObjects.Length)
            {
                GameObject terrain = terrainObjects[terrainIndex];
                string terrainName = terrain != null ? terrain.name : $"Terrain {terrainIndex + 1}";
                return $"None + {terrainName}";
            }
            return "None + Unknown";
        }
        else if (currentIndex >= 2 + terrainCount && currentIndex < 2 + (2 * terrainCount))
        {
            // Cube + Terrain
            int terrainIndex = currentIndex - (2 + terrainCount);
            if (terrainObjects != null && terrainIndex >= 0 && terrainIndex < terrainObjects.Length)
            {
                GameObject terrain = terrainObjects[terrainIndex];
                string terrainName = terrain != null ? terrain.name : $"Terrain {terrainIndex + 1}";
                return $"Cube + {terrainName}";
            }
            return "Cube + Unknown";
        }
        
        return "Unknown";
    }

    /// <summary>
    /// Optional: Set environment directly by index (0 = None, 1 = Cube, 2+ = Terrains)
    /// </summary>
    public void SetEnvironmentByIndex(int index)
    {
        if (index >= 0 && index < totalOptions)
        {
            currentIndex = index;
            UpdateEnvironment();
        }
        else
        {
            Debug.LogWarning($"Invalid environment index: {index}. Valid range: 0-{totalOptions - 1}");
        }
    }

    /// <summary>
    /// Optional: Get current environment index
    /// </summary>
    public int GetCurrentIndex()
    {
        return currentIndex;
    }
}
