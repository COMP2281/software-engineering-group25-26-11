using UnityEngine;

public class ObjectToggler : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Drag the GameObject you want to show/hide into this slot")]
    public GameObject targetObject;

    // This is the function the button will trigger
    public void ToggleVisibility()
    {
        if (targetObject != null)
        {
            // Flips the active state: if it's ON, turn it OFF. If it's OFF, turn it ON.
            targetObject.SetActive(!targetObject.activeSelf);
        }
        else
        {
            Debug.LogWarning("ObjectToggler: You forgot to assign a target object in the Inspector!");
        }
    }
}