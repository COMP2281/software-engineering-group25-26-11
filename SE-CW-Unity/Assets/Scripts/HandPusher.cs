using UnityEngine;

public class HandPusher : MonoBehaviour
{
    public float pushStrength = 10f; // Strength of push force
    private Vector3 previousPosition; // To track hand movement direction

    void Start()
    {
        previousPosition = transform.position;
    }

    void Update()
    {
        // Track hand movement
        Vector3 handVelocity = (transform.position - previousPosition) / Time.deltaTime;
        previousPosition = transform.position;
    }

    private void OnTriggerStay(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null)
        {
            Vector3 pushDirection = (transform.position - previousPosition).normalized;
            float handSpeed = (transform.position - previousPosition).magnitude / Time.deltaTime;

            // Apply force only if moving forward, not pulling back
            if (handSpeed > 0.01f)
            {
                rb.AddForce(pushDirection * pushStrength * handSpeed, ForceMode.Impulse);
            }
        }
    }
}
