using UnityEngine;
using Seb.Fluid2D.Simulation;
using Unity.Mathematics;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;

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

    // XR Grab handling
    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;
    private bool hasBeenGrabbed = false;

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

        // Get components for XR grab handling
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (grabInteractable == null)
        {
            Debug.LogError($"SpawnOnContact on {gameObject.name}: XRGrabInteractable component is MISSING! Add it to the prefab.");
        }
        else
        {
            Debug.Log($"SpawnOnContact on {gameObject.name}: XRGrabInteractable found");
        }

        // Make paintball kinematic (anchored) until grabbed
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = true;
            Debug.Log($"SpawnOnContact: Initial state - {gameObject.name} isKinematic: {rb.isKinematic}, useGravity: {rb.useGravity}");
        }
        else
        {
            Debug.LogError($"SpawnOnContact on {gameObject.name}: Rigidbody component is MISSING!");
        }

        // Listen for grab and release events
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
            
            // Configure for throwing
            grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grabInteractable.throwOnDetach = true;
            grabInteractable.throwSmoothingDuration = 0.25f;
            grabInteractable.throwVelocityScale = 1.5f;
            
            Debug.Log($"SpawnOnContact: XRGrabInteractable configured on {gameObject.name}");
        }
    }

    void OnDestroy()
    {
        // Clean up listeners
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        Debug.Log($"OnGrabbed EVENT FIRED for {gameObject.name}!");
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            hasBeenGrabbed = true;
            Debug.Log($"OnGrabbed: Set {gameObject.name} to non-kinematic. isKinematic={rb.isKinematic}");
        }
        else
        {
            Debug.LogError($"OnGrabbed: Rigidbody is NULL on {gameObject.name}!");
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        Debug.Log($"OnReleased EVENT FIRED for {gameObject.name}!");
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            Debug.Log($"OnReleased: Keeping {gameObject.name} non-kinematic for throw. isKinematic={rb.isKinematic}");
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

    void FixedUpdate()
    {
        // Force non-kinematic if currently being held OR if has been grabbed before
        if (rb != null && grabInteractable != null)
        {
            // Check if currently selected/held
            if (grabInteractable.isSelected)
            {
                if (rb.isKinematic)
                {
                    rb.isKinematic = false;
                    hasBeenGrabbed = true;
                    Debug.LogWarning($"FixedUpdate: Forced {gameObject.name} to non-kinematic (is being held)");
                }
            }
            // Also keep non-kinematic if it has been grabbed before
            else if (hasBeenGrabbed && rb.isKinematic)
            {
                rb.isKinematic = false;
                Debug.LogWarning($"FixedUpdate: Forced {gameObject.name} to non-kinematic (was grabbed before)");
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

            // Make the respawned paintball kinematic (anchored) until grabbed
            Rigidbody newRb = newBall.GetComponent<Rigidbody>();
            if (newRb != null)
            {
                newRb.isKinematic = true;
                Debug.Log("SpawnOnContact: Set respawned paintball to kinematic");
            }

            // Extract button index from the original ball's name (e.g., "Paintball_RGBA(...)_Button3")
            int buttonIndex = -1;
            if (gameObject.name.Contains("Button"))
            {
                string nameStr = gameObject.name;
                int buttonStartIndex = nameStr.LastIndexOf("Button") + 6; // "Button" is 6 chars
                if (buttonStartIndex < nameStr.Length)
                {
                    // Extract just the digit(s) after "Button"
                    string buttonNumberStr = "";
                    for (int i = buttonStartIndex; i < nameStr.Length; i++)
                    {
                        if (char.IsDigit(nameStr[i]))
                        {
                            buttonNumberStr += nameStr[i];
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (!string.IsNullOrEmpty(buttonNumberStr))
                    {
                        buttonIndex = int.Parse(buttonNumberStr);
                    }
                }
            }

            // Set the new ball's name to match the pattern
            if (buttonIndex != -1)
            {
                newBall.name = $"Paintball_{colorKey}_Button{buttonIndex}";
                
                // Update the ColorSelectionManager's tracking dictionary
                if (ColorSelectionManager.buttonToPaintball.ContainsKey(buttonIndex))
                {
                    // Destroy the old tracked ball if it still exists
                    GameObject oldTrackedBall = ColorSelectionManager.buttonToPaintball[buttonIndex];
                    if (oldTrackedBall != null && oldTrackedBall != gameObject)
                    {
                        Destroy(oldTrackedBall);
                    }
                }
                ColorSelectionManager.buttonToPaintball[buttonIndex] = newBall;
                Debug.Log($"SpawnOnContact: Updated buttonToPaintball[{buttonIndex}] with respawned ball");
            }
            else
            {
                newBall.name = $"Paintball_{colorKey}";
            }

            // Re-add the spawn position to the queue for future respawns
            if (!ColorSelectionManager.colorToSpawnQueue.ContainsKey(colorKey))
            {
                ColorSelectionManager.colorToSpawnQueue[colorKey] = new Queue<Vector3>();
            }
            ColorSelectionManager.colorToSpawnQueue[colorKey].Enqueue(spawnPos);

            Debug.Log($"SpawnOnContact: New paintball respawned at {spawnPos} for color {colorKey}");
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
