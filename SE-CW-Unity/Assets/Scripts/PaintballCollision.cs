using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;

public class PaintballCollision : MonoBehaviour
{
    public GameObject paintballPrefab;         // Assign in Inspector
    public GameObject paintOnWaterPrefab;      // Assign your OilPaint prefab
    public Transform waterSurface;             // Assign your WaterCube object
    public float yOffset = 0.01f;              // Slight inward offset to avoid z-fighting

    public ParticleSystem ripplePS;            // Assign in Inspector
    private Transform safeRespawnPoint;
    private bool isDissolving = false;

    public float minY = -20f;   // if ball falls below this, respawn

    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;
    private bool hasBeenGrabbed = false; // Track if paintball has ever been grabbed

    void Start()
    {
        GameObject respawnObj = GameObject.Find("PaintballRespawnPoint");
        if (respawnObj != null)
        {
            safeRespawnPoint = respawnObj.transform;
        }
        else
        {
            Debug.LogError("PaintballRespawnPoint not found in the scene.");
        }

        // Get components
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (grabInteractable == null)
        {
            Debug.LogError($"PaintballCollision on {gameObject.name}: XRGrabInteractable component is MISSING! Add it to the prefab.");
        }
        else
        {
            Debug.Log($"PaintballCollision on {gameObject.name}: XRGrabInteractable found");
        }

        // Make paintball kinematic (anchored) until grabbed
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = true; // Ensure gravity is enabled for when it becomes non-kinematic
            Debug.Log($"PaintballCollision: Initial state - isKinematic: {rb.isKinematic}, useGravity: {rb.useGravity}");
        }
        else
        {
            Debug.LogError($"PaintballCollision on {gameObject.name}: Rigidbody component is MISSING!");
        }

        // Listen for grab and release events
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
            
            // Use Instantaneous movement (most stable for VR)
            // This handles the object position while held, but releases control when detached
            grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grabInteractable.throwOnDetach = true;
            grabInteractable.throwSmoothingDuration = 0.25f;
            grabInteractable.throwVelocityScale = 1.5f;
            
