using UnityEngine;
using Seb.Fluid2D.Simulation;
using Unity.Mathematics;

[RequireComponent(typeof(Collider))]
public class SpawnOnContact : MonoBehaviour
{
    public FluidSim2D sim;
    [Tooltip("Tag on the collider that should trigger spawning")]
    public string targetTag = "Water";
    public float cooldown = 0.25f;

    [Header("Color Settings")]
    [Tooltip("Default color if the triggering object has no color")]
    public Color defaultColor = Color.white;

    float lastSpawnTime = -999f;

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
        TrySpawn(other);
    }

    void OnTriggerStay(Collider other)
    {
        TrySpawn(other);
    }

    void TrySpawn(Collider other)
    {
        if (sim == null || sim.spawner2D == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag))
        {
            return;
        }

        if (Time.time - lastSpawnTime < cooldown)
        {
            return;
        }

        Transform anchor = sim.particleDisplay != null ? sim.particleDisplay.worldAnchor : null;
        if (anchor == null)
        {
            return;
        }

        // Get color from the triggering object
        Color spawnColor = GetColorFromObject(other.gameObject);
        float4 color = new float4(spawnColor.r, spawnColor.g, spawnColor.b, spawnColor.a);

        Vector3 samplePoint = other.ClosestPoint(transform.position);
        Vector2 localSpawn = sim.WorldToSimLocal(samplePoint);
        sim.SpawnParticles(sim.spawner2D.GetSpawnData(color), localSpawn);
        lastSpawnTime = Time.time;
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
