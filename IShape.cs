using UnityEngine;

/// <summary>
/// Interface for all 3D shape generators in the engineering graphics system.
/// 
/// Implements the Strategy Pattern - each shape (Cube, Pyramid, Cone, etc.) provides
/// its own implementation while maintaining a consistent API.
/// 
/// Benefits:
/// - Polymorphic shape creation (ShapeGenerator works with any IShape)
/// - Easy to add new shapes (Open/Closed Principle)
/// - Type-safe and testable
/// 
/// Usage:
/// ```csharp
/// IShape generator = new Cube(); // or Pyramid, Cone, etc.
/// GameObject shape = generator.Generate(data);
/// ```
/// 
/// Engineering Context:
/// Shapes are positioned relative to two projection planes:
/// - HP (Horizontal Plane): XZ plane at Y=0 (Top View)
/// - VP (Vertical Plane): YZ plane at X=0 (Front View)
/// </summary>
public interface IShape
{
    /// <summary>
    /// Generates a 3D solid GameObject based on shape parameters.
    /// 
    /// Implementation must:
    /// 1. Create GameObject with MeshFilter and MeshRenderer
    /// 2. Generate mesh (vertices, triangles, normals, bounds)
    /// 3. Apply transformations (position and rotation from ShapeData)
    /// 4. Return fully configured GameObject (not added to scene)
    /// 
    /// Parameters from ShapeData:
    /// - baseLength: Base dimension (edge length or diameter)
    /// - height: Vertical extent
    /// - distHP/distVP: Position offsets from HP/VP planes
    /// - angleHP: X-axis rotation (forward/backward tilt)
    /// - angleVP: Z-axis rotation (sideways lean, inverted: -angleVP)
    /// - rotationY: Y-axis rotation (horizontal spin)
    /// 
    /// Transformation Order (Unity's Euler Z→X→Y):
    /// 1. Z-rotation: -angleVP (sideways)
    /// 2. X-rotation: angleHP (forward/back)
    /// 3. Y-rotation: rotationY (spin)
    /// 
    /// Note: Creates Unity GameObjects - must run on main thread.
    /// </summary>
    /// <param name="data">Shape parameters (dimensions, position, rotation)</param>
    /// <returns>Configured GameObject with mesh and transformations applied</returns>
    GameObject Generate(ShapeData data);
}