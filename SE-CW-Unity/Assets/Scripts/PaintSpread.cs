using UnityEngine;

public class PaintSpread : MonoBehaviour
{
    public float spreadSpeed = 0.1f;
    public float maxScale = 1.2f;

    private Vector3 targetScale;

    void Start()
    {
        targetScale = transform.localScale * maxScale;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, spreadSpeed * Time.deltaTime);
    }
}
