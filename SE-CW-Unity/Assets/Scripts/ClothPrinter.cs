using UnityEngine;

public class ClothPrinter : MonoBehaviour
{
    public GameObject clothPrefab;
    public WaterSimulation waterSim;

    public void PrintCloth()
    {
        GameObject cloth = Instantiate(clothPrefab);
        Texture2D paintedTexture = waterSim.GetSurfaceTexture();
        cloth.GetComponent<Renderer>().material.mainTexture = paintedTexture;

        float paintedRatio = CalculatePaintRatio(paintedTexture);
        Debug.Log($"Paint Coverage: {paintedRatio * 100}%");
    }

    float CalculatePaintRatio(Texture2D tex)
    {
        Color[] pixels = tex.GetPixels();
        int paintCount = 0;
        foreach (Color c in pixels)
        {
            if (c.a > 0.1f) paintCount++;
        }
        return (float)paintCount / pixels.Length;
    }
}
