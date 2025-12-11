using UnityEngine;

/// <summary>
/// Generates a cylinder mesh with circular top and bottom caps connected by a curved side surface.
/// Implements hard-edge geometry using completely separate vertex sets for clear projection visualization.
/// 
/// Geometry Details:
/// - Top cap: SEGMENTS triangles (fan pattern from center)
/// - Bottom cap: SEGMENTS triangles (fan pattern from center)
/// - Side surface: SEGMENTS quads (2 triangles each)
/// - Total faces: SEGMENTS (top) + SEGMENTS (bottom) + SEGMENTS×2 (sides)
/// 
/// Hard Edge Strategy (Critical for Projections):
/// Uses THREE separate vertex sets for top cap, bottom cap, and side surface.
/// Even though rim vertices are at the same physical position, they are NOT shared.
/// 
/// Why Separate Vertices?
/// - Top cap rim needs upward-facing normals (for top face shading)
/// - Bottom cap rim needs downward-facing normals (for bottom face shading)
/// - Side surface rim needs outward-facing normals (for curved surface shading)
/// - Sharing vertices would create smooth shading → blurred edges in projections
/// - Separate vertices create hard edges → clear, distinct lines in HP/VP views
/// 
/// Vertex Layout:
/// [0]: Top cap center
/// [1 to SEGMENTS]: Top cap rim (SEGMENTS vertices)
/// [SEGMENTS+1]: Bottom cap center
/// [SEGMENTS+2 to 2×SEGMENTS+1]: Bottom cap rim (SEGMENTS vertices)
/// [2×SEGMENTS+2 to 3×SEGMENTS+2]: Side top ring (SEGMENTS+1 vertices, includes wrap-around)
/// [3×SEGMENTS+3 to 4×SEGMENTS+3]: Side bottom ring (SEGMENTS+1 vertices, includes wrap-around)
/// 
/// Educational Purpose:
/// - Demonstrates circular projection in HP (circle view from top)
/// - Shows rectangular projection in VP (rectangle view from front)
/// - Illustrates curved surface projection onto flat planes
/// - Shows the principle of a prism with infinite sides
/// </summary>
public class Cylinder : BaseShape
{
    /// <summary>
    /// Number of triangular segments that approximate the circular cross-section.
    /// Higher values = smoother circle, but more vertices/triangles.
    /// 24 provides a good balance between visual quality and performance.
    /// 
    /// Vertex/Triangle Count:
    /// - Vertices: 4×SEGMENTS + 4 = 100 vertices (for SEGMENTS=24)
    /// - Triangles: 4×SEGMENTS = 96 triangles (for SEGMENTS=24)
    /// </summary>
    private const int SEGMENTS = 24;

