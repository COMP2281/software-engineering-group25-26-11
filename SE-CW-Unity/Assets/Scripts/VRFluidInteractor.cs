using UnityEngine;
using StableFluids.Marbling;

public class VRFluidInteractor : MonoBehaviour
{
    [Header("VR Setup")]
    [Tooltip("The transform from which the ray is cast (e.g., Right Controller).")]
    public Transform controllerTransform;
    [Tooltip("Map this to your Quest controller trigger button using the new Input System.")]
    public UnityEngine.InputSystem.InputActionReference triggerAction; 
    public float interactionDistance = 10f;
    public LayerMask canvasLayer; // Set this to 'FluidCanvasLayer'

    [Header("Simulation References")]
    public MarblingController fluidController;
    public RenderTexture targetTexture; // E.g., the _forceField or _colorInjection texture

    private Vector2 _previousUV;
    private bool _wasInteracting = false;
    private float _targetAspectRatio;

    void Start()
    {
        if (targetTexture != null)
            _targetAspectRatio = (float)targetTexture.width / targetTexture.height;
    }

    void Update()
    {
        Debug.DrawRay(controllerTransform.position, controllerTransform.forward * interactionDistance, Color.red);
        // Read trigger state (True if pulled, False if released)
        bool isTriggerPulled = triggerAction != null && triggerAction.action.IsPressed();
        if (isTriggerPulled) Debug.Log("<color=green>INPUT: Trigger is being detected as PRESSED!</color>");

        Ray ray = new Ray(controllerTransform.position, controllerTransform.forward);
        
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, canvasLayer))
        {
            Debug.Log("<color=blue>RAYCAST: Hit something on the Fluid Layer!</color>");
            // The magic happens here: getting the UV coordinate of the mesh hit
            Vector2 hitUV = hit.textureCoord;

            if (hitUV == Vector2.zero) 
            {
                Debug.LogWarning("RAYCAST: Hit the object, but UV is (0,0). Is there a MeshCollider attached?");
            }

            // Convert UV (0 to 1) to StableFluids normalized space (-0.5 to 0.5)
            Vector2 normalizedPos = new Vector2(
                (hitUV.x - 0.5f) * _targetAspectRatio, 
                hitUV.y - 0.5f
            );

            // Calculate velocity based on difference from last frame
            Vector2 velocity = _wasInteracting ? (normalizedPos - _previousUV) : Vector2.zero;
            
            
            // Send to the modified MarblingController
            fluidController.Position = normalizedPos;
            fluidController.Velocity = velocity;
            
            // We simulate 'LeftPressed' to push fluid around, and 'RightPressed' could be a secondary button for injecting color
            fluidController.LeftPressed = isTriggerPulled; 
            fluidController.RightPressed = false; // Map this to another button if you want color injection!

            _previousUV = normalizedPos;
            _wasInteracting = true;
        }
        else
        {
            // Raycast missed the canvas, reset inputs
            if (_wasInteracting)
            {
                fluidController.LeftPressed = false;
                fluidController.RightPressed = false;
                fluidController.Velocity = Vector2.zero;
                _wasInteracting = false;
            }
        }
    }
}