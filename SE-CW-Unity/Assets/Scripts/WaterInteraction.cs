using UnityEngine;

public class WaterInteraction : MonoBehaviour
{
    public Camera mainCamera;
    public Collider waterCollider;
    public ParticleSystem splashParticles;
    public int particlesPerClick = 1;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (waterCollider.Raycast(ray, out RaycastHit hit, float.MaxValue))
            {
                // Emit particles at the click location
                ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
                emitParams.position = hit.point;
                
                splashParticles.Emit(emitParams, particlesPerClick);
            }
        }
    }
}
