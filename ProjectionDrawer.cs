using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Handles the rendering of 2D orthographic projections (HP and VP) from 3D mesh data.
/// Creates LineRenderer-based projections with support for edge classification (visible/hidden/silhouette),
/// dotted connector lines, and floating dimension labels with billboard behavior.
/// 
/// Key Features:
/// - Position-based edge rendering (works with MeshAnalyzer's welded edges)
/// - Edge type classification with distinct visual styles
/// - Automatic connector line generation from 3D vertices to 2D projections
/// - Dimension labels with camera-facing billboard behavior
/// - Configurable line widths and colors
/// </summary>
public static class ProjectionDrawer
{
    /// <summary>
    /// Classification of edges based on visibility and geometric properties.
    /// Used to apply different visual styles (line width, color, visibility).
    /// </summary>
    public enum EdgeType
    {
        /// <summary>Boundary edge (shared by only 1 face) - always visible, forms shape outline</summary>
        Silhouette,
        
        /// <summary>Sharp internal edge (2 faces with angle > threshold) - visible when not occluded</summary>
        Visible,
        
        /// <summary>Smooth internal edge (2 faces nearly coplanar) - typically hidden in technical drawings</summary>
        Hidden
    }

    /// <summary>
    /// Container for parent GameObjects that organize projection geometry.
    /// Enables efficient cleanup and selective visibility toggling.
    /// </summary>
    public struct ProjectionParents
    {
        /// <summary>Parent GameObject containing all HP (top view) projection lines</summary>
        public GameObject hpParent;
        
        /// <summary>Parent GameObject containing all VP (front view) projection lines</summary>
        public GameObject vpParent;
        
        /// <summary>Parent GameObject containing all dotted connector lines from 3D to 2D</summary>
        public GameObject connectorParent;
    }

    // ========================================
    // VISUAL STYLE CONSTANTS
    // ========================================
    
    /// <summary>Line width for visible edges (bold, primary geometry)</summary>
    private const float VISIBLE_LINE_WIDTH = 0.03f;
    
    /// <summary>Line width for hidden edges (thinner, secondary geometry)</summary>
    private const float HIDDEN_LINE_WIDTH = 0.02f;
    
    /// <summary>Line width for connector lines (thinnest, auxiliary geometry)</summary>
    private const float CONNECTOR_WIDTH = 0.01f;
    
    /// <summary>Minimum distance threshold for line creation (prevents zero-length lines)</summary>
    private const float EPSILON = 0.0001f;
    
    /// <summary>Minimum line length to display dimension labels (prevents label clutter on tiny edges)</summary>
    private const float MIN_LABEL_LENGTH = 0.2f;
    
    /// <summary>Dot density for dotted connector lines (dots per unit length)</summary>
    private const float DOTS_PER_UNIT = 20f;
    
    /// <summary>Distance tolerance for vertex deduplication (prevents duplicate connectors)</summary>
    private const float VERTEX_MERGE_TOLERANCE = 0.001f;
    
    /// <summary>Dot product threshold for sharp edge detection (cos(8°) ≈ 0.99)</summary>
    private const float SHARP_EDGE_THRESHOLD = 0.99f;

