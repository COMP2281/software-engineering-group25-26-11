using UnityEngine;
using System.Collections.Generic;
using StableFluids.Marbling; // Ensure this matches your namespace

public class ColorSelectionManager : MonoBehaviour
{
    [Header("Fluid Integration")]
    [Tooltip("Drag your FluidManager here")]
    public MarblingController fluidController;

    [Header("UI References")]
    [Tooltip("The color selection panel that opens when a button is clicked")]
    public GameObject colorSelectionPanel;

    [Header("Button UI References")]
    public GameObject button1;
    public GameObject button2;
    public GameObject button3;
    public GameObject button4;
    public GameObject button5;

    private int pendingButtonIndex = -1;
    private Color pendingColor;

    void Start()
    {
        // ENSURE THE MENU IS OPEN BY DEFAULT
        if (colorSelectionPanel != null)
        {
            colorSelectionPanel.SetActive(true);
        }
        
        // Default to slot 1 so you can start picking immediately
        pendingButtonIndex = 1; 
    }

    // --- Panel Opening Logic ---
    public void OnButton1Clicked() { OpenColorPanel(1);  }
    public void OnButton2Clicked() { OpenColorPanel(2); }
    public void OnButton3Clicked() { OpenColorPanel(3); }
    public void OnButton4Clicked() { OpenColorPanel(4); }
    public void OnButton5Clicked() { OpenColorPanel(5); }

    private void OpenColorPanel(int buttonIndex)
    {
        pendingButtonIndex = buttonIndex;
        if (colorSelectionPanel != null) colorSelectionPanel.SetActive(true);
    }

    // --- Color Processing Logic ---
    public void OnColorPicked(Color color)
    {
        // 1. Get the current transparency from the fluid controller (set by your slider)
        float currentTransparency = 1f;
        if (fluidController != null)
        {
            currentTransparency = fluidController.paintColor.a;
        }

        // 2. Apply that transparency to the new color before saving it
        pendingColor = color;
        pendingColor.a = currentTransparency;

        // 3. Update the fluid color LIVE as you drag the slider
        if (fluidController != null) 
        {
            fluidController.paintColor = pendingColor;
        }
    }

    public void OnColorConfirmed()
    {
        if (pendingButtonIndex != -1)
        {
            // Optional: If you want the UI buttons to always be solid (so you can see them clearly),
            // you can force the button color to have an alpha of 1 here. 
            // Currently, it uses the transparent pendingColor.
            UpdateButtonColor(pendingButtonIndex, pendingColor);
            
            if (fluidController != null) fluidController.paintColor = pendingColor;
        }

        pendingButtonIndex = -1;
    }

    public void OnColorCancelled()
    {
        if (colorSelectionPanel != null) colorSelectionPanel.SetActive(false);
        pendingButtonIndex = -1;
    }

    private void UpdateButtonColor(int buttonIndex, Color color)
    {
        GameObject button = null;
        switch (buttonIndex)
        {
            case 1: button = button1; break;
            case 2: button = button2; break;
            case 3: button = button3; break;
            case 4: button = button4; break;
            case 5: button = button5; break;
        }
        
        // (If you want the UI buttons to always be solid, uncomment the line below)
        // color.a = 1f;

        if (button != null)
        {
            UnityEngine.UI.Image imageComponent = button.GetComponent<UnityEngine.UI.Image>();
            if (imageComponent != null) imageComponent.color = color;

            Renderer renderer = button.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = color;
        }
    }

    // ====================================================================
    // LEGACY VARIABLES & METHODS (Required to stop compiler errors)
    // ====================================================================
    
    [Header("Legacy Settings (Ignored)")]
    public GameObject ballPrefab;
    public Transform waterSurface;
    
    public static Dictionary<Color32, Queue<Vector3>> colorToSpawnQueue = new Dictionary<Color32, Queue<Vector3>>();
    public static Dictionary<int, GameObject> buttonToPaintball = new Dictionary<int, GameObject>();

    public void HandleColorSelection(Color color)
    {
        // Also preserve transparency for legacy scripts
        if (fluidController != null) 
        {
            float currentTransparency = fluidController.paintColor.a;
            color.a = currentTransparency;
            fluidController.paintColor = color;
        }
    }

    public Vector3 GetSpawnPositionForButton(int buttonIndex)
    {
        // Dummy return to satisfy the compiler
        return Vector3.zero;
    }
}