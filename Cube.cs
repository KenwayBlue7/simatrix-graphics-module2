using UnityEngine;

/// <summary>
/// Generates a cube mesh using Unity's built-in primitive cube.
/// Optimized implementation leveraging Unity's pre-built geometry.
/// 
/// Geometry Details:
/// - 8 vertices (corners)
/// - 12 triangles (2 per face, 6 faces total)
/// - 6 faces (front, back, left, right, top, bottom)
/// - Pre-calculated normals and UVs
/// 
/// Design Choice: Unity Primitive vs Custom Mesh
/// This class uses GameObject.CreatePrimitive() instead of building vertices manually.
/// 
/// Advantages of Unity Primitive:
/// + Optimized mesh topology (shared vertices, efficient triangulation)
/// + Pre-calculated normals (correct lighting out-of-the-box)
/// + Built-in UVs (texture mapping ready)
/// + Faster generation (no vertex/triangle array allocation)
/// + Less code to maintain
/// 
/// When to Use Custom Mesh Instead:
/// - Need non-uniform scaling per-axis BEFORE rotation (primitive uses transform.localScale)
/// - Require custom UV mapping (e.g., different textures per face)
/// - Need hard-edge control (primitive has smooth vertex normals)
/// - Want consistent mesh structure with other shapes (pyramids, prisms use custom meshes)
/// 
/// Educational Purpose:
/// - Demonstrates basic rectangular solid projection
/// - Shows square projection in HP (top view)
/// - Shows square projection in VP (front view)
/// - Illustrates axis-aligned geometry (no complex angles)
/// 
/// Special Constraint in ShapeData:
/// For a Cube, height is automatically locked to baseLength to maintain 1:1:1 proportions.
/// This is enforced by UIManager when the shape type is Cube.
/// </summary>
public class Cube : BaseShape
{
    /// <summary>
    /// Generates a cube GameObject using Unity's primitive cube mesh.
    /// 
    /// Algorithm:
    /// 1. Create primitive cube GameObject (1×1×1 unit cube at origin)
    /// 2. Disable shadow casting for clean technical drawing aesthetic
    /// 3. Scale cube to match ShapeData dimensions (baseLength × height × baseLength)
    /// 4. Apply transformations via PostProcessShape() (rotation, position)
    /// 
    /// Scaling Behavior:
    /// Unity primitive starts as a 1×1×1 unit cube.
    /// transform.localScale directly multiplies dimensions:
    /// - X scale: baseLength (width)
    /// - Y scale: height (typically equals baseLength for true cube)
    /// - Z scale: baseLength (depth)
    /// 
    /// Example: baseLength=2.5, height=2.5
    /// Result: 2.5×2.5×2.5 unit cube (true cube)
    /// 
    /// Example: baseLength=2, height=3 (rectangular prism)
    /// Result: 2×3×2 unit box (not a perfect cube, but allowed for flexibility)
    /// Note: UIManager typically prevents height≠baseLength for Cube shape type
    /// 
    /// Shadow Casting:
    /// Disabled for technical drawing aesthetic:
    /// - Shadows can obscure projection lines
    /// - Educational focus is on geometry, not lighting
    /// - Keeps visualization clean and uncluttered
    /// </summary>
    /// <param name="data">ShapeData containing baseLength (width/depth) and height (typically equal for cube)</param>
    /// <returns>GameObject with cube mesh, MeshFilter, MeshRenderer, and applied transformations</returns>
    public override GameObject Generate(ShapeData data)
    {
        // === CREATE PRIMITIVE CUBE ===
        // Unity's built-in cube:
        // - 1×1×1 unit dimensions (before scaling)
        // - Centered at origin (0, 0, 0)
        // - 8 vertices at corners: (±0.5, ±0.5, ±0.5)
        // - 12 triangles (6 quads, 2 triangles each)
        // - Smooth vertex normals (shared vertices)
        GameObject cubeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        
        // === DISABLE SHADOW CASTING ===
        // Get the Renderer component (automatically added by CreatePrimitive)
        Renderer cubeRenderer = cubeObject.GetComponent<Renderer>();
        if (cubeRenderer != null)
        {
            // Turn off shadows for clean technical drawing appearance
            // ShadowCastingMode options:
            // - Off: No shadows (used here)
            // - On: Casts shadows on other objects
            // - TwoSided: Casts shadows from both sides
            // - ShadowsOnly: Invisible but casts shadows
            cubeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        
        // === APPLY DIMENSIONS ===
        // Scale the 1×1×1 unit cube to desired dimensions
        // Vector3(X, Y, Z) where:
        // - X: Width (baseLength)
        // - Y: Height (data.height, typically equals baseLength for true cube)
        // - Z: Depth (baseLength)
        //
        // Why baseLength for X and Z?
        // In engineering graphics, a cube's "base" is the XZ plane (horizontal).
        // baseLength defines the side length of this square base.
        // Height (Y-axis) extends upward from the base.
        //
        // For a perfect cube: baseLength == height (enforced by UIManager)
        // For a rectangular prism: baseLength ≠ height (allowed but not typical for "Cube" type)
        Vector3 scale = new Vector3(data.baseLength, data.height, data.baseLength);
        cubeObject.transform.localScale = scale;
        
        // === APPLY TRANSFORMATIONS ===
        // Call BaseShape.PostProcessShape() to apply:
        // - Position: Based on distHP (vertical offset) and distVP (horizontal offset)
        // - Rotation: Based on angleHP (pitch), angleVP (roll), rotationY (yaw)
        //
        // Rotation Order: Z → X → Y (Euler angles)
        // - angleHP: Tilt toward/away from horizontal plane
        // - angleVP: Lean toward/away from vertical plane (inverted for correct handedness)
        // - rotationY: Spin around vertical axis (e.g., 0° for square, 45° for diamond)
        //
        // Position Calculation:
        // - X = distVP + baseLength/2 (distance from VP to center)
        // - Y = distHP + height/2 (distance from HP to center)
        // - Z = 0 (aligned with projection planes)
        PostProcessShape(cubeObject, data);
        
        // Return the fully configured GameObject
        return cubeObject;
    }
}