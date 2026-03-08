using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ColorUIBinder : MonoBehaviour
{
    [Header("RGB Sliders (0..1)")]
    public Slider rSlider;
    public Slider gSlider;
    public Slider bSlider;

    [Header("Hex UI")]
    public TMP_Text hexText;              // displays like "#FFAABB"
    public HexInputController hexInput;   // your keypad controller (the one storing buffer)

    [Header("Preview / Target (optional)")]
    public Image preview;
    public Renderer targetRenderer;       // e.g. the ball preview renderer (optional)

    [Header("Color Selection Integration")]
    [Tooltip("Reference to ColorSelectionManager to update pending color")]
    public ColorSelectionManager selectionManager;

    bool _isUpdatingUI;

    public Image sliderPreview;
    public Image hexPreview;

    void Awake()
    {
        // Slider -> update color
        rSlider.onValueChanged.AddListener(_ => OnSliderChanged());
        gSlider.onValueChanged.AddListener(_ => OnSliderChanged());
        bSlider.onValueChanged.AddListener(_ => OnSliderChanged());

        // Hex -> update color (we'll call this from HexInputController when hex changes)
        // If you don't want code changes, you can also poll, but event is cleaner.
    }

    void Start()
    {
        // Initialise from sliders on start
        OnSliderChanged();
    }

    void OnSliderChanged()
    {
        if (_isUpdatingUI) return;

        Color c = new Color(rSlider.value, gSlider.value, bSlider.value, 1f);
        SetColorFromAnySource(c, updateSliders: false, updateHex: true);
    }

    // Call this when hex changes
    public void OnHexChanged(Color c)
    {
        if (_isUpdatingUI) return;
        SetColorFromAnySource(c, updateSliders: true, updateHex: true);
    }

    void SetColorFromAnySource(Color c, bool updateSliders, bool updateHex)
    {
        _isUpdatingUI = true;

        // Update previews
        if (sliderPreview) sliderPreview.color = c;
        if (hexPreview) hexPreview.color = c;

        // Update target object (e.g. ball)
        if (targetRenderer) targetRenderer.material.color = c;

        // Update sliders
        if (updateSliders)
        {
            rSlider.SetValueWithoutNotify(c.r);
            gSlider.SetValueWithoutNotify(c.g);
            bSlider.SetValueWithoutNotify(c.b);
        }

        // Update hex
        if (updateHex)
        {
            string hex = ColorUtility.ToHtmlStringRGB(c); // "FFAABB"
            if (hexText) hexText.text = "#" + hex;

            if (hexInput != null)
                hexInput.SetHexExternal(hex);
        }

        // Notify ColorSelectionManager of the pending color
        if (selectionManager != null)
        {
            selectionManager.OnColorPicked(c);
        }

        _isUpdatingUI = false;
    }   
}
