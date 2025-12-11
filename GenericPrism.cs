using UnityEngine;

/// <summary>
/// Generates a prism mesh with N-sided regular polygon bases (top and bottom) connected by rectangular side faces.
/// Implements hard-edge geometry using completely separate vertex sets for clear projection visualization.
/// 
/// Supported Prism Types (by number of sides):
/// - N=3: Triangular Prism (triangle bases)
/// - N=4: Square Prism (square bases, similar to cube but with independent height)
/// - N=5: Pentagonal Prism (pentagon bases)
/// - N=6: Hexagonal Prism (hexagon bases)
/// - N>6: Any regular polygon prism
/// 
/// Geometry Details:
/// - Top face: 1 N-gon (N-2 triangles using fan triangulation)
/// - Bottom face: 1 N-gon (N-2 triangles using fan triangulation)
/// - Side faces: N rectangles (2 triangles each)
/// - Total vertices: N (top) + N (bottom) + 4N (sides) = 6N vertices
/// - Total triangles: (N-2)×2 (caps) + 2N (sides) = 4N-4 triangles
/// 
/// Hard Edge Strategy:
/// Uses THREE separate vertex sets (top, bottom, sides) with identical positions but different contexts.
/// This ensures sharp edges between faces for clear engineering projection visualization.
/// 
/// Radius Calculation:
/// For a regular N-gon with side length s:
/// - Circumradius R (center to corner): R = s / (2 × sin(π/N))
/// - Example: Hexagon with s=2: R = 2 / (2 × sin(30°)) = 2 / 1 = 2
/// 
/// Educational Purpose:
/// - Demonstrates regular polygon projection (N-gon in HP, rectangle in VP)
/// - Shows the relationship between side length and circumradius
/// - Illustrates how number of sides affects shape appearance
/// - Generalizes the concept of prisms (cylinder is prism with N→∞)
/// </summary>
public class GenericPrism : BaseShape
{
    /// <summary>
    /// Number of sides for the polygon base (N in N-gon).
    /// Must be ≥ 3 for valid polygon.
    /// Common values: 3 (triangle), 4 (square), 5 (pentagon), 6 (hexagon)
    /// </summary>
    private int sides;

    /// <summary>
    /// Constructs a GenericPrism generator for an N-sided prism.
    /// 
    /// Parameter Validation:
    /// While not enforced here, numberOfSides should be ≥ 3.
    /// - N=1: Invalid (point, not a polygon)
    /// - N=2: Invalid (line, not a polygon)
    /// - N≥3: Valid regular polygon
    /// </summary>
    /// <param name="numberOfSides">Number of sides for the polygon base (N in N-gon)</param>
    public GenericPrism(int numberOfSides)
    {
        this.sides = numberOfSides;
    }

