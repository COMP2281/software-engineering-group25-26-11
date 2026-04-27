using UnityEngine;
using StableFluids.Marbling;
using UnityEngine.InputSystem;

public class VRFluidInteractor : MonoBehaviour
{
    public enum HandRole { PushForce, AddColor }

    [Header("Hand Configuration")]
    public HandRole role; // Set this in the Inspector!
    public Transform controllerTransform;
    public InputActionReference triggerAction; 

    [Header("Setup")]
    public LayerMask canvasLayer;
    public MarblingController fluidController;
    public RenderTexture targetTexture;
    public float interactionDistance = 10f;

    private Vector2 _previousUV;
    private bool _wasInteracting = false;
    private float _targetAspectRatio;

    void OnEnable() { if (triggerAction != null) triggerAction.action.Enable(); }

    void Start()
    {
        if (targetTexture != null)
            _targetAspectRatio = (float)targetTexture.width / targetTexture.height;
    }

    void Update()
    {
        bool isTriggerPulled = triggerAction != null && triggerAction.action.IsPressed();
        Ray ray = new Ray(controllerTransform.position, controllerTransform.forward);
        
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, canvasLayer))
        {
            Vector2 hitUV = hit.textureCoord;
            Vector2 normalizedPos = new Vector2((hitUV.x - 0.5f) * _targetAspectRatio, hitUV.y - 0.5f);
            Vector2 velocity = _wasInteracting ? (normalizedPos - _previousUV) : Vector2.zero;

            // Apply data to the specific "Role" slots in the controller
            if (role == HandRole.PushForce)
            {
                fluidController.ForcePosition = normalizedPos;
                fluidController.ForceVelocity = velocity;
                fluidController.IsApplyingForce = isTriggerPulled;
            }
            else if (role == HandRole.AddColor)
            {
                fluidController.ColorPosition = normalizedPos;
                fluidController.IsApplyingColor = isTriggerPulled;
            }

            _previousUV = normalizedPos;
            _wasInteracting = true;
        }
        else
        {
            if (_wasInteracting)
            {
                if (role == HandRole.PushForce) fluidController.IsApplyingForce = false;
                else fluidController.IsApplyingColor = false;
                _wasInteracting = false;
            }
        }
    }
}