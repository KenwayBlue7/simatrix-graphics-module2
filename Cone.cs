// Scripts/SolidsProjection/Cone.cs
using UnityEngine;

/// <summary>
/// Generates a cone mesh with a circular base and apex at the top.
/// Implements hard-edge geometry for clear projection visualization in engineering graphics.
/// 
/// Geometry Details:
/// - Base: Circle with SEGMENTS vertices (24 by default)
/// - Side: SEGMENTS triangular faces connecting base rim to apex
/// - Total faces: SEGMENTS (bottom cap) + SEGMENTS (side)
/// 
/// Hard Edge Strategy:
/// Uses separate vertices for bottom cap and side faces to ensure distinct edges
/// are visible in orthographic projections (HP and VP views).
/// 
/// Vertex Layout:
/// [0]: Bottom cap center
/// [1 to SEGMENTS]: Bottom cap rim (CCW from +X axis)
/// [SEGMENTS+1 onwards]: Side triangles (3 vertices each: apex, base_left, base_right)
/// 
/// Educational Purpose:
/// - Demonstrates circular base projection (circle in HP, triangle in VP)
/// - Shows converging generator lines from rim to apex
/// - Illustrates the principle of a pyramid with infinite sides
/// </summary>
public class Cone : BaseShape
{
    /// <summary>
    /// Number of triangular segments that approximate the circular base.
    /// Higher values = smoother circle, but more vertices/triangles.
    /// 24 provides a good balance between visual quality and performance.
    /// </summary>
    private const int SEGMENTS = 24;

