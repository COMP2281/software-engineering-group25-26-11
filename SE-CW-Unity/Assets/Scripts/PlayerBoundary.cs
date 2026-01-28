using UnityEngine;
using Unity.XR.CoreUtils;

public class PlayerBoundary : MonoBehaviour
{
    [Header("Drag your 5 Barrier Objects here")]
    public GameObject[] barriers;

    [Header("Teleport Target")]
    public Transform safeZoneMarker; // Drag an empty GameObject here to act as your 'Home'

    private float minX, maxX, minZ, maxZ;
    private XROrigin xrOrigin;
    private CharacterController controller;

    void Start()
    {
        xrOrigin = GetComponent<XROrigin>();
        controller = GetComponent<CharacterController>();
        CalculateWorldBoundaries();
    }

    void CalculateWorldBoundaries()
    {
        if (barriers.Length == 0) return;

        // Initialize with the first barrier's world position
        minX = maxX = barriers[0].transform.position.x;
        minZ = maxZ = barriers[0].transform.position.z;

        // Expand the "fence" to include the world position of every barrier
        foreach (GameObject barrier in barriers)
        {
            Vector3 pos = barrier.transform.position;
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.z < minZ) minZ = pos.z;
            if (pos.z > maxZ) maxZ = pos.z;
        }
        
        Debug.Log($"Boundaries Synced! World Box: X({minX} to {maxX}) Z({minZ} to {maxZ})");
    }

    void Update()
    {
        Vector3 headPos = xrOrigin != null ? xrOrigin.Camera.transform.position : transform.position;

        if (headPos.x < minX || headPos.x > maxX || headPos.z < minZ || headPos.z > maxZ)
        {
            ExecuteTeleport();
        }
    }

    public void ExecuteTeleport()
    {
        if (controller != null) controller.enabled = false;

        // Use the marker's position so you can move it easily in the editor
        Vector3 target = safeZoneMarker != null ? safeZoneMarker.position : new Vector3(-30, 1, -93);

        if (xrOrigin != null)
        {
            Vector3 physicalOffset = xrOrigin.Camera.transform.position - transform.position;
            physicalOffset.y = 0; 
            transform.position = target - physicalOffset;
        }
        else
        {
            transform.position = target;
        }

        Physics.SyncTransforms();
        if (controller != null) controller.enabled = true;
    }

    private void OnDrawGizmos()
    {
        // This will draw the box in World Space so you can see if it matches the walls
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((minX + maxX) / 2, 1, (minZ + maxZ) / 2);
        Vector3 size = new Vector3(maxX - minX, 4, maxZ - minZ);
        Gizmos.DrawWireCube(center, size);
    }
}