using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Main controller for the 3D shape visualization system.
/// Manages shape creation, projection analysis, and advanced rotation modes (face inclination, orientation control).
/// Coordinates between ShapeGenerator, MeshAnalyzer, and ProjectionDrawer.
/// </summary>
public class Visualizer : MonoBehaviour
{
    [Header("Shape Configuration")]
    public ShapeData shapeData;
    
    [Header("Material Settings")]
    public Material shapeMaterial;
    public Material lineMaterial; // For solid projection lines
    public Material dottedLineMaterial; // For dotted projector lines
    
    [Header("Pyramid Face Inclination Logic")]
    [Tooltip("Enables HP face inclination mode - calculates rotation to make a slant face parallel to the horizontal plane")]
    public bool useFaceInclinationHP = false;
    
    [Tooltip("Enables VP face inclination mode - calculates multi-axis rotation to make a slant face parallel to the vertical plane")]
    public bool useFaceInclinationVP = false;
    
    [Header("Orientation Control")]
    [Tooltip("Rotates shape to present corners vs edges (shape-specific preset angles)")]
    public bool isDiamondOrientation = false;
    
    // Current 3D shape GameObject
    private GameObject currentShape;
    
    // Edge analysis data from MeshAnalyzer
    private Dictionary<MeshAnalyzer.Edge, List<MeshAnalyzer.Face>> edgeMap;
    
    // References to projection parent objects for efficient cleanup
    private GameObject hpProjectionParent;
    private GameObject vpProjectionParent;
    private GameObject connectorLinesParent;
    
    void Start()
    {
        UpdateVisualization();
    }
    
    void OnDestroy()
    {
        CleanupOldObjects();
    }
    
    /// <summary>
    /// Public method for real-time updates from UI Manager.
    /// Triggers full regeneration of shape and projections.
    /// </summary>
    public void UpdateVisualization()
    {
        CleanupOldObjects();
        CreateAndConfigureShape();
        AnalyzeShapeMesh();
    }
    
    /// <summary>
    /// Checks if a shape type is classified as a pyramid (supports face inclination logic).
    /// </summary>
    /// <param name="type">The shape type to check</param>
    /// <returns>True if the shape is a pyramid or cone, false otherwise</returns>
    public bool IsPyramidType(ShapeData.ShapeType type)
    {
        return type == ShapeData.ShapeType.Pyramid ||
               type == ShapeData.ShapeType.TriangularPyramid ||
               type == ShapeData.ShapeType.PentagonalPyramid ||
               type == ShapeData.ShapeType.HexagonalPyramid ||
               type == ShapeData.ShapeType.Cone;
    }
    
    /// <summary>
    /// Toggles the visibility of the current 3D shape.
    /// Projections remain visible when shape is hidden.
    /// </summary>
    /// <returns>The new active state of the shape (true if visible, false if hidden)</returns>
    public bool ToggleShapeVisibility()
    {
        if (currentShape != null)
        {
            currentShape.SetActive(!currentShape.activeSelf);
            return currentShape.activeSelf;
        }
        
        return false;
    }
    
    /// <summary>
    /// Toggles the visibility of the dotted connector lines between 3D vertices and 2D projections.
    /// </summary>
    /// <returns>The new active state of the connector lines (true if visible, false if hidden)</returns>
    public bool ToggleConnectorLinesVisibility()
    {
        if (connectorLinesParent != null)
        {
            connectorLinesParent.SetActive(!connectorLinesParent.activeSelf);
            return connectorLinesParent.activeSelf;
        }
        
        return false;
    }
    
    /// <summary>
    /// Enables Face Inclination mode for HP (Horizontal Plane) and disables VP mode.
    /// Mutual exclusion ensures only one face inclination mode is active at a time.
    /// Forces square orientation (disables diamond toggle).
    /// </summary>
    /// <returns>True if the operation was successful, false if shape doesn't support face inclination</returns>
    public bool EnableFaceInclinationHP()
    {
        if (!IsPyramidType(shapeData.shape))
        {
            return false;
        }
        
        useFaceInclinationHP = true;
        useFaceInclinationVP = false;
        isDiamondOrientation = false;
        
        return true;
    }
    
    /// <summary>
    /// Enables Face Inclination mode for VP (Vertical Plane) and disables HP mode.
    /// Uses multi-axis rotation (X, Y, Z) to correctly present slant face to VP.
    /// Forces square orientation (disables diamond toggle).
    /// </summary>
    /// <returns>True if the operation was successful, false if shape doesn't support face inclination</returns>
    public bool EnableFaceInclinationVP()
    {
        if (!IsPyramidType(shapeData.shape))
        {
            return false;
        }
        
        useFaceInclinationHP = false;
        useFaceInclinationVP = true;
        isDiamondOrientation = false;
        
        return true;
    }
    
