using UnityEngine;
using UnityEngine.EventSystems;

public class ColorWheelPicker : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public RectTransform wheelRect;
    public RectTransform handle;
    public ColorUIBinder binder;

    [Range(0f, 1f)] public float value = 1f; // brightness (V)
    [Header("Hue calibration")]
    [Range(0f, 1f)] public float hueOffset = 0f; // rotate wheel
    public bool clockwise = false;   

    public void OnPointerDown(PointerEventData eventData) => Pick(eventData);
    public void OnDrag(PointerEventData eventData) => Pick(eventData);

    void Pick(PointerEventData eventData)
    {
        if (!wheelRect || !binder) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                wheelRect, eventData.position, eventData.pressEventCamera, out Vector2 local))
            return;

        Vector2 size = wheelRect.rect.size;
        Vector2 p = new Vector2(local.x / (size.x * 0.5f), local.y / (size.y * 0.5f));

        if (p.magnitude > 1f) p = p.normalized;

        float angle = Mathf.Atan2(p.y, p.x);                 // -pi..pi
        float hue = (angle / (2f * Mathf.PI) + 1f) % 1f;     // 0..1

        // If the wheel is mirrored
        if (clockwise)
            hue = 1f - hue;

        // If the wheel artwork is rotated
        hue = (hue + hueOffset) % 1f;
        float sat = Mathf.Clamp01(p.magnitude);

        Color c = Color.HSVToRGB(hue, sat, value);

        if (handle)
            handle.anchoredPosition = new Vector2(p.x * (size.x * 0.5f), p.y * (size.y * 0.5f));

        binder.OnHexChanged(c); // pushes into sliders/preview/hex
    }
}
