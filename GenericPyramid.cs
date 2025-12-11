using UnityEngine;

/// <summary>
/// Generates a pyramid mesh with an N-sided regular polygon base and an apex at the top.
/// Implements hard-edge geometry using completely separate vertex sets for clear projection visualization.
/// 
/// Supported Pyramid Types (by number of sides):
/// - N=3: Triangular Pyramid (Tetrahedron, triangle base)
/// - N=4: Square Pyramid (square base, classic pyramid shape)
/// - N=5: Pentagonal Pyramid (pentagon base)
/// - N=6: Hexagonal Pyramid (hexagon base)
/// - N>6: Any regular polygon pyramid
/// 
/// Geometry Details:
/// - Base face: 1 N-gon (N-2 triangles using fan triangulation)
/// - Side faces: N triangles (each connecting an edge of the base to the apex)
/// - Total vertices: N (base) + 3N (sides) = 4N vertices
/// - Total triangles: (N-2) (base) + N (sides) = 2N-2 triangles
/// 
/// Hard Edge Strategy:
/// Uses TWO separate vertex sets (base and sides) with identical positions but different contexts.
/// This ensures sharp edges between faces for clear engineering projection visualization.
/// 
/// Radius Calculation:
/// For a regular N-gon with side length s:
/// - Circumradius R (center to corner): R = s / (2 × sin(π/N))
/// - Example: Square pyramid with s=2: R = 2 / (2 × sin(45°)) = 2 / 1.414 ≈ 1.414
/// 
/// Special Alignment for "Slant Face to HP" Inclination:
/// All pyramids are oriented with a FLAT EDGE facing front (toward +Z axis).
/// This is CRITICAL for proper angleHP behavior:
/// - Flat edge front: angleHP rotates the pyramid correctly (slant face tilts toward HP)
/// - Corner front: angleHP would rotate incorrectly (corner tilts, not face)
/// 
/// Educational Purpose:
/// - Demonstrates polygon-to-point projection (N-gon in HP, triangle in VP when edge-front)
/// - Shows converging edges from base to apex
/// - Illustrates the effect of angleHP on slant face inclination
/// - Generalizes the concept of pyramids (cone is pyramid with N→∞)
/// </summary>
public class GenericPyramid : BaseShape
{
    /// <summary>
    /// Number of sides for the polygon base (N in N-gon).
    /// Must be ≥ 3 for valid polygon.
    /// Common values: 3 (triangle), 4 (square), 5 (pentagon), 6 (hexagon)
    /// </summary>
    private int sides;

    /// <summary>
    /// Constructs a GenericPyramid generator for an N-sided pyramid.
    /// 
    /// Parameter Validation:
    /// While not enforced here, numberOfSides should be ≥ 3.
    /// - N=1: Invalid (point, not a polygon)
    /// - N=2: Invalid (line, not a polygon)
    /// - N≥3: Valid regular polygon
    /// 
    /// Special Cases:
    /// - N=3 (Tetrahedron): Regular pyramid where all 4 faces are equilateral triangles
    ///   Note: User-specified height may differ from geometric tetrahedron height
    /// - N=4 (Square Pyramid): Classic pyramid shape (e.g., Egyptian pyramids)
    /// </summary>
    /// <param name="numberOfSides">Number of sides for the polygon base (N in N-gon)</param>
    public GenericPyramid(int numberOfSides)
    {
        this.sides = numberOfSides;
    }