            Debug.Log("PaintballCollision: XRGrabInteractable configured - Movement: Instantaneous, ThrowOnDetach: true");
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
        // When grabbed, enable physics immediately
        Debug.Log($"OnGrabbed EVENT FIRED for {gameObject.name}!");
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            hasBeenGrabbed = true;
            Debug.Log($"OnGrabbed: Set {gameObject.name} to non-kinematic. isKinematic={rb.isKinematic}, hasBeenGrabbed={hasBeenGrabbed}");
        }
        else
        {
            Debug.LogError($"OnGrabbed: Rigidbody is NULL on {gameObject.name}!");
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        // Ensure it's non-kinematic for throwing
        Debug.Log($"OnReleased EVENT FIRED for {gameObject.name}!");
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            Debug.Log($"OnReleased: Keeping {gameObject.name} non-kinematic for throw. isKinematic={rb.isKinematic}");
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

            Debug.Log("PaintballCollision: fell below minY, teleported to safeRespawnPoint");
        }
    }


    void OnTriggerEnter(Collider other)
    {
        if (!isDissolving && other.CompareTag("Water"))
        {
            Debug.Log("Paintball entered water trigger: " + other.name);
            if (Accuracy.Instance != null) Accuracy.Instance.RegisterHit();
            // Spawn particle effect at ball position
            ripplePS.Emit(1);

            SpawnPaintOnWater();
            StartCoroutine(RespawnPaintball());
        } else if (other.CompareTag("Barrier")) {
            Debug.Log("Paintball left boundaries: " + other.name);
            if (Accuracy.Instance != null) Accuracy.Instance.RegisterMiss();
            StartCoroutine(RespawnPaintball());
        }
        
    }

    void SpawnPaintOnWater()
    {
        Color paintColor = Color.white;
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            paintColor = rend.material.color;
            paintColor.a = 1f;
        }

        Vector3 cubeCenter = waterSurface.position;
        Vector3 cubeSize = waterSurface.localScale;
        Vector3 hitPos = transform.position;
        float margin = 0.15f;

        float minX = cubeCenter.x - cubeSize.x / 2f + margin;
        float maxX = cubeCenter.x + cubeSize.x / 2f - margin;
        float minY = cubeCenter.y - cubeSize.y / 2f + margin;
        float maxY = cubeCenter.y + cubeSize.y / 2f - margin;
        float minZ = cubeCenter.z - cubeSize.z / 2f + margin;
        float maxZ = cubeCenter.z + cubeSize.z / 2f - margin;

        Vector3 spawnPos = new Vector3(
            Mathf.Clamp(hitPos.x, minX, maxX),
            Mathf.Clamp(hitPos.y, minY, maxY),
            Mathf.Clamp(hitPos.z, minZ, maxZ)
        );

        Vector3 toCenter = (cubeCenter - spawnPos).normalized;
        Vector3 faceDir = GetClosestAxisAlignedNormal(toCenter);
        Quaternion rotation = Quaternion.LookRotation(faceDir);

        GameObject paint = Instantiate(paintOnWaterPrefab, spawnPos + faceDir * yOffset, rotation);

        // 1) Parent under WaterCube, keeping current world pose
        paint.transform.SetParent(waterSurface, true);

        // 2) Now force a uniform world scale (e.g. 0.3 units)
        float targetWorldSize = 0.3f;

        // current world scale after parenting (already includes WaterCube rotation/scale)
        Vector3 currentWorld = paint.transform.lossyScale;

        // compute the factor needed on localScale to reach targetWorldSize
        Vector3 local = paint.transform.localScale;
        paint.transform.localScale = new Vector3(
            local.x * (targetWorldSize / currentWorld.x),
            local.y * (targetWorldSize / currentWorld.y),
            local.z * (targetWorldSize / currentWorld.z)
        );

        paint.tag = "OilPaint";




        Renderer paintRend = paint.GetComponent<Renderer>();
        if (paintRend != null)
        {
            Material runtimeMat = new Material(paintRend.sharedMaterial);
            runtimeMat.color = paintColor;
            runtimeMat.SetFloat("_Smoothness", 0.5f);
            paintRend.material = runtimeMat;
        }

        StartCoroutine(AnimatePaintExpansionWithPush(paint.transform, 5f, 3f));

        Debug.Log("Paint spawned inside WaterCube at " + spawnPos);
    }

    IEnumerator AnimatePaintExpansionWithPush(Transform paint, float finalScale, float duration)
    {
        float startScale = paint.localScale.x;   // use as reference, but don't overwrite scale
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            // Use a radius based on the original scale, not by changing paint.localScale
            float currentScale = Mathf.Lerp(startScale, finalScale, t);

            Collider[] others = Physics.OverlapSphere(paint.position, currentScale * 3f);
            foreach (Collider col in others)
            {
                if (col.transform != paint && col.CompareTag("OilPaint"))
                {
                    Vector3 pushDir = (col.transform.position - paint.position).normalized;
                    float dist = Vector3.Distance(col.transform.position, paint.position);
                    float strength = Mathf.Clamp01(1f - dist / currentScale);

                    // Fake distortion by stretching in the direction of push
                    Vector3 scale = col.transform.localScale;
                    scale += new Vector3(pushDir.x, 0, pushDir.z) * strength * Time.deltaTime * 0.4f;
                    scale = Vector3.Min(scale, Vector3.one * 1.5f);
                    col.transform.localScale = scale;

                    // Try pushing while clamping inside cube
                    Vector3 proposedPos = col.transform.position + pushDir * strength * Time.deltaTime * 0.3f;
                    proposedPos = ClampInsideCube(proposedPos, waterSurface.position, waterSurface.localScale, 0.15f);
                    col.transform.position = proposedPos;
                }
            }

            yield return null;
        }

        // Do NOT override the final scale; keep whatever SpawnPaintOnWater set
        // paint.localScale = Vector3.one * finalScale;
    }


    Vector3 ClampInsideCube(Vector3 pos, Vector3 cubeCenter, Vector3 cubeSize, float margin)
    {
        float minX = cubeCenter.x - cubeSize.x / 2f + margin;
        float maxX = cubeCenter.x + cubeSize.x / 2f - margin;
        float minY = cubeCenter.y - cubeSize.y / 2f + margin;
        float maxY = cubeCenter.y + cubeSize.y / 2f - margin;
        float minZ = cubeCenter.z - cubeSize.z / 2f + margin;
        float maxZ = cubeCenter.z + cubeSize.z / 2f - margin;

        return new Vector3(
            Mathf.Clamp(pos.x, minX, maxX),
            Mathf.Clamp(pos.y, minY, maxY),
            Mathf.Clamp(pos.z, minZ, maxZ)
        );
    }

    // just make it spawn on the front face
    Vector3 GetClosestAxisAlignedNormal(Vector3 direction)
    {
        // Use the WaterCube's visual front face after a +90° X rotation.
        // If your cube is rotated -90° instead, flip the sign.
        return -waterSurface.up;
        // or waterSurface.up;  // try this if the normal points the wrong way
    }



    //Vector3 GetClosestAxisAlignedNormal(Vector3 direction)
    //{
    //    Vector3[] normals = {
    //        Vector3.right, Vector3.left,
    //        Vector3.up, Vector3.down,
    //        Vector3.forward, Vector3.back
    //    };

    //    Vector3 closest = normals[0];
    //    float maxDot = Vector3.Dot(direction, closest);

    //    for (int i = 1; i < normals.Length; i++)
    //    {
    //        float dot = Vector3.Dot(direction, normals[i]);
    //        if (dot > maxDot)
    //        {
    //            maxDot = dot;
    //            closest = normals[i];
    //        }
    //    }
    //    return closest;
    //}

    IEnumerator RespawnPaintball()
    {
        isDissolving = true;

        Quaternion rot = transform.rotation;

        Color paintColor = Color.white;
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            paintColor = rend.material.color;
        }

        // yield return new WaitForSeconds(0.5f);

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
            }

            Debug.Log($"New paintball respawned at {spawnPos} for color {colorKey}");
        }
        else
        {
            Debug.LogWarning("No stored position for color: " + colorKey);
        }

        Destroy(gameObject);
        yield break;
    }
}