    /// <summary>
    /// Disables both HP and VP face inclination modes.
    /// Returns to direct angle control or manual rotation.
    /// </summary>
    public void DisableFaceInclination()
    {
        useFaceInclinationHP = false;
        useFaceInclinationVP = false;
    }
    
    /// <summary>
    /// Checks if any face inclination mode is currently active.
    /// </summary>
    /// <returns>True if either HP or VP face inclination is enabled</returns>
    public bool IsFaceInclinationActive()
    {
        return useFaceInclinationHP || useFaceInclinationVP;
    }
    
    /// <summary>
    /// Calculates the X-axis rotation needed to make a pyramid's slant face parallel to HP or VP.
    /// 
    /// Mathematical Foundation:
    /// 1. Calculate apothem (perpendicular distance from center to edge midpoint):
    ///    a = s / (2 * tan(π/n)) where s=side length, n=number of sides
    /// 
    /// 2. Calculate natural face angle:
    ///    α = arctan(height / apothem)
    /// 
    /// 3. Apply correction offset:
    ///    HP: rotation = (α - 180°) + targetAngle  (aligns face to floor)
    ///    VP: rotation = (α - 90°) + targetAngle   (aligns back face to wall, accounts for Z-inversion)
    /// 
    /// Supports: Triangular (n=3), Square (n=4), Pentagonal (n=5), Hexagonal (n=6) Pyramids, and Cone (radius-based)
    /// </summary>
    /// <param name="targetAngle">The desired inclination angle for the slant face (from angleHP or angleVP slider)</param>
    /// <param name="height">The height of the pyramid</param>
    /// <param name="baseLength">The base length (side length) of the pyramid</param>
    /// <param name="isHP">True for Horizontal Plane (floor), False for Vertical Plane (wall)</param>
    /// <returns>The X-axis rotation angle in degrees needed to achieve the target inclination</returns>
    private float CalculateFaceInclination(float targetAngle, float height, float baseLength, bool isHP)
    {
        // Determine number of sides based on pyramid type
        int sides = 4; // Default to Square Pyramid
        
        if (shapeData.shape == ShapeData.ShapeType.TriangularPyramid)
            sides = 3;
        else if (shapeData.shape == ShapeData.ShapeType.Pyramid)
            sides = 4;
        else if (shapeData.shape == ShapeData.ShapeType.PentagonalPyramid)
            sides = 5;
        else if (shapeData.shape == ShapeData.ShapeType.HexagonalPyramid)
            sides = 6;
        else if (shapeData.shape == ShapeData.ShapeType.Cone)
        {
            // Special case: Cone uses radius instead of apothem
            float coneRadius = baseLength / 2f;
            float coneAlpha = Mathf.Atan(height / coneRadius) * Mathf.Rad2Deg;
            float coneOffset = isHP ? -180f : -90f;
            float coneRotation = (coneAlpha + coneOffset) + targetAngle;
            
            return coneRotation;
        }
        
        // Calculate Apothem using General Polygon Formula
        // apothem = s / (2 * tan(π/n))
        float apothem = baseLength / (2f * Mathf.Tan(Mathf.PI / sides));
        
        // Calculate Natural Face Angle (α) in degrees
        // α = arctan(height / apothem)
        float alpha = Mathf.Atan(height / apothem) * Mathf.Rad2Deg;
        
        // Apply correction offset based on plane
        // HP: -180° offset aligns face to floor
        // VP: -90° offset aligns back face to wall (accounts for Unity's Z-axis inversion)
        float offset = isHP ? -180f : -90f;
        float rotation = (alpha + offset) + targetAngle;
        
        return rotation;
    }
    
    /// <summary>
    /// Cleans up all existing GameObjects (shape, projections, connectors) before regeneration.
    /// Uses cached references for efficient cleanup without scene traversal.
    /// </summary>
    private void CleanupOldObjects()
    {
        // Clean up the current 3D shape GameObject
        if (currentShape != null)
        {
            if (Application.isPlaying)
            {
                Destroy(currentShape);
            }
            else
            {
                DestroyImmediate(currentShape);
            }
            currentShape = null;
        }
        
        // Clean up HP projection parent and all its children
        if (hpProjectionParent != null)
        {
            if (Application.isPlaying)
            {
                Destroy(hpProjectionParent);
            }
            else
            {
                DestroyImmediate(hpProjectionParent);
            }
            hpProjectionParent = null;
        }
        
        // Clean up VP projection parent and all its children
        if (vpProjectionParent != null)
        {
            if (Application.isPlaying)
            {
                Destroy(vpProjectionParent);
            }
            else
            {
                DestroyImmediate(vpProjectionParent);
            }
            vpProjectionParent = null;
        }
        
        // Clean up connector lines parent and all its children
        if (connectorLinesParent != null)
        {
            if (Application.isPlaying)
            {
                Destroy(connectorLinesParent);
            }
            else
            {
                DestroyImmediate(connectorLinesParent);
            }
            connectorLinesParent = null;
        }
        
        // Reset edge analysis data
        edgeMap = null;
    }
    
