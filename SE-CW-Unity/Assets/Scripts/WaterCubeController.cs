using UnityEngine;

public class WaterCubeFlipper : MonoBehaviour
{
    [SerializeField] private Transform waterCube; // child under Panel_PaintingExperience

    private bool isFlipped;

    public void FlipWaterCube()
    {
        if (waterCube == null) return;

        isFlipped = !isFlipped;

        // State A: local Z = 0, local Y = 218, X = 90
        // State B: local Z = 470, local Y = 100, X = 180
        float targetLocalZ = isFlipped ? 470f : 0f;
        float targetLocalY = isFlipped ? 100f : 218f;
        float targetXRot = isFlipped ? 180f : 90f;

        Vector3 localPos = waterCube.localPosition;
        localPos.z = targetLocalZ;
        localPos.y = targetLocalY;
        waterCube.localPosition = localPos;

        Vector3 e = waterCube.localEulerAngles;
        e.x = targetXRot;
        waterCube.localEulerAngles = e;
    }
}
