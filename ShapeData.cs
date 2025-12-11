using UnityEngine;
using System;

/// <summary>
/// Data container for 3D shape generation and positioning parameters.
/// Encapsulates all properties needed to generate and position a solid in engineering graphics projections.
/// 
/// Purpose:
/// - Stores shape type selection (Cube, Pyramid, Cone, Prisms, etc.)
/// - Defines dimensional parameters (baseLength, height)
/// - Specifies position relative to projection planes (distHP, distVP)
/// - Controls orientation/inclination angles (angleHP, angleVP, rotationY)
/// - Provides serialization for Unity Inspector and scene persistence
/// 
/// Engineering Graphics Context:
/// In engineering drawing, objects are positioned relative to two principal planes:
/// - HP (Horizontal Plane): XZ plane at Y=0, viewed from above (Top View)
/// - VP (Vertical Plane): YZ plane at X=0, viewed from front (Front View)
/// 
/// This class stores the distances and angles that define how a 3D solid
/// relates to these projection planes for orthographic projection visualization.
/// 
/// Usage:
/// 1. Create ShapeData instance (uses default constructor values)
/// 2. Modify properties via UI sliders or direct assignment
/// 3. Pass to shape generator (Cube, Pyramid, Cone, etc.)
/// 4. Generator creates GameObject with specified dimensions and transformations
/// </summary>
[Serializable]
public class ShapeData
{
    /// <summary>
    /// Enumeration of all supported 3D solid shapes for engineering graphics projection.
    /// 
    /// Shape Categories:
    /// 
    /// BASIC SOLIDS (Legacy/Common):
    /// - Cube: Regular hexahedron (all edges equal)
    /// - Pyramid: Square pyramid (4-sided base)
    /// - Cylinder: Circular prism (curved sides)
    /// - Cone: Circular pyramid (curved sides tapering to apex)
    /// 
    /// PRISMS (Polygonal bases with uniform height):
    /// - TriangularPrism: Triangle bases (3 sides)
    /// - SquarePrism: Square bases (4 sides, similar to cube but independent height)
    /// - PentagonalPrism: Pentagon bases (5 sides)
    /// - HexagonalPrism: Hexagon bases (6 sides)
    /// 
    /// PYRAMIDS (Polygonal bases tapering to apex):
    /// - TriangularPyramid: Triangle base (Tetrahedron, 3 sides)
    /// - PentagonalPyramid: Pentagon base (5 sides)
    /// - HexagonalPyramid: Hexagon base (6 sides)
    /// 
    /// Note: Square pyramid available as both "Pyramid" (legacy) and via GenericPyramid(4)
    /// </summary>
    public enum ShapeType
    {
        // === BASIC SOLIDS ===
        /// <summary>Regular cube (all edges equal, 6 square faces)</summary>
        Cube,
        
        /// <summary>Square pyramid (4-sided base, legacy implementation)</summary>
        Pyramid,
        
        /// <summary>Cylinder (circular bases, curved side surface)</summary>
        Cylinder,

        // === NEW SHAPES ===
        
        /// <summary>Cone (circular base, apex at top, curved side surface)</summary>
        Cone,
        
        // --- PRISMS (N-gon bases with uniform height) ---
        /// <summary>Triangular prism (triangle bases, 3 rectangular sides)</summary>
        TriangularPrism,
        
        /// <summary>Square prism (square bases, 4 rectangular sides)</summary>
        SquarePrism,
        
        /// <summary>Pentagonal prism (pentagon bases, 5 rectangular sides)</summary>
        PentagonalPrism,
        
        /// <summary>Hexagonal prism (hexagon bases, 6 rectangular sides)</summary>
        HexagonalPrism,
        
        // --- PYRAMIDS (N-gon bases tapering to apex) ---
        /// <summary>Triangular pyramid (triangle base, Tetrahedron, 3 triangular sides)</summary>
        TriangularPyramid, // (Tetrahedron)
        
        /// <summary>Pentagonal pyramid (pentagon base, 5 triangular sides)</summary>
        PentagonalPyramid,
        
        /// <summary>Hexagonal pyramid (hexagon base, 6 triangular sides)</summary>
        HexagonalPyramid
    }

    /// <summary>
    /// Selected shape type for generation.
    /// Determines which shape generator class will be instantiated.
    /// </summary>
    public ShapeType shape;
    
    /// <summary>
    /// Base dimension of the shape (interpretation varies by shape type).
    /// 
    /// CUBES:
    /// - Side length of the cube (all edges equal)
    /// - Example: baseLength = 2.0 → 2×2×2 cube
    /// 
    /// PYRAMIDS & CONES:
    /// - Base edge length (Pyramids) or base diameter (Cone)
    /// - Pyramid: Side length of square/polygon base
    /// - Cone: Diameter of circular base (radius = baseLength/2)
    /// - Example: baseLength = 3.0 → Pyramid with 3×3 base, Cone with diameter 3.0
    /// 
    /// PRISMS & CYLINDERS:
    /// - Base edge length (Prisms) or base diameter (Cylinder)
    /// - Prism: Side length of polygon base
    /// - Cylinder: Diameter of circular base (radius = baseLength/2)
    /// - Example: baseLength = 2.5 → Prism with 2.5 edge, Cylinder with diameter 2.5
    /// 
    /// Units: World units (typically meters in Unity)
    /// Range: Typically 0.5 to 5.0 (adjustable via UI slider)
    /// </summary>
    public float baseLength;
    
