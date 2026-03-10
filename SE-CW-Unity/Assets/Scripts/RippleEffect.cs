using Seb.Fluid2D.Rendering;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.Rendering;

public class RippleEffect : MonoBehaviour
{
    public static RippleEffect Instance;

    public float timeBetweenRipples = 0.2f;
    public Collider waterCollider;
    public ParticleSystem splashParticleSystem;
    public bool isPaused = false;

    public int TextureWidth  = 1024;
    public int TextureHeight = 400;
    public RenderTexture ObjectsRT;

    // CurrRT / PrevRT are PURE wave-height buffers — ObjectsRT is never baked
    // into them.  TempRT is used both as a working scratch buffer during wave
    // propagation and as the final display buffer (wave + ObjectsRT) that is
    // bound to the renderer's _RippleTex each frame.
    private RenderTexture CurrRT, PrevRT, TempRT;
    public  RenderTexture RippleRT => CurrRT;

    public Shader RippleShader, AddShader;

    [Header("Ripple Stamp")]
    [Tooltip("RippleStamp shader — stamps a circular displacement at a UV hit point.")]
    public Shader StampShader;
    [Tooltip("Radius of the stamped ripple in UV space (0.005 = tiny, 0.1 = ~10 % of surface).")]
    [Range(0.005f, 0.2f)]
    public float rippleRadius   = 0.03f;
    [Tooltip("Peak displacement strength of the stamped ripple.")]
    [Range(0.1f, 3f)]
    public float rippleStrength = 1.0f;

    private Material RippleMat, AddMat, StampMat;
    private float    lastRippleTime;
    private Camera   _mainCam;
    private Renderer _renderer;   // cached — bounds used for UV mapping & RT sizing

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        _mainCam  = Camera.main;
        _renderer = GetComponent<Renderer>();

        // Use the RENDERER's world-space bounds as the single source of truth.
        // The renderer bounds exactly match the surface area on which _RippleTex
        // is displayed, so UV derived from those bounds is pixel-perfect at every
        // point — including the edges.  The collider is used only for raycasting.
        Bounds b = _renderer.bounds;
        ComputeTextureDimensions(b.size.x, b.size.y, out TextureWidth, out TextureHeight);
        CreateRenderTextures();

        RippleMat = new Material(RippleShader);
        AddMat    = new Material(AddShader);

        if (StampShader != null)
            StampMat = new Material(StampShader);
        else
            Debug.LogWarning("RippleEffect: StampShader is not assigned — ripples will not appear at raycast/touch points.");

        // Bind the display buffer.  TempRT will be updated each ripples() step.
        _renderer.material.SetTexture("_RippleTex", TempRT);

