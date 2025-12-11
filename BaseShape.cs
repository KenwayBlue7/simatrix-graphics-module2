using UnityEngine;

/// <summary>
/// Abstract base class for all shape generators, implementing the Template Method pattern.
/// Provides common post-processing logic for position and rotation transformations.
/// 
/// Design Pattern: Template Method
/// - Abstract method Generate() is customized by each concrete shape class
/// - Protected method PostProcessShape() provides shared transformation logic
/// - Ensures consistent positioning and rotation across all shape types
/// 
/// Coordinate System Overview:
/// - X-Axis: Horizontal (left-right), controlled by distVP
/// - Y-Axis: Vertical (up-down), controlled by distHP
/// - Z-Axis: Depth (forward-back), typically 0 for 2D projection alignment
/// 
/// Rotation System:
/// - X-Rotation (angleHP): Tilt toward/away from viewer (pitch)
/// - Y-Rotation (rotationY): Spin around vertical axis (yaw)
/// - Z-Rotation (angleVP): Lean left/right (roll) - INVERTED for correct handedness
/// </summary>
public abstract class BaseShape : IShape
{
    /// <summary>
    /// Abstract method that each shape must implement to generate its specific mesh and GameObject.
    /// 
    /// Responsibilities of Generate():
    /// 1. Create vertex positions (local space)
    /// 2. Define triangle indices (face connectivity)
    /// 3. Calculate normals (for lighting and edge detection)
    /// 4. Set up UVs (texture coordinates, if needed)
    /// 5. Create GameObject with MeshFilter and MeshRenderer
    /// 6. Call PostProcessShape() to apply transformations
    /// 
    /// Example Implementation Flow:
    /// ```
    /// public override GameObject Generate(ShapeData data)
    /// {
    ///     // 1. Generate mesh geometry
    ///     Mesh mesh = new Mesh();
    ///     mesh.vertices = CalculateVertices(data);
    ///     mesh.triangles = CalculateTriangles();
    ///     mesh.RecalculateNormals();
    ///     
    ///     // 2. Create GameObject
    ///     GameObject obj = new GameObject("MyShape");
    ///     obj.AddComponent<MeshFilter>().mesh = mesh;
    ///     obj.AddComponent<MeshRenderer>();
    ///     
    ///     // 3. Apply transformations
    ///     PostProcessShape(obj, data);
    ///     
    ///     return obj;
    /// }
    /// ```
    /// </summary>
    /// <param name="data">ShapeData containing dimensions, angles, and position parameters</param>
    /// <returns>Fully configured GameObject with mesh, renderer, and applied transformations</returns>
    public abstract GameObject Generate(ShapeData data);

