using UnityEngine;

public class OilPaintSpawner : MonoBehaviour
{
    public GameObject paintPrefab;        // Assign the prefab in Inspector
    public Transform waterSurface;        // Reference to the water cube
    public float yOffset = 0.01f;         // Slight float above water

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            Vector3 spawnPosition = new Vector3(
                transform.position.x,
                waterSurface.position.y + yOffset,
                transform.position.z
            );

            Quaternion rotation = Quaternion.Euler(90, Random.Range(0, 360), 0);
            Instantiate(paintPrefab, spawnPosition, rotation);

            Destroy(gameObject); // Remove the paintball
        }
    }
}
