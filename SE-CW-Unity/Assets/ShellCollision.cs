using UnityEngine;

public class ShellRelease : MonoBehaviour
{
    [SerializeField] private string waterTag = "Water";

    private bool hasReleased = false;

    private void Start()
    {
        // Inside the shell: mini balls have no active physics
        foreach (Transform child in transform)
        {
            Rigidbody rb = child.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;   // follows parent, no forces
                rb.useGravity = false;   // no gravity while inside
            }

            Collider col = child.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;     // no collisions while inside
            }
        }
    }

    // Use this if water has a normal (non-trigger) collider
    private void OnTriggerEnter(Collision collision)
    {
        if (hasReleased) return;

        if (collision.collider.CompareTag(waterTag))
        {
            ReleaseChildren();
            hasReleased = true;
        }
    }

    /*
    // Use this instead if the water collider is set to "Is Trigger"
    private void OnTriggerEnter(Collider other)
    {
        if (hasReleased) return;

        if (other.CompareTag(waterTag))
        {
            ReleaseChildren();
            hasReleased = true;
        }
    }
    */

    private void ReleaseChildren()
    {
        int childCount = transform.childCount;

        for (int i = childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            Rigidbody rb = child.GetComponent<Rigidbody>();
            Collider col = child.GetComponent<Collider>();

            if (rb != null)
            {
                // Detach from shell
                child.parent = null;

                // Turn physics on
                rb.isKinematic = false;
                rb.useGravity = true;

                // Optional random push so they spread out
                Vector3 randomImpulse = Random.insideUnitSphere * 1.5f;
                rb.AddForce(randomImpulse, ForceMode.Impulse);
            }

            if (col != null)
            {
                col.enabled = true; // now they can collide with world
            }
        }

        // Destroy the shell after releasing the mini balls
        Destroy(gameObject);
    }
}
