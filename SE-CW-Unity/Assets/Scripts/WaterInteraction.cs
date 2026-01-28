using UnityEngine;

public class WaterInteraction : MonoBehaviour
{
    public Camera mainCamera;
    public Collider waterCollider;
    public RenderTexture rippleTexture;
    public float splashSize = 0.05f;
    public float timeBetweenRipples = 0.05f; // Cooldown between ripples

    private Material drawMaterial;
    private float lastRippleTime;

    void Start()
    {
        // Create a simple white material for drawing splashes
        drawMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
    }

    void Update()
    {
        // GetMouseButton instead of GetMouseButtonDown for continuous drawing while dragging
        if (Input.GetMouseButton(0) && Time.time - lastRippleTime >= timeBetweenRipples)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (waterCollider.Raycast(ray, out RaycastHit hit, float.MaxValue))
            {
                // Convert hit point to UV coordinates (0-1 range)
                Bounds bounds = waterCollider.bounds;
                
                // Flip both coordinates to fix the mirroring
                float u = 1.0f - (hit.point.x - bounds.min.x) / bounds.size.x;
                float v = 1.0f - (hit.point.y - bounds.min.y) / bounds.size.y;
                
                // Draw directly to the ripple texture
                DrawSplash(u, v);
                lastRippleTime = Time.time;
            }
        }
    }

    void DrawSplash(float u, float v)
    {
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rippleTexture;
        
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, rippleTexture.width, rippleTexture.height, 0);
        
        drawMaterial.SetPass(0);
        
        float x = u * rippleTexture.width;
        float y = (1 - v) * rippleTexture.height;
        float radius = splashSize * rippleTexture.width;
        
        // Adjust for water aspect ratio
        Bounds bounds = waterCollider.bounds;
        float aspectRatio = bounds.size.x / bounds.size.y;
        
        // Draw a simple circle using 8 triangles (fast)
        GL.Begin(GL.TRIANGLES);
        GL.Color(Color.white);
        
        int segments = 8;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (float)i / segments * Mathf.PI * 2;
            float angle2 = (float)(i + 1) / segments * Mathf.PI * 2;
            
            GL.Vertex3(x, y, 0);
            GL.Vertex3(x + Mathf.Cos(angle1) * radius, y + Mathf.Sin(angle1) * radius * aspectRatio, 0);
            GL.Vertex3(x + Mathf.Cos(angle2) * radius, y + Mathf.Sin(angle2) * radius * aspectRatio, 0);
        }
        
        GL.End();
        GL.PopMatrix();
        
        RenderTexture.active = prev;
    }
}