    /// <summary>
    /// Generates a cylinder mesh based on ShapeData parameters.
    /// 
    /// Algorithm:
    /// 1. Calculate dimensions (radius from baseLength, height)
    /// 2. Generate top cap vertices (center + rim, separate set)
    /// 3. Generate bottom cap vertices (center + rim, separate set)
    /// 4. Generate side surface vertices (top ring + bottom ring, separate set with wrap-around)
    /// 5. Define top cap triangles (fan pattern, CCW from above)
    /// 6. Define bottom cap triangles (fan pattern, CCW from below)
    /// 7. Define side triangles (quads as triangle pairs, CW from outside)
    /// 8. Create mesh and apply to GameObject
    /// 9. Apply transformations via PostProcessShape()
    /// 
    /// Coordinate System:
    /// - Center at origin (0, 0, 0)
    /// - Top cap at Y = +halfHeight
    /// - Bottom cap at Y = -halfHeight
    /// - Circular cross-section extends in XZ plane
    /// - Radius measured from Y-axis
    /// 
    /// Hard Edge Rim Solution:
    /// The rim is defined THREE times with identical positions but different contexts:
    /// 1. Top cap rim: Part of top cap faces (upward normals)
    /// 2. Bottom cap rim: Part of bottom cap faces (downward normals)
    /// 3. Side surface rim: Part of side faces (outward normals)
    /// This triplication ensures MeshAnalyzer can detect distinct edges for projection drawing.
    /// </summary>
    /// <param name="data">ShapeData containing baseLength (diameter) and height</param>
    /// <returns>GameObject with cylinder mesh, MeshFilter, MeshRenderer, and applied transformations</returns>
    public override GameObject Generate(ShapeData data)
    {
        // === CREATE GAMEOBJECT AND COMPONENTS ===
        GameObject cylinderObject = new GameObject("Cylinder");

        // Add mesh components
        MeshFilter meshFilter = cylinderObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = cylinderObject.AddComponent<MeshRenderer>();
        
        // Disable shadows for clean technical drawing aesthetic
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // === CALCULATE DIMENSIONS ===
        // Radius: Half of base length (diameter → radius)
        float radius = data.baseLength / 2f;
        
        // Total height (distance between caps)
        float height = data.height;
        
        // Half height for centering (top at +h/2, bottom at -h/2)
        float halfHeight = height / 2f;

        // === VERTEX COUNT CALCULATION ===
        // CRITICAL: Using completely separate vertex sets for hard edges
        
        // Top Cap: SEGMENTS + 1 vertices
        //   - 1 center vertex (at top)
        //   - SEGMENTS rim vertices (circular perimeter)
        int topCapVertexCount = SEGMENTS + 1;
        
        // Bottom Cap: SEGMENTS + 1 vertices
        //   - 1 center vertex (at bottom)
        //   - SEGMENTS rim vertices (circular perimeter)
        int bottomCapVertexCount = SEGMENTS + 1;
        
        // Side Surface: (SEGMENTS + 1) × 2 vertices
        //   - (SEGMENTS + 1) top ring vertices (includes wrap-around vertex)
        //   - (SEGMENTS + 1) bottom ring vertices (includes wrap-around vertex)
        //   - Wrap-around: Last vertex duplicates first to close the surface seamlessly
        int sideVertexCount = (SEGMENTS + 1) * 2;
        
        // Total vertices (no sharing between caps and sides)
        int totalVertices = topCapVertexCount + bottomCapVertexCount + sideVertexCount;

        Vector3[] vertices = new Vector3[totalVertices];
        int vertexIndex = 0; // Current vertex insertion index

        // === TOP CAP VERTICES ===
        // Completely separate from side surface for hard edge at rim
        
        // Center vertex of top cap (apex of fan triangles)
        vertices[vertexIndex++] = new Vector3(0, halfHeight, 0);

        // Perimeter vertices of top cap (rim of the circle)
        // Parametric circle: x = r×cos(θ), z = r×sin(θ)
        // Counterclockwise from +X axis when viewed from above
        for (int i = 0; i < SEGMENTS; i++)
        {
            // Angle for this segment (0 to 2π)
            float angle = (float)i / SEGMENTS * Mathf.PI * 2f;
            
            // Calculate position on circle
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            
            // Place vertex on top circle
            vertices[vertexIndex++] = new Vector3(x, halfHeight, z);
        }

        // === BOTTOM CAP VERTICES ===
        // Completely separate from side surface for hard edge at rim
        
        // Center vertex of bottom cap (apex of fan triangles)
        vertices[vertexIndex++] = new Vector3(0, -halfHeight, 0);

        // Perimeter vertices of bottom cap (rim of the circle)
        // Same circular pattern as top, but at bottom Y level
        for (int i = 0; i < SEGMENTS; i++)
        {
            float angle = (float)i / SEGMENTS * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            
            // Place vertex on bottom circle
            vertices[vertexIndex++] = new Vector3(x, -halfHeight, z);
        }

        // === SIDE SURFACE VERTICES ===
        // Completely separate from cap rims for hard edges
        // Uses (SEGMENTS+1) vertices to include wrap-around for seamless UV mapping
        
        // Top ring of side surface
        // Note: Goes to SEGMENTS+1 (inclusive) to create wrap-around vertex
        // wrap-around vertex: vertices[i=SEGMENTS] = vertices[i=0] (same position, different index)
        for (int i = 0; i <= SEGMENTS; i++)
        {
            // Modulo ensures wrap-around: segment 24 uses angle of segment 0
            float angle = (float)(i % SEGMENTS) / SEGMENTS * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            
            vertices[vertexIndex++] = new Vector3(x, halfHeight, z);
        }

        // Bottom ring of side surface
        // Same wrap-around pattern as top ring
        for (int i = 0; i <= SEGMENTS; i++)
        {
            float angle = (float)(i % SEGMENTS) / SEGMENTS * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            
            vertices[vertexIndex++] = new Vector3(x, -halfHeight, z);
        }

        // === TRIANGLE INDEX CALCULATION ===
        // Top cap: SEGMENTS triangles × 3 indices = SEGMENTS×3
        // Bottom cap: SEGMENTS triangles × 3 indices = SEGMENTS×3
        // Side surface: SEGMENTS quads × 2 triangles × 3 indices = SEGMENTS×6
        int topCapTriangleCount = SEGMENTS * 3;
        int bottomCapTriangleCount = SEGMENTS * 3;
        int sideTriangleCount = SEGMENTS * 2 * 3; // 2 triangles per quad
        int totalTriangleIndices = topCapTriangleCount + bottomCapTriangleCount + sideTriangleCount;

        int[] triangles = new int[totalTriangleIndices];
        int triangleIndex = 0; // Current triangle insertion index

        // === TOP CAP TRIANGLES (Fan Pattern) ===
        // Winding: Counter-Clockwise from above (Unity's left-handed system)
        // Normal direction: +Y (upward)
        // Pattern: Center → Next → Current (CCW when viewed from top)
        int topCenterIndex = 0;
        int topPerimeterStart = 1;

        for (int i = 0; i < SEGMENTS; i++)
        {
            // Current rim vertex
            int current = topPerimeterStart + i;
            
            // Next rim vertex (wraps around at last segment)
            int next = topPerimeterStart + ((i + 1) % SEGMENTS);

            // Define triangle in CCW order (viewed from above)
            triangles[triangleIndex++] = topCenterIndex; // Center
            triangles[triangleIndex++] = next;           // Next (CCW)
            triangles[triangleIndex++] = current;        // Current
        }

        // === BOTTOM CAP TRIANGLES (Fan Pattern) ===
        // Winding: Counter-Clockwise from below (flipped for downward normal)
        // Normal direction: -Y (downward)
        // Pattern: Center → Current → Next (CCW when viewed from below = CW from above)
        int bottomCenterIndex = topCapVertexCount;
        int bottomPerimeterStart = bottomCenterIndex + 1;

        for (int i = 0; i < SEGMENTS; i++)
        {
            int current = bottomPerimeterStart + i;
            int next = bottomPerimeterStart + ((i + 1) % SEGMENTS);

            // Define triangle in CCW order when viewed from below
            // This appears CW from above, creating downward-facing normal
            triangles[triangleIndex++] = bottomCenterIndex; // Center
            triangles[triangleIndex++] = current;           // Current (swapped)
            triangles[triangleIndex++] = next;              // Next (swapped)
        }

        // === SIDE SURFACE TRIANGLES (Quad Strip as Triangle Pairs) ===
        // Winding: Clockwise from outside
        // Normal direction: Radially outward (perpendicular to Y-axis)
        // Each quad: 2 triangles sharing the diagonal
        int sideTopStart = topCapVertexCount + bottomCapVertexCount;
        int sideBottomStart = sideTopStart + (SEGMENTS + 1);

        for (int i = 0; i < SEGMENTS; i++)
        {
            // Quad corners (4 vertices):
            int topCurrent = sideTopStart + i;       // Top-left
            int topNext = sideTopStart + i + 1;      // Top-right
            int bottomCurrent = sideBottomStart + i; // Bottom-left
            int bottomNext = sideBottomStart + i + 1; // Bottom-right

            // First triangle of quad (Lower-left triangle)
            // Winding: Bottom-left → Top-left → Bottom-right (CW from outside)
            triangles[triangleIndex++] = bottomCurrent; // Bottom-left
            triangles[triangleIndex++] = topCurrent;    // Top-left
            triangles[triangleIndex++] = bottomNext;    // Bottom-right

            // Second triangle of quad (Upper-right triangle)
            // Winding: Top-left → Top-right → Bottom-right (CW from outside)
            triangles[triangleIndex++] = topCurrent; // Top-left
            triangles[triangleIndex++] = topNext;    // Top-right
            triangles[triangleIndex++] = bottomNext; // Bottom-right
        }

        // === CREATE MESH ===
        Mesh cylinderMesh = new Mesh();
        cylinderMesh.name = "Cylinder Mesh";
        cylinderMesh.vertices = vertices;
        cylinderMesh.triangles = triangles;

        // Recalculate normals for proper lighting (if enabled)
        // Hard edges: Each face gets its own normal due to separate vertices
        // Top cap: Upward normals (+Y)
        // Bottom cap: Downward normals (-Y)
        // Side surface: Radially outward normals
        cylinderMesh.RecalculateNormals();
        
        // Recalculate bounds for proper frustum culling
        cylinderMesh.RecalculateBounds();

        // Assign mesh to MeshFilter component
        meshFilter.mesh = cylinderMesh;

        // === APPLY TRANSFORMATIONS ===
        // Apply rotation and position from ShapeData
        // Handles angleHP, angleVP, rotationY, distHP, distVP
        PostProcessShape(cylinderObject, data);

        return cylinderObject;
    }
}