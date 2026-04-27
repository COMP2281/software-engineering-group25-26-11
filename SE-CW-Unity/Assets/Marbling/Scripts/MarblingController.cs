using UnityEngine;

namespace StableFluids.Marbling {

public sealed class MarblingController : MonoBehaviour
{
    #region Public properties

    [field:SerializeField] public float PointForce { get; set; } = 300;
    [field:SerializeField] public float PointFalloff { get; set; } = 200;

    // --- NEW: Public properties for VR injection ---
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public bool LeftPressed { get; set; }
    public bool RightPressed { get; set; }

    #endregion

    #region Editable attributes

    [SerializeField] RenderTexture _colorInjection = null;
    [SerializeField] RenderTexture _forceField = null;

    #endregion

    #region Project asset references

    [SerializeField] Shader _shader = null;

    #endregion

    #region Private members

    Material _material;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        // Removed the old MarblingInputHandler initialization

        _material = new Material(_shader);
        _material.SetFloat("_Aspect", (float)_forceField.width / _forceField.height);

        Graphics.Blit(Texture2D.blackTexture, _colorInjection);
        Graphics.Blit(Texture2D.blackTexture, _forceField);
    }

    void OnDestroy()
      => Destroy(_material);

    void Update()
    {
        // Removed _input.Update();
        UpdateColorInjection();
        UpdateForceField();
    }

    #endregion

    #region Update methods

    void UpdateColorInjection()
    {
        if (RightPressed)
        {
            _material.color = Color.HSVToRGB(Time.time % 1, 1, 1);
            _material.SetVector("_Origin", Position);
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
        if (RightPressed)
        {
            BlitToForceField(Random.insideUnitCircle * PointForce * 0.025f);
        }
        else if (LeftPressed)
        {
            BlitToForceField(Velocity * PointForce);
        }
        else
        {
            Graphics.Blit(Texture2D.blackTexture, _forceField);
        }
    }

    void BlitToForceField(Vector2 force)
    {
        _material.SetVector("_Origin", Position);
        _material.SetFloat("_Falloff", PointFalloff);
        _material.SetVector("_Force", force);
        Graphics.Blit(null, _forceField, _material, 1);
    }

    #endregion
}

} // namespace StableFluids.Marbling