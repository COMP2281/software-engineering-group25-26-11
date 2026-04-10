using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Accuracy : MonoBehaviour
{
    public static Accuracy Instance;
    
    [Header("UI Reference")]
    [Tooltip("Drag the TextMeshProUGUI component here from your Canvas")]
    public TextMeshProUGUI accuracyText;

    private int totalShots = 0;
    private int hits = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        UpdateDisplay();
    }

    public void RegisterHit()
    {
        hits++;
        totalShots++;
        UpdateDisplay();
    }

    public void RegisterMiss()
    {
        totalShots++;
        UpdateDisplay();
    }

    public void ResetAccuracy()
    {
        hits = 0;
        totalShots = 0;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (accuracyText == null) return;
        
        float accuracy = totalShots == 0 ? 100f : (float)hits / totalShots * 100f;
        accuracyText.text = $"{accuracy:F0}%";
    }
}
