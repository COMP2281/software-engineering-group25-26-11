using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Accuracy : MonoBehaviour
{
    public static Accuracy Instance;
    public TextMeshPro accuracyText;

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

    private void UpdateDisplay()
    {
        float accuracy = totalShots == 0 ? 0 : (float)hits / totalShots * 100f;
        accuracyText.text = $"Accuracy: {accuracy:F1}%";
    }
}
