using UnityEngine;

public class ShellRelease : MonoBehaviour
{
    [SerializeField] private string waterTag = "Water";

    private bool hasReleased = false;

    private void Start()
    {
        foreach (Transform child in transform)
        {
            Rigidbody rb = child.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            Collider col = child.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }
        }
    }

    // MUST be Collider, not Collision
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Shell trigger with: " + other.name + " tag=" + other.tag);

        if (hasReleased) return;

        if (other.CompareTag(waterTag))
        {
            Debug.Log("Hit water, releasing children");
            ReleaseChildren();
            hasReleased = true;
        }
    }

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
                child.parent = null;

                rb.isKinematic = false;
                rb.useGravity = true;

                Vector3 randomImpulse = Random.insideUnitSphere * 1.5f;
                rb.AddForce(randomImpulse, ForceMode.Impulse);
            }

            if (col != null)
            {
                col.enabled = true;
            }
        }

        Destroy(gameObject);
    }
}
