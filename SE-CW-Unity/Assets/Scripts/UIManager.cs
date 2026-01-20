using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject colorPalettePanel;
    public GameObject submitClothButton;
    public GameObject paintingExperiencePanel;

    public PaintballManager paintballManager; //  Assign this in the Inspector

    [Header("Camera Switching")]
    public Camera playerCamera;
    public Camera birdsEyeCamera;
    public Camera sideViewCamera;
    public Button cycleCameraButton;
    public Text cameraLabel; // Optional

    private Camera[] cameras;
    private int currentCameraIndex = 0;
    private string[] cameraNames = { "Player View", "Bird's Eye View", "Side View" };

    void Start()
    {
        // Initialize cameras
        if (playerCamera != null && birdsEyeCamera != null && sideViewCamera != null)
        {
            cameras = new Camera[] { playerCamera, birdsEyeCamera, sideViewCamera };
            ActivateCamera(0);
        }

        // Attach camera button click event
        if (cycleCameraButton != null)
        {
            cycleCameraButton.onClick.AddListener(CycleCamera);
        }
    }

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

    public void CycleCamera()
    {
        if (cameras == null || cameras.Length == 0) return;

        currentCameraIndex = (currentCameraIndex + 1) % cameras.Length;
        ActivateCamera(currentCameraIndex);
    }

    private void ActivateCamera(int index)
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null)
            {
                cameras[i].enabled = (i == index);
            }
        }

        if (cameraLabel != null)
        {
            cameraLabel.text = cameraNames[index];
        }

        Debug.Log($"Switched to {cameraNames[index]}");
    }
}