        StartCoroutine(ripples());
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Stamps a ripple at <paramref name="worldPoint"/> (must be on the water
    /// surface) and emits a splash particle.
    /// </summary>
    public void RippleAtPoint(Vector3 worldPoint)
    {
        // Don't create ripples when paused
        if (isPaused) return;
        
        if (splashParticleSystem != null)
        {
            splashParticleSystem.transform.position = worldPoint;
            splashParticleSystem.Emit(1);
        }

        // Map world hit point to UV using the RENDERER's world-space bounds.
        // Renderer bounds == exact pixel extent of the displayed surface, so this
        // UV matches the shader's sampling coordinates at every point, including edges.
        Bounds b = _renderer.bounds;
        float u = Mathf.Clamp01(1f - (worldPoint.x - b.min.x) / b.size.x); // negate X
        float v = Mathf.Clamp01(1f - (worldPoint.y - b.min.y) / b.size.y); // negate Y (UV origin is top-left)

        StampRipple(u, v);
    }

    /// <summary>
    /// Toggles the ripple simulation pause state.
    /// </summary>
    public void TogglePause()
    {
        isPaused = !isPaused;
    }

    /// <summary>
    /// Sets the pause state directly.
    /// </summary>
    public void SetPaused(bool paused)
    {
        isPaused = paused;
        
        // Pause or resume the splash particle system
        if (splashParticleSystem != null)
        {
            if (paused)
            {
                splashParticleSystem.Pause();
            }
            else
            {
                splashParticleSystem.Play();
            }
        }
        
        // Pause or resume all Animator components in the scene
        Animator[] animators = FindObjectsOfType<Animator>();
        foreach (Animator animator in animators)
        {
            if (animator != null)
            {
                animator.enabled = !paused;
            }
        }
        
        // Pause or resume all ParticleSystem components in the scene
        ParticleSystem[] particleSystems = FindObjectsOfType<ParticleSystem>();
        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps != null)
            {
                if (paused)
                {
                    ps.Pause();
                }
                else
                {
                    ps.Play();
                }
            }
        }
    }

    /// <summary>
    /// Rebuilds the RenderTextures so their pixel aspect matches the water
    /// surface's current world-space dimensions.  Call this whenever the WaterCube
    /// is resized so that the wave equation produces circular (not elliptical) ripples.
    /// Reads directly from <see cref="waterCollider"/>.bounds — no parameters needed.
    /// </summary>
    public void UpdateWaterScale()
    {
        if (_renderer == null) _renderer = GetComponent<Renderer>();
        Bounds b = _renderer.bounds;
        if (b.size.x <= 0f || b.size.y <= 0f) return;

        ComputeTextureDimensions(b.size.x, b.size.y, out int newW, out int newH);
        if (newW == TextureWidth && newH == TextureHeight) return;

        TextureWidth  = newW;
        TextureHeight = newH;

        // Stash old wave buffers before overwriting the references.
        RenderTexture oldCurr = CurrRT;
        RenderTexture oldPrev = PrevRT;
        TempRT?.Release();   // display buffer — contents are regenerated each frame anyway

        CreateRenderTextures();   // assigns new CurrRT / PrevRT / TempRT at the new size

        // Scale-blit old wave content into the new buffers so the simulation
        // doesn't reset to zero every time dimensions cross an integer-pixel
        // boundary during a slider drag.
        if (oldCurr != null) { Graphics.Blit(oldCurr, CurrRT); oldCurr.Release(); }
        if (oldPrev != null) { Graphics.Blit(oldPrev, PrevRT); oldPrev.Release(); }

        _renderer.material.SetTexture("_RippleTex", TempRT);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Keeps one axis at 1 024 and derives the other from the world aspect ratio
    /// so every texel covers the same physical distance in X and Z.
    /// </summary>
    private static void ComputeTextureDimensions(float worldW, float worldD,
                                                 out int texW, out int texH)
    {
        const int kBase = 1024;
        float aspect = Mathf.Max(worldW, 0.001f) / Mathf.Max(worldD, 0.001f);
        if (aspect >= 1f)
        {
            texW = kBase;
            texH = Mathf.Max(64, Mathf.RoundToInt(kBase / aspect));
        }
        else
        {
            texH = kBase;
            texW = Mathf.Max(64, Mathf.RoundToInt(kBase * aspect));
        }
    }

    private void CreateRenderTextures()
    {
        CurrRT = new RenderTexture(TextureWidth, TextureHeight, 0, RenderTextureFormat.RFloat);
        PrevRT = new RenderTexture(TextureWidth, TextureHeight, 0, RenderTextureFormat.RFloat);
        TempRT = new RenderTexture(TextureWidth, TextureHeight, 0, RenderTextureFormat.RFloat);
    }

    /// <summary>
    /// Stamps a circular bump into CurrRT (the pure-wave buffer) at UV (u, v).
    /// Uses TempRT as a scratch buffer; TempRT's display content is intentionally
    /// overwritten — ripples() will regenerate it on the next frame.
    /// </summary>
    private void StampRipple(float u, float v)
    {
        if (StampMat == null || CurrRT == null || TempRT == null) return;

        // Pass world dimensions so the stamp shader can compute world-space
        // distance and produce a circle on the physical surface (not a UV ellipse).
        Bounds b = _renderer.bounds;
        StampMat.SetVector ("_HitUV",      new Vector4(u, v, 0f, 0f));
        StampMat.SetFloat  ("_Radius",     rippleRadius);
        StampMat.SetFloat  ("_Strength",   rippleStrength);
        StampMat.SetFloat  ("_WorldWidth",  b.size.x);
        StampMat.SetFloat  ("_WorldHeight", b.size.y);
        StampMat.SetTexture("_CurrentRT",  CurrRT);

        // TempRT = CurrRT + bump
        Graphics.Blit(null, TempRT, StampMat);
        // CurrRT = stamped result (pure wave buffer is updated; TempRT is now stale
        // display-wise but ripples() will refresh it before the next render)
        Graphics.Blit(TempRT, CurrRT);
    }

    // -------------------------------------------------------------------------
    // Frame loop
    // -------------------------------------------------------------------------

    private void Update()
    {
        // Don't process mouse input when paused
        if (isPaused) return;
        
        if (Input.GetMouseButton(0) && Time.time - lastRippleTime >= timeBetweenRipples)
        {
            Ray ray = _mainCam.ScreenPointToRay(Input.mousePosition);
            if (waterCollider.Raycast(ray, out RaycastHit hit, float.MaxValue))
            {
                RippleAtPoint(hit.point);
                lastRippleTime = Time.time;
                
                // Reset inactivity timer when mouse creates ripples
                if (InactivityWarning.Instance != null)
                {
                    InactivityWarning.Instance.RegisterActivity();
                }
            }
        }
    }

    IEnumerator ripples()
    {
        // Skip wave propagation if paused
        if (!isPaused)
        {
            // ── Step 1: Wave propagation on pure wave buffers ─────────────────────
            // CurrRT and PrevRT contain ONLY wave displacement — no ObjectsRT mixed
            // in — so the wave equation never sees a permanent height bias that would
            // generate a restoring (backwards-travelling) wave.
            RippleMat.SetTexture("_PrevRT",    PrevRT);
            RippleMat.SetTexture("_CurrentRT", CurrRT);
            Graphics.Blit(null, TempRT, RippleMat);
            // TempRT = wave(CurrRT, PrevRT)

            // ── Step 2: Rotate pure-wave ping-pong ───────────────────────────────
            // PrevRT ← CurrRT  (previous pure-wave step)
            // CurrRT ← TempRT  (new pure-wave step)
            // TempRT ← old PrevRT  (now free — will become the display buffer)
            RenderTexture oldPrev = PrevRT;
            PrevRT = CurrRT;   // pure previous
            CurrRT = TempRT;   // pure current
            TempRT = oldPrev;  // free scratch

            // ── Step 3: Build display buffer = pure wave + ObjectsRT ──────────────
            // This is written into TempRT only; it does NOT flow back into CurrRT or
            // PrevRT, so ObjectsRT can never contaminate the wave simulation.
            AddMat.SetTexture("_ObjectsRT", ObjectsRT);
            AddMat.SetTexture("_CurrentRT", CurrRT);
            Graphics.Blit(null, TempRT, AddMat);
            // TempRT = CurrRT + ObjectsRT  (display-only)

            _renderer.material.SetTexture("_RippleTex", TempRT);
        }

        yield return null;
        StartCoroutine(ripples());
    }
}