    /// <summary>
    /// Generates an N-sided pyramid mesh based on ShapeData parameters.
    /// 
    /// Algorithm:
    /// 1. Calculate circumradius from side length (baseLength)
    /// 2. Generate base face vertices (N vertices on lower polygon)
    /// 3. Generate side face vertices (3 vertices per triangle, N triangles)
    /// 4. Triangulate base face (fan pattern, CW from below)
    /// 5. Triangulate side faces (apex-base2-base1 pattern, CCW from outside)
    /// 6. Create mesh and apply to GameObject
    /// 7. Apply transformations via PostProcessShape()
    /// 
    /// Coordinate System:
    /// - Center at origin (0, 0, 0)
    /// - Base at Y = -halfHeight
    /// - Apex at Y = +halfHeight
    /// - Polygon vertices on circle of radius R in XZ plane
    /// - First vertex/edge oriented based on GetAngle() offset (flat edge facing front)
    /// 
    /// Hard Edge Implementation:
    /// Each corner of the polygon base is represented TWICE:
    /// 1. Base face vertex (part of base N-gon, downward normal)
    /// 2. Side face vertices (part of adjacent triangular faces, outward normals)
    /// This ensures MeshAnalyzer detects distinct edges for projection drawing.
    /// 
    /// Height Behavior:
    /// - Uses user-specified height from data.height slider
    /// - For Tetrahedron (N=3): May differ from geometric tetrahedron height
    ///   (Geometric: h = √(2/3) × s ≈ 0.816s, but slider allows any height)
    /// - Allows exploration of "squat" vs "tall" pyramids
    /// </summary>
    /// <param name="data">ShapeData containing baseLength (side length) and height</param>
    /// <returns>GameObject with N-sided pyramid mesh, MeshFilter, MeshRenderer, and transformations</returns>
    public override GameObject Generate(ShapeData data)
    {
        // === CREATE GAMEOBJECT AND COMPONENTS ===
        GameObject obj = new GameObject($"{sides}-Sided Pyramid");
        MeshFilter mf = obj.AddComponent<MeshFilter>();
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();
        
        // Disable shadows for clean technical drawing aesthetic
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // === CALCULATE DIMENSIONS ===
        // Side Length: Edge length of the regular polygon base (user-specified)
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
        // - Pentagon (N=5): R = s / (2 × sin(36°)) = s / 1.176 ≈ 0.851s
        // - Hexagon (N=6): R = s / (2 × sin(30°)) = s / 1 = s
        float radius = sideLength / (2f * Mathf.Sin(Mathf.PI / sides));
        
        // Height: Distance from base to apex (user-specified via slider)
        // Note: For Tetrahedron, geometric height would be h = √(2/3) × s ≈ 0.816s
        // but we allow user to override this for educational flexibility
        float height = data.height;
        
        // Half height for centering (base at -h/2, apex at +h/2)
        float halfHeight = height / 2f;

        // === VERTEX COUNT CALCULATION ===
        // CRITICAL: Using completely separate vertex sets for hard edges
        //
        // Base Face: N vertices (one per polygon corner)
        // Side Faces: N × 3 vertices (3 corners per triangular face: apex + 2 base vertices)
        //
        // Total: N + 3N = 4N vertices
        //
        // Hard Edge Rationale:
        // - Base face needs downward-facing normal (-Y)
        // - Side faces need outward-facing normals (radially outward and upward)
        // - Sharing vertices would create smooth shading → blurred edges in projections
        // - Separate vertices create hard edges → clear, distinct lines in HP/VP views
        int totalVertices = sides + (sides * 3);
        Vector3[] vertices = new Vector3[totalVertices];
        int v = 0; // Current vertex insertion index

        // === BASE FACE VERTICES ===
        // N vertices on the lower polygon perimeter
        // Separate from side faces for hard edge at base rim
        int baseStartIndex = v;
        for (int i = 0; i < sides; i++)
        {
            // Get angle for this vertex (includes rotation offset for flat-edge-front alignment)
            float angle = GetAngle(i);
            
            // Parametric circle/polygon: x = R×cos(θ), z = R×sin(θ)
            vertices[v++] = new Vector3(
                Mathf.Cos(angle) * radius,  // X coordinate
                -halfHeight,                // Y coordinate (base level)
                Mathf.Sin(angle) * radius   // Z coordinate
            );
        }

        // === SIDE FACE VERTICES ===
        // N triangular faces, each with 3 vertices (no vertex sharing between faces)
        // Separate from base for hard edges at rim
        int sideStartIndex = v;
        
        // Apex position (shared conceptually, but duplicated for each triangle)
        Vector3 apex = new Vector3(0, halfHeight, 0);

        for (int i = 0; i < sides; i++)
        {
            // Current side's two base corner angles (defines the triangle base edge)
            float angle1 = GetAngle(i);         // Left corner angle
            float angle2 = GetAngle(i + 1);     // Right corner angle (next vertex)

            // Three corners of the triangular side face:
            
            // Left base corner (current angle, base level)
            Vector3 base1 = new Vector3(
                Mathf.Cos(angle1) * radius, 
                -halfHeight, 
                Mathf.Sin(angle1) * radius
            );
            
            // Right base corner (next angle, base level)
            Vector3 base2 = new Vector3(
                Mathf.Cos(angle2) * radius, 
                -halfHeight, 
                Mathf.Sin(angle2) * radius
            );

            // Add vertices in triangle order: Apex, Base2 (right), Base1 (left)
            // This creates CCW winding when viewed from outside
            vertices[v++] = apex;  // 0: Apex (top tip)
            vertices[v++] = base2; // 1: Base right corner
            vertices[v++] = base1; // 2: Base left corner
        }

        // === TRIANGLE INDEX CALCULATION ===
        // Base face: (N-2) triangles × 3 indices = (N-2)×3
        //   - Fan triangulation from first vertex: N-gon → (N-2) triangles
        // Side faces: N triangles × 3 indices = N×3
        //   - Each side is one triangle: Apex + 2 base vertices
        //
        // Total: (N-2)×3 + N×3 = 3N + 3N - 6 = 6N - 6 indices
        int[] triangles = new int[(sides - 2) * 3 + (sides * 3)];
        int t = 0; // Current triangle index insertion position

        // === BASE FACE TRIANGULATION (Fan Pattern) ===
        // Winding: Clockwise from below (creates downward-facing normal)
        // Normal direction: -Y (downward)
        // Pattern: Anchor (first vertex) → Current → Next
        //
        // Fan Triangulation:
        // For N-gon with vertices [0, 1, 2, ..., N-1]:
        // Triangles: [0,1,2], [0,2,3], [0,3,4], ..., [0,N-2,N-1]
        // Total: N-2 triangles
        //
        // CW vs CCW:
        // From below (looking up at base): This appears CCW → Normal points down (-Y) ✓
        // From above (looking down at base): This appears CW → Confirms -Y normal
        for (int i = 1; i < sides - 1; i++)
        {
            triangles[t++] = baseStartIndex;         // Anchor (vertex 0)
            triangles[t++] = baseStartIndex + i;     // Current vertex
            triangles[t++] = baseStartIndex + i + 1; // Next vertex
        }

        // === SIDE FACE TRIANGULATION ===
        // Winding: Counter-Clockwise from outside
        // Normal direction: Radially outward and upward (perpendicular to face, pointing away from pyramid center)
        // Pattern: Apex → Base2 (right) → Base1 (left)
        //
        // Each side face is a single triangle connecting:
        // - Apex (top tip)
        // - Two adjacent base vertices (edge of the base)
        //
        // CCW Winding Ensures Outward Normal:
        // When viewed from outside, vertices appear in CCW order → Normal points outward
        for (int i = 0; i < sides; i++)
        {
            // Base index for this triangle's 3 vertices
            int baseIdx = sideStartIndex + (i * 3);
            
            // Triangle vertices in CCW order (viewed from outside)
            triangles[t++] = baseIdx;     // Apex (top tip)
            triangles[t++] = baseIdx + 1; // Base2 (right corner)
            triangles[t++] = baseIdx + 2; // Base1 (left corner)
        }

        // === CREATE MESH ===
        Mesh mesh = new Mesh();
        mesh.name = $"{sides}-Sided Pyramid Mesh";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        
        // Recalculate normals for proper lighting (if enabled)
        // Hard edges: Each face gets its own normal due to separate vertices
        // - Base face: Downward normal (-Y)
        // - Side faces: Outward and upward normals (perpendicular to each triangular face)
        mesh.RecalculateNormals();
        
        // Recalculate bounds for proper frustum culling
        mesh.RecalculateBounds();
        
        // Assign mesh to MeshFilter component
        mf.mesh = mesh;

        // === APPLY TRANSFORMATIONS ===
        // Apply rotation and position from ShapeData
        // Handles angleHP, angleVP, rotationY, distHP, distVP
        //
        // CRITICAL: angleHP behavior depends on flat-edge-front orientation
        // With flat edge front: angleHP tilts the slant face toward HP (correct)
        // With corner front: angleHP would tilt a corner (incorrect, not intuitive)
        PostProcessShape(obj, data);
        
        return obj;
    }

