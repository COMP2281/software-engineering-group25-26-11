using UnityEngine;
using Seb.Fluid2D.Simulation;

[RequireComponent(typeof(Collider))]
public class SpawnOnContact : MonoBehaviour
{
    public FluidSim2D sim;
    [Tooltip("Tag on the collider that should trigger spawning")]
    public string targetTag = "Water";
    public float cooldown = 0.25f;

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

        Vector3 samplePoint = other.ClosestPoint(transform.position);
        Vector2 localSpawn = sim.WorldToSimLocal(samplePoint);
        sim.SpawnParticles(sim.spawner2D.GetSpawnData(), localSpawn);
        lastSpawnTime = Time.time;
    }
}
