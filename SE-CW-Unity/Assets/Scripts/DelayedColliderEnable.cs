using UnityEngine;

public class DelayedColliderEnable : MonoBehaviour
{
    public float delay = 0.2f;

    void Start()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
            Invoke(nameof(EnableCollider), delay);
        }
    }

    void EnableCollider()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }
    }
}
