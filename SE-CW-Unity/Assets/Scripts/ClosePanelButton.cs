using UnityEngine;
using UnityEngine.UI;

/// Toggles a panel open/closed when the button is clicked.
/// Opens if closed, closes if open.
public class ClosePanelButton : MonoBehaviour
{
    [Header("Panel to Toggle")]
    [Tooltip("The panel GameObject to toggle when this button is clicked")]
    public GameObject panelToClose;

    [Header("Button")]
    [Tooltip("The button (optional - auto-finds if not assigned)")]
    public Button closeButton;

    void Start()
    {
        // Auto-find button on this GameObject if not assigned
        if (closeButton == null)
        {
            closeButton = GetComponent<Button>();
        }

        // Add click listener
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(TogglePanel);
        }
        else
        {
            Debug.LogWarning("ClosePanelButton: No Button component found!");
        }
    }

    /// Toggles the panel active state - opens if closed, closes if open
    public void TogglePanel()
    {
        if (panelToClose != null)
        {
            bool newState = !panelToClose.activeSelf;
            panelToClose.SetActive(newState);
            Debug.Log($"Panel '{panelToClose.name}' {(newState ? "opened" : "closed")}.");
        }
        else
        {
            Debug.LogWarning("ClosePanelButton: Panel is not assigned!");
        }
    }

    /// Call this method directly from a Unity Button's OnClick event
    public void OnCloseButtonClick()
    {
        TogglePanel();
    }
}
