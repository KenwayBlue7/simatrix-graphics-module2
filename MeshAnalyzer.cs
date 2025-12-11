using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Analyzes mesh geometry to extract edges and faces for projection drawing.
/// Uses position-based edge identification to automatically "weld" duplicate edges
/// (e.g., cylinder cap rim to side wall) without modifying the original mesh.
/// 
/// Key Innovation: Edge struct uses world-space positions instead of vertex indices,
/// allowing edges at the same physical location to be recognized as identical even if
/// they reference different vertices in the mesh.
/// </summary>
public class MeshAnalyzer : MonoBehaviour
{
    /// <summary>
    /// Represents an edge in 3D space defined by two world-space endpoint positions.
    /// 
    /// Position-Based Design:
    /// - Traditional approach: Edge(vertexIndex1, vertexIndex2) - creates duplicates
    /// - Our approach: Edge(worldPos1, worldPos2) - automatically merges duplicates
    /// 
    /// Example Problem Solved:
    /// Cylinder has separate vertices for cap (12 vertices) and side wall (12 vertices),
    /// even though they occupy the same physical positions at the rim.
    /// Position-based edges automatically recognize these as the same edge.
    /// 
    /// Implementation Details:
    /// - Points are sorted during construction to ensure Edge(A,B) == Edge(B,A)
    /// - Equality uses distance threshold (0.001 units) to handle floating-point errors
    /// - Hash function quantizes positions to 3 decimal places for Dictionary lookup
    /// </summary>
    [System.Serializable]
    public struct Edge : System.IEquatable<Edge>
    {
        /// <summary>First endpoint in world space (always the "lesser" point after sorting)</summary>
        public Vector3 p1;
        
        /// <summary>Second endpoint in world space (always the "greater" point after sorting)</summary>
        public Vector3 p2;

        /// <summary>
        /// Constructs an edge from two world-space points.
        /// Automatically sorts points to ensure consistent ordering for equality checks.
        /// </summary>
        /// <param name="point1">First endpoint</param>
        /// <param name="point2">Second endpoint</param>
        public Edge(Vector3 point1, Vector3 point2)
        {
            // Sort points deterministically (X, then Y, then Z) to ensure
            // Edge(A,B) is treated identically to Edge(B,A)
            if (CompareVectors(point1, point2) < 0)
            {
                p1 = point1;
                p2 = point2;
            }
            else
            {
                p1 = point2;
                p2 = point1;
            }
        }

        /// <summary>
        /// Compares two vectors component-wise for deterministic sorting.
        /// Uses X as primary key, Y as secondary, Z as tertiary.
        /// Includes tolerance (0.0001) to handle floating-point precision.
        /// </summary>
        /// <returns>-1 if a < b, 1 if a > b, 0 if equal</returns>
        private static int CompareVectors(Vector3 a, Vector3 b)
        {
            // Compare X component
            if (Mathf.Abs(a.x - b.x) > 0.0001f) return a.x.CompareTo(b.x);
            
            // If X is equal, compare Y component
            if (Mathf.Abs(a.y - b.y) > 0.0001f) return a.y.CompareTo(b.y);
            
            // If X and Y are equal, compare Z component
            return a.z.CompareTo(b.z);
        }

        /// <summary>
        /// Checks if two edges are equal by comparing their endpoint positions.
        /// Uses distance threshold (0.001 units) instead of exact equality to handle
        /// floating-point rounding errors from mesh generation and transformations.
        /// 
        /// This tolerance is what enables cylinder rim welding: cap vertices and
        /// side vertices are close enough to be considered the same position.
        /// </summary>
        public bool Equals(Edge other)
        {
            return Vector3.Distance(p1, other.p1) < 0.001f && 
                   Vector3.Distance(p2, other.p2) < 0.001f;
        }

        /// <summary>
        /// Object.Equals override for compatibility with generic collections.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is Edge other)
            {
                return Equals(other);
            }
            return false;
        }

