using UnityEngine;

namespace StableFluids.Marbling {

public sealed class MarblingController : MonoBehaviour
{
    [field:SerializeField] public float PointForce { get; set; } = 300;
    [field:SerializeField] public float PointFalloff { get; set; } = 200;

    // --- Separate Slots for Dual Hand Interaction ---
    public Vector2 ForcePosition { get; set; }
    public Vector2 ForceVelocity { get; set; }
    public bool IsApplyingForce { get; set; }

    public Vector2 ColorPosition { get; set; }
    public bool IsApplyingColor { get; set; }

    public Color paintColor = Color.red;

    [SerializeField] RenderTexture _colorInjection = null;
    [SerializeField] RenderTexture _forceField = null;
    [SerializeField, HideInInspector] Shader _shader = null;

    Material _material;

    void Start()
    {
        _material = new Material(_shader);
        if (_forceField != null)
            _material.SetFloat("_Aspect", (float)_forceField.width / _forceField.height);
        
        Graphics.Blit(Texture2D.blackTexture, _colorInjection);
        Graphics.Blit(Texture2D.blackTexture, _forceField);
    }

    void OnDestroy() => Destroy(_material);

    void Update()
    {
        UpdateColorInjection();
        UpdateForceField();
    }

    void UpdateColorInjection()
    {
        if (IsApplyingColor)
        {
            _material.color = paintColor;
            _material.SetVector("_Origin", ColorPosition);
            _material.SetFloat("_Falloff", PointFalloff);
            Graphics.Blit(null, _colorInjection, _material, 0);
        }
        else
        {
            Graphics.Blit(Texture2D.blackTexture, _colorInjection);
        }
    }

    void UpdateForceField()
    {
        if (IsApplyingForce)
        {
            _material.SetVector("_Origin", ForcePosition);
            _material.SetFloat("_Falloff", PointFalloff);
            _material.SetVector("_Force", ForceVelocity * PointForce);
            Graphics.Blit(null, _forceField, _material, 1);
        }
        else
        {
            Graphics.Blit(Texture2D.blackTexture, _forceField);
        }
    }
}

} // namespace StableFluids.Marbling