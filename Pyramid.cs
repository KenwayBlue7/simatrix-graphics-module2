using UnityEngine;

/// <summary>
/// Generates a square pyramid mesh with a square base and apex at the top.
/// Implements a simplified vertex structure for basic pyramid visualization.
/// 
/// ⚠️ NOTE: This is a LEGACY implementation kept for backward compatibility.
/// For new projects, use GenericPyramid(4) which provides:
/// - Hard-edge geometry (better projection visualization)
/// - Consistent vertex structure with other shapes
/// - Proper normal calculation per face
/// 
/// Geometry Details:
/// - Base: Square (2 triangles forming a quad)
/// - Side faces: 4 triangular faces
/// - Total vertices: 18 (3 per triangle × 6 triangles)
/// - Total triangles: 6 (2 base + 4 sides)
/// 
/// Vertex Structure:
/// Uses 18 separate vertices (no sharing) for simple sequential triangle definition.
/// Each triangle has its own 3 vertices, which creates hard edges automatically.
/// 
/// Educational Purpose:
/// - Demonstrates basic square pyramid projection
/// - Shows converging edges from base to apex
/// - Illustrates square-to-point projection (square in HP, triangle in VP)
/// 
/// When to Use:
/// - Legacy scenes that reference "Pyramid" specifically
/// - Simple demonstrations where hard-edge control isn't critical
/// 
/// When to Use GenericPyramid Instead:
/// - New engineering graphics projects
/// - When you need consistent mesh structure
/// - When projection clarity is important
/// </summary>
public class Pyramid : BaseShape
{
    /// <summary>
    /// Generates a square pyramid mesh based on ShapeData parameters.
    /// 
    /// Algorithm:
    /// 1. Create GameObject and add mesh components
    /// 2. Calculate base corner positions from baseLength
    /// 3. Define apex position from height
    /// 4. Create 18 vertices (6 triangles × 3 vertices each):
    ///    - 2 base triangles (square base)
    ///    - 4 side triangles (connecting base edges to apex)
    /// 5. Define triangle indices (simple sequential: 0,1,2,3,4,5,...)
    /// 6. Create mesh and assign to GameObject
    /// 7. Apply transformations via PostProcessPyramid()
    /// 
    /// Coordinate System:
    /// - Center at origin (0, 0, 0)
    /// - Base at Y = 0 (not centered vertically like other shapes)
    /// - Apex at Y = height
    /// - Square base extends ±halfBase in X and Z
    /// 
    /// Vertex Layout:
    /// [0-2]:   Base triangle 1 (backLeft, backRight, frontRight)
    /// [3-5]:   Base triangle 2 (backLeft, frontRight, frontLeft)
    /// [6-8]:   Back face (backLeft, apex, backRight)
    /// [9-11]:  Right face (backRight, apex, frontRight)
    /// [12-14]: Front face (frontRight, apex, frontLeft)
    /// [15-17]: Left face (frontLeft, apex, backLeft)
    /// 
    /// Difference from GenericPyramid:
    /// - Base at Y=0 (not centered at -halfHeight)
    /// - Simpler vertex structure (no separate vertex sets for faces)
    /// - Sequential triangle indices (not fan/strip patterns)
    /// - Base defined as 2 triangles (not N-gon with fan triangulation)
    /// </summary>
    /// <param name="data">ShapeData containing baseLength (square side) and height</param>
    /// <returns>GameObject with square pyramid mesh, MeshFilter, MeshRenderer, and transformations</returns>
    public override GameObject Generate(ShapeData data)
    {
        // === CREATE GAMEOBJECT AND COMPONENTS ===
        GameObject pyramidObject = new GameObject("Pyramid");

        // Add mesh components
        MeshFilter meshFilter = pyramidObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = pyramidObject.AddComponent<MeshRenderer>();
        
        // Disable shadows for clean technical drawing aesthetic
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    
        // === CALCULATE DIMENSIONS ===
        // Half of base side length (for positioning corners at ±halfBase)
        float halfBase = data.baseLength / 2f;

        // === DEFINE KEY POSITIONS ===
        // Base corners at Y = 0 (base sits on XZ plane at origin)
        // Note: Different from GenericPyramid which centers at Y = -halfHeight
        Vector3 backLeft = new Vector3(-halfBase, 0, -halfBase);   // (-X, 0, -Z)
        Vector3 backRight = new Vector3(halfBase, 0, -halfBase);    // (+X, 0, -Z)
        Vector3 frontRight = new Vector3(halfBase, 0, halfBase);    // (+X, 0, +Z)
        Vector3 frontLeft = new Vector3(-halfBase, 0, halfBase);    // (-X, 0, +Z)
        
        // Apex at top (centered horizontally, elevated vertically)
        Vector3 apex = new Vector3(0, data.height, 0);              // (0, +Y, 0)

        // === VERTEX ARRAY (18 vertices = 6 triangles × 3 vertices) ===
        // Using separate vertices for each triangle (no sharing)
        // This automatically creates hard edges between faces
        Vector3[] vertices = new Vector3[18];

        // === BASE TRIANGLES (2 triangles forming square base) ===
        // Winding: Counter-Clockwise from above (viewed from +Y looking down)
        // Normal direction: +Y (upward, out of base)
        
        // Base Triangle 1: Back-left corner to front-right corner diagonal
        // Vertices: backLeft → backRight → frontRight (CCW from above)
        vertices[0] = backLeft;
        vertices[1] = backRight;
        vertices[2] = frontRight;

        // Base Triangle 2: Back-left corner to front-left corner diagonal
        // Vertices: backLeft → frontRight → frontLeft (CCW from above)
        // Note: Shares diagonal edge with Triangle 1 (backLeft-frontRight)
        vertices[3] = backLeft;
        vertices[4] = frontRight;
        vertices[5] = frontLeft;

        // === SIDE TRIANGLES (4 triangular faces) ===
        // Each face connects one base edge to the apex
        // Winding: Counter-Clockwise from outside (viewed from outside pyramid)
        // Normal direction: Radially outward and upward (perpendicular to face)

        // Side Triangle 1: Back face
        // Connects back edge (backLeft-backRight) to apex
        // Vertices: backLeft → apex → backRight (CCW from outside/behind)
        vertices[6] = backLeft;
        vertices[7] = apex;
        vertices[8] = backRight;

        // Side Triangle 2: Right face
        // Connects right edge (backRight-frontRight) to apex
        // Vertices: backRight → apex → frontRight (CCW from outside/right)
        vertices[9] = backRight;
        vertices[10] = apex;
        vertices[11] = frontRight;

        // Side Triangle 3: Front face
        // Connects front edge (frontRight-frontLeft) to apex
        // Vertices: frontRight → apex → frontLeft (CCW from outside/front)
        vertices[12] = frontRight;
        vertices[13] = apex;
        vertices[14] = frontLeft;

        // Side Triangle 4: Left face
        // Connects left edge (frontLeft-backLeft) to apex
        // Vertices: frontLeft → apex → backLeft (CCW from outside/left)
        vertices[15] = frontLeft;
        vertices[16] = apex;
        vertices[17] = backLeft;

        // === TRIANGLE INDICES (Sequential) ===
        // Simple 0,1,2,3,4,5,... sequence since each vertex is used only once
        // No vertex sharing means no need for complex indexing
        //
        // Triangle Index Mapping:
        // [0,1,2]:     Base triangle 1
        // [3,4,5]:     Base triangle 2
        // [6,7,8]:     Back face
        // [9,10,11]:   Right face
        // [12,13,14]:  Front face
        // [15,16,17]:  Left face
        int[] triangles = new int[18];
        for (int i = 0; i < 18; i++)
        {
            triangles[i] = i;
        }

        // === CREATE MESH ===
        Mesh pyramidMesh = new Mesh();
        pyramidMesh.name = "Pyramid Mesh";
        pyramidMesh.vertices = vertices;
        pyramidMesh.triangles = triangles;

        // Recalculate normals for proper lighting (if enabled)
        // Each triangle gets its own normal due to separate vertices:
        // - Base triangles: Upward normals (+Y)
        // - Side triangles: Outward and upward normals (perpendicular to each face)
        pyramidMesh.RecalculateNormals();
        
        // Recalculate bounds for proper frustum culling
        // Ensures Unity knows the mesh's spatial extent for rendering optimization
        pyramidMesh.RecalculateBounds();

        // Assign the mesh to the MeshFilter component
        meshFilter.mesh = pyramidMesh;

        // === APPLY TRANSFORMATIONS ===
        // Apply position and rotation from ShapeData
        // Uses custom PostProcessPyramid() instead of BaseShape.PostProcessShape()
        // because base is at Y=0 (not centered)
        PostProcessPyramid(pyramidObject, data);

        return pyramidObject;
    }

