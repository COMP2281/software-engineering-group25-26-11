using UnityEngine;
using UnityEngine.UI;

public class CircularRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
{
    public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
    {
        RectTransform rt = transform as RectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt, sp, eventCamera, out Vector2 local);

        Vector2 norm = local / (rt.rect.size * 0.5f);
        return norm.magnitude <= 1f;
    }
}
