using Seb.Fluid2D.Simulation;
using Seb.Helpers;
using UnityEngine;

namespace Seb.Fluid2D.Rendering
{
    /// <summary>
    /// Screen-space fluid rendering using SPH-style density accumulation.
    /// Renders particles to a density texture, blurs it, and extracts a smooth surface.
    /// </summary>
    public class FluidRenderer2D : MonoBehaviour
    {
        [Header("References")]
        public FluidSim2D sim;
        public Mesh particleMesh;
        
        [Header("Shaders")]
        public Shader densityShader;      // Renders particles to density texture
        public Shader blurShader;         // Gaussian blur
        public Shader fluidSurfaceShader; // Final fluid surface rendering
        
        [Header("Rendering Settings")]
        public Transform worldAnchor;
        [Range(0.1f, 5f)] public float particleRadius = 1f;
        [Range(1, 10)] public int blurIterations = 3;
        [Range(0.5f, 10f)] public float blurRadius = 2f;
        
        [Header("Fluid Appearance")]
        public Color fluidColor = new Color(0.2f, 0.5f, 0.9f, 0.9f);
        public Color edgeColor = new Color(0.4f, 0.7f, 1f, 1f);
        [Range(0f, 1f)] public float densityThreshold = 0.3f;
        [Range(0f, 1f)] public float edgeWidth = 0.1f;
        [Range(0f, 2f)] public float fresnelPower = 1f;
        
        [Header("Resolution")]
        [Range(0.25f, 1f)] public float resolutionScale = 0.5f;
        
        // Materials
        private Material densityMaterial;
        private Material blurMaterial;
        private Material surfaceMaterial;
        
        // Render Textures
        private RenderTexture densityTexture;
        private RenderTexture blurTempTexture;
        private RenderTexture finalFluidTexture;
        
        private ComputeBuffer argsBuffer;
        private Bounds bounds;
        
        private Camera renderCamera;

        void Start()
        {
            InitializeMaterials();
            CreateRenderTextures();
            
            // Create a camera for rendering the fluid (or use main camera)
            renderCamera = Camera.main;
        }

        void InitializeMaterials()
        {
            if (densityShader != null)
                densityMaterial = new Material(densityShader);
            if (blurShader != null)
                blurMaterial = new Material(blurShader);
            if (fluidSurfaceShader != null)
                surfaceMaterial = new Material(fluidSurfaceShader);
        }

        void CreateRenderTextures()
        {
            int width = Mathf.RoundToInt(Screen.width * resolutionScale);
            int height = Mathf.RoundToInt(Screen.height * resolutionScale);
            
            // Release old textures
            ReleaseRenderTextures();
            
            // Create new textures
            densityTexture = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat);
            densityTexture.filterMode = FilterMode.Bilinear;
            densityTexture.Create();
            
            blurTempTexture = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat);
            blurTempTexture.filterMode = FilterMode.Bilinear;
            blurTempTexture.Create();
            
            finalFluidTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            finalFluidTexture.filterMode = FilterMode.Bilinear;
            finalFluidTexture.Create();
        }

        void ReleaseRenderTextures()
        {
            if (densityTexture != null) densityTexture.Release();
            if (blurTempTexture != null) blurTempTexture.Release();
            if (finalFluidTexture != null) finalFluidTexture.Release();
        }

        void LateUpdate()
        {
            if (sim == null || sim.numParticles <= 0) return;
            if (densityMaterial == null || blurMaterial == null || surfaceMaterial == null) return;
            
            // Check if screen size changed
            int targetWidth = Mathf.RoundToInt(Screen.width * resolutionScale);
            int targetHeight = Mathf.RoundToInt(Screen.height * resolutionScale);
            if (densityTexture == null || densityTexture.width != targetWidth || densityTexture.height != targetHeight)
            {
                CreateRenderTextures();
            }
            
            // Step 1: Render particles to density texture
            RenderDensityPass();
            
            // Step 2: Blur the density texture
            BlurPass();
            
            // Step 3: Render final fluid surface
            RenderFluidSurface();
        }

        void RenderDensityPass()
        {
            // Set up density material
            densityMaterial.SetFloat("_ParticleRadius", particleRadius);
            densityMaterial.SetBuffer("Positions2D", sim.positionBuffer);
            
            // Transform settings
            Matrix4x4 anchorMatrix = worldAnchor != null ? worldAnchor.localToWorldMatrix : Matrix4x4.identity;
            densityMaterial.SetMatrix("_WorldAnchorMatrix", anchorMatrix);
            densityMaterial.SetVector("_SimWorldOffset", new Vector4(sim.worldOffset.x, sim.worldOffset.y, 0f, 0f));
            densityMaterial.SetFloat("_SimWorldScale", sim.worldScale);
            
            // Set up args buffer for instanced rendering
            ComputeHelper.CreateArgsBuffer(ref argsBuffer, particleMesh, sim.numParticles);
            Vector3 centre = worldAnchor != null ? worldAnchor.position : Vector3.zero;
            bounds = new Bounds(centre, Vector3.one * 10000);
            
            // Clear and render to density texture
            RenderTexture previousRT = RenderTexture.active;
            RenderTexture.active = densityTexture;
            GL.Clear(true, true, Color.clear);
            
            Graphics.DrawMeshInstancedIndirect(particleMesh, 0, densityMaterial, bounds, argsBuffer);
            
            RenderTexture.active = previousRT;
        }

        void BlurPass()
        {
            RenderTexture src = densityTexture;
            RenderTexture dst = blurTempTexture;
            
            for (int i = 0; i < blurIterations; i++)
            {
                // Horizontal blur
                blurMaterial.SetVector("_BlurDirection", new Vector4(blurRadius / src.width, 0, 0, 0));
                Graphics.Blit(src, dst, blurMaterial);
                
                // Vertical blur
                blurMaterial.SetVector("_BlurDirection", new Vector4(0, blurRadius / src.height, 0, 0));
                Graphics.Blit(dst, src, blurMaterial);
            }
        }

        void RenderFluidSurface()
        {
            surfaceMaterial.SetTexture("_DensityTex", densityTexture);
            surfaceMaterial.SetColor("_FluidColor", fluidColor);
            surfaceMaterial.SetColor("_EdgeColor", edgeColor);
            surfaceMaterial.SetFloat("_DensityThreshold", densityThreshold);
            surfaceMaterial.SetFloat("_EdgeWidth", edgeWidth);
            surfaceMaterial.SetFloat("_FresnelPower", fresnelPower);
            
            // Render full-screen quad with the fluid surface shader
            Graphics.Blit(densityTexture, finalFluidTexture, surfaceMaterial);
        }

        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (finalFluidTexture != null && surfaceMaterial != null && sim != null && sim.numParticles > 0)
            {
                // Composite the fluid on top of the scene
                surfaceMaterial.SetTexture("_MainTex", src);
                surfaceMaterial.SetTexture("_DensityTex", densityTexture);
                Graphics.Blit(src, dest, surfaceMaterial, 1); // Pass 1 for compositing
            }
            else
            {
                Graphics.Blit(src, dest);
            }
        }

        void OnDestroy()
        {
            ReleaseRenderTextures();
            ComputeHelper.Release(argsBuffer);
            
            if (densityMaterial != null) Destroy(densityMaterial);
            if (blurMaterial != null) Destroy(blurMaterial);
            if (surfaceMaterial != null) Destroy(surfaceMaterial);
        }

        void OnValidate()
        {
            if (Application.isPlaying && densityTexture != null)
            {
                // Recreate textures if resolution scale changed
                CreateRenderTextures();
            }
        }
    }
}
