using UnityEngine;
using Seb.Fluid2D.Simulation;
using Unity.Mathematics;
using System.Collections.Generic;

namespace Seb.Fluid2D.Rendering
{
    /// Renders the fluid simulation as a dynamic triangle mesh instead of individual ball-shaped particles.
    /// Particles act as invisible vertex points; a Delaunay triangulation connects neighbouring
    /// particles (within a radius) into a continuous surface mesh that deforms with the simulation
    /// every frame.

    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class MeshFluidDisplay2D : MonoBehaviour
    {
        // ───────── Inspector ─────────
        public FluidSim2D sim;
        public Transform worldAnchor;

        public float maxEdgeLength = 4f;

        public bool autoMaxEdgeLength = true;

        [Range(1.0f, 4.0f)]
        public float edgeLengthFactor = 2.5f;

        [Range(0, 5)]
        public int smoothingIterations = 2;

        [Range(0f, 1f)]
        public float smoothingStrength = 0.5f;

        [Range(0, 3)]
        public int colorBlendingPasses = 2;

        [Range(0f, 1f)]
        public float colorBlendingStrength = 0.6f;

        [Range(0f, 1f)]
        public float baseAlpha = 0.85f;

        public RippleEffect rippleEffect;

        Mesh fluidMesh;
        MeshFilter meshFilter;
        MeshRenderer meshRenderer;

        // Cached renderer on the WaterCube
        Renderer _rippleRenderer;

        // GPU read-back buffers 
        float2[] readPos;
        float4[] readCol;

        // Mesh data
        readonly List<Vector3> meshVerts = new List<Vector3>();
        readonly List<Color> meshCols = new List<Color>();
        readonly List<int> meshTris = new List<int>();

        // Smoothing data
        readonly List<Vector3> smoothedVerts = new List<Vector3>();
        readonly List<Color> blendedColors = new List<Color>();
        readonly Dictionary<int, List<int>> vertexNeighbors = new Dictionary<int, List<int>>();

        // Triangulator 
        readonly Delaunay2D delaunay = new Delaunay2D();

        // Lifecycle 
        void Start()
        {
            fluidMesh = new Mesh { name = "FluidMesh2D" };
            fluidMesh.MarkDynamic(); // optimise for frequent updates

            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshFilter.mesh = fluidMesh;

            if (sim != null)
            {
                // Also auto-resolve world anchor
                if (worldAnchor == null && sim.particleDisplay != null)
                    worldAnchor = sim.particleDisplay.worldAnchor;

                // Auto-disable ParticleDisplay2D so the old balls disappear
                if (sim.particleDisplay != null && sim.particleDisplay.enabled)
                {
                    sim.particleDisplay.enabled = false;
                    Debug.Log("[MeshFluidDisplay2D] Auto-disabled ParticleDisplay2D (ball rendering).");
                }
            }

            // Auto-resolve RippleEffect
            if (rippleEffect == null)
                rippleEffect = RippleEffect.Instance;

            // Cache the WaterCube renderer so LateUpdate can read its world bounds
            // without a per-frame GetComponent call.
            if (rippleEffect != null)
                _rippleRenderer = rippleEffect.GetComponent<Renderer>();
        }

        void LateUpdate()
        {
            if (sim == null || sim.numParticles < 3)
            {
                if (fluidMesh != null) fluidMesh.Clear();
                return;
            }

            int n = sim.numParticles;

            // Resize read-back arrays when particle count changes
            if (readPos == null || readPos.Length < n)
            {
                readPos = new float2[n];
                readCol = new float4[n];
            }

            // GPU → CPU  
            sim.positionBuffer.GetData(readPos, 0, 0, n);
            sim.colorBuffer.GetData(readCol, 0, 0, n);

            float maxE = autoMaxEdgeLength
                ? sim.smoothingRadius * edgeLengthFactor
                : maxEdgeLength;

            RebuildMesh(n, maxE);

            // Push ripple RT and world-space bounds so the shader UV matches the
            // stamp UV used by RippleEffect.RippleAtPoint exactly.
            if (rippleEffect != null && rippleEffect.RippleRT != null)
            {
                meshRenderer.material.SetTexture("_RippleTex", rippleEffect.RippleRT);

                if (_rippleRenderer != null)
                {
                    // Bounds are re-read every frame so dynamic WaterCube resizes
                    // propagate to the mesh automatically.
                    Bounds b = _rippleRenderer.bounds;
                    meshRenderer.material.SetVector("_RippleBoundsMin",
                        new Vector4(b.min.x, b.min.y, 0f, 0f));
                    meshRenderer.material.SetVector("_RippleBoundsSize",
                        new Vector4(b.size.x, b.size.y, 0f, 0f));
                }
            }
        }

        // Mesh construction 
        void RebuildMesh(int n, float maxEdge)
        {
            // Delaunay triangulate in sim-space, then alpha-shape filter
            meshTris.Clear();
            delaunay.Run(readPos, n, maxEdge * maxEdge, meshTris);

            if (meshTris.Count == 0)
            {
                fluidMesh.Clear();
                return;
            }

            // Map sim-space positions → mesh-local vertices
            float scale = Mathf.Approximately(sim.worldScale, 0f) ? 1f : sim.worldScale;
            Vector3 off = sim.worldOffset;

            Transform anchor = ResolveAnchor();
            Matrix4x4 anchorToWorld = anchor != null ? anchor.localToWorldMatrix : Matrix4x4.identity;
            Matrix4x4 worldToMesh = transform.worldToLocalMatrix;
            Matrix4x4 simToMesh = worldToMesh * anchorToWorld;

            meshVerts.Clear();
            meshCols.Clear();

            for (int i = 0; i < n; i++)
            {
                float2 p = readPos[i];
                Vector3 anchorLocal = new Vector3(
                    p.x * scale + off.x,
                    p.y * scale + off.y,
                    off.z);
                meshVerts.Add(simToMesh.MultiplyPoint3x4(anchorLocal));

                float4 c = readCol[i];
                meshCols.Add(new Color(c.x, c.y, c.z, baseAlpha));
            }

            // Apply Laplacian smoothing for curved, liquid-like edges
            if (smoothingIterations > 0 && smoothingStrength > 0f)
            {
                ApplyLaplacianSmoothing(n);
            }

            // Apply color blending for smooth color transitions
            if (colorBlendingPasses > 0 && colorBlendingStrength > 0f)
            {
                ApplyColorBlending(n);
            }

            // Upload to mesh (no need for RecalculateNormals in 2D)
            fluidMesh.Clear();
            fluidMesh.SetVertices(smoothingIterations > 0 ? smoothedVerts : meshVerts);
            fluidMesh.SetColors(colorBlendingPasses > 0 ? blendedColors : meshCols);
            fluidMesh.SetTriangles(meshTris, 0);
            
            // Compute simple forward-facing normals for 2D 
            SetForwardNormals(smoothingIterations > 0 ? smoothedVerts.Count : meshVerts.Count);
            
            fluidMesh.RecalculateBounds();
        }

        Transform ResolveAnchor()
        {
            if (worldAnchor != null) return worldAnchor;
            if (sim != null && sim.particleDisplay != null) return sim.particleDisplay.worldAnchor;
            return null;
        }

        // Smoothing 
        void ApplyLaplacianSmoothing(int vertexCount)
        {
            // Build neighbor connectivity from triangles
            BuildNeighborConnectivity(vertexCount);

            // Initialize smoothed vertices
            smoothedVerts.Clear();
            smoothedVerts.AddRange(meshVerts);

            // Iterative Laplacian smoothing
            for (int iter = 0; iter < smoothingIterations; iter++)
            {
                List<Vector3> tempVerts = new List<Vector3>(smoothedVerts);

                for (int i = 0; i < vertexCount; i++)
                {
                    if (!vertexNeighbors.ContainsKey(i)) continue;
                    
                    List<int> neighbors = vertexNeighbors[i];
                    if (neighbors.Count == 0) continue;

                    // Average neighbor positions
                    Vector3 avgPos = Vector3.zero;
                    foreach (int neighborIdx in neighbors)
                    {
                        avgPos += smoothedVerts[neighborIdx];
                    }
                    avgPos /= neighbors.Count;

                    // Lerp between current position and averaged neighbor position
                    tempVerts[i] = Vector3.Lerp(smoothedVerts[i], avgPos, smoothingStrength);
                }

                // Copy back to smoothedVerts without reassigning the readonly field
                smoothedVerts.Clear();
                smoothedVerts.AddRange(tempVerts);
            }
        }

        void AddNeighbor(int vertex, int neighbor)
        {
            if (!vertexNeighbors[vertex].Contains(neighbor))
            {
                vertexNeighbors[vertex].Add(neighbor);
            }
        }

        void ApplyColorBlending(int vertexCount)
        {
            // Use the neighbor connectivity already built by ApplyLaplacianSmoothing
            // If smoothing wasn't run, build it now
            if (vertexNeighbors.Count == 0)
            {
                BuildNeighborConnectivity(vertexCount);
            }

            // Initialize blended colors
            blendedColors.Clear();
            blendedColors.AddRange(meshCols);

            // Iterative color blending
            for (int iter = 0; iter < colorBlendingPasses; iter++)
            {
                List<Color> tempColors = new List<Color>(blendedColors);

                for (int i = 0; i < vertexCount; i++)
                {
                    if (!vertexNeighbors.ContainsKey(i)) continue;
                    
                    List<int> neighbors = vertexNeighbors[i];
                    if (neighbors.Count == 0) continue;

                    // Average neighbor colors (in RGB space)
                    Vector4 avgColor = Vector4.zero;
                    foreach (int neighborIdx in neighbors)
                    {
                        Color neighborCol = blendedColors[neighborIdx];
                        avgColor += new Vector4(neighborCol.r, neighborCol.g, neighborCol.b, neighborCol.a);
                    }
                    avgColor /= neighbors.Count;

                    // Lerp between current color and averaged neighbor color
                    Color currentColor = blendedColors[i];
                    Color targetColor = new Color(avgColor.x, avgColor.y, avgColor.z, avgColor.w);
                    tempColors[i] = Color.Lerp(currentColor, targetColor, colorBlendingStrength);
                }

                // Copy back to blendedColors
                blendedColors.Clear();
                blendedColors.AddRange(tempColors);
            }
        }

        void BuildNeighborConnectivity(int vertexCount)
        {
            vertexNeighbors.Clear();
            for (int i = 0; i < vertexCount; i++)
            {
                vertexNeighbors[i] = new List<int>();
            }

            // Extract edges from triangles
            for (int i = 0; i < meshTris.Count; i += 3)
            {
                int a = meshTris[i];
                int b = meshTris[i + 1];
                int c = meshTris[i + 2];

                AddNeighbor(a, b);
                AddNeighbor(b, a);
                AddNeighbor(b, c);
                AddNeighbor(c, b);
                AddNeighbor(c, a);
                AddNeighbor(a, c);
            }
        }

        void SetForwardNormals(int vertexCount)
        {
            // For 2D fluid, all normals point forward 
            // This is much faster than RecalculateNormals()
            Vector3[] normals = new Vector3[vertexCount];
            Vector3 forward = Vector3.forward;
            
            for (int i = 0; i < vertexCount; i++)
            {
                normals[i] = forward;
            }
            
            fluidMesh.normals = normals;
        }

        void OnDestroy()
        {
            if (fluidMesh != null) Destroy(fluidMesh);
        }

        //  Bowyer–Watson incremental Delaunay triangulation  
        sealed class Delaunay2D
        {
            // SoA triangle storage 
            int triCap;
            int triCount;
            int[] va, vb, vc;             // vertex indices
            float[] cxArr, cyArr, crSqArr; // circumcentre x, y  &  circumradius²
            bool[] alive;

            // Point buffer  
            float2[] pts;

            // Super-triangle vertex indices
            int sA, sB, sC;

            // Scratch lists
            readonly List<int> bad = new List<int>(64);
            readonly List<int> polyA = new List<int>(64);
            readonly List<int> polyB = new List<int>(64);

            /// Triangulate
            public void Run(float2[] positions, int n, float maxEdgeSq, List<int> outTris)
            {
                outTris.Clear();
                if (n < 3) return;

                // prepare point array (real + 3 super) 
                int total = n + 3;
                if (pts == null || pts.Length < total)
                    pts = new float2[total];
                System.Array.Copy(positions, pts, n);

                // Bounding box
                float2 mn = positions[0], mx = positions[0];
                for (int i = 1; i < n; i++)
                {
                    mn = math.min(mn, positions[i]);
                    mx = math.max(mx, positions[i]);
                }
                float2 ctr = (mn + mx) * 0.5f;
                float span = math.max(mx.x - mn.x, mx.y - mn.y) + 1f;
                float d = span * 10f; 

                sA = n; sB = n + 1; sC = n + 2;
                pts[sA] = ctr + new float2(-d, -d * 0.5f);
                pts[sB] = ctr + new float2(d * 2f, -d * 0.5f);
                pts[sC] = ctr + new float2(0, d * 2f);

                // allocate / reset triangle storage 
                int initCap = Mathf.Max(n * 10 + 10, 128);
                EnsureTriArrays(initCap);
                triCount = 0;

                AddTri(sA, sB, sC);

                // incremental insertion 
                for (int i = 0; i < n; i++)
                    InsertPoint(i);

                // collect output (skip super-triangle verts & long edges) 
                for (int t = 0; t < triCount; t++)
                {
                    if (!alive[t]) continue;
                    int a = va[t], b = vb[t], c = vc[t];
                    if (a >= n || b >= n || c >= n) continue; // touches super

                    float2 pa = pts[a], pb = pts[b], pc = pts[c];
                    float e1 = math.lengthsq(pb - pa);
                    float e2 = math.lengthsq(pc - pb);
                    float e3 = math.lengthsq(pa - pc);
                    if (e1 > maxEdgeSq || e2 > maxEdgeSq || e3 > maxEdgeSq) continue;

                    outTris.Add(a);
                    outTris.Add(b);
                    outTris.Add(c);
                }
            }

            // single point insertion
            void InsertPoint(int pi)
            {
                float px = pts[pi].x, py = pts[pi].y;

                // Find every triangle whose circumcircle contains the point
                bad.Clear();
                for (int t = 0; t < triCount; t++)
                {
                    if (!alive[t]) continue;
                    float dx = px - cxArr[t];
                    float dy = py - cyArr[t];
                    if (dx * dx + dy * dy - crSqArr[t] < 1e-6f)
                        bad.Add(t);
                }

                // Boundary polygon = edges of bad triangles NOT shared by another bad triangle
                polyA.Clear();
                polyB.Clear();
                int badCount = bad.Count;
                for (int bi = 0; bi < badCount; bi++)
                {
                    int t = bad[bi];
                    TryAddEdge(va[t], vb[t], bi);
                    TryAddEdge(vb[t], vc[t], bi);
                    TryAddEdge(vc[t], va[t], bi);
                }

                // Kill bad triangles
                for (int bi = 0; bi < badCount; bi++)
                    alive[bad[bi]] = false;

                // Create fan of new triangles from the point to each boundary edge
                int edgeCount = polyA.Count;
                for (int e = 0; e < edgeCount; e++)
                    AddTri(pi, polyA[e], polyB[e]);
            }

            void TryAddEdge(int ea, int eb, int skipBadIdx)
            {
                // If another bad triangle shares this edge it is internal → skip
                for (int j = 0; j < bad.Count; j++)
                {
                    if (j == skipBadIdx) continue;
                    if (TriHasEdge(bad[j], ea, eb)) return;
                }
                polyA.Add(ea);
                polyB.Add(eb);
            }

            bool TriHasEdge(int t, int a, int b)
            {
                int ta = va[t], tb = vb[t], tc = vc[t];
                return (ta == a && tb == b) || (tb == a && tc == b) || (tc == a && ta == b)
                    || (ta == b && tb == a) || (tb == b && tc == a) || (tc == b && ta == a);
            }

            // add a triangle with CCW winding
            void AddTri(int a, int b, int c)
            {
                float2 pa = pts[a], pb = pts[b], pc = pts[c];
                float cross = (pb.x - pa.x) * (pc.y - pa.y)
                            - (pb.y - pa.y) * (pc.x - pa.x);
                if (cross < 0)
                {
                    // Swap b <-> c to make CCW
                    int ti = b; b = c; c = ti;
                    float2 tp = pb; pb = pc; pc = tp;
                }

                if (triCount >= triCap) GrowTriArrays();

                va[triCount] = a;
                vb[triCount] = b;
                vc[triCount] = c;
                alive[triCount] = true;
                ComputeCircumcircle(pa, pb, pc, triCount);
                triCount++;
            }

            void ComputeCircumcircle(float2 a, float2 b, float2 c, int idx)
            {
                float D = 2f * (a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y));
                if (math.abs(D) < 1e-10f)
                {
                    cxArr[idx] = (a.x + b.x + c.x) / 3f;
                    cyArr[idx] = (a.y + b.y + c.y) / 3f;
                    crSqArr[idx] = float.MaxValue;
                    return;
                }
                float asq = a.x * a.x + a.y * a.y;
                float bsq = b.x * b.x + b.y * b.y;
                float csq = c.x * c.x + c.y * c.y;

                float ux = (asq * (b.y - c.y) + bsq * (c.y - a.y) + csq * (a.y - b.y)) / D;
                float uy = (asq * (c.x - b.x) + bsq * (a.x - c.x) + csq * (b.x - a.x)) / D;

                cxArr[idx] = ux;
                cyArr[idx] = uy;
                float dx = a.x - ux, dy = a.y - uy;
                crSqArr[idx] = dx * dx + dy * dy;
            }

            // array management 
            void EnsureTriArrays(int cap)
            {
                if (triCap >= cap) return;
                triCap = cap;
                va = new int[cap];
                vb = new int[cap];
                vc = new int[cap];
                cxArr = new float[cap];
                cyArr = new float[cap];
                crSqArr = new float[cap];
                alive = new bool[cap];
            }

            void GrowTriArrays()
            {
                int newCap = triCap * 2;
                Grow(ref va, newCap);
                Grow(ref vb, newCap);
                Grow(ref vc, newCap);
                Grow(ref cxArr, newCap);
                Grow(ref cyArr, newCap);
                Grow(ref crSqArr, newCap);
                Grow(ref alive, newCap);
                triCap = newCap;
            }

            static void Grow<T>(ref T[] arr, int newSize)
            {
                T[] replacement = new T[newSize];
                System.Array.Copy(arr, replacement, arr.Length);
                arr = replacement;
            }
        }
    }
}