    /// <summary>
    /// Main entry point for projection rendering system.
    /// Processes edge map from MeshAnalyzer and generates HP/VP projections with connectors.
    /// 
    /// Algorithm:
    /// 1. Create parent GameObjects for organization
    /// 2. For each edge in the edge map:
    ///    a. Extract world-space positions (position-based edges from MeshAnalyzer)
    ///    b. Classify edge type (silhouette/visible/hidden)
    ///    c. Project to HP (flatten Y to 0)
    ///    d. Project to VP (flatten X to 0)
    ///    e. Draw projection lines with appropriate styling
    ///    f. Collect unique vertices for connector generation
    /// 3. Draw connector lines from 3D vertices to 2D projections
    /// 4. Add dimension labels to connectors
    /// 
    /// Position-Based Edge System:
    /// Uses edge.p1 and edge.p2 (world-space positions) instead of vertex indices.
    /// This allows MeshAnalyzer's welded edges (e.g., cylinder rim) to render correctly
    /// without duplicate lines.
    /// </summary>
    /// <param name="edgeMap">Position-based edge map from MeshAnalyzer with face associations</param>
    /// <param name="solidLineMaterial">Material for solid projection lines (requires basic shader)</param>
    /// <param name="dottedLineMaterial">Material for dotted connectors (requires tiling support)</param>
    /// <param name="mesh">Original mesh (kept for compatibility, not currently used)</param>
    /// <param name="shapeTransform">Transform of the 3D shape (kept for compatibility, not currently used)</param>
    /// <returns>ProjectionParents struct containing references to HP, VP, and connector parent GameObjects</returns>
    public static ProjectionParents DrawProjections(
        Dictionary<MeshAnalyzer.Edge, List<MeshAnalyzer.Face>> edgeMap,
        Material solidLineMaterial,
        Material dottedLineMaterial,
        Mesh mesh,
        Transform shapeTransform)
    {
        // === VALIDATION ===
        if (edgeMap == null || mesh == null || shapeTransform == null)
        {
            return new ProjectionParents();
        }

        // === CREATE PARENT GAMEOBJECTS ===
        // Organized hierarchy enables efficient cleanup and selective visibility
        GameObject hpProjectionParent = new GameObject("HP Projection");
        GameObject vpProjectionParent = new GameObject("VP Projection");
        GameObject connectorLinesParent = new GameObject("Connector Lines");

        // === VERTEX DEDUPLICATION ===
        // HashSet prevents duplicate connector lines from the same 3D vertex
        // Important for corners where multiple edges meet
        HashSet<Vector3> processedVertices = new HashSet<Vector3>();

        // === PROCESS EACH EDGE ===
        foreach (var kvp in edgeMap)
        {
            MeshAnalyzer.Edge edge = kvp.Key;
            List<MeshAnalyzer.Face> faces = kvp.Value;

            // EXTRACT WORLD POSITIONS (Position-Based System)
            // These are actual 3D coordinates, not vertex indices
            // MeshAnalyzer already welded duplicate edges at the same position
            Vector3 worldP1 = edge.p1;
            Vector3 worldP2 = edge.p2;

            // CLASSIFY EDGE (Determines visual style)
            EdgeType edgeType = DetermineEdgeType(faces);

            // PROJECT TO HP (Horizontal Plane at Y = 0)
            // Orthographic projection: drop the Y coordinate
            Vector3 hpP1 = new Vector3(worldP1.x, 0, worldP1.z);
            Vector3 hpP2 = new Vector3(worldP2.x, 0, worldP2.z);

            // PROJECT TO VP (Vertical Plane at X = 0)
            // Orthographic projection: drop the X coordinate
            Vector3 vpP1 = new Vector3(0, worldP1.y, worldP1.z);
            Vector3 vpP2 = new Vector3(0, worldP2.y, worldP2.z);

            // DRAW PROJECTION LINES
            // Applies edge-type-specific styling (width, color, visibility)
            DrawEdgesByType(
                edgeType, 
                worldP1, worldP2,   // Original 3D positions (for reference)
                hpP1, hpP2,         // HP projected positions
                vpP1, vpP2,         // VP projected positions
                solidLineMaterial, 
                dottedLineMaterial,
                hpProjectionParent, 
                vpProjectionParent
            );

            // COLLECT VERTICES FOR CONNECTORS
            // Use tolerance-based deduplication to handle floating-point drift
            AddVertexIfNew(processedVertices, worldP1);
            AddVertexIfNew(processedVertices, worldP2);
        }

        // === DRAW CONNECTOR LINES ===
        // Dotted lines from 3D vertices to their HP/VP projections
        // Includes dimension labels showing connector length
        DrawConnectorLines(processedVertices, dottedLineMaterial, connectorLinesParent);

        // Return parent GameObjects for external management (cleanup, visibility toggling)
        return new ProjectionParents
        {
            hpParent = hpProjectionParent,
            vpParent = vpProjectionParent,
            connectorParent = connectorLinesParent
        };
    }

