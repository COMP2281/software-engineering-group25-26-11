using UnityEngine;
using UnityEngine.UI;

public class SimpleSliderPalette : MonoBehaviour
{
    public Slider rSlider;
    public Slider gSlider;
    public Slider bSlider;
    public Image preview;
    public ColorPaletteSpawner spawner;

    public void OnSliderChanged()
    {
        Color c = new Color(rSlider.value, gSlider.value, bSlider.value, 1f);
        if (preview != null)
            preview.color = c;
    }

    public void OnConfirmColor()
    {
        Color c = new Color(rSlider.value, gSlider.value, bSlider.value, 1f);
        if (spawner != null)
            spawner.SpawnBallFromPalette(c);
        else
            Debug.LogError("Spawner not assigned on SimpleSliderPalette.");
    }
}
