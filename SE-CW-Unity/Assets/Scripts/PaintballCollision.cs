using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PaintballCollision : MonoBehaviour
{
    public GameObject paintballPrefab;         // Assign in Inspector
    public GameObject paintOnWaterPrefab;      // Assign your OilPaint prefab
    public Transform waterSurface;             // Assign your WaterCube object
    public float yOffset = 0.01f;              // Slight inward offset to avoid z-fighting

    private Transform safeRespawnPoint;
    private bool isDissolving = false;

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
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isDissolving && other.CompareTag("Water"))
        {
            Debug.Log("Paintball entered water trigger: " + other.name);
            if (Accuracy.Instance != null) Accuracy.Instance.RegisterHit();
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
        paint.transform.localScale = Vector3.one * 0.3f;
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
        float startScale = paint.localScale.x;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float currentScale = Mathf.Lerp(startScale, finalScale, t);
            paint.localScale = Vector3.one * currentScale;

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

        paint.localScale = Vector3.one * finalScale;
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

    Vector3 GetClosestAxisAlignedNormal(Vector3 direction)
    {
        Vector3[] normals = {
            Vector3.right, Vector3.left,
            Vector3.up, Vector3.down,
            Vector3.forward, Vector3.back
        };

        Vector3 closest = normals[0];
        float maxDot = Vector3.Dot(direction, closest);

        for (int i = 1; i < normals.Length; i++)
        {
            float dot = Vector3.Dot(direction, normals[i]);
            if (dot > maxDot)
            {
                maxDot = dot;
                closest = normals[i];
            }
        }
        return closest;
    }

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

        yield return new WaitForSeconds(0.5f);

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

            Debug.Log($"New paintball respawned at {spawnPos} for color {colorKey}");
        }
        else
        {
            Debug.LogWarning("No stored position for color: " + colorKey);
        }

        Destroy(gameObject);
    }
}
