using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Factory class responsible for creating 3D shape GameObjects from ShapeData specifications.
/// Uses the Strategy pattern with a dictionary-based dispatch system for extensibility.
/// 
/// Architecture:
/// - Static class (no instantiation needed, pure utility)
/// - Dictionary maps ShapeType enum to IShape implementations
/// - Each IShape handles its own mesh generation logic
/// - Automatic camera targeting after shape creation
/// 
/// Design Benefits:
/// - Adding new shapes: Simply register a new IShape in the dictionary
/// - No switch/case statements (Open/Closed Principle)
/// - Type-safe enum-based dispatch
/// - Centralized shape creation logic
/// </summary>
public class ShapeGenerator
{
    /// <summary>
    /// Registry of all available shapes mapped to their generator implementations.
    /// 
    /// Shape Categories:
    /// 1. Basic Solids: Cube, Cylinder, Cone
    /// 2. Pyramids: Triangular (Tetrahedron), Square, Pentagonal, Hexagonal
    /// 3. Prisms: Triangular, Square, Pentagonal, Hexagonal
    /// 
    /// Generic Implementations:
    /// - GenericPyramid(n): Generates n-sided pyramid (n = number of base sides)
    /// - GenericPrism(n): Generates n-sided prism (n = number of base sides)
    /// 
    /// Special Cases:
    /// - Pyramid: Legacy square pyramid (kept for backward compatibility)
    /// - Cube: Dedicated implementation (could use GenericPrism(4) but has optimized mesh)
    /// - Cone: Circular base (special case of pyramid with infinite sides)
    /// - Cylinder: Circular bases (special case of prism with infinite sides)
    /// 
    /// To Add a New Shape:
    /// 1. Add enum value to ShapeData.ShapeType
    /// 2. Create class implementing IShape interface
    /// 3. Register here: { ShapeData.ShapeType.NewShape, new NewShapeClass() }
    /// </summary>
    private static readonly Dictionary<ShapeData.ShapeType, IShape> shapeMap = new Dictionary<ShapeData.ShapeType, IShape>
    {
        // === BASIC SOLIDS ===
        { ShapeData.ShapeType.Cube, new Cube() },
        { ShapeData.ShapeType.Cylinder, new Cylinder() },
        { ShapeData.ShapeType.Cone, new Cone() },
        
        // === PYRAMIDS ===
        // Note: ShapeType.Pyramid kept for backward compatibility with existing ShapeData
        { ShapeData.ShapeType.Pyramid, new Pyramid() },              // Legacy square pyramid
        { ShapeData.ShapeType.TriangularPyramid, new GenericPyramid(3) },  // Tetrahedron (n=3)
        { ShapeData.ShapeType.PentagonalPyramid, new GenericPyramid(5) },  // Pentagon base (n=5)
        { ShapeData.ShapeType.HexagonalPyramid, new GenericPyramid(6) },   // Hexagon base (n=6)
        
        // === PRISMS ===
        { ShapeData.ShapeType.TriangularPrism, new GenericPrism(3) },   // Triangle base (n=3)
        { ShapeData.ShapeType.SquarePrism, new GenericPrism(4) },       // Square base (n=4)
        { ShapeData.ShapeType.PentagonalPrism, new GenericPrism(5) },   // Pentagon base (n=5)
        { ShapeData.ShapeType.HexagonalPrism, new GenericPrism(6) }     // Hexagon base (n=6)
    };

    /// <summary>
    /// Creates a 3D shape GameObject from ShapeData specification and automatically sets it as the camera target.
    /// 
    /// Process Flow:
    /// 1. Lookup: Find IShape implementation for requested shape type
    /// 2. Generation: Call IShape.Generate() to create mesh and GameObject
    /// 3. Post-Processing: BaseShape.PostProcessShape() applies transformations (rotation, position)
    /// 4. Camera Setup: Automatically set OrbitCameraController to orbit this shape
    /// 
    /// Error Handling:
    /// - Unknown shape types: Returns null (silent failure for extensibility)
    /// - Missing camera: Shape still created, but not auto-targeted
    /// - Null ShapeData: Handled by individual IShape implementations
    /// 
    /// Camera Targeting:
    /// The OrbitCameraController needs a target Transform to orbit around.
    /// This method automatically updates the camera target whenever a new shape is created,
    /// ensuring the camera always focuses on the active shape.
    /// </summary>
    /// <param name="data">ShapeData containing shape type and parameters (baseLength, height, angles, etc.)</param>
    /// <returns>Generated GameObject with MeshFilter, MeshRenderer, and configured mesh. Returns null if shape type not found.</returns>
    public static GameObject CreateShape(ShapeData data)
    {
        GameObject generatedObject = null;
        
        // === DICTIONARY LOOKUP ===
        // Strategy Pattern: Dispatch to appropriate IShape implementation
        if (shapeMap.TryGetValue(data.shape, out IShape shape))
        {
            // Generate the shape mesh and GameObject
            // IShape.Generate() handles:
            // - Mesh creation (vertices, triangles, normals, UVs)
            // - GameObject setup (MeshFilter, MeshRenderer)
            // - BaseShape.PostProcessShape() for transformations
            generatedObject = shape.Generate(data);
        }
        else
        {
            // Shape type not registered in dictionary
            // Silent failure allows forward compatibility:
            // - Old builds won't crash if new shape types are added to enum
            // - UI can gracefully handle missing shapes
            // Consider logging to external analytics for production debugging
        }

        // === AUTOMATIC CAMERA TARGETING ===
        if (generatedObject != null)
        {
            // Find the main camera's OrbitCameraController component
            OrbitCameraController cameraController = Camera.main?.GetComponent<OrbitCameraController>();
            
            if (cameraController != null)
            {
                // Set the new shape as the orbit target
                // This ensures the camera:
                // - Orbits around the new shape's center
                // - Adjusts zoom based on shape bounds
                // - Maintains relative viewing angle
                cameraController.target = generatedObject.transform;
            }
            // Note: Missing camera controller is acceptable
            // - Editor mode: May not have camera setup yet
            // - Test scenarios: Shape generation without visualization
            // - Multi-camera setups: May use different camera system
        }

        return generatedObject;
    }
}