using UnityEngine;
using UnityEngine.UI;

public class VRMenuController : MonoBehaviour
{
    [Header("Menu Components")]
    // The main parent object of the menu (The Canvas or a parent GameObject)
    public GameObject menuRoot; 
    
    // Assign your panels here in the Inspector (e.g., Main, Audio, Gameplay)
    public GameObject[] menuPages; 

    [Header("Tai Chi Settings")]
    public GameObject instructorObject; // The teacher model
    public Transform mirrorPoint; // The point to reflect across

    private bool isMenuOpen = false;

    void Start()
    {
        // Ensure menu starts closed
        CloseMenu(); 
    }

    // --- Core Menu Logic ---

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        menuRoot.SetActive(isMenuOpen);

        if (isMenuOpen)
        {
            // Always start at the first page (Main Menu) when opening
            OpenPage(0); 
            
            // Optional: Play a soft "gong" or "wind chime" sound here
        }
    }

    public void CloseMenu()
    {
        isMenuOpen = false;
        menuRoot.SetActive(false);
    }

    // Call this from buttons to switch pages
    // 0 = Main, 1 = Audio, 2 = Gameplay, etc.
    public void OpenPage(int pageIndex)
    {
        // specific logic to hide all pages first
        foreach (GameObject page in menuPages)
        {
            page.SetActive(false);
        }

        // Show the specific requested page
        if (pageIndex >= 0 && pageIndex < menuPages.Length)
        {
            menuPages[pageIndex].SetActive(true);
        }
    }

    // --- Tai Chi Specific Logic ---

    // Connect this to a Toggle UI element
    public void SetMirrorMode(bool isMirrored)
    {
        if (instructorObject == null) return;

        if (isMirrored)
        {
            // Simple logic: Flip the instructor on the X axis
            // For complex rigs, you might swap the animation source instead
            Vector3 scale = instructorObject.transform.localScale;
            scale.x = -1 * Mathf.Abs(scale.x); 
            instructorObject.transform.localScale = scale;
        }
        else
        {
            // Restore normal scale
            Vector3 scale = instructorObject.transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            instructorObject.transform.localScale = scale;
        }
    }
}