    /// <summary>
    /// Adds a vertex to the processed set only if it's not already present within tolerance.
    /// Prevents duplicate connector lines from vertices at the same physical location.
    /// 
    /// Example: A cube corner has 3 edges meeting, but we only want 1 connector from that point.
    /// Without deduplication: 6 connector lines (3 edges × 2 endpoints, but 3 share position)
    /// With deduplication: 2 connector lines (to HP and VP)
    /// </summary>
    /// <param name="set">HashSet of unique vertex positions</param>
    /// <param name="v">Vertex position to add</param>
    private static void AddVertexIfNew(HashSet<Vector3> set, Vector3 v)
    {
        // Check if a vertex within tolerance already exists
        foreach (Vector3 existing in set)
        {
            if (Vector3.Distance(existing, v) < VERTEX_MERGE_TOLERANCE)
            {
                return; // Too close to existing vertex - skip
            }
        }
        
        // No nearby vertex found - add this one
        set.Add(v);
    }

    /// <summary>
    /// Determines the visual classification of an edge based on its adjacent faces.
    /// 
    /// Classification Rules:
    /// - 1 face: Silhouette/Boundary edge (outline of the shape, always visible)
    /// - 2 faces with sharp angle (dot < 0.99): Visible edge (crease, fold, sharp corner)
    /// - 2 faces nearly coplanar (dot ≥ 0.99): Hidden edge (smooth surface, typically not drawn)
    /// - >2 faces: Visible (non-manifold geometry, treated as sharp edge)
    /// 
    /// Dot Product Explanation:
    /// - dot = 1.0: Normals parallel (coplanar faces)
    /// - dot = 0.0: Normals perpendicular (90° fold)
    /// - dot = -1.0: Normals opposite (180° fold, sharp crease)
    /// - Threshold 0.99 ≈ 8° angle between faces
    /// </summary>
    /// <param name="faces">List of faces that share this edge</param>
    /// <returns>EdgeType classification (Silhouette/Visible/Hidden)</returns>
    private static EdgeType DetermineEdgeType(List<MeshAnalyzer.Face> faces)
    {
        if (faces.Count == 1)
        {
            // Boundary edge - part of the shape's outline
            return EdgeType.Silhouette;
        }
        else if (faces.Count == 2)
        {
            // Internal edge - check if it's a sharp fold or smooth surface
            float dotProduct = Vector3.Dot(faces[0].worldNormal, faces[1].worldNormal);
            
            // Sharp edge: faces are at a significant angle
            if (dotProduct < SHARP_EDGE_THRESHOLD)
            {
                return EdgeType.Visible;
            }
            else
            {
                // Smooth edge: faces are nearly coplanar
                return EdgeType.Hidden;
            }
        }
        
        // Non-manifold geometry (>2 faces) - treat as visible
        return EdgeType.Visible;
    }

    /// <summary>
    /// Draws projection lines for both HP and VP planes with edge-type-specific styling.
    /// 
    /// Visual Style by Edge Type:
    /// - Silhouette: Black, bold (VISIBLE_LINE_WIDTH = 0.03)
    /// - Visible: Black, bold (VISIBLE_LINE_WIDTH = 0.03)
    /// - Hidden: NOT DRAWN (can be changed to grey/dotted if desired)
    /// 
    /// Educational Rationale:
    /// In engineering drawings, hidden lines are often omitted in simple projections
    /// to reduce visual clutter. For educational clarity, we currently hide them.
    /// To show hidden lines as dashed, remove the early return and set appropriate styling.
    /// </summary>
    /// <param name="edgeType">Edge classification (determines styling)</param>
    /// <param name="worldP1">3D position of edge start (not currently used, kept for extensibility)</param>
    /// <param name="worldP2">3D position of edge end (not currently used, kept for extensibility)</param>
    /// <param name="hpP1">HP projection of edge start (Y=0 plane)</param>
    /// <param name="hpP2">HP projection of edge end (Y=0 plane)</param>
    /// <param name="vpP1">VP projection of edge start (X=0 plane)</param>
    /// <param name="vpP2">VP projection of edge end (X=0 plane)</param>
    /// <param name="solidMat">Material for solid lines</param>
    /// <param name="dottedMat">Material for dotted lines (not currently used for projections)</param>
    /// <param name="hpParent">Parent GameObject for HP lines</param>
    /// <param name="vpParent">Parent GameObject for VP lines</param>
    private static void DrawEdgesByType(
        EdgeType edgeType,
        Vector3 worldP1, Vector3 worldP2,
        Vector3 hpP1, Vector3 hpP2,
        Vector3 vpP1, Vector3 vpP2,
        Material solidMat,
        Material dottedMat,
        GameObject hpParent,
        GameObject vpParent)
    {
        // DETERMINE VISUAL STYLE
        float width = (edgeType == EdgeType.Hidden) ? HIDDEN_LINE_WIDTH : VISIBLE_LINE_WIDTH;
        Color color = (edgeType == EdgeType.Hidden) ? Color.grey : Color.black;

        // OPTIONAL: Skip hidden lines for simple projection mode
        // Comment out this line to draw hidden edges as grey/dashed lines
        if (edgeType == EdgeType.Hidden) return;

        // DRAW HP PROJECTION LINE
        CreateLine(
            hpParent, 
            solidMat, 
            hpP1, hpP2, 
            color, 
            width, 
            "HP_Edge", 
            isDotted: false,    // Solid line for projections
            createLabel: false  // No labels on projection lines
        );

        // DRAW VP PROJECTION LINE
        CreateLine(
            vpParent, 
            solidMat, 
            vpP1, vpP2, 
            color, 
            width, 
            "VP_Edge", 
            isDotted: false,    // Solid line for projections
            createLabel: false  // No labels on projection lines
        );
    }