    /// <summary>
    /// Generates an N-sided prism mesh based on ShapeData parameters.
    /// 
    /// Algorithm:
    /// 1. Calculate circumradius from side length (baseLength)
    /// 2. Generate top face vertices (N vertices on upper polygon)
    /// 3. Generate bottom face vertices (N vertices on lower polygon)
    /// 4. Generate side face vertices (4 vertices per side, N sides)
    /// 5. Triangulate top face (fan pattern, CCW from above)
    /// 6. Triangulate bottom face (fan pattern, CCW from below)
    /// 7. Triangulate side faces (2 triangles per quad, CCW from outside)
    /// 8. Create mesh and apply to GameObject
    /// 9. Apply transformations via PostProcessShape()
    /// 
    /// Coordinate System:
    /// - Center at origin (0, 0, 0)
    /// - Top face at Y = +halfHeight
    /// - Bottom face at Y = -halfHeight
    /// - Polygon vertices on circle of radius R in XZ plane
    /// - First vertex typically aligned to +X axis (with rotation offset)
    /// 
    /// Hard Edge Implementation:
    /// Each corner of the polygon is represented THREE times:
    /// 1. Top face vertex (part of top N-gon, upward normal)
    /// 2. Bottom face vertex (part of bottom N-gon, downward normal)
    /// 3. Side face vertices (part of adjacent rectangular faces, outward normals)
    /// This ensures MeshAnalyzer detects distinct edges for projection drawing.
    /// </summary>
    /// <param name="data">ShapeData containing baseLength (side length) and height</param>
    /// <returns>GameObject with N-sided prism mesh, MeshFilter, MeshRenderer, and transformations</returns>
    public override GameObject Generate(ShapeData data)
    {
        // === CREATE GAMEOBJECT AND COMPONENTS ===
        GameObject obj = new GameObject($"{sides}-Sided Prism");
        MeshFilter mf = obj.AddComponent<MeshFilter>();
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();
        
        // Disable shadows for clean technical drawing aesthetic
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // === CALCULATE DIMENSIONS ===
        // Side Length: Edge length of the regular polygon (user-specified)
        float sideLength = data.baseLength;
        
        // Circumradius Calculation:
        // For a regular N-gon with side length s, the circumradius R is:
        // R = s / (2 × sin(π/N))
        //
        // Derivation:
        // - Central angle per side: θ = 360°/N = 2π/N
        // - Half of central angle: θ/2 = π/N
        // - Using Law of Sines in isosceles triangle (center + 2 adjacent vertices):
        //   s/2 = R × sin(π/N)
        //   R = s / (2 × sin(π/N))
        //
        // Examples:
        // - Triangle (N=3): R = s / (2 × sin(60°)) = s / 1.732 ≈ 0.577s
        // - Square (N=4): R = s / (2 × sin(45°)) = s / 1.414 ≈ 0.707s
        // - Hexagon (N=6): R = s / (2 × sin(30°)) = s / 1 = s
        float radius = sideLength / (2f * Mathf.Sin(Mathf.PI / sides));
        
        // Half height for centering (top at +h/2, bottom at -h/2)
        float halfHeight = data.height / 2f;

        // === VERTEX COUNT CALCULATION ===
        // CRITICAL: Using completely separate vertex sets for hard edges
        //
        // Top Face: N vertices (one per polygon corner)
        // Bottom Face: N vertices (one per polygon corner)
        // Side Faces: N × 4 vertices (4 corners per rectangular face)
        //
        // Total: N + N + 4N = 6N vertices
        //
        // Hard Edge Rationale:
        // - Top face needs upward-facing normals (+Y)
        // - Bottom face needs downward-facing normals (-Y)
        // - Side faces need outward-facing normals (perpendicular to edge)
        // - Sharing vertices would create smooth shading → blurred edges in projections
        // - Separate vertices create hard edges → clear, distinct lines in HP/VP views
        int totalVertices = sides + sides + (sides * 4);
        Vector3[] vertices = new Vector3[totalVertices];
        int v = 0; // Current vertex insertion index

        // === TOP FACE VERTICES ===
        // N vertices on the upper polygon perimeter
        // Separate from side faces for hard edge at top rim
        int topStartIndex = v;
        for (int i = 0; i < sides; i++)
        {
            // Get angle for this vertex (includes rotation offset for alignment)
            float angle = GetAngle(i);
            
            // Parametric circle/polygon: x = R×cos(θ), z = R×sin(θ)
            vertices[v++] = new Vector3(
                Mathf.Cos(angle) * radius,  // X coordinate
                halfHeight,                 // Y coordinate (top level)
                Mathf.Sin(angle) * radius   // Z coordinate
            );
        }

        // === BOTTOM FACE VERTICES ===
        // N vertices on the lower polygon perimeter
        // Separate from side faces for hard edge at bottom rim
        int bottomStartIndex = v;
        for (int i = 0; i < sides; i++)
        {
            float angle = GetAngle(i);
            
            // Same circular pattern as top, but at bottom Y level
            vertices[v++] = new Vector3(
                Mathf.Cos(angle) * radius,  // X coordinate
                -halfHeight,                // Y coordinate (bottom level)
                Mathf.Sin(angle) * radius   // Z coordinate
            );
        }

        // === SIDE FACE VERTICES ===
        // N rectangular faces, each with 4 vertices (no vertex sharing between faces)
        // Separate from top/bottom for hard edges at rims
        int sideStartIndex = v;
        for (int i = 0; i < sides; i++)
        {
            // Current side's two angles (defines the rectangle)
            float angle1 = GetAngle(i);         // Left edge angle
            float angle2 = GetAngle(i + 1);     // Right edge angle (next vertex)

            // Four corners of the rectangular side face:
            // Top-left corner (current angle, top level)
            Vector3 top1 = new Vector3(
                Mathf.Cos(angle1) * radius, 
                halfHeight, 
                Mathf.Sin(angle1) * radius
            );
            
            // Top-right corner (next angle, top level)
            Vector3 top2 = new Vector3(
                Mathf.Cos(angle2) * radius, 
                halfHeight, 
                Mathf.Sin(angle2) * radius
            );
            
            // Bottom-left corner (current angle, bottom level)
            Vector3 bot1 = new Vector3(
                Mathf.Cos(angle1) * radius, 
                -halfHeight, 
                Mathf.Sin(angle1) * radius
            );
            
            // Bottom-right corner (next angle, bottom level)
            Vector3 bot2 = new Vector3(
                Mathf.Cos(angle2) * radius, 
                -halfHeight, 
                Mathf.Sin(angle2) * radius
            );

            // Add vertices in quad order: BL, TL, TR, BR
            vertices[v++] = bot1; // 0: Bottom Left
            vertices[v++] = top1; // 1: Top Left
            vertices[v++] = top2; // 2: Top Right
            vertices[v++] = bot2; // 3: Bottom Right
        }

        // === TRIANGLE INDEX CALCULATION ===
        // Top face: (N-2) triangles × 3 indices = (N-2)×3
        //   - Fan triangulation from first vertex: N-gon → (N-2) triangles
        // Bottom face: (N-2) triangles × 3 indices = (N-2)×3
        //   - Fan triangulation from first vertex: N-gon → (N-2) triangles
        // Side faces: N quads × 2 triangles × 3 indices = N×6
        //   - Each rectangular face = 2 triangles
        //
        // Total: (N-2)×3×2 + N×6 = 6N + 6N - 12 = 12N - 12 indices
        int[] triangles = new int[(sides - 2) * 3 * 2 + (sides * 6)]; 
        int t = 0; // Current triangle index insertion position

        // === TOP FACE TRIANGULATION (Fan Pattern) ===
        // Winding: Counter-Clockwise from above (Unity's left-handed system)
        // Normal direction: +Y (upward)
        // Pattern: Anchor (first vertex) → Next → Current
        //
        // Fan Triangulation:
        // For N-gon with vertices [0, 1, 2, ..., N-1]:
        // Triangles: [0,2,1], [0,3,2], [0,4,3], ..., [0,N-1,N-2]
        // Total: N-2 triangles
        for (int i = 1; i < sides - 1; i++)
        {
            triangles[t++] = topStartIndex;         // Anchor (vertex 0)
            triangles[t++] = topStartIndex + i + 1; // Next vertex (CCW)
            triangles[t++] = topStartIndex + i;     // Current vertex
        }

        // === BOTTOM FACE TRIANGULATION (Fan Pattern) ===
        // Winding: Counter-Clockwise from below (flipped for downward normal)
        // Normal direction: -Y (downward)
        // Pattern: Anchor → Current → Next (reversed from top)
        //
        // This appears Clockwise from above, creating downward-facing normal
        for (int i = 1; i < sides - 1; i++)
        {
            triangles[t++] = bottomStartIndex;         // Anchor (vertex 0)
            triangles[t++] = bottomStartIndex + i;     // Current vertex (CW from above)
            triangles[t++] = bottomStartIndex + i + 1; // Next vertex (CW from above)
        }

        // === SIDE FACE TRIANGULATION (Quads as Triangle Pairs) ===
        // Winding: Counter-Clockwise from outside
        // Normal direction: Radially outward (perpendicular to face)
        // Each rectangular face: 2 triangles sharing the BL-TR diagonal
        //
        // Quad layout (viewed from outside):
        //   TL --- TR
        //   |  \   |
        //   |   \  |
        //   BL --- BR
        //
        // Triangle 1 (Lower-left): BL → TL → TR (CCW from outside)
        // Triangle 2 (Upper-right): BL → TR → BR (CCW from outside)
        for (int i = 0; i < sides; i++)
        {
            // Base index for this quad's 4 vertices
            int baseIdx = sideStartIndex + (i * 4);
            
            // First Triangle (Lower-left triangle of the quad)
            // Winding: Bottom-Left → Top-Left → Top-Right (CCW from outside)
            triangles[t++] = baseIdx;     // BL (vertex 0)
            triangles[t++] = baseIdx + 1; // TL (vertex 1)
            triangles[t++] = baseIdx + 2; // TR (vertex 2)
            
            // Second Triangle (Upper-right triangle of the quad)
            // Winding: Bottom-Left → Top-Right → Bottom-Right (CCW from outside)
            triangles[t++] = baseIdx;     // BL (vertex 0)
            triangles[t++] = baseIdx + 2; // TR (vertex 2)
            triangles[t++] = baseIdx + 3; // BR (vertex 3)
        }

        // === CREATE MESH ===
        Mesh mesh = new Mesh();
        mesh.name = $"{sides}-Sided Prism Mesh";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        
        // Recalculate normals for proper lighting (if enabled)
        // Hard edges: Each face gets its own normal due to separate vertices
        // - Top face: Upward normals (+Y)
        // - Bottom face: Downward normals (-Y)
        // - Side faces: Outward normals (perpendicular to each rectangular face)
        mesh.RecalculateNormals();
        
        // Recalculate bounds for proper frustum culling
        mesh.RecalculateBounds();
        
        // Assign mesh to MeshFilter component
        mf.mesh = mesh;

        // === APPLY TRANSFORMATIONS ===
        // Apply rotation and position from ShapeData
        // Handles angleHP, angleVP, rotationY, distHP, distVP
        PostProcessShape(obj, data);
        
        return obj;
    }