    /// <summary>
    /// Generates a cone mesh based on ShapeData parameters.
    /// 
    /// Algorithm:
    /// 1. Calculate dimensions from ShapeData (radius from baseLength, height)
    /// 2. Generate bottom cap vertices (center + rim)
    /// 3. Generate side vertices (apex + rim pairs for each segment)
    /// 4. Define triangles for bottom cap (center-current-next pattern)
    /// 5. Define triangles for sides (apex-right-left pattern)
    /// 6. Create mesh and apply to GameObject
    /// 7. Apply transformations via PostProcessShape()
    /// 
    /// Coordinate System:
    /// - Center at origin (0, 0, 0)
    /// - Base at Y = -halfHeight
    /// - Apex at Y = +halfHeight
    /// - Circular rim extends in XZ plane
    /// </summary>
    /// <param name="data">ShapeData containing baseLength (diameter) and height</param>
    /// <returns>GameObject with cone mesh, MeshFilter, and MeshRenderer</returns>
    public override GameObject Generate(ShapeData data)
    {
        // === CREATE GAMEOBJECT AND COMPONENTS ===
        GameObject coneObj = new GameObject("Cone");
        MeshFilter meshFilter = coneObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = coneObj.AddComponent<MeshRenderer>();
        
        // Disable shadows for clean technical drawing aesthetic
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // === CALCULATE DIMENSIONS ===
        // Radius: Half of base length (diameter → radius)
        float radius = data.baseLength / 2f;
        
        // Total height (apex to base)
        float height = data.height;
        
        // Half height for centering (apex at +h/2, base at -h/2)
        float halfHeight = height / 2f;

        // === VERTEX COUNT CALCULATION ===
        // Bottom Cap: SEGMENTS + 1 vertices
        //   - 1 center vertex
        //   - SEGMENTS rim vertices (circle approximation)
        //
        // Sides: SEGMENTS * 3 vertices
        //   - Each segment is a triangle: Apex + BaseLeft + BaseRight
        //   - We use separate vertices (not shared) for hard edges
        //
        // Hard Edge Rationale:
        // Sharing vertices between bottom cap and sides would create smooth shading.
        // Separate vertices ensure distinct edges are visible in projections.
        int bottomCapCount = SEGMENTS + 1;
        int sideCount = SEGMENTS * 3; 
        Vector3[] vertices = new Vector3[bottomCapCount + sideCount];
        int v = 0; // Vertex index counter

        // === BOTTOM CAP VERTICES (Facing Down, -Y) ===
        // Center vertex at base level
        int bottomCenterIndex = v;
        vertices[v++] = new Vector3(0, -halfHeight, 0); // Center point

        // Rim vertices in counterclockwise order (viewed from top)
        // Parametric circle: x = r*cos(θ), z = r*sin(θ)
        for (int i = 0; i < SEGMENTS; i++)
        {
            // Angle for this segment (0 to 2π)
            float angle = (float)i / SEGMENTS * Mathf.PI * 2f;
            
            // Place vertex on circle at base level
            vertices[v++] = new Vector3(
                Mathf.Cos(angle) * radius,  // X coordinate
                -halfHeight,                 // Y coordinate (base level)
                Mathf.Sin(angle) * radius   // Z coordinate
            );
        }

        // === SIDE VERTICES (Facing Outward) ===
        int sideStartIndex = v;
        
        // Apex position (shared by all side triangles)
        Vector3 tip = new Vector3(0, halfHeight, 0);

        // Generate vertices for each side triangle
        // Each triangle uses 3 separate vertices (apex + 2 base vertices)
        // This creates hard edges for clear projection visibility
        for (int i = 0; i < SEGMENTS; i++)
        {
            // Current segment angle
            float angle1 = (float)i / SEGMENTS * Mathf.PI * 2f;
            
            // Next segment angle (wraps around at last segment)
            float angle2 = (float)((i + 1) % SEGMENTS) / SEGMENTS * Mathf.PI * 2f;

            // Triangle vertices:
            vertices[v++] = tip; // Apex (top tip)
            
            // Base left vertex (current angle)
            vertices[v++] = new Vector3(
                Mathf.Cos(angle1) * radius, 
                -halfHeight, 
                Mathf.Sin(angle1) * radius
            );
            
            // Base right vertex (next angle)
            vertices[v++] = new Vector3(
                Mathf.Cos(angle2) * radius, 
                -halfHeight, 
                Mathf.Sin(angle2) * radius
            );
        }

        // === TRIANGLE INDICES ===
        // Bottom cap: SEGMENTS triangles (3 indices each)
        // Sides: SEGMENTS triangles (3 indices each)
        int[] triangles = new int[SEGMENTS * 3 + SEGMENTS * 3];
        int t = 0; // Triangle index counter

        // === BOTTOM CAP TRIANGLES (Clockwise from bottom) ===
        // Each triangle: Center → Current → Next
        // Clockwise winding when viewed from below (correct for down-facing normal)
        for (int i = 0; i < SEGMENTS; i++)
        {
            int current = bottomCenterIndex + 1 + i;
            int next = bottomCenterIndex + 1 + ((i + 1) % SEGMENTS);
            
            triangles[t++] = bottomCenterIndex; // Center
            triangles[t++] = current;           // Current rim vertex
            triangles[t++] = next;              // Next rim vertex (CCW order)
        }

        // === SIDE TRIANGLES (Clockwise from outside) ===
        // Each triangle: Apex → Right → Left
        // Clockwise winding when viewed from outside (correct for outward-facing normal)
        for (int i = 0; i < SEGMENTS; i++)
        {
            int baseIndex = sideStartIndex + (i * 3);
            
            triangles[t++] = baseIndex;     // Apex (tip)
            triangles[t++] = baseIndex + 2; // Base right vertex
            triangles[t++] = baseIndex + 1; // Base left vertex
        }

        // === CREATE MESH ===
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        
        // Recalculate normals for proper lighting (if enabled)
        // Each face will have its own normal due to separate vertices
        mesh.RecalculateNormals();
        
        // Assign mesh to MeshFilter component
        meshFilter.mesh = mesh;

        // === APPLY TRANSFORMATIONS ===
        // Apply rotation and position from ShapeData
        // Handles angleHP, angleVP, rotationY, distHP, distVP
        PostProcessShape(coneObj, data);
        
        return coneObj;
    }
}