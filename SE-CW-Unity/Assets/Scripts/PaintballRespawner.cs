using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PaintballRespawner : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;
    private Transform spawnPoint;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void Start()
    {
        // Set spawn point as parent by default
        spawnPoint = transform.parent;

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
        }
        else
        {
            Debug.LogWarning("PaintballRespawner: No XRGrabInteractable found on this object.");
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (spawnPoint != null)
        {
            // Instantiate a new paintball at the spawn point's position
            GameObject newPaintball = Instantiate(gameObject, spawnPoint.position, spawnPoint.rotation, spawnPoint);

            // Optional: remove "(Clone)" from the new object's name for cleanliness
            newPaintball.name = gameObject.name;
        }
        else
        {
            Debug.LogWarning("PaintballRespawner: Spawn point not found!");
        }
    }
}
