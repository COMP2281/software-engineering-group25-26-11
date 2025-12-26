using UnityEngine;

public class Paintball : MonoBehaviour
{
    private Color paintColor;

    public void SetColor(Color color)
    {
        paintColor = color;
        GetComponent<Renderer>().material.color = color;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            if (Accuracy.Instance != null) Accuracy.Instance.RegisterHit();
            other.GetComponent<WaterSimulation>().AddPaint(transform.position, paintColor);
            Destroy(gameObject);
        }
    }
}