    /// <summary>
    /// Positions and rotates the pyramid according to the engineering graphics coordinate system.
    /// 
    /// Coordinate System:
    /// - HP (Horizontal Plane): XZ plane at Y = 0
    /// - VP (Vertical Plane): YZ plane at X = 0
    /// - X-axis: Distance from VP (distVP, horizontal)
    /// - Y-axis: Height above HP (distHP, vertical)
    /// - Z-axis: Depth (typically 0 for centered shapes)
    /// 
    /// Position Calculation:
    /// Since the pyramid's base is at Y = 0 (not centered), positioning differs from other shapes:
    /// - X = distVP (distance in front of Vertical Plane)
    /// - Y = distHP (height above Horizontal Plane)
    /// - Z = 0 (centered in depth)
    /// 
    /// Note: Other shapes (Cone, Cylinder, etc.) use centered geometry:
    /// - Their base is at Y = -halfHeight
    /// - Position formula: Y = distHP + halfHeight
    /// This pyramid uses Y = distHP directly because base is already at Y = 0.
    /// 
    /// Rotation System (CORRECTED AXIS MAPPING):
    /// Unity uses Quaternion.Euler(X, Y, Z) for rotation, applied in Z→X→Y order.
    /// 
    /// Rotation Parameters:
    /// - X-Axis (angleHP): Controls forward/backward tilt
    ///   → Inclination to Horizontal Plane (HP)
    ///   → Tilts the slant face toward/away from XZ plane
    ///   → Example: angleHP = 30° tilts front face downward 30°
    /// 
    /// - Y-Axis (rotationY): Controls spin/orientation
    ///   → Rotation around vertical axis
    ///   → Changes which face is front-facing
    ///   → Example: rotationY = 45° creates diamond orientation (corner front)
    /// 
    /// - Z-Axis (-angleVP): Controls sideways tilt (INVERTED)
    ///   → Inclination to Vertical Plane (VP)
    ///   → Tilts the pyramid left/right
    ///   → Example: angleVP = 20° (UI) → -20° (Z-rotation, tilts right)
    ///   → IMPORTANT: Negated to match engineering convention (positive = lean right)
    /// 
    /// Why -angleVP (Inverted)?
    /// Engineering convention: Positive angleVP = lean away from VP (toward +X)
    /// Unity Z-rotation: Positive Z = roll left (counterclockwise viewed from front)
    /// Negation corrects the handedness mismatch: -angleVP gives expected behavior
    /// 
    /// Rotation Order (Unity's Euler Application):
    /// 1. Z-rotation: -angleVP (sideways tilt)
    /// 2. X-rotation: angleHP (forward/backward tilt)
    /// 3. Y-rotation: rotationY (spin/orientation)
    /// This order prevents gimbal lock and produces intuitive combined rotations.
    /// 
    /// Example Transformations:
    /// ```
    /// // Standard orientation (square front)
    /// distVP=5, distHP=3, angleHP=0, angleVP=0, rotationY=0
    /// → Position: (5, 3, 0)
    /// → Rotation: (0°, 0°, 0°)
    /// → Result: Square base faces camera, no tilt
    /// 
    /// // Tilted toward HP (slant face inclined)
    /// angleHP=30
    /// → Rotation: (30°, 0°, 0°)
    /// → Result: Front face tilts down 30° toward XZ plane
    /// 
    /// // Diamond orientation
    /// rotationY=45
    /// → Rotation: (0°, 45°, 0°)
    /// → Result: Corner faces camera instead of flat edge
    /// 
    /// // Leaning toward VP
    /// angleVP=20
    /// → Rotation: (0°, 0°, -20°)
    /// → Result: Pyramid leans right (toward +X, away from VP)
    /// ```
    /// </summary>
    /// <param name="pyramidObject">The pyramid GameObject to transform</param>
    /// <param name="data">ShapeData containing position and rotation parameters</param>
    private void PostProcessPyramid(GameObject pyramidObject, ShapeData data)
    {
        // === POSITION ===
        // Set pyramid position in engineering coordinate system
        // X: distVP (distance from Vertical Plane, horizontal offset)
        // Y: distHP (distance from Horizontal Plane, vertical offset)
        // Z: 0 (centered in depth)
        //
        // Note: No halfHeight adjustment needed because base is at Y=0
        pyramidObject.transform.position = new Vector3(
            data.distVP,   // X: Horizontal offset from VP
            data.distHP,   // Y: Vertical offset from HP (no centering needed)
            0f             // Z: Centered
        );

        // === ROTATION (CORRECTED AXIS MAPPING) ===
        // Apply rotation in Z→X→Y order (Unity's Euler application sequence)
        // 
        // X-Axis: angleHP (forward/backward tilt, inclination to HP)
        // Y-Axis: rotationY (spin/orientation around vertical axis)
        // Z-Axis: -angleVP (sideways tilt, inclination to VP, INVERTED)
        //
        // Negation of angleVP corrects handedness:
        // - Positive angleVP (UI) → Negative Z-rotation → Lean right (expected)
        // - Without negation: Positive angleVP → Lean left (unexpected)
        Quaternion rotation = Quaternion.Euler(
            data.angleHP,    // X: Forward/backward tilt (inclination to HP)
            data.rotationY,  // Y: Spin/orientation (0° = square, 45° = diamond)
            -data.angleVP    // Z: Sideways tilt (INVERTED, inclination to VP)
        );
        pyramidObject.transform.rotation = rotation;
    }
}