    /// <summary>
    /// Calculates the angle (in radians) for the i-th vertex of the regular N-gon base.
    /// Includes rotation offset to ensure a FLAT EDGE faces the front (+Z axis).
    /// 
    /// Angle Formula:
    /// angle = (i / N) × 2π + offset
    /// 
    /// Where:
    /// - i: Vertex index (0 to N-1)
    /// - N: Number of sides
    /// - 2π: Full circle (360°)
    /// - offset: Rotation adjustment for flat-edge-front alignment
    /// 
    /// CRITICAL DESIGN DECISION: Flat Edge Front (Not Corner Front)
    /// 
    /// Why Flat Edge Front Matters:
    /// The orientation directly affects how angleHP (inclination to HP) behaves:
    /// 
    /// ✓ FLAT EDGE FRONT (Current Implementation):
    ///   - angleHP rotates around the flat edge
    ///   - The SLANT FACE tilts toward HP
    ///   - Intuitive: "Tilt the pyramid face down"
    ///   - Engineering convention: Inclination measured from a face
    /// 
    /// ✗ CORNER FRONT (Previous Bug):
    ///   - angleHP rotates around a corner vertex
    ///   - A CORNER tilts toward HP (not a face)
    ///   - Confusing: "Why is only a point tilting?"
    ///   - Not standard in engineering graphics
    /// 
    /// Rotation Offset Strategy:
    /// The offset rotates the entire polygon so a flat edge faces front (+Z).
    /// 
    /// TRIANGLE (N=3):
    ///   Offset: +90° (+π/2)
    ///   Vertices at: 90°, 210°, 330°
    ///   Flat edge: From 210° to 330° (bottom edge faces front) ✓
    ///   Previous bug: -90° placed CORNER at front ✗
    /// 
    /// SQUARE (N=4):
    ///   Offset: +45° (+π/4)
    ///   Vertices at: 45°, 135°, 225°, 315°
    ///   Flat edge: From 315° to 45° (bottom-right edge faces front) ✓
    ///   Edges parallel to axes (diamond orientation)
    /// 
    /// PENTAGON (N=5):
    ///   Offset: +90° (+π/2)
    ///   Vertices at: 90°, 162°, 234°, 306°, 18°
    ///   Flat edge: From 306° to 18° (bottom edge faces front) ✓
    ///   Previous bug: -90° placed CORNER at front ✗
    /// 
    /// HEXAGON (N=6):
    ///   Offset: 0° (no rotation needed)
    ///   Vertices at: 0°, 60°, 120°, 180°, 240°, 300°
    ///   Flat edges: 0°-60° (front) and 180°-240° (back) ✓
    ///   Natural hexagon orientation has flat top/bottom
    ///   Note: Corners on left/right (geometric constraint of hexagon)
    /// 
    /// Educational Impact:
    /// - Students can see "Slant Face to HP" angle behavior clearly
    /// - angleHP slider tilts the visible front face (not just a corner)
    /// - Projection views show face inclination (standard engineering practice)
    /// - Consistent with textbook diagrams and problem sets
    /// </summary>
    /// <param name="index">Vertex index (0 to N-1, wraps around for index=N)</param>
    /// <returns>Angle in radians for vertex position on circle</returns>
    private float GetAngle(int index)
    {
        // Calculate rotation offset based on polygon type
        float offset;
        
        if (sides == 3)
        {
            // TRIANGLE: +90° rotation for flat-edge-front
            // Vertices: 90°, 210°, 330°
            // Flat edge: 210° to 330° (bottom edge faces front)
            // FIXED: Was -π/2 (corner front) → Now +π/2 (edge front) ✓
            offset = Mathf.PI / 2f;
        }
        else if (sides == 4)
        {
            // SQUARE: +45° rotation for axis-aligned diamond
            // Vertices: 45°, 135°, 225°, 315°
            // Flat edge: 315° to 45° (bottom-right edge faces front)
            // Edges parallel to X and Z axes
            offset = Mathf.PI / 4f;
        }
        else if (sides == 5)
        {
            // PENTAGON: +90° rotation for flat-edge-front
            // Vertices: 90°, 162°, 234°, 306°, 18°
            // Flat edge: 306° to 18° (bottom edge faces front)
            // FIXED: Was -π/2 (corner front) → Now +π/2 (edge front) ✓
            offset = Mathf.PI / 2f;
        }
        else if (sides == 6)
        {
            // HEXAGON: 0° rotation (natural orientation)
            // Vertices: 0°, 60°, 120°, 180°, 240°, 300°
            // Flat edges: 0°-60° (front), 180°-240° (back)
            // Corners: 90° (right), 270° (left) - geometric constraint
            offset = 0f;
        }
        else
        {
            // DEFAULT: No rotation for other N-gons
            // First vertex at 0° (pointing right, +X axis)
            offset = 0f;
        }
        
        // Calculate final angle: base angle + offset
        // Base angle: (index / sides) × 2π (evenly distributed around circle)
        return ((float)index / sides * Mathf.PI * 2f) + offset;
    }
}