    /// <summary>
    /// Creates and configures the 3D shape based on current ShapeData and rotation mode settings.
    /// 
    /// Rotation Priority System:
    /// 1. Face Inclination (highest) - Uses calculated angles for slant face alignment
    /// 2. Orientation Toggle - Uses preset angles (30°, 45°, 54°, 180°) for corner/edge presentation
    /// 3. Manual Rotation (lowest) - Uses rotationY slider value directly
    /// 
    /// Coordinate Mapping:
    /// - angleHP → X-axis (pitch)
    /// - rotationY → Y-axis (yaw)
    /// - angleVP → Z-axis (roll, inverted in BaseShape.PostProcessShape)
    /// </summary>
    private void CreateAndConfigureShape()
    {
        if (shapeData == null)
        {
            return;
        }
        
        // Initialize effective angles with default values
        float effectiveAngleHP;
        float effectiveAngleVP;
        float effectiveRotationY = 0f;
        
        // === PRIORITY 1: FACE INCLINATION MODES ===
        if (useFaceInclinationHP && IsPyramidType(shapeData.shape))
        {
            // HP Face Inclination: Single-axis rotation (X) to align slant face to floor
            effectiveAngleHP = CalculateFaceInclination(shapeData.angleHP, shapeData.height, shapeData.baseLength, true);
            effectiveAngleVP = shapeData.angleVP; // Keep VP angle unchanged
            effectiveRotationY = 0f; // Standard front-facing orientation
        }
        else if (useFaceInclinationVP && IsPyramidType(shapeData.shape))
        {
            // VP Face Inclination: Multi-axis rotation (X, Y, Z) to align slant face to wall
            var (xRot, yRot, zRot) = CalculateVPFaceRotation(shapeData.angleVP, shapeData.height, shapeData.baseLength);
            
            // X-axis: VP face inclination correction (calculated)
            effectiveAngleHP = xRot;
            
            // Y-axis: 90° orientation to present side face to VP
            effectiveRotationY = yRot;
            
            // Z-axis: User's HP angle + any VP Z correction
            effectiveAngleVP = shapeData.angleHP + zRot;
        }
        // === PRIORITY 2: MANUAL ROTATION (BASE VALUE) ===
        else
        {
            // Start with direct slider values
            effectiveAngleHP = shapeData.angleHP;
            effectiveAngleVP = shapeData.angleVP;
            effectiveRotationY = shapeData.rotationY; // Manual rotation slider value
            
            // === PRIORITY 3: ORIENTATION TOGGLE (OVERRIDE) ===
            if (isDiamondOrientation)
            {
                // Override manual rotation with preset orientation angles
                
                // PYRAMIDS: Shape-specific corner/edge orientation
                if (IsPyramidType(shapeData.shape))
                {
                    if (shapeData.shape == ShapeData.ShapeType.HexagonalPyramid)
                    {
                        // Hexagonal: Default is corner-facing, toggle to edge-facing
                        effectiveRotationY = 30f;
                    }
                    else if (shapeData.shape == ShapeData.ShapeType.TriangularPyramid)
                    {
                        // Triangular: 30° to corner
                        effectiveRotationY = 30f;
                    }
                    else if (shapeData.shape == ShapeData.ShapeType.PentagonalPyramid)
                    {
                        // Pentagonal: 54° to corner (90° - 360°/10)
                        effectiveRotationY = 54f;
                    }
                    else if (shapeData.shape == ShapeData.ShapeType.Pyramid)
                    {
                        // Square: 45° to corner (diamond)
                        effectiveRotationY = 45f;
                    }
                    else
                    {
                        // Cone and others: 45° default
                        effectiveRotationY = 45f;
                    }
                }
                // PRISMS: Diamond/flipped orientations
                else
                {
                    if (shapeData.shape == ShapeData.ShapeType.Cube || shapeData.shape == ShapeData.ShapeType.SquarePrism)
                    {
                        // Square-based: 45° diamond orientation
                        effectiveRotationY = 45f;
                    }
                    else if (shapeData.shape == ShapeData.ShapeType.TriangularPrism)
                    {
                        // Triangular: 180° flip
                        effectiveRotationY = 180f;
                    }
                    else if (shapeData.shape == ShapeData.ShapeType.HexagonalPrism)
                    {
                        // Hexagonal: 30° to corner
                        effectiveRotationY = 30f;
                    }
                    else if (shapeData.shape == ShapeData.ShapeType.PentagonalPrism)
                    {
                        // Pentagonal: 180° flip
                        effectiveRotationY = 180f;
                    }
                    else
                    {
                        // Cylinder and others: 45° default
                        effectiveRotationY = 45f;
                    }
                }
            }
        }
        
        // Force orientation toggle OFF if face inclination is active (mutual exclusion)
        if (IsFaceInclinationActive())
        {
            isDiamondOrientation = false;
        }
        
        // Create temporary ShapeData with effective (calculated) angles
        ShapeData effectiveShapeData = new ShapeData();
        effectiveShapeData.shape = shapeData.shape;
        effectiveShapeData.baseLength = shapeData.baseLength;
        effectiveShapeData.height = shapeData.height;
        effectiveShapeData.angleHP = effectiveAngleHP;         // X-axis rotation
        effectiveShapeData.angleVP = effectiveAngleVP;         // Z-axis rotation (inverted in BaseShape)
        effectiveShapeData.distHP = shapeData.distHP;          // Y-axis position
        effectiveShapeData.distVP = shapeData.distVP;          // X-axis position
        effectiveShapeData.rotationY = effectiveRotationY;     // Y-axis rotation
        
        // Generate the shape mesh via ShapeGenerator
        currentShape = ShapeGenerator.CreateShape(effectiveShapeData);
        
        if (currentShape != null)
        {
            ApplyMaterial();
        }
    }
    
