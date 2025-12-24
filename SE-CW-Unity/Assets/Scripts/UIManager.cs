using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject colorPalettePanel;
    public GameObject submitClothButton;
    public GameObject paintingExperiencePanel;

    public PaintballManager paintballManager; //  Assign this in the Inspector

    public void SetInitialState()
    {
        colorPalettePanel.SetActive(true);
        submitClothButton.SetActive(true);
        paintingExperiencePanel.SetActive(false);
    }

    public void OnColorsSelected()
    {
        colorPalettePanel.SetActive(false);
        submitClothButton.SetActive(true);
    }

    public void OnSubmit()
    {
        paintingExperiencePanel.SetActive(true);
        colorPalettePanel.SetActive(false);
        submitClothButton.SetActive(false);

        if (paintballManager != null)
        {
            paintballManager.GeneratePaintballs();
            paintballManager.EnableAllPaintballPhysics(); //  Activate paintball physics
        }
        else
        {
            Debug.LogWarning("PaintballManager not assigned in UIManager.");
        }
    }
}
