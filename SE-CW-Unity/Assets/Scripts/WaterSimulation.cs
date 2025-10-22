using UnityEngine;
using System.Collections.Generic;

public class WaterSimulation : MonoBehaviour
{
    private List<Vector3> paintPositions = new List<Vector3>();
    private List<Color> paintColors = new List<Color>();

    public void AddPaint(Vector3 position, Color color)
    {
        paintPositions.Add(position);
        paintColors.Add(color);
        // simulate floating paint on surface
    }

    public Texture2D GetSurfaceTexture()
    {
        return new Texture2D(512, 512); // placeholder
    }
}