    /// <summary>
    /// Calculates the angle (in radians) for the i-th vertex of the regular N-gon.
    /// Includes rotation offset for proper alignment based on number of sides.
    /// 
    /// Angle Formula:
    /// angle = (i / N) × 2π + offset
    /// 
    /// Where:
    /// - i: Vertex index (0 to N-1)
    /// - N: Number of sides
    /// - 2π: Full circle (360°)
    /// - offset: Rotation adjustment for aesthetic alignment
    /// 
    /// Rotation Offset Strategy:
    /// - Even-sided polygons (N=4,6,8,...): offset = π/N
    ///   → Aligns a flat edge to the front (Y-axis aligned)
    ///   → Example: Hexagon has flat edge facing forward, not a vertex
    /// 
    /// - Odd-sided polygons (N=3,5,7,...): offset = 0
    ///   → Aligns a vertex to the front (+X axis)
    ///   → Example: Pentagon has vertex pointing forward
    /// 
    /// - Square (N=4): Special case offset = π/4 (45°)
    ///   → Aligns edges to axes (edge parallel to X and Z axes)
    ///   → Creates axis-aligned square (0°, 90°, 180°, 270° vertices)
    /// 
    /// Why Alignment Matters:
    /// - Engineering drawings: Prefer flat edges front-facing for clarity
    /// - Axis alignment: Makes HP/VP projections more intuitive
    /// - Aesthetic: Looks more "correct" in standard orthographic views
    /// </summary>
    /// <param name="index">Vertex index (0 to N-1, wraps around for index=N)</param>
    /// <returns>Angle in radians for vertex position on circle</returns>
    private float GetAngle(int index)
    {
        // Calculate rotation offset based on polygon type
        float offset;
        
        if (sides == 4)
        {
            // Square: Special 45° rotation for axis alignment
            offset = Mathf.PI / 4; // 45° in radians
        }
        else if (sides % 2 == 0)
        {
            // Even-sided polygons: Rotate by half the central angle
            // This aligns a flat edge to the front
            offset = Mathf.PI / sides;
        }
        else
        {
            // Odd-sided polygons: No rotation offset
            // First vertex points to +X axis
            offset = 0;
        }
        
        // Calculate final angle: base angle + offset
        // Base angle: (index / sides) × 2π (evenly distributed around circle)
        return ((float)index / sides * Mathf.PI * 2f) + offset;
    }
}