using UnityEngine;
using Seb.Fluid2D.Simulation;
using Unity.Mathematics;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class SpawnOnContact : MonoBehaviour
{
    FluidSim2D sim;
    public GameObject paintballPrefab;
    private Transform safeRespawnPoint;
    public float minY = -20f; // if object falls below this, respawn
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
        GameObject respawnObj = GameObject.Find("PaintballRespawnPoint");
        if (respawnObj != null)
        {
            safeRespawnPoint = respawnObj.transform;
        }
    }

    void Update()
    {
        // Only do the check if we have a respawn point
        if (safeRespawnPoint == null) return;

        // If ball fell below the threshold, teleport it back
        if (transform.position.y < minY)
        {
            transform.position = safeRespawnPoint.position;
            transform.rotation = safeRespawnPoint.rotation;

            // Reset physics so it doesn't keep falling
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
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
        TrySpawn(other);
    }

    void OnTriggerStay(Collider other)
    {
        TrySpawn(other);
    }

    void TrySpawn(Collider other)
    {
        FluidSim2D currentSim = GetSim();
        if (currentSim == null || currentSim.spawner2D == null)
            return;

        if (!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag))
            return;

        if (Time.time - lastSpawnTime < cooldown)
            return;

        Transform anchor = currentSim.particleDisplay != null ? currentSim.particleDisplay.worldAnchor : null;
        if (anchor == null)
            return;

        // Get color from THIS object (the paintball), not the trigger
        Color spawnColor = GetColorFromObject(gameObject);
        float4 color = new float4(spawnColor.r, spawnColor.g, spawnColor.b, spawnColor.a);

        Vector3 samplePoint = other.ClosestPoint(transform.position);
        Vector2 localSpawn = currentSim.WorldToSimLocal(samplePoint);
        currentSim.SpawnParticles(currentSim.spawner2D.GetSpawnData(color), localSpawn);
        lastSpawnTime = Time.time;

        RippleEffect.Instance.RippleAtPoint(samplePoint);

        // Respawn paintball after spawning particles
        RespawnPaintball(spawnColor);
    }

    void RespawnPaintball(Color paintColor)
    {
        if (paintballPrefab == null)
            return;

        Quaternion rot = transform.rotation;
        Vector3 spawnPos = Vector3.zero;
        bool hasSpawnPos = false;

        Color32 colorKey = (Color32)paintColor;

        if (ColorSelectionManager.colorToSpawnQueue.TryGetValue(colorKey, out Queue<Vector3> queue) && queue.Count > 0)
        {
            spawnPos = queue.Peek();
            hasSpawnPos = true;
        }

        if (hasSpawnPos)
        {
            GameObject newBall = Instantiate(paintballPrefab, spawnPos, rot);

            Renderer newRend = newBall.GetComponent<Renderer>();
            if (newRend != null)
            {
                newRend.material.color = paintColor;
            }
        }

        Destroy(gameObject);
    }

    /// Attempts to get the color from the object's Renderer material.
    /// Falls back to defaultColor if no color can be found.
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