    /// <summary>
    /// Vertical extent of the shape from base to top.
    /// 
    /// CUBES:
    /// - For true cubes: height = baseLength (enforced by UIManager)
    /// - Slider typically locked when shape type is Cube
    /// - Example: baseLength = 2.0, height = 2.0 → Perfect cube
    /// 
    /// PYRAMIDS & CONES:
    /// - Distance from base to apex (top point)
    /// - Independent of base size (allows tall/squat variations)
    /// - Example: baseLength = 2.0, height = 4.0 → Tall pyramid
    /// 
    /// PRISMS & CYLINDERS:
    /// - Distance between top and bottom bases
    /// - Independent of base size
    /// - Example: baseLength = 3.0, height = 1.5 → Short, wide prism
    /// 
    /// Units: World units (typically meters in Unity)
    /// Range: Typically 0.5 to 5.0 (adjustable via UI slider)
    /// </summary>
    public float height;
    
    /// <summary>
    /// Distance from the Horizontal Plane (HP) to the shape's base or center.
    /// 
    /// Engineering Graphics Context:
    /// HP (Horizontal Plane) = XZ plane at Y=0
    /// - Typically represents the ground plane or reference surface
    /// - Viewed from above in Top View
    /// 
    /// Position Interpretation:
    /// For MOST shapes (Cone, Cylinder, GenericPyramid, GenericPrism):
    /// - Shape is vertically centered around its geometric center
    /// - Position = distHP + halfHeight
    /// - Example: distHP = 2.0, height = 4.0 → Center at Y = 2.0 + 2.0 = 4.0
    ///   → Base at Y = 2.0, Top at Y = 6.0
    /// 
    /// For LEGACY Pyramid.cs:
    /// - Base sits directly on HP (not centered)
    /// - Position = distHP (no halfHeight adjustment)
    /// - Example: distHP = 2.0, height = 4.0 → Base at Y = 2.0, Top at Y = 6.0
    /// 
    /// Units: World units (typically meters in Unity)
    /// Range: Typically 0.0 to 10.0 (adjustable via UI slider)
    /// Default: 1.0 (shape sits slightly above origin)
    /// </summary>
    public float distHP;
    
    /// <summary>
    /// Distance from the Vertical Plane (VP) to the shape's center.
    /// 
    /// Engineering Graphics Context:
    /// VP (Vertical Plane) = YZ plane at X=0
    /// - Typically represents the front reference plane
    /// - Viewed from front in Front View
    /// 
    /// Position Interpretation:
    /// Shapes with circular bases (Cone, Cylinder):
    /// - Position = distVP + radius (center at distVP + baseLength/2)
    /// - Ensures specified distance is from VP to nearest point on shape
    /// - Example: distVP = 3.0, baseLength = 2.0 → Center at X = 3.0 + 1.0 = 4.0
    /// 
    /// Shapes with polygon bases (Pyramids, Prisms, Cube):
    /// - Position = distVP + halfBase (center at distVP + baseLength/2)
    /// - Ensures consistent positioning across all shape types
    /// - Example: distVP = 2.0, baseLength = 4.0 → Center at X = 2.0 + 2.0 = 4.0
    /// 
    /// Units: World units (typically meters in Unity)
    /// Range: Typically 0.0 to 10.0 (adjustable via UI slider)
    /// Default: 1.0 (shape positioned in front of origin)
    /// </summary>
    public float distVP;
    
    /// <summary>
    /// Angle of inclination to the Horizontal Plane (HP) in degrees.
    /// Controls forward/backward tilt of the shape.
    /// 
    /// Engineering Graphics Context:
    /// - Measures how much the shape tilts toward or away from the HP
    /// - Affects the shape's projection onto the HP (Top View)
    /// - Applied as rotation around the X-axis in Unity
    /// 
    /// Rotation Behavior:
    /// - 0°: Shape is upright (vertical axis perpendicular to HP)
    /// - Positive values: Shape tilts forward (top moves toward -Z, front face tilts down)
    /// - Negative values: Shape tilts backward (top moves toward +Z, back face tilts down)
    /// 
    /// Examples:
    /// Pyramid with angleHP = 30°:
    /// - Front slant face tilts 30° toward the HP
    /// - Top View: Base appears larger, apex offset toward back
    /// - Front View: Pyramid appears tilted forward
    /// 
    /// Cylinder with angleHP = 45°:
    /// - Cylinder tilts 45° forward
    /// - Top View: Circular base becomes ellipse
    /// - Front View: Rectangle becomes parallelogram
    /// 
    /// Units: Degrees
    /// Range: Typically -90° to +90° (adjustable via UI slider)
    /// Default: 0° (upright orientation)
    /// </summary>
    public float angleHP;
    
