using UnityEngine;

public class PanelManager : MonoBehaviour
{
    [Header("Panels")]
    [Tooltip("The wheel panel GameObject")]
    public GameObject wheelPanel;
    [Tooltip("The slider panel GameObject")]
    public GameObject sliderPanel;
    [Tooltip("The code panel GameObject")]
    public GameObject codePanel;

    void Start()
    {
        // Close all panels at start
        CloseAllPanels();
    }

    /// <summary>
    /// Called when the Wheel button is clicked
    /// </summary>
    public void OnWheelButtonClicked()
    {
        CloseAllPanels();
        if (wheelPanel != null)
        {
            wheelPanel.SetActive(true);
            Debug.Log("Wheel panel opened");
        }
        else
        {
            Debug.LogWarning("Wheel panel is not assigned in the Inspector!");
        }
    }

    /// <summary>
    /// Called when the Slider button is clicked
    /// </summary>
    public void OnSliderButtonClicked()
    {
        CloseAllPanels();
        if (sliderPanel != null)
        {
            sliderPanel.SetActive(true);
            Debug.Log("Slider panel opened");
        }
        else
        {
            Debug.LogWarning("Slider panel is not assigned in the Inspector!");
        }
    }

    /// <summary>
    /// Called when the Code button is clicked
    /// </summary>
    public void OnCodeButtonClicked()
    {
        CloseAllPanels();
        if (codePanel != null)
        {
            codePanel.SetActive(true);
            Debug.Log("Code panel opened");
        }
        else
        {
            Debug.LogWarning("Code panel is not assigned in the Inspector!");
        }
    }

    /// <summary>
    /// Closes all panels
    /// </summary>
    private void CloseAllPanels()
    {
        if (wheelPanel != null)
        {
            wheelPanel.SetActive(false);
        }
        if (sliderPanel != null)
        {
            sliderPanel.SetActive(false);
        }
        if (codePanel != null)
        {
            codePanel.SetActive(false);
        }
    }

    /// <summary>
    /// Optional: Public method to close all panels (can be called from other scripts or UI)
    /// </summary>
    public void CloseAll()
    {
        CloseAllPanels();
        Debug.Log("All panels closed");
    }
}
