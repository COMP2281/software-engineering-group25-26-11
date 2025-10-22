using UnityEngine;

public class PaintDissolve : MonoBehaviour
{
    public GameObject paintTrailPrefab;  // Prefab for the paint spread
    public float spreadRadius = 0.5f;    // Control the paint spread size

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Water"))
        {
            // Instantiate paint at contact point
            GameObject paintTrail = Instantiate(paintTrailPrefab, collision.contacts[0].point, Quaternion.identity);

            // Adjust particle size based on paintball volume
            ParticleSystem particleSystem = paintTrail.GetComponent<ParticleSystem>();
            var main = particleSystem.main;
            main.startSize = transform.localScale.x; // Use paintball size to determine paint volume

            // Control Spread Radius
            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.radius = spreadRadius;
        }
    }
}