    /// <summary>
    /// Draws dotted connector lines from 3D vertices to their 2D projections on HP and VP.
    /// Also creates dimension labels showing the connector length.
    /// 
    /// Purpose:
    /// - Visual aid: Shows the relationship between 3D shape and 2D projections
    /// - Educational: Students can trace how 3D vertices map to 2D planes
    /// - Dimension labels: Provide numerical feedback on distances
    /// 
    /// For each unique 3D vertex:
    /// - Draw dotted line to HP projection (vertical drop)
    /// - Draw dotted line to VP projection (horizontal line to wall)
    /// - Add dimension labels showing connector lengths
    /// </summary>
    /// <param name="vertices">Set of unique 3D vertex positions (deduplicated)</param>
    /// <param name="dottedMat">Material with texture tiling support for dotted effect</param>
    /// <param name="parent">Parent GameObject to organize connector lines</param>
    private static void DrawConnectorLines(HashSet<Vector3> vertices, Material dottedMat, GameObject parent)
    {
        // Semi-transparent grey for connector lines (less prominent than projections)
        Color connectorColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);

        int i = 0;
        foreach (Vector3 vertex in vertices)
        {
            // Calculate projection points
            Vector3 hpProj = new Vector3(vertex.x, 0, vertex.z);      // Drop to floor
            Vector3 vpProj = new Vector3(0, vertex.y, vertex.z);      // Project to wall

            // DRAW HP CONNECTOR (3D vertex to floor)
            CreateLine(
                parent, 
                dottedMat, 
                vertex, hpProj, 
                connectorColor, 
                CONNECTOR_WIDTH, 
                $"HP_Conn_{i}",
                isDotted: true,     // Texture-based dotted line
                createLabel: true   // Show distance label
            );

            // DRAW VP CONNECTOR (3D vertex to wall)
            CreateLine(
                parent, 
                dottedMat, 
                vertex, vpProj, 
                connectorColor, 
                CONNECTOR_WIDTH, 
                $"VP_Conn_{i}",
                isDotted: true,     // Texture-based dotted line
                createLabel: true   // Show distance label
            );
            
            i++;
        }
    }

    /// <summary>
    /// Core line rendering method. Creates a LineRenderer GameObject with optional dotted texture and dimension label.
    /// 
    /// Features:
    /// - LineRenderer-based rendering (GPU-efficient, supports materials)
    /// - Dotted line effect via texture tiling (requires material with repeating texture)
    /// - Dimension labels with Billboard component (always face camera)
    /// - Zero-length line filtering (prevents rendering artifacts)
    /// 
    /// Dotted Line Technique:
    /// - Uses material.mainTextureScale to repeat a dotted texture along the line
    /// - Scale = (length × DOTS_PER_UNIT, 1) creates uniform dot spacing
    /// - Requires material with a texture containing dot pattern (e.g., black dot on transparent)
    /// 
    /// Label Placement:
    /// - Positioned at line midpoint (Lerp factor 0.5)
    /// - Only created for lines longer than MIN_LABEL_LENGTH (0.2 units)
    /// - Billboard component makes text always face camera
    /// </summary>
    /// <param name="parent">Parent GameObject for organization</param>
    /// <param name="material">Material for LineRenderer (must support transparency for dotted lines)</param>
    /// <param name="start">Start position in world space</param>
    /// <param name="end">End position in world space</param>
    /// <param name="color">Line color (applied to startColor and endColor)</param>
    /// <param name="width">Line width (uniform, applied to startWidth and endWidth)</param>
    /// <param name="name">GameObject name for hierarchy organization</param>
    /// <param name="isDotted">If true, applies texture tiling for dotted effect</param>
    /// <param name="createLabel">If true, creates a TextMeshPro dimension label at midpoint</param>
    private static void CreateLine(
        GameObject parent, 
        Material material, 
        Vector3 start, 
        Vector3 end,
        Color color, 
        float width, 
        string name, 
        bool isDotted, 
        bool createLabel)
    {
        // FILTER ZERO-LENGTH LINES
        // Prevents rendering artifacts and unnecessary GameObjects
        if (Vector3.Distance(start, end) < EPSILON) return;

        // CREATE LINERENDERER GAMEOBJECT
        GameObject lineObj = new GameObject(name);
        lineObj.transform.SetParent(parent.transform);

        // CONFIGURE LINERENDERER COMPONENT
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = material;
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.useWorldSpace = true;
        
        // Disable shadows for technical drawing aesthetic
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        // APPLY DOTTED TEXTURE (if requested)
        if (isDotted)
        {
            float length = Vector3.Distance(start, end);
            
            // Texture tiling creates dotted effect
            // Formula: scale = length × dotsPerUnit
            // Example: 2-unit line × 20 dots/unit = 40 texture repetitions
            lr.material.mainTextureScale = new Vector2(length * DOTS_PER_UNIT, 1f);
        }

        // CREATE DIMENSION LABEL (if requested and line is long enough)
        if (createLabel)
        {
            float length = Vector3.Distance(start, end);
            
            // Only label significant lines (prevents clutter on tiny edges)
            if (length > MIN_LABEL_LENGTH)
            {
                Vector3 midpoint = Vector3.Lerp(start, end, 0.5f);
                CreateDimensionLabel(midpoint, length, parent.transform);
            }
        }
    }

    /// <summary>
    /// Creates a floating 3D text label displaying the length of a line segment.
    /// Uses TextMeshPro for high-quality text rendering and Billboard component for camera-facing behavior.
    /// 
    /// Billboard Behavior:
    /// - Label always faces the camera (readable from any viewing angle)
    /// - Implemented via Billboard.cs component (rotates to match camera orientation)
    /// - Critical for educational use: labels remain legible during orbit camera movement
    /// 
    /// Text Formatting:
    /// - Precision: F2 (2 decimal places, e.g., "2.35")
    /// - Color: Black for contrast against background
    /// - Alignment: Center (balanced around anchor point)
    /// - Font Size: 1.0 (world-space units, adjust for different camera distances)
    /// </summary>
    /// <param name="position">World-space position for label (typically line midpoint)</param>
    /// <param name="length">Numerical value to display (line length in world units)</param>
    /// <param name="parent">Parent transform for hierarchy organization</param>
    private static void CreateDimensionLabel(Vector3 position, float length, Transform parent)
    {
        // CREATE LABEL GAMEOBJECT
        GameObject labelObj = new GameObject("Dim_Label");
        labelObj.transform.SetParent(parent);
        labelObj.transform.position = position;

        // CONFIGURE TEXTMESHPRO
        TextMeshPro tmp = labelObj.AddComponent<TextMeshPro>();
        tmp.text = length.ToString("F2");                    // Format: 2 decimal places
        tmp.fontSize = 1f;                                   // World-space font size
        tmp.color = Color.black;                             // High contrast
        tmp.alignment = TextAlignmentOptions.Center;         // Center-aligned

        // ADD BILLBOARD COMPONENT
        // Automatically rotates label to face camera every frame
        // Requires Billboard.cs to be present in the project
        labelObj.AddComponent<Billboard>();
    }
}