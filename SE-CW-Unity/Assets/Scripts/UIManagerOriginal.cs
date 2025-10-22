using UnityEngine;

public class UIManagerOriginal : MonoBehaviour
{
    public GameObject colorPalettePanel;
    public GameObject submitClothButton;
    public GameObject paintingExperiencePanel;  // Add this line

    void Start()
    {
        colorPalettePanel.SetActive(true);
        submitClothButton.SetActive(true);
        paintingExperiencePanel.SetActive(false); // Start hidden
    }

    public void OnColorsSelected()
    {
        colorPalettePanel.SetActive(false);
        submitClothButton.SetActive(true);
    }

    public void OnSubmit()  // Add this function
    {
        paintingExperiencePanel.SetActive(true);
        colorPalettePanel.SetActive(false);
        submitClothButton.SetActive(false);
        
    }
}