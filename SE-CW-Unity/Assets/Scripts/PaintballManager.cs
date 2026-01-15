using UnityEngine;
using System.Collections.Generic;

public class PaintballManager : MonoBehaviour
{
    public GameObject paintballPrefab;
    public Transform paintballSpawnArea;
    public int totalPaintballs = 20;
    public List<Color> selectedColors = new List<Color>();
    public Dictionary<Color, int> paintballCounts = new Dictionary<Color, int>();
    public Dictionary<GameObject, Vector3> paintballOriginalPositions = new Dictionary<GameObject, Vector3>();
    
    private List<GameObject> allPaintballs = new List<GameObject>(); //  Keep track

    public void GeneratePaintballs()
    {
        foreach (Color color in selectedColors)
        {
            // Skip if color is not in the paintballCounts dictionary
            if (!paintballCounts.TryGetValue(color, out int count))
            {
                Debug.LogWarning($"PaintballManager: Color {color} not found in paintballCounts, skipping.");
                continue;
            }
            
            for (int i = 0; i < count; i++)
            {
                Vector3 position = paintballSpawnArea.position + new Vector3(0, 0, i * 0.2f);
                GameObject pb = Instantiate(paintballPrefab, position, Quaternion.identity);
                pb.GetComponent<Paintball>().SetColor(color);

                // Disable physics initially
                Rigidbody rb = pb.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.useGravity = false;
                    rb.isKinematic = true;
                }

                Collider col = pb.GetComponent<Collider>();
                if (col != null)
                {
                    col.enabled = false;
                }

                allPaintballs.Add(pb);
                paintballOriginalPositions[pb] = position;  // Save original spawn point
            }
        }
    }





// Call this when switching to Painting panel
public void EnableAllPaintballPhysics()
    {
        foreach (GameObject pb in allPaintballs)
        {
            Rigidbody rb = pb.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = true;
                rb.isKinematic = false;
            }

            Collider col = pb.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = true;
            }
        }
    }
}