    /// <summary>
    /// Angle of inclination to the Vertical Plane (VP) in degrees.
    /// Controls sideways tilt of the shape (lean left/right).
    /// 
    /// Engineering Graphics Context:
    /// - Measures how much the shape leans toward or away from the VP
    /// - Affects the shape's projection onto the VP (Front View)
    /// - Applied as NEGATIVE rotation around the Z-axis in Unity (-angleVP)
    /// 
    /// Rotation Behavior (INVERTED in code):
    /// - 0°: Shape is upright (vertical axis perpendicular to VP)
    /// - Positive values: Shape leans RIGHT (away from VP, toward +X)
    /// - Negative values: Shape leans LEFT (toward VP, toward -X)
    /// 
    /// Why Inverted (-angleVP)?
    /// Engineering convention: Positive angleVP = lean away from VP (right)
    /// Unity Z-rotation: Positive Z = roll left (counterclockwise from front)
    /// Negation corrects the handedness mismatch for expected behavior
    /// 
    /// Examples:
    /// Pyramid with angleVP = 20°:
    /// - Pyramid leans 20° to the right (away from VP)
    /// - Applied as Z-rotation of -20° in Unity
    /// - Front View: Base appears shifted right, apex offset left
    /// - Top View: Shape appears tilted (not axis-aligned)
    /// 
    /// Cone with angleVP = -15°:
    /// - Cone leans 15° to the left (toward VP)
    /// - Applied as Z-rotation of +15° in Unity
    /// - Front View: Circular base becomes ellipse, shifted left
    /// 
    /// Units: Degrees
    /// Range: Typically -90° to +90° (adjustable via UI slider)
    /// Default: 0° (upright orientation)
    /// </summary>
    public float angleVP;
    
    /// <summary>
    /// Rotation around the vertical axis (Y-axis) in degrees.
    /// Controls the shape's orientation/spin in the horizontal plane.
    /// 
    /// Purpose:
    /// - Changes which face/edge is front-facing
    /// - Useful for creating diamond orientations or specific edge alignments
    /// - Applied as rotation around the Y-axis in Unity
    /// 
    /// Rotation Behavior:
    /// - 0°: Default orientation (typically square/flat edge facing front)
    /// - 45°: Diamond orientation (corner facing front for square-based shapes)
    /// - 90°: Rotated 90° (side face becomes front face)
    /// - Positive values: Counterclockwise rotation (viewed from above)
    /// 
    /// Examples:
    /// Square Pyramid with rotationY = 0°:
    /// - Flat edge faces front (standard orientation)
    /// - Top View: Square base with edge parallel to VP
    /// 
    /// Square Pyramid with rotationY = 45°:
    /// - Corner faces front (diamond orientation)
    /// - Top View: Square base rotated 45° (diamond shape)
    /// 
    /// Hexagonal Prism with rotationY = 30°:
    /// - Rotates to align a specific edge or corner to the front
    /// - Top View: Hexagon rotated 30°
    /// 
    /// Cube with rotationY = 45°:
    /// - Corner faces front instead of flat face
    /// - Top View: Square rotated 45° (diamond)
    /// - Front View: Rectangular profile changes width
    /// 
    /// Units: Degrees
    /// Range: Typically 0° to 360° (adjustable via UI slider)
    /// Default: 0° (standard orientation)
    /// </summary>
    public float rotationY;

    /// <summary>
    /// Default constructor initializing all properties to standard engineering graphics values.
    /// 
    /// Default Values:
    /// - shape: Cube (simplest regular solid)
    /// - baseLength: 2.0 (moderate size, easily visible)
    /// - height: 2.0 (perfect cube proportions)
    /// - distHP: 1.0 (slightly above origin, clear of HP)
    /// - distVP: 1.0 (in front of origin, clear of VP)
    /// - angleHP: 0.0 (upright, no forward/backward tilt)
    /// - angleVP: 0.0 (upright, no sideways lean)
    /// - rotationY: 0.0 (standard orientation, square front)
    /// 
    /// Usage:
    /// ShapeData data = new ShapeData(); // Creates cube at (1, 1, 0) with no rotation
    /// 
    /// Why These Defaults?
    /// - Cube: Most recognizable shape, validates all 3 axes
    /// - 2×2×2 dimensions: Easy to understand, fits well in default camera view
    /// - Position (1, 1, 0): Clearly separated from both projection planes
    /// - No rotation: Allows user to see default orientation first
    /// </summary>
    public ShapeData()
    {
        shape = ShapeType.Cube;
        baseLength = 2.0f;
        height = 2.0f;
        distHP = 1.0f;
        distVP = 1.0f;
        angleHP = 0.0f;
        angleVP = 0.0f;
        rotationY = 0f;
    }
}