    /// <summary>
    /// Applies position and rotation transformations to a generated shape GameObject.
    /// 
    /// POSITIONING SYSTEM:
    /// ==================
    /// The shape is positioned relative to the projection planes (HP at Y=0, VP at X=0).
    /// 
    /// X Position (Horizontal Offset from VP):
    /// - Controlled by distVP (Distance from Vertical Plane)
    /// - Formula: X = distVP + (baseLength / 2)
    /// - Rationale: distVP measures from VP (X=0) to the shape's LEFT edge
    /// - Adding half the base length centers the shape at the correct distance
    /// 
    /// Example: Cube with baseLength=2, distVP=3
    /// - Left edge at X=3 (distVP)
    /// - Center at X=3+1=4 (distVP + baseLength/2)
    /// - Right edge at X=5 (distVP + baseLength)
    /// 
    /// Y Position (Vertical Offset from HP):
    /// - Controlled by distHP (Distance from Horizontal Plane)
    /// - Formula: Y = distHP + (height / 2)
    /// - Rationale: distHP measures from HP (Y=0) to the shape's BOTTOM edge
    /// - Adding half the height centers the shape at the correct elevation
    /// 
    /// Example: Cube with height=2, distHP=1
    /// - Bottom edge at Y=1 (distHP)
    /// - Center at Y=1+1=2 (distHP + height/2)
    /// - Top edge at Y=3 (distHP + height)
    /// 
    /// Z Position (Depth):
    /// - Fixed at Z=0 for alignment with projection planes
    /// - Keeps shape centered between HP and VP in the visualization
    /// 
    /// ROTATION SYSTEM:
    /// ================
    /// Three-axis rotation using Euler angles with Unity's left-handed coordinate system.
    /// 
    /// X-Axis Rotation (angleHP - Pitch):
    /// - Controls forward/backward tilt (inclination toward/away from Horizontal Plane)
    /// - Positive angle tilts top forward (toward viewer)
    /// - Range: 0° to 90° (0° = upright, 90° = horizontal)
    /// - Engineering: "Inclination to HP"
    /// 
    /// Y-Axis Rotation (rotationY - Yaw):
    /// - Controls spin around vertical axis (orientation)
    /// - Positive angle rotates clockwise (viewed from top)
    /// - Range: 0° to 360° (arbitrary rotation)
    /// - Use cases: Square (0°) vs Diamond (45°), corner vs edge presentation
    /// 
    /// Z-Axis Rotation (angleVP - Roll, INVERTED):
    /// - Controls sideways tilt (inclination toward/away from Vertical Plane)
    /// - NEGATIVE angleVP is used: -data.angleVP
    /// - Inversion Rationale: Unity's left-handed system vs engineering convention
    /// - Without inversion: Shape tilts opposite to expected direction
    /// - Positive angleVP (input) → Negative Z-rotation (Unity) → Correct lean toward VP
    /// - Range: 0° to 90° input → 0° to -90° applied (0° = upright, -90° = sideways)
    /// - Engineering: "Inclination to VP"
    /// 
    /// WHY Z-AXIS INVERSION?
    /// Unity's left-handed coordinate system:
    /// - Positive Z-rotation rotates clockwise when viewed from front
    /// - Engineering convention expects positive angle to tilt RIGHT (toward +X direction)
    /// - Without inversion: angleVP=30° would tilt LEFT (incorrect)
    /// - With inversion: angleVP=30° → Z=-30° → tilts RIGHT (correct)
    /// 
    /// Gimbal Lock Consideration:
    /// Euler angle order matters. Unity applies rotations in Z→X→Y order.
    /// For educational visualization, this order works well as angleHP and angleVP
    /// are typically used independently (one at a time in most exercises).
    /// </summary>
    /// <param name="shapeObject">The GameObject to transform (must not be null)</param>
    /// <param name="data">ShapeData containing position and rotation parameters</param>
    protected void PostProcessShape(GameObject shapeObject, ShapeData data)
    {
        // Validate input
        if (shapeObject == null) return;

        // === CALCULATE POSITION ===
        
        // X Position: Distance from Vertical Plane (X=0) to shape center
        // Formula: distVP (to left edge) + baseLength/2 (half-width to center)
        float xPosition = data.distVP + (data.baseLength / 2f);

        // Y Position: Distance from Horizontal Plane (Y=0) to shape center
        // Formula: distHP (to bottom edge) + height/2 (half-height to center)
        float yPosition = data.distHP + (data.height / 2f);

        // Z Position: Fixed at origin for projection plane alignment
        // Keeps shape centered in the visualization space
        float zPosition = 0f;

        // Apply final position
        shapeObject.transform.position = new Vector3(xPosition, yPosition, zPosition);
        
        // === APPLY ROTATION ===
        
        // Create Euler rotation from three angles
        // Order: Z → X → Y (Unity's internal rotation order)
        // - X-Axis: angleHP (pitch, tilt toward/away from HP)
        // - Y-Axis: rotationY (yaw, spin/orientation)
        // - Z-Axis: -angleVP (roll, tilt toward/away from VP, INVERTED for correct direction)
        shapeObject.transform.rotation = Quaternion.Euler(
            data.angleHP,      // X-rotation: Pitch (inclination to HP)
            data.rotationY,    // Y-rotation: Yaw (spin/orientation)
            -data.angleVP      // Z-rotation: Roll (inclination to VP, INVERTED)
        );
    }
}

