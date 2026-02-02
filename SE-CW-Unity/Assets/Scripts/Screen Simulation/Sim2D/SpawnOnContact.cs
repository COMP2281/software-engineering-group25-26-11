using UnityEngine;
using Seb.Fluid2D.Simulation;
using Unity.Mathematics;

[RequireComponent(typeof(Collider))]
public class SpawnOnContact : MonoBehaviour
{
    FluidSim2D sim;
    [Tooltip("Tag on the collider that should trigger spawning")]
    public string targetTag = "Water";
    public float cooldown = 0.25f;

    [Header("Color Settings")]
    [Tooltip("Default color if the triggering object has no color")]
    public Color defaultColor = Color.white;

    float lastSpawnTime = -999f;

    FluidSim2D GetSim()
    {
        if (sim == null)
        {
            GameObject fluidSimObj = GameObject.FindWithTag("FLUIDSIM");
            if (fluidSimObj != null)
            {
                sim = fluidSimObj.GetComponent<FluidSim2D>();
            }
        }
        return sim;
    }

    void Start()
    {
        GetSim(); // Try to find it at start
    }

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"SpawnOnContact.OnTriggerEnter called on {gameObject.name} with {other.name} (tag: {other.tag})");
        TrySpawn(other);
    }

    void OnTriggerStay(Collider other)
    {
        TrySpawn(other);
    }

    void TrySpawn(Collider other)
    {
        Debug.Log($"TrySpawn entered for {gameObject.name}");
        
        FluidSim2D currentSim = GetSim();
        if (currentSim == null || currentSim.spawner2D == null)
        {
            Debug.LogWarning($"SpawnOnContact on {gameObject.name}: sim is null={currentSim == null}, spawner2D is null={currentSim?.spawner2D == null}");
            return;
        }

        if (!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag))
        {
            Debug.Log($"SpawnOnContact on {gameObject.name}: Collided with {other.name} but tag '{other.tag}' doesn't match '{targetTag}'");
            return;
        }

        if (Time.time - lastSpawnTime < cooldown)
        {
            Debug.Log($"SpawnOnContact on {gameObject.name}: Cooldown active ({Time.time - lastSpawnTime:F2}s ago)");
            return;
        }

        Transform anchor = currentSim.particleDisplay != null ? currentSim.particleDisplay.worldAnchor : null;
        if (anchor == null)
        {
            Debug.LogWarning($"SpawnOnContact on {gameObject.name}: No worldAnchor found on particleDisplay");
            return;
        }

        Debug.Log($"SpawnOnContact on {gameObject.name}: Spawning particles!");

        // Get color from THIS object (the paintball), not the trigger
        Color spawnColor = GetColorFromObject(gameObject);
        Debug.Log($"SpawnOnContact color: {spawnColor}");
        float4 color = new float4(spawnColor.r, spawnColor.g, spawnColor.b, spawnColor.a);

        Vector3 samplePoint = other.ClosestPoint(transform.position);
        Vector2 localSpawn = currentSim.WorldToSimLocal(samplePoint);
        currentSim.SpawnParticles(currentSim.spawner2D.GetSpawnData(color), localSpawn);
        lastSpawnTime = Time.time;
        
        // Trigger ripple effect at the impact position
        WaterInteraction waterInteraction = Object.FindFirstObjectByType<WaterInteraction>();
        if (waterInteraction != null)
        {
            waterInteraction.CreateRippleAtPosition(samplePoint);
        }
    }

    /// <summary>
    /// Attempts to get the color from the object's Renderer material.
    /// Falls back to defaultColor if no color can be found.
    /// </summary>
    Color GetColorFromObject(GameObject obj)
    {
        // Try to get color from Renderer
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            // Try common color property names
            if (renderer.material.HasProperty("_Color"))
            {
                return renderer.material.color;
            }
            if (renderer.material.HasProperty("_BaseColor"))
            {
                return renderer.material.GetColor("_BaseColor");
            }
        }

        return defaultColor;
    }
}
