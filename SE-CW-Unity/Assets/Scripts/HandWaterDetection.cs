using UnityEngine;
using StableFluids.Marbling; 

public class HandWaterDetection : MonoBehaviour
{
    [Header("Fluid Integration")]
    [Tooltip("Drag your MarblingController here")]
    public MarblingController fluidController; 
    
    [Tooltip("Drag the physical Water Canvas here")]
    public Renderer canvasRenderer;

    [Header("Settings")]
    [Tooltip("How hard the hand pushes the fluid")]
    public float pushStrength = 5f;
    
    [Tooltip("Make sure your canvas has this exact tag!")]
    public string targetTag = "Water";

    private Vector3 previousPosition;
    private Vector3 smoothVelocity;
    private float _targetAspectRatio = 1f;

    void Start()
    {
        previousPosition = transform.position;
        
        // Calculate aspect ratio so the fluid brush stays circular
        if (canvasRenderer != null)
        {
            _targetAspectRatio = canvasRenderer.bounds.size.x / canvasRenderer.bounds.size.y;
        }
    }

    void Update()
    {
        // Calculate hand velocity smoothly every frame
        Vector3 rawVelocity = (transform.position - previousPosition) / Time.deltaTime;
        smoothVelocity = Vector3.Lerp(smoothVelocity, rawVelocity, Time.deltaTime * 15f);
        previousPosition = transform.position;
    }

    private void OnTriggerStay(Collider other)
    {
        // Only run if we hit the canvas and our slots are filled
        if (other.CompareTag(targetTag) && fluidController != null && canvasRenderer != null)
        {
            // 1. Calculate where the hand is relative to the canvas bounds
            Bounds b = canvasRenderer.bounds;
            float u = Mathf.Clamp01(1f - (transform.position.x - b.min.x) / b.size.x);
            float v = Mathf.Clamp01(1f - (transform.position.y - b.min.y) / b.size.y);

            // 2. Center it (-0.5 to 0.5) and apply aspect ratio
            Vector2 normalizedPos = new Vector2((u - 0.5f) * _targetAspectRatio, v - 0.5f);
            
            // 3. Convert 3D hand speed to 2D push velocity
            // (Note: If the water pushes the opposite way your hand moves, remove the minus signs below!)
            Vector2 canvasVelocity = new Vector2(-smoothVelocity.x, -smoothVelocity.y) * pushStrength;

            // 4. Fire the force into the Marbling Controller
            fluidController.IsApplyingForce = true;
            fluidController.ForcePosition = normalizedPos;
            fluidController.ForceVelocity = canvasVelocity;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Turn off the force immediately when the hand leaves the water
        if (other.CompareTag(targetTag) && fluidController != null)
        {
            fluidController.IsApplyingForce = false;
        }
    }
}