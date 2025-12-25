using UnityEngine;

public class PanelSwitcher : MonoBehaviour
{
    public GameObject colorSelectionPanel;
    public GameObject paintingExperiencePanel;

    public void ShowPaintingPanel()
    {
        colorSelectionPanel.SetActive(false);
        paintingExperiencePanel.SetActive(true);
    }

    public void ShowColorSelectionPanel()
    {
        colorSelectionPanel.SetActive(true);
        paintingExperiencePanel.SetActive(false);
    }
}
