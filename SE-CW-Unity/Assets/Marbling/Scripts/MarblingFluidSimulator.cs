using UnityEngine;

namespace StableFluids.Marbling {

public sealed class MarblingFluidSimulator : MonoBehaviour
{
    #region Public properties

    [field:SerializeField] public float Viscosity { get; set; } = 1e-6f;

    #endregion

    #region Editable attributes

    [SerializeField] RenderTexture _velocityField = null;
    [SerializeField] RenderTexture _forceField = null;

    #endregion

    #region Project asset references

    [SerializeField, HideInInspector] Shader _kernelShader = null;

    #endregion

    #region Private objects

    FluidSimulation _simulation;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _simulation = new FluidSimulation(_velocityField, _kernelShader);
        _simulation.ClearVelocityField();
    }

    void OnDestroy()
    {
        if (_simulation != null)
        {
            _simulation.Dispose();
            _simulation = null;
        }
    }

    void Update()
    {
        if (_simulation == null) return;
        
        _simulation.Viscosity = Viscosity;
        _simulation.PreStep();
        _simulation.ApplyForceField(_forceField);
        _simulation.PostStep();
    }

    // THE ULTIMATE RESET
    public void ResetSimulation()
    {
        // 1. Destroy the entire physics engine, wiping all 6 hidden memory buffers instantly
        if (_simulation != null)
        {
            _simulation.Dispose(); 
        }

        // 2. Clear the external memory textures
        if (_velocityField != null) Graphics.Blit(Texture2D.blackTexture, _velocityField);
        if (_forceField != null) Graphics.Blit(Texture2D.blackTexture, _forceField);

        // 3. Rebuild a completely fresh, 100% clean physics engine from scratch
        _simulation = new FluidSimulation(_velocityField, _kernelShader);

        Debug.Log("Fluid Physics Hard Factory Reset Complete.");
    }

    #endregion
}

} // namespace StableFluids.Marbling