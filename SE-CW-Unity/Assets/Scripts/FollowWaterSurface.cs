using UnityEngine;

public class FollowWaterSurface : MonoBehaviour
{
    public Transform waterSurface;
    public float waveSpeed = 1f;
    public float waveHeight = 0.5f;
    public float waveFrequency = 1f;

    void Update()
    {
        Vector3 pos = transform.position;
        pos.y = waterSurface.position.y + Mathf.Sin(Time.time * waveSpeed + pos.x * waveFrequency) * waveHeight;
        transform.position = pos;
    }
}