        /// <summary>
        /// Generates hash code for Dictionary key lookup.
        /// Quantizes positions to 3 decimal places (0.001 precision) to ensure
        /// that edges with nearly identical positions hash to the same value.
        /// This is critical for Dictionary.ContainsKey() to work correctly with floating-point positions.
        /// </summary>
        public override int GetHashCode()
        {
            // Quantize both endpoints to 3 decimal places
            Vector3 q1 = Quantize(p1);
            Vector3 q2 = Quantize(p2);
            
            // XOR the hash codes for a simple but effective combined hash
            return q1.GetHashCode() ^ q2.GetHashCode();
        }

        /// <summary>
        /// Quantizes a vector to 3 decimal places (0.001 precision).
        /// Example: (1.2345, 2.3456, 3.4567) → (1.235, 2.346, 3.457)
        /// This ensures consistent hash codes for positions that differ only due to floating-point error.
        /// </summary>
        private static Vector3 Quantize(Vector3 v)
        {
            return new Vector3(
                Mathf.Round(v.x * 1000f) / 1000f,
                Mathf.Round(v.y * 1000f) / 1000f,
                Mathf.Round(v.z * 1000f) / 1000f
            );
        }
    }

    /// <summary>
    /// Represents a triangular face in 3D space with geometric properties needed for projection analysis.
    /// 
    /// Used for:
    /// - Edge visibility determination (front-facing vs back-facing)
    /// - Silhouette edge detection (edges shared by front and back faces)
    /// - Normal-based lighting calculations (if implemented)
    /// </summary>
    [System.Serializable]
    public class Face
    {
        /// <summary>Surface normal in world space (points outward from solid)</summary>
        public Vector3 worldNormal;
        
        /// <summary>Geometric center of the triangle (average of 3 vertices)</summary>
        public Vector3 center;
        
        /// <summary>Original vertex indices from mesh (kept for compatibility with legacy code)</summary>
        public List<int> vertices;
        
        /// <summary>
        /// Signed distance from origin to the face plane.
        /// Calculated using plane equation: n · p = d
        /// where n is the normal and p is any point on the plane.
        /// Used for depth sorting and visibility determination.
        /// </summary>
        public float distanceToOrigin;

        /// <summary>
        /// Constructs a Face with calculated geometric properties.
        /// </summary>
        /// <param name="normal">World-space surface normal (should be normalized)</param>
        /// <param name="faceCenter">World-space center point of the triangle</param>
        /// <param name="vertexIndices">Original mesh vertex indices (for compatibility)</param>
        /// <param name="distance">Signed distance to origin (from plane equation)</param>
        public Face(Vector3 normal, Vector3 faceCenter, List<int> vertexIndices, float distance)
        {
            worldNormal = normal;
            center = faceCenter;
            vertices = new List<int>(vertexIndices);
            distanceToOrigin = distance;
        }
    }

    /// <summary>
    /// Builds a comprehensive edge map from a mesh, associating each edge with the faces that share it.
    /// 
    /// Algorithm:
    /// 1. Convert all vertex positions to world space
    /// 2. For each triangle:
    ///    - Calculate face normal and center
    ///    - Create 3 edges using world positions
    ///    - Add each edge to the map with its associated face
    /// 3. Duplicate edges automatically merge due to position-based equality
    /// 
    /// Result Structure:
    /// Dictionary<Edge, List<Face>>
    /// - Key: Unique edge (by position)
    /// - Value: All faces that share this edge (1 for boundary, 2 for internal, >2 for non-manifold)
    /// 
    /// Edge Classification:
    /// - 1 face: Boundary/silhouette edge (always visible)
    /// - 2 faces: Internal edge (visible only if one face is front-facing and one is back-facing)
    /// - >2 faces: Non-manifold geometry (edge shared by 3+ faces, uncommon in solid modeling)
    /// 
    /// Cylinder Rim Solution:
    /// The cylinder cap and side wall share 12 vertices at the rim, but Unity's mesh
    /// has separate vertex data for each. Position-based edges recognize that:
    /// Edge(capVertex[i], capVertex[i+1]) == Edge(sideVertex[i], sideVertex[i+1])
    /// because they have the same world positions, even though indices differ.
    /// </summary>
    /// <param name="mesh">The mesh to analyze (in local space)</param>
    /// <param name="transform">Transform to convert mesh to world space</param>
    /// <returns>Dictionary mapping each unique edge to the faces that contain it</returns>
    public static Dictionary<Edge, List<Face>> BuildEdgeMap(Mesh mesh, Transform transform)
    {
        // Validate input
        if (mesh == null || transform == null)
        {
            return new Dictionary<Edge, List<Face>>();
        }

        var edgeMap = new Dictionary<Edge, List<Face>>();
        
        // Get mesh data
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        
        int triangleCount = triangles.Length / 3;
        
        // Process each triangle (3 indices per triangle)
        for (int i = 0; i < triangles.Length; i += 3)
        {
            // === STEP 1: GET VERTEX INDICES ===
            int i1 = triangles[i];
            int i2 = triangles[i + 1];
            int i3 = triangles[i + 2];

            // === STEP 2: CONVERT TO WORLD SPACE ===
            // CRITICAL: This is where position-based edge welding happens
            // We use actual 3D positions, not vertex indices, to define edges
            Vector3 v1 = transform.TransformPoint(vertices[i1]);
            Vector3 v2 = transform.TransformPoint(vertices[i2]);
            Vector3 v3 = transform.TransformPoint(vertices[i3]);
            
            // === STEP 3: CALCULATE FACE NORMAL ===
            // Use cross product of two edges: normal = (v2-v1) × (v3-v1)
            Vector3 edge1 = v2 - v1;
            Vector3 edge2 = v3 - v1;
            Vector3 normal = Vector3.Cross(edge1, edge2).normalized;
            
            // === STEP 4: CALCULATE FACE CENTER ===
            // Simple average of the 3 vertices
            Vector3 center = (v1 + v2 + v3) / 3f;
            
            // === STEP 5: CALCULATE PLANE DISTANCE ===
            // Plane equation: n · p = d
            // Use any vertex (v1) to calculate the signed distance
            float distanceToOrigin = Vector3.Dot(normal, v1);

            // === STEP 6: CREATE FACE OBJECT ===
            Face face = new Face(
                normal,
                center,
                new List<int> { i1, i2, i3 }, // Keep original indices for compatibility
                distanceToOrigin
            );

            // === STEP 7: ADD EDGES TO MAP ===
            // Create 3 edges from the triangle's vertices
            // Position-based edges automatically merge duplicates:
            // - Cylinder cap edge: Edge(capV[0], capV[1])
            // - Cylinder side edge: Edge(sideV[0], sideV[1])
            // → Same world position → Same Dictionary key → Same List<Face>
            AddEdgeToMap(edgeMap, new Edge(v1, v2), face);
            AddEdgeToMap(edgeMap, new Edge(v2, v3), face);
            AddEdgeToMap(edgeMap, new Edge(v3, v1), face);
        }

        return edgeMap;
    }

    /// <summary>
    /// Helper method to add an edge-face association to the edge map.
    /// If the edge already exists (position match), appends the face to its list.
    /// If the edge is new, creates a new list with this face.
    /// 
    /// This is where duplicate edges are merged:
    /// - First call: Creates new entry with List containing Face1
    /// - Second call (duplicate edge): Appends Face2 to existing list
    /// - Result: Dictionary contains Edge → [Face1, Face2]
    /// </summary>
    /// <param name="map">The edge map being built</param>
    /// <param name="edge">The edge to add (position-based)</param>
    /// <param name="face">The face that contains this edge</param>
    private static void AddEdgeToMap(Dictionary<Edge, List<Face>> map, Edge edge, Face face)
    {
        if (!map.ContainsKey(edge))
        {
            // First time seeing this edge - create new list
            map[edge] = new List<Face>();
        }
        
        // Add this face to the edge's face list
        map[edge].Add(face);
    }

    // Unity lifecycle methods (empty - this is a static utility class)
    // Kept for MonoBehaviour compatibility if script is attached to a GameObject
    
    void Start()
    {
        // No initialization needed - all methods are static
    }

    void Update()
    {
        // No per-frame updates needed - analysis happens on-demand via BuildEdgeMap()
    }
}