    /// <summary>
    /// Applies the assigned material to the generated shape's renderer.
    /// </summary>
    private void ApplyMaterial()
    {
        if (shapeMaterial == null) return;
        
        Renderer renderer = currentShape.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = shapeMaterial;
        }
    }
    
    /// <summary>
    /// Analyzes the shape's mesh to extract edges and faces, then triggers projection drawing.
    /// Pipeline: MeshFilter → MeshAnalyzer → ProjectionDrawer
    /// </summary>
    private void AnalyzeShapeMesh()
    {
        if (currentShape == null) return;

        MeshFilter meshFilter = currentShape.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.mesh != null)
        {
            // Build edge map with face associations for visibility determination
            edgeMap = MeshAnalyzer.BuildEdgeMap(meshFilter.mesh, currentShape.transform);

            // Draw HP and VP projections with connector lines
            var projectionParents = ProjectionDrawer.DrawProjections(
                edgeMap,
                lineMaterial,
                dottedLineMaterial,
                meshFilter.mesh,
                currentShape.transform
            );
            
            // Cache parent references for efficient cleanup
            hpProjectionParent = projectionParents.hpParent;
            vpProjectionParent = projectionParents.vpParent;
            connectorLinesParent = projectionParents.connectorParent;
        }
    }
    
    /// <summary>
    /// Calculates multi-axis rotation for VP face inclination mode.
    /// 
    /// Strategy (derived from manual testing):
    /// - Y-axis: Always 90° (rotates pyramid to present a side face to the VP/wall)
    /// - X-axis: (α - 90°) + targetAngle (tilts the face to desired inclination)
    /// - Z-axis: 0° (no roll needed)
    /// 
    /// Where α = arctan(height / apothem) is the natural face angle.
    /// 
    /// This ensures the correct slant face is both facing the VP and inclined at the target angle.
    /// </summary>
    /// <param name="targetAngle">The desired VP inclination angle from the slider</param>
    /// <param name="height">Pyramid height</param>
    /// <param name="baseLength">Pyramid base side length</param>
    /// <returns>Tuple of (X-rotation, Y-rotation, Z-rotation) in degrees</returns>
    private (float xRot, float yRot, float zRot) CalculateVPFaceRotation(float targetAngle, float height, float baseLength)
    {
        // Determine polygon sides
        int sides = 4; // Default to square
        
        if (shapeData.shape == ShapeData.ShapeType.TriangularPyramid)
            sides = 3;
        else if (shapeData.shape == ShapeData.ShapeType.Pyramid)
            sides = 4;
        else if (shapeData.shape == ShapeData.ShapeType.PentagonalPyramid)
            sides = 5;
        else if (shapeData.shape == ShapeData.ShapeType.HexagonalPyramid)
            sides = 6;
        
        // Calculate apothem and natural face angle
        float apothem = baseLength / (2f * Mathf.Tan(Mathf.PI / sides));
        float alpha = Mathf.Atan(height / apothem) * Mathf.Rad2Deg;
        
        // Apply multi-axis rotation formula
        float yRot = 90f;                        // Present side face to VP
        float xRot = alpha - 90f + targetAngle;  // Tilt face to target angle
        float zRot = 0f;                         // No roll needed
        
        return (xRot, yRot, zRot);
    }
}