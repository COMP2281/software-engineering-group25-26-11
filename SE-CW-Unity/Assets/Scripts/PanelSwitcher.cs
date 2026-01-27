using UnityEngine;

public class PanelSwitcher : MonoBehaviour
{
    public GameObject colorSelectionPanel;
    public GameObject paintingExperiencePanel;

    public void ShowPaintingPanel()
    {
        colorSelectionPanel.SetActive(false);
        paintingExperiencePanel.SetActive(true);
        
        // Clear the submit button when switching to painting panel
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null && uiManager.submitClothButton != null)
        {
            uiManager.submitClothButton.SetActive(false);
        }
    }

    public void ShowColorSelectionPanel()
    {
        colorSelectionPanel.SetActive(true);
        paintingExperiencePanel.SetActive(false);
    }
}
