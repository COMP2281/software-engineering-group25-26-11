using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HexInputController : MonoBehaviour
{
    public TMP_Text hexText;      // displays "#FFAABB"
    public Image preview;         // colour preview square
    public GameObject keypadPanel;

    string buffer = "";

    public ColorUIBinder binder;

    void Start()
    {
        UpdateUI();
        if (keypadPanel) keypadPanel.SetActive(true);
    }

    // -------- Called by buttons --------

    public void PressHexChar(string c)
    {
        if (buffer.Length >= 6) return;
        buffer += c;
        UpdateUI();
    }

    public void Backspace()
    {
        if (buffer.Length == 0) return;
        buffer = buffer.Substring(0, buffer.Length - 1);
        UpdateUI();
    }

    public void Clear()
    {
        buffer = "";
        UpdateUI();
    }

    public void Apply()
    {
        keypadPanel.SetActive(false);
    }

    // -------- Helpers --------

    void UpdateUI()
    {
        if (hexText)
            hexText.text = "#" + buffer;

        if (buffer.Length == 6 &&
            ColorUtility.TryParseHtmlString("#" + buffer, out Color c))
        {
            if (preview) preview.color = c;

            if (binder != null)
                binder.OnHexChanged(c);
        }
        
    }

    public void SetHexExternal(string hexNoHash)
    {
        buffer = hexNoHash.ToUpperInvariant();
        UpdateUI();
    }

}
