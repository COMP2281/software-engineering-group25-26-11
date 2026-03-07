using UnityEngine;
using Seb.Fluid2D.Simulation;
using Unity.Mathematics;
using System.Collections.Generic;

namespace Seb.Fluid2D.Rendering
{
    /// <summary>
    /// Renders the fluid simulation as a dynamic triangle mesh instead of individual ball-shaped particles.
    /// Particles act as invisible vertex points; a Delaunay triangulation connects neighbouring
    /// particles (within a radius) into a continuous surface mesh that deforms with the simulation
    /// every frame.
    ///
    /// Setup:
    ///   1. Add this component to a GameObject (MeshFilter + MeshRenderer are auto-added).
    ///   2. Assign the FluidSim2D reference and (optionally) the worldAnchor transform.
    ///      If worldAnchor is left empty it falls back to sim.particleDisplay.worldAnchor.
    ///   3. Assign a material (e.g. the included FluidMesh2D shader).
    ///   4. Optionally disable or hide ParticleDisplay2D so the old ball rendering is invisible.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class MeshFluidDisplay2D : MonoBehaviour
    {
        // ───────── Inspector ─────────
        [Header("References")]
        public FluidSim2D sim;
        [Tooltip("World-space anchor transform. Falls back to sim.particleDisplay.worldAnchor if null.")]
        public Transform worldAnchor;

        [Header("Mesh Generation")]
        [Tooltip("Maximum triangle edge length in simulation-space units. " +
                 "Triangles with any edge longer than this are culled (alpha-shape). " +
                 "Controls how 'tight' the mesh wraps the fluid body.")]
        public float maxEdgeLength = 4f;

        [Tooltip("Automatically derive maxEdgeLength from sim.smoothingRadius × edgeLengthFactor.")]
        public bool autoMaxEdgeLength = true;

        [Range(1.0f, 4.0f)]
        [Tooltip("Multiplier on smoothingRadius when autoMaxEdgeLength is true. Higher = less seams.")]
        public float edgeLengthFactor = 2.5f;

        [Header("Smoothing")]
        [Range(0, 8)]
        [Tooltip("Number of Taubin smoothing iterations (shrink+inflate pair). Higher = smoother edges.")]
        public int smoothingIterations = 3;

        [Range(0f, 1f)]
        [Tooltip("Strength of each smoothing pass. 0 = no smoothing, 1 = maximum smoothing.")]
        public float smoothingStrength = 0.6f;

        [Tooltip("Subdivide boundary edges to create smoother silhouettes.")]
        public bool subdivideEdges = true;

        [Header("Boundary Normals")]
        [Tooltip("Tilt normals outward at boundary vertices for Fresnel rim highlights.")]
        public bool boundaryNormalTilt = true;

        [Range(0f, 60f)]
        [Tooltip("Angle in degrees to tilt boundary normals outward. Creates a rounded, glossy edge.")]
        public float normalTiltAngle = 35f;

        [Header("Appearance")]
        [Range(0f, 1f)]
        public float baseAlpha = 0.85f;

        // ───────── Private state ─────────
        Mesh fluidMesh;
        MeshFilter meshFilter;
        MeshRenderer meshRenderer;

        // GPU read-back buffers (reused across frames)
        float2[] readPos;
        float4[] readCol;

        // Mesh data (reused)
        readonly List<Vector3> meshVerts = new List<Vector3>();
        readonly List<Color>   meshCols  = new List<Color>();
        readonly List<int>     meshTris  = new List<int>();

        // Smoothing data
        readonly List<Vector3> smoothedVerts = new List<Vector3>();
        readonly Dictionary<int, List<int>> vertexNeighbors = new Dictionary<int, List<int>>();

        // Boundary detection (reused across frames)
        readonly HashSet<int> boundaryVertSet = new HashSet<int>();
        readonly Dictionary<long, int> edgeFaceCount = new Dictionary<long, int>();
        readonly List<int> boundaryEdgeA = new List<int>();
        readonly List<int> boundaryEdgeB = new List<int>();

        // Triangulator (holds its own working memory)
        readonly Delaunay2D delaunay = new Delaunay2D();

        // ───────── Lifecycle ─────────
        void Start()
        {
            fluidMesh = new Mesh { name = "FluidMesh2D" };
            fluidMesh.MarkDynamic(); // optimise for frequent updates

            meshFilter   = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshFilter.mesh = fluidMesh;

            // Auto-find FluidSim2D if not assigned in Inspector
            if (sim == null)
            {
                // Try by tag first (same approach as SpawnOnContact)
                GameObject fluidSimObj = GameObject.FindWithTag("FLUIDSIM");
                if (fluidSimObj != null)
                    sim = fluidSimObj.GetComponent<FluidSim2D>();
            }
            if (sim == null)
            {
                // Fallback: search scene for any FluidSim2D
                sim = FindObjectOfType<FluidSim2D>();
            }
            if (sim == null)
            {
                Debug.LogError("[MeshFluidDisplay2D] Could not find a FluidSim2D in the scene! " +
                               "Assign it manually or ensure a GameObject tagged 'FLUIDSIM' exists.");
            }
            else
            {
                Debug.Log($"[MeshFluidDisplay2D] Auto-found FluidSim2D on '{sim.gameObject.name}'");

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

            // GPU → CPU  (synchronous; acceptable for moderate particle counts)
            sim.positionBuffer.GetData(readPos, 0, 0, n);
            sim.colorBuffer.GetData(readCol, 0, 0, n);

            float maxE = autoMaxEdgeLength
                ? sim.smoothingRadius * edgeLengthFactor
                : maxEdgeLength;

            RebuildMesh(n, maxE);
        }

        // ───────── Mesh construction ─────────
        void RebuildMesh(int n, float maxEdge)
        {
            // 1. Delaunay triangulate in sim-space, then alpha-shape filter
            meshTris.Clear();
            delaunay.Run(readPos, n, maxEdge * maxEdge, meshTris);

            if (meshTris.Count == 0)
            {
                fluidMesh.Clear();
                return;
            }

            // 2. Detect boundary edges for subdivision and normal tilting
            DetectBoundaryEdges(n);

            // 3. Subdivide boundary edges for smoother silhouettes
            int vertCount = n;
            if (subdivideEdges && boundaryEdgeA.Count > 0)
            {
                vertCount = SubdivideBoundaryEdges(n);
            }

            // 4. Map sim-space positions → mesh-local vertices
            float scale = Mathf.Approximately(sim.worldScale, 0f) ? 1f : sim.worldScale;
            Vector2 off = sim.worldOffset;

            Transform anchor = ResolveAnchor();
            Matrix4x4 anchorToWorld = anchor != null ? anchor.localToWorldMatrix : Matrix4x4.identity;
            Matrix4x4 worldToMesh  = transform.worldToLocalMatrix;
            Matrix4x4 simToMesh    = worldToMesh * anchorToWorld;

            meshVerts.Clear();
            meshCols.Clear();

            for (int i = 0; i < vertCount; i++)
            {
                float2 p = readPos[i];
                Vector3 anchorLocal = new Vector3(
                    p.x * scale + off.x,
                    p.y * scale + off.y,
                    0f);
                meshVerts.Add(simToMesh.MultiplyPoint3x4(anchorLocal));

                float4 c = readCol[i];
                meshCols.Add(new Color(c.x, c.y, c.z, baseAlpha));
            }

            // 5. Apply Taubin smoothing (volume-preserving) for curved, liquid-like edges
            if (smoothingIterations > 0 && smoothingStrength > 0f)
            {
                ApplyTaubinSmoothing(vertCount);
            }

            // 6. Upload to mesh
            fluidMesh.Clear();
            List<Vector3> finalVerts = smoothingIterations > 0 ? smoothedVerts : meshVerts;
            fluidMesh.SetVertices(finalVerts);
            fluidMesh.SetColors(meshCols);
            fluidMesh.SetTriangles(meshTris, 0);

            // 7. Compute normals with optional boundary tilt for Fresnel rim highlights
            if (boundaryNormalTilt && boundaryVertSet.Count > 0)
                ComputeNormalsWithBoundaryTilt(finalVerts);
            else
                SetForwardNormals(finalVerts.Count);

            fluidMesh.RecalculateBounds();
        }

        Transform ResolveAnchor()
        {
            if (worldAnchor != null) return worldAnchor;
            if (sim != null && sim.particleDisplay != null) return sim.particleDisplay.worldAnchor;
            return null;
        }

        // ───────── Boundary detection ─────────
        void DetectBoundaryEdges(int vertexCount)
        {
            boundaryVertSet.Clear();
            edgeFaceCount.Clear();
            boundaryEdgeA.Clear();
            boundaryEdgeB.Clear();

            // Count how many triangles each edge belongs to
            for (int i = 0; i < meshTris.Count; i += 3)
            {
                IncrementEdge(meshTris[i],     meshTris[i + 1]);
                IncrementEdge(meshTris[i + 1], meshTris[i + 2]);
                IncrementEdge(meshTris[i + 2], meshTris[i]);
            }

            // Boundary edges belong to exactly 1 triangle
            foreach (var kvp in edgeFaceCount)
            {
                if (kvp.Value == 1)
                {
                    long key = kvp.Key;
                    int a = (int)(key >> 32);
                    int b = (int)(key & 0xFFFFFFFFL);
                    boundaryEdgeA.Add(a);
                    boundaryEdgeB.Add(b);
                    boundaryVertSet.Add(a);
                    boundaryVertSet.Add(b);
                }
            }
        }

        void IncrementEdge(int a, int b)
        {
            long key = EdgeKey(a, b);
            edgeFaceCount.TryGetValue(key, out int count);
            edgeFaceCount[key] = count + 1;
        }

        static long EdgeKey(int a, int b)
        {
            int lo = a < b ? a : b;
            int hi = a < b ? b : a;
            return ((long)lo << 32) | (uint)hi;
        }

        // ───────── Boundary subdivision ─────────
        /// <summary>
        /// Inserts midpoint vertices on boundary edges and splits affected triangles.
        /// This doubles silhouette resolution so smoothing produces curved outlines.
        /// </summary>
        int SubdivideBoundaryEdges(int origCount)
        {
            int numEdges = boundaryEdgeA.Count;
            int totalVerts = origCount + numEdges;

            // Ensure read-back arrays have room for the new midpoint vertices
            if (readPos.Length < totalVerts)
            {
                var newPos = new float2[totalVerts];
                var newCol = new float4[totalVerts];
                System.Array.Copy(readPos, newPos, origCount);
                System.Array.Copy(readCol, newCol, origCount);
                readPos = newPos;
                readCol = newCol;
            }

            // Create midpoint vertices and map each boundary edge to its midpoint index
            var edgeMidpoint = new Dictionary<long, int>(numEdges);
            for (int i = 0; i < numEdges; i++)
            {
                int a = boundaryEdgeA[i], b = boundaryEdgeB[i];
                int mid = origCount + i;
                readPos[mid] = (readPos[a] + readPos[b]) * 0.5f;
                readCol[mid] = (readCol[a] + readCol[b]) * 0.5f;
                edgeMidpoint[EdgeKey(a, b)] = mid;
                boundaryVertSet.Add(mid); // midpoints are also boundary vertices
            }

            // Rebuild triangle list, splitting triangles that contain boundary edges
            int triCount = meshTris.Count;
            var newTris = new List<int>(triCount * 2);

            for (int i = 0; i < triCount; i += 3)
            {
                int a = meshTris[i], b = meshTris[i + 1], c = meshTris[i + 2];

                bool hasAB = edgeMidpoint.TryGetValue(EdgeKey(a, b), out int mAB);
                bool hasBC = edgeMidpoint.TryGetValue(EdgeKey(b, c), out int mBC);
                bool hasCA = edgeMidpoint.TryGetValue(EdgeKey(c, a), out int mCA);

                int splits = (hasAB ? 1 : 0) + (hasBC ? 1 : 0) + (hasCA ? 1 : 0);

                if (splits == 0)
                {
                    newTris.Add(a); newTris.Add(b); newTris.Add(c);
                }
                else if (splits == 1)
                {
                    if (hasAB)      { newTris.Add(a); newTris.Add(mAB); newTris.Add(c);   newTris.Add(mAB); newTris.Add(b);   newTris.Add(c);  }
                    else if (hasBC) { newTris.Add(a); newTris.Add(b);   newTris.Add(mBC);  newTris.Add(a);   newTris.Add(mBC); newTris.Add(c);  }
                    else            { newTris.Add(a); newTris.Add(b);   newTris.Add(mCA);  newTris.Add(mCA); newTris.Add(b);   newTris.Add(c);  }
                }
                else if (splits == 2)
                {
                    if (!hasAB)      { newTris.Add(a); newTris.Add(b);   newTris.Add(mBC);  newTris.Add(a); newTris.Add(mBC); newTris.Add(mCA); newTris.Add(mCA); newTris.Add(mBC); newTris.Add(c); }
                    else if (!hasBC) { newTris.Add(mAB); newTris.Add(b); newTris.Add(c);    newTris.Add(a); newTris.Add(mAB); newTris.Add(mCA); newTris.Add(mCA); newTris.Add(mAB); newTris.Add(c); }
                    else             { newTris.Add(a); newTris.Add(mAB); newTris.Add(c);    newTris.Add(mAB); newTris.Add(b); newTris.Add(mBC); newTris.Add(mAB); newTris.Add(mBC); newTris.Add(c); }
                }
                else // splits == 3: standard 1-to-4 subdivision
                {
                    newTris.Add(a);   newTris.Add(mAB); newTris.Add(mCA);
                    newTris.Add(mAB); newTris.Add(b);   newTris.Add(mBC);
                    newTris.Add(mCA); newTris.Add(mBC); newTris.Add(c);
                    newTris.Add(mAB); newTris.Add(mBC); newTris.Add(mCA);
                }
            }

            meshTris.Clear();
            meshTris.AddRange(newTris);
            return totalVerts;
        }

        // ───────── Smoothing (Taubin λ|μ — volume-preserving) ─────────
        /// <summary>
        /// Taubin smoothing alternates a shrinking pass (λ) with an inflating pass (μ).
        /// This prevents the volume loss that plain Laplacian smoothing causes,
        /// keeping the fluid body from visibly shrinking over many iterations.
        /// Boundary vertices receive slightly stronger smoothing for cleaner silhouettes.
        /// </summary>
        void ApplyTaubinSmoothing(int vertexCount)
        {
            BuildNeighborConnectivity(vertexCount);

            smoothedVerts.Clear();
            smoothedVerts.AddRange(meshVerts);

            float lambda = Mathf.Clamp(smoothingStrength, 0.01f, 0.99f);
            float mu = -(lambda + 0.02f); // |μ| > λ ensures no net shrinkage

            for (int iter = 0; iter < smoothingIterations; iter++)
            {
                ApplySmoothPass(vertexCount, lambda); // shrink
                ApplySmoothPass(vertexCount, mu);      // inflate
            }
        }

        void BuildNeighborConnectivity(int vertexCount)
        {
            vertexNeighbors.Clear();
            for (int i = 0; i < vertexCount; i++)
                vertexNeighbors[i] = new List<int>();

            for (int i = 0; i < meshTris.Count; i += 3)
            {
                int a = meshTris[i], b = meshTris[i + 1], c = meshTris[i + 2];
                AddNeighbor(a, b); AddNeighbor(b, a);
                AddNeighbor(b, c); AddNeighbor(c, b);
                AddNeighbor(c, a); AddNeighbor(a, c);
            }
        }

        void ApplySmoothPass(int vertexCount, float factor)
        {
            List<Vector3> tempVerts = new List<Vector3>(smoothedVerts);

            for (int i = 0; i < vertexCount; i++)
            {
                if (!vertexNeighbors.ContainsKey(i)) continue;
                List<int> neighbors = vertexNeighbors[i];
                if (neighbors.Count == 0) continue;

                Vector3 avgPos = Vector3.zero;
                foreach (int ni in neighbors)
                    avgPos += smoothedVerts[ni];
                avgPos /= neighbors.Count;

                Vector3 laplacian = avgPos - smoothedVerts[i];

                // Boundary vertices get stronger smoothing for cleaner silhouettes
                float f = boundaryVertSet.Contains(i) ? factor * 1.3f : factor;
                tempVerts[i] = smoothedVerts[i] + laplacian * f;
            }

            smoothedVerts.Clear();
            smoothedVerts.AddRange(tempVerts);
        }

        void AddNeighbor(int vertex, int neighbor)
        {
            if (!vertexNeighbors[vertex].Contains(neighbor))
                vertexNeighbors[vertex].Add(neighbor);
        }

        // ───────── Normals ─────────
        /// <summary>
        /// Tilts normals outward at boundary vertices so the shader's Fresnel and specular
        /// terms create glossy rim highlights, giving the flat 2D mesh a rounded, liquid look.
        /// Interior vertices keep forward-facing normals.
        /// </summary>
        void ComputeNormalsWithBoundaryTilt(List<Vector3> verts)
        {
            int vertexCount = verts.Count;
            Vector3[] normals = new Vector3[vertexCount];

            float tiltRad = normalTiltAngle * Mathf.Deg2Rad;
            float sinTilt = Mathf.Sin(tiltRad);
            float cosTilt = Mathf.Cos(tiltRad);

            // Compute centre of mass for outward direction reference
            Vector3 center = Vector3.zero;
            for (int i = 0; i < vertexCount; i++)
                center += verts[i];
            center /= vertexCount;

            for (int i = 0; i < vertexCount; i++)
            {
                if (boundaryVertSet.Contains(i))
                {
                    Vector3 outward = verts[i] - center;
                    outward.z = 0f;
                    float mag = outward.magnitude;
                    if (mag > 0.001f)
                    {
                        outward /= mag;
                        normals[i] = new Vector3(
                            outward.x * sinTilt,
                            outward.y * sinTilt,
                            cosTilt).normalized;
                    }
                    else
                    {
                        normals[i] = Vector3.forward;
                    }
                }
                else
                {
                    normals[i] = Vector3.forward;
                }
            }

            fluidMesh.normals = normals;
        }

        void SetForwardNormals(int vertexCount)
        {
            Vector3[] normals = new Vector3[vertexCount];
            for (int i = 0; i < vertexCount; i++)
                normals[i] = Vector3.forward;
            fluidMesh.normals = normals;
        }

        void OnDestroy()
        {
            if (fluidMesh != null) Destroy(fluidMesh);
        }

        // ═══════════════════════════════════════════════════════════════
        //  Bowyer–Watson incremental Delaunay triangulation  (2-D)
        //
        //  Produces a proper non-overlapping triangulation, then applies
        //  an alpha-shape filter (max edge length) to carve out the
        //  fluid body silhouette.
        // ═══════════════════════════════════════════════════════════════
        sealed class Delaunay2D
        {
            // SoA triangle storage (cache-friendly)
            int   triCap;
            int   triCount;
            int[] va, vb, vc;             // vertex indices
            float[] cxArr, cyArr, crSqArr; // circumcentre x, y  &  circumradius²
            bool[]  alive;

            // Point buffer  (n real points + 3 super-triangle vertices)
            float2[] pts;

            // Super-triangle vertex indices
            int sA, sB, sC;

            // Scratch lists (reused every insertion)
            readonly List<int> bad   = new List<int>(64);
            readonly List<int> polyA = new List<int>(64);
            readonly List<int> polyB = new List<int>(64);

            /// <summary>
            /// Triangulate <paramref name="n"/> points from <paramref name="positions"/>
            /// and append the resulting triangle indices (groups of 3) into <paramref name="outTris"/>.
            /// Triangles with any edge² &gt; <paramref name="maxEdgeSq"/> are discarded (alpha shape).
            /// </summary>
            public void Run(float2[] positions, int n, float maxEdgeSq, List<int> outTris)
            {
                outTris.Clear();
                if (n < 3) return;

                // --- prepare point array (real + 3 super) ---
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
                float2 ctr  = (mn + mx) * 0.5f;
                float  span = math.max(mx.x - mn.x, mx.y - mn.y) + 1f;
                float  d    = span * 10f; // well outside all data

                sA = n; sB = n + 1; sC = n + 2;
                pts[sA] = ctr + new float2(-d,       -d * 0.5f);
                pts[sB] = ctr + new float2( d * 2f,  -d * 0.5f);
                pts[sC] = ctr + new float2( 0,        d * 2f);

                // --- allocate / reset triangle storage ---
                int initCap = Mathf.Max(n * 10 + 10, 128);
                EnsureTriArrays(initCap);
                triCount = 0;

                AddTri(sA, sB, sC);

                // --- incremental insertion ---
                for (int i = 0; i < n; i++)
                    InsertPoint(i);

                // --- collect output (skip super-triangle verts & long edges) ---
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

            // ── single point insertion ──────────────────────────────
            void InsertPoint(int pi)
            {
                float px = pts[pi].x, py = pts[pi].y;

                // 1. Find every triangle whose circumcircle contains the point
                bad.Clear();
                for (int t = 0; t < triCount; t++)
                {
                    if (!alive[t]) continue;
                    float dx = px - cxArr[t];
                    float dy = py - cyArr[t];
                    if (dx * dx + dy * dy - crSqArr[t] < 1e-6f)
                        bad.Add(t);
                }

                // 2. Boundary polygon = edges of bad triangles NOT shared by another bad triangle
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

                // 3. Kill bad triangles
                for (int bi = 0; bi < badCount; bi++)
                    alive[bad[bi]] = false;

                // 4. Create fan of new triangles from the point to each boundary edge
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

            // ── add a triangle with CCW winding ────────────────────
            void AddTri(int a, int b, int c)
            {
                float2 pa = pts[a], pb = pts[b], pc = pts[c];
                float cross = (pb.x - pa.x) * (pc.y - pa.y)
                            - (pb.y - pa.y) * (pc.x - pa.x);
                if (cross < 0)
                {
                    // Swap b ↔ c to make CCW
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
                    cxArr[idx]   = (a.x + b.x + c.x) / 3f;
                    cyArr[idx]   = (a.y + b.y + c.y) / 3f;
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

            // ── array management ───────────────────────────────────
            void EnsureTriArrays(int cap)
            {
                if (triCap >= cap) return;
                triCap   = cap;
                va       = new int[cap];
                vb       = new int[cap];
                vc       = new int[cap];
                cxArr    = new float[cap];
                cyArr    = new float[cap];
                crSqArr  = new float[cap];
                alive    = new bool[cap];
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
