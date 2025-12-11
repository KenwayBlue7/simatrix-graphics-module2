using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Advanced orbit camera controller with support for both perspective and orthographic projection modes.
/// Provides preset engineering views (Top/HP, Front/VP, Isometric) essential for technical drawings.
/// 
/// Features:
/// - Mouse drag orbit rotation with axis constraints
/// - Scroll wheel zoom (perspective distance or orthographic size)
/// - Preset views: Top (HP), Front (VP), Isometric
/// - Camera state reset to initial configuration
/// - UI-aware input (ignores input when over UI elements)
/// - Automatic angle normalization (-180° to +180°)
/// 
/// Projection Modes:
/// 1. PERSPECTIVE (Default):
///    - Natural 3D depth perception
///    - Field of view (FOV) defines frustum
///    - Zoom adjusts camera distance from target
///    - Used for free exploration and realistic visualization
/// 
/// 2. ORTHOGRAPHIC (Engineering Views):
///    - Parallel projection (no depth distortion)
///    - Orthographic size defines visible area
///    - Zoom adjusts visible area (not distance)
///    - Used for Top (HP), Front (VP), and Isometric views
///    - Essential for accurate engineering measurements
/// 
/// Engineering View Conventions:
/// - HP (Horizontal Plane): Top view, looking down -Y axis
/// - VP (Vertical Plane): Front view, looking from +X toward origin
/// - Isometric: 35.264° pitch, -45° yaw (classic isometric angle)
/// 
/// Input Handling:
/// - Left Mouse Button + Drag: Orbit rotation
/// - Scroll Wheel: Zoom in/out
/// - UI Override: Input ignored when pointer is over UI elements
/// 
/// Rotation System:
/// - rotationX: Horizontal orbit (yaw, -180° to 0°)
/// - rotationY: Vertical orbit (pitch, 0° to 90°)
/// - Constraints prevent camera from flipping upside-down or going underground
/// - Angles normalized to -180° to +180° range for consistent behavior
/// </summary>
public class OrbitCameraController : MonoBehaviour
{
    [Header("Orbit Settings")]
    [Tooltip("The Transform to orbit around (typically the generated shape)")]
    public Transform target;
    
    [Tooltip("Horizontal/vertical rotation speed (degrees per second per mouse unit)")]
    public float rotationSpeed = 100f;
    
    [Tooltip("Zoom speed (units per scroll tick for perspective, size units for orthographic)")]
    public float zoomSpeed = 5f;
    
    [Header("Zoom Limits - Perspective Mode")]
    [Tooltip("Minimum distance from target (prevents clipping inside object)")]
    public float minDistance = 2f;
    
    [Tooltip("Maximum distance from target (prevents zooming too far)")]
    public float maxDistance = 20f;
    
    [Header("View Settings")]
    [Tooltip("Orthographic size for preset views (controls visible area height/2)")]
    public float orthographicSize = 5f;
    
    [Tooltip("Distance from target for orthographic preset views")]
    public float viewDistance = 10f;
    
    [Header("Zoom Limits - Orthographic Mode")]
    [Tooltip("Minimum orthographic size (maximum zoom in)")]
    public float minOrthographicSize = 1f;
    
    [Tooltip("Maximum orthographic size (maximum zoom out)")]
    public float maxOrthographicSize = 20f;
    
    // === INTERNAL STATE ===
    
    /// <summary>
    /// Current distance from camera to target (used in perspective mode).
    /// Adjusted by scroll wheel zoom in perspective projection.
    /// </summary>
    private float currentDistance;
    
    /// <summary>
    /// Horizontal orbit rotation (yaw) in degrees.
    /// Range: -180° to 0° (constrained to prevent disorientation)
    /// - 0°: Camera on +X axis (right side)
    /// - -90°: Camera on +Z axis (front)
    /// - -180°: Camera on -X axis (left side)
    /// </summary>
    private float rotationX = 0f;
    
    /// <summary>
    /// Vertical orbit rotation (pitch) in degrees.
    /// Range: 0° to 90° (constrained to prevent flipping)
    /// - 0°: Camera at target's Y level (horizon view)
    /// - 45°: Camera above target at 45° angle
    /// - 90°: Camera directly above target (top view)
    /// </summary>
    private float rotationY = 0f;
    
    // === STATE SAVING (For Reset Functionality) ===
    
    /// <summary>Initial camera world position (saved in Start(), restored in ResetCamera())</summary>
    private Vector3 initialPosition;
    
    /// <summary>Initial camera world rotation (saved in Start(), restored in ResetCamera())</summary>
    private Quaternion initialRotation;
    
    /// <summary>Initial projection mode (true = orthographic, false = perspective)</summary>
    private bool initialOrthographic;
    
    /// <summary>Initial orthographic size (for orthographic mode)</summary>
    private float initialOrthographicSize;
    
    /// <summary>Initial field of view (for perspective mode)</summary>
    private float initialFieldOfView;
    
    /// <summary>Cached reference to Camera component</summary>
    private Camera mainCamera;
    
    /// <summary>
    /// Initializes camera controller by saving initial state and calculating rotation angles.
    /// Called once when the script starts.
    /// 
    /// Initialization Steps:
    /// 1. Get Camera component reference
    /// 2. Create default target if none assigned (empty GameObject at origin)
    /// 3. Save initial camera state for ResetCamera() functionality
    /// 4. Calculate rotationX/Y from current transform.eulerAngles
    /// 5. Normalize angles to -180° to +180° range
    /// 6. Calculate initial distance from target
    /// 7. Clamp distance to valid range
    /// 
    /// Angle Normalization:
    /// Unity's eulerAngles returns values in 0° to 360° range.
    /// Example: 315° → -45° (more intuitive for rotation deltas)
    /// This prevents unexpected jumps when interpolating or accumulating angles.
    /// </summary>
    void Start()
    {
        // === GET CAMERA COMPONENT ===
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            // Critical error: This script requires a Camera component to function
            Debug.LogError("OrbitCameraController requires a Camera component!");
            return;
        }
        
        // === CREATE DEFAULT TARGET IF MISSING ===
        if (target == null)
        {
            // Create an empty GameObject at world origin to serve as orbit target
            // This prevents null reference errors and provides a default orbit point
            GameObject targetObject = new GameObject("Camera Target");
            targetObject.transform.position = Vector3.zero;
            target = targetObject.transform;
        }
        
        // === SAVE INITIAL STATE ===
        // Store initial configuration for ResetCamera() functionality
        // Allows user to return to starting view after exploration
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialOrthographic = mainCamera.orthographic;
        initialOrthographicSize = mainCamera.orthographicSize;
        initialFieldOfView = mainCamera.fieldOfView;
        
        // === INITIALIZE ROTATION FROM CURRENT ORIENTATION ===
        // Extract Euler angles from current rotation
        // This ensures smooth continuation if camera was pre-positioned in the scene
        Vector3 angles = transform.eulerAngles;
        rotationY = angles.x; // X component of Euler angles = pitch (vertical rotation)
        rotationX = angles.y; // Y component of Euler angles = yaw (horizontal rotation)
        
        // === NORMALIZE ANGLES TO -180° TO +180° RANGE ===
        // Unity's eulerAngles returns 0° to 360°, but we use -180° to +180° for consistency
        // 
        // Why Normalize?
        // - Example: Camera at 315° (northwest) → Convert to -45° (more intuitive)
        // - Prevents jump from 359° to 0° when rotating clockwise
        // - Makes angle deltas consistent (always use smallest rotation)
        // 
        // Conversion Logic:
        // If angle > 180°, subtract 360° to get negative equivalent
        // Example: 224° → 224° - 360° = -136°
        if (rotationX > 180f)
        {
            rotationX -= 360f;
        }

        if (rotationY > 180f)
        {
            rotationY -= 360f;
        }
        
        // === CALCULATE INITIAL DISTANCE ===
        currentDistance = Vector3.Distance(transform.position, target.position);
        
        // Clamp to valid range (prevents issues if camera starts too close or too far)
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
    }

    /// <summary>
    /// Updates camera position and rotation every frame based on user input.
    /// 
    /// Frame Execution Flow:
    /// 1. Validate target exists
    /// 2. Handle mouse drag input (orbit rotation)
    /// 3. Handle scroll wheel input (zoom)
    /// 4. Update camera position and rotation to match new state
    /// 
    /// Order Matters:
    /// Input handling before position update ensures changes are applied immediately.
    /// Using Update() (not LateUpdate()) allows other scripts to read camera state in their LateUpdate().
    /// </summary>
    void Update()
    {
        // Early exit if no target assigned
        if (target == null) return;
        
        // Process user input
        HandleMouseInput();    // Orbit rotation (left mouse button drag)
        HandleScrollInput();   // Zoom (scroll wheel)
        
        // Apply calculated rotation and position
        UpdateCameraPosition();
    }
    
    /// <summary>
    /// Handles left mouse button drag for orbit rotation around target.
    /// 
    /// Input:
    /// - Left Mouse Button (Button 0) must be held down
    /// - Mouse movement delta (Input.GetAxis("Mouse X/Y"))
    /// 
    /// Behavior:
    /// - Horizontal mouse movement → rotationX (yaw, side-to-side orbit)
    /// - Vertical mouse movement → rotationY (pitch, up-down orbit)
    /// - Both axes clamped to prevent disorienting camera flips
    /// 
    /// UI Override:
    /// If mouse pointer is over a UI element (button, slider, etc.),
    /// camera input is ignored to prevent accidental camera movement while adjusting UI.
    /// Uses Unity's EventSystem to detect UI overlap.
    /// 
    /// Rotation Constraints:
    /// ORTHOGRAPHIC & PERSPECTIVE:
    /// - rotationY: 0° to 90° (prevents flipping upside-down or going underground)
    /// - rotationX: -180° to 0° (limits horizontal orbit to reasonable range)
    /// 
    /// Why These Limits?
    /// - Y: 0° = horizon level, 90° = top view (consistent with engineering views)
    /// - X: -180° to 0° provides full 180° horizontal range without redundancy
    /// - Prevents disorientation from camera flipping or wrapping around
    /// </summary>
    void HandleMouseInput()
    {
        // === UI OVERRIDE CHECK ===
        // Check if mouse pointer is over a UI element
        // If true, ignore camera input to prevent UI interaction conflicts
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        
        // === MOUSE BUTTON CHECK ===
        // Only process rotation if left mouse button (button 0) is held down
        if (Input.GetMouseButton(0))
        {
            // === GET MOUSE MOVEMENT DELTA ===
            // Input.GetAxis returns normalized movement (-1 to 1) scaled by sensitivity settings
            // Multiply by Time.deltaTime for frame-rate independent rotation
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            
            // === ACCUMULATE ROTATION ===
            // Horizontal movement (mouseX) → Yaw rotation (rotationX)
            // Positive mouseX (drag right) → Increase rotationX → Orbit counterclockwise (viewed from top)
            rotationX += mouseX * rotationSpeed * Time.deltaTime;
            
            // Vertical movement (mouseY) → Pitch rotation (rotationY)
            // Subtract because positive mouseY (drag up) should decrease pitch (standard FPS camera convention)
            // Positive mouseY (drag up) → Decrease rotationY → Look down (toward horizon)
            rotationY -= mouseY * rotationSpeed * Time.deltaTime;
            
            // === APPLY ROTATION CONSTRAINTS ===
            // Both orthographic and perspective modes use the same constraints
            // These limits prevent camera from:
            // - Flipping upside-down (rotationY > 90°)
            // - Going underground (rotationY < 0°)
            // - Excessive horizontal rotation (rotationX outside -180° to 0°)
            if (mainCamera.orthographic)
            {
                // Orthographic mode constraints
                // Y: 0° (Front View) to 90° (Top View)
                // X: -180° (Left View) to 0° (Front/Top View alignment)
                rotationY = Mathf.Clamp(rotationY, 0f, 90f);
                rotationX = Mathf.Clamp(rotationX, -180f, 0f);
            }
            else
            {
                // Perspective mode constraints (same as orthographic)
                // Y: 0° (horizon) to 90° (directly above)
                // X: -180° to 0° (full 180° horizontal range)
                rotationY = Mathf.Clamp(rotationY, 0f, 90f);
                rotationX = Mathf.Clamp(rotationX, -180f, 0f);
            }
        }
    }
    
    /// <summary>
    /// Handles scroll wheel input for zoom in both projection modes.
    /// 
    /// Zoom Behavior:
    /// PERSPECTIVE MODE:
    ///   - Adjusts currentDistance (camera moves closer/farther from target)
    ///   - Scroll up (positive) → Decrease distance → Zoom in
    ///   - Scroll down (negative) → Increase distance → Zoom out
    ///   - Clamped to minDistance and maxDistance
    /// 
    /// ORTHOGRAPHIC MODE:
    ///   - Adjusts orthographicSize (visible area changes, camera stays at fixed distance)
    ///   - Scroll up (positive) → Decrease size → Zoom in (see less area)
    ///   - Scroll down (negative) → Increase size → Zoom out (see more area)
    ///   - Clamped to minOrthographicSize and maxOrthographicSize
    /// 
    /// Why Different Zoom Mechanisms?
    /// - Perspective: Moving camera closer/farther changes FOV perspective (realistic)
    /// - Orthographic: Changing visible area maintains parallel projection (no perspective distortion)
    /// 
    /// UI Override:
    /// If mouse pointer is over a UI element, zoom is ignored to prevent
    /// accidental camera zoom while adjusting sliders or scrolling lists.
    /// </summary>
    void HandleScrollInput()
    {
        // === UI OVERRIDE CHECK ===
        // Ignore zoom input if pointer is over UI element
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        
        // === GET SCROLL WHEEL INPUT ===
        // Input.GetAxis("Mouse ScrollWheel") returns:
        // - Positive value: Scroll up (zoom in)
        // - Negative value: Scroll down (zoom out)
        // - Zero: No scroll input this frame
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        if (scroll != 0f && mainCamera != null)
        {
            if (mainCamera.orthographic)
            {
                // === ORTHOGRAPHIC MODE: ADJUST VISIBLE AREA ===
                // orthographicSize = half of camera's visible height in world units
                // Smaller size = zoomed in (see less area)
                // Larger size = zoomed out (see more area)
                mainCamera.orthographicSize -= scroll * zoomSpeed;
                
                // Clamp to prevent excessively zoomed in/out views
                mainCamera.orthographicSize = Mathf.Clamp(
                    mainCamera.orthographicSize, 
                    minOrthographicSize, 
                    maxOrthographicSize
                );
            }
            else
            {
                // === PERSPECTIVE MODE: ADJUST CAMERA DISTANCE ===
                // Move camera closer (zoom in) or farther (zoom out) from target
                currentDistance -= scroll * zoomSpeed;
                
                // Clamp to prevent camera from:
                // - Going inside the target object (minDistance)
                // - Zooming infinitely far away (maxDistance)
                currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
            }
        }
    }
    
    /// <summary>
    /// Updates camera transform based on current rotation and distance values.
    /// Called every frame after input processing.
    /// 
    /// Position Calculation (Spherical to Cartesian):
    /// 1. Create rotation quaternion from rotationX (yaw) and rotationY (pitch)
    /// 2. Calculate direction vector: rotation * Vector3.back (0, 0, -1)
    /// 3. Scale direction by currentDistance
    /// 4. Add target.position to get world position
    /// 
    /// Why Vector3.back?
    /// Unity's camera looks down its -Z axis (local forward = -Z).
    /// Vector3.back = (0, 0, -1) points in the direction camera should be positioned.
    /// Rotating this vector and scaling it creates the orbit offset.
    /// 
    /// LookAt Behavior:
    /// transform.LookAt(target) automatically rotates the camera to face the target.
    /// This ensures the camera's -Z axis (viewing direction) points at the target.
    /// No manual rotation calculation needed.
    /// </summary>
    void UpdateCameraPosition()
    {
        // === CREATE ROTATION QUATERNION ===
        // Quaternion.Euler(pitch, yaw, roll)
        // - rotationY: Pitch (vertical tilt, 0° to 90°)
        // - rotationX: Yaw (horizontal spin, -180° to 0°)
        // - 0: Roll (no sideways tilt)
        Quaternion rotation = Quaternion.Euler(rotationY, rotationX, 0);
        
        // === CALCULATE CAMERA POSITION ===
        // Step 1: Get direction vector (rotate Vector3.back by rotation)
        //   Vector3.back = (0, 0, -1) in local space
        //   rotation * Vector3.back = rotated direction in world space
        Vector3 direction = rotation * Vector3.back;
        
        // Step 2: Scale direction by distance (orbit radius)
        //   direction * currentDistance = offset from target
        // Step 3: Add target position to get world position
        //   target.position + offset = final camera position
        Vector3 newPosition = target.position + direction * currentDistance;
        
        // === APPLY TRANSFORMATION ===
        // Set camera position (orbit offset from target)
        transform.position = newPosition;
        
        // Set camera rotation (look at target from current position)
        // LookAt automatically calculates the correct rotation to face the target
        transform.LookAt(target);
    }
    
    /// <summary>
    /// Resets the camera to its initial position, rotation, and projection settings.
    /// Restores the camera state that was saved in Start().
    /// 
    /// Restored Properties:
    /// - transform.position (world position)
    /// - transform.rotation (world rotation)
    /// - mainCamera.orthographic (projection mode)
    /// - mainCamera.orthographicSize (orthographic visible area)
    /// - mainCamera.fieldOfView (perspective FOV)
    /// - rotationX/Y (internal rotation state)
    /// - currentDistance (orbit radius)
    /// 
    /// Angle Normalization:
    /// After restoring rotation, angles are normalized to -180° to +180° range.
    /// This prevents issues if initial rotation used angles > 180° (e.g., 315°).
    /// 
    /// Use Cases:
    /// - Return to starting view after exploring with orbit/zoom
    /// - Reset after switching between orthographic preset views
    /// - Restore default camera settings
    /// </summary>
    /// <returns>False (indicates perspective view is active after reset)</returns>
    public bool ResetCamera()
    {
        if (mainCamera == null) return false;
        
        // === RESTORE TRANSFORM ===
        // Return to initial world position and rotation
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        
        // === RESTORE CAMERA SETTINGS ===
        // Switch back to initial projection mode (typically perspective)
        mainCamera.orthographic = initialOrthographic;
        mainCamera.orthographicSize = initialOrthographicSize;
        mainCamera.fieldOfView = initialFieldOfView;
        
        // === UPDATE INTERNAL STATE ===
        // Recalculate rotationX/Y from restored rotation
        // Ensures internal state matches visual state for smooth subsequent input
        Vector3 angles = transform.eulerAngles;
        rotationX = angles.y; // Yaw (horizontal)
        rotationY = angles.x; // Pitch (vertical)
        
        // === NORMALIZE ANGLES ===
        // Convert 0° to 360° range to -180° to +180° range
        // Example: Initial rotation of 315° (northwest) → -45°
        if (rotationX > 180f)
        {
            rotationX -= 360f;
        }
        
        if (rotationY > 180f)
        {
            rotationY -= 360f;
        }
        
        // Recalculate distance (should match initial state, but ensures consistency)
        currentDistance = Vector3.Distance(transform.position, target.position);
        
        return false; // Perspective view (typically)
    }
    
    /// <summary>
    /// Sets camera to Top View (HP - Horizontal Plane projection).
    /// 
    /// View Configuration:
    /// - Position: (0, 10, 0) - Directly above target at viewDistance height
    /// - Rotation: (90°, 0°, 0°) - Looking straight down -Y axis
    /// - Projection: Orthographic with size 2.5 units
    /// 
    /// Engineering Context:
    /// HP (Horizontal Plane) = XZ plane at Y=0
    /// Top view shows the shape's projection onto the HP:
    /// - Circle appears as circle (true shape of circular base)
    /// - Square appears as square (true shape of square base)
    /// - Height information is lost (3D → 2D projection)
    /// 
    /// Coordinate System:
    /// - X-axis: Horizontal (left-right on screen)
    /// - Z-axis: Vertical (up-down on screen)
    /// - Y-axis: Depth (perpendicular to screen, not visible)
    /// 
    /// Internal State Update:
    /// - rotationX = 0° (no horizontal rotation)
    /// - rotationY = 90° (maximum vertical rotation, directly above)
    /// - currentDistance = viewDistance (fixed for orthographic mode)
    /// </summary>
    /// <returns>True (indicates orthographic view is active)</returns>
    public bool SetTopView()
    {
        if (mainCamera == null || target == null) return false;
        
        // === SWITCH TO ORTHOGRAPHIC PROJECTION ===
        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 2.5f; // Half-height of visible area
        
        // === POSITION CAMERA ABOVE TARGET ===
        // Position: (0, viewDistance, 0)
        // - X = 0: Centered horizontally
        // - Y = viewDistance: Above target (typically 10 units)
        // - Z = 0: Centered depth-wise
        Vector3 topPosition = new Vector3(0f, viewDistance, 0f);
        transform.position = topPosition;
        
        // === SET ROTATION TO LOOK DOWN ===
        // Rotation: (90°, 0°, 0°)
        // - X = 90°: Tilt down to look at XZ plane
        // - Y = 0°: No horizontal rotation (aligned with +Z axis)
        // - Z = 0°: No roll (upright)
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        
        // === UPDATE INTERNAL STATE ===
        rotationX = 0f;           // No horizontal rotation
        rotationY = 90f;          // Maximum vertical rotation (directly above)
        currentDistance = viewDistance; // Fixed distance for orthographic
        
        return true; // Orthographic view
    }
    
    /// <summary>
    /// Sets camera to Front View (VP - Vertical Plane projection).
    /// 
    /// View Configuration:
    /// - Position: (10, 5, 0) - On positive X axis, slightly elevated
    /// - Rotation: (0°, -90°, 0°) - Looking from +X toward origin (negative X direction)
    /// - Projection: Orthographic with size 2.5 units
    /// 
    /// Engineering Context:
    /// VP (Vertical Plane) = YZ plane at X=0
    /// Front view shows the shape's projection onto the VP:
    /// - Height is visible (Y-axis, vertical on screen)
    /// - Depth is visible (Z-axis, but projected as width on screen)
    /// - Width information is lost (3D → 2D projection)
    /// 
    /// Why Position at (10, 5, 0)?
    /// - X = 10: On positive X axis at viewDistance (looking toward origin)
    /// - Y = 5: Slightly elevated to center typical shapes (usually positioned at Y > 0)
    /// - Z = 0: Aligned with VP (no depth offset)
    /// 
    /// Why Y-Rotation = -90°?
    /// - Default camera looks down -Z axis
    /// - Rotate -90° around Y to look down -X axis (toward origin from +X position)
    /// 
    /// Coordinate System (On Screen):
    /// - Z-axis: Horizontal (left-right on screen)
    /// - Y-axis: Vertical (up-down on screen)
    /// - X-axis: Depth (perpendicular to screen, not visible)
    /// 
    /// Internal State Update:
    /// - rotationX = -90° (horizontal rotation to face origin from +X)
    /// - rotationY = 0° (horizon level, no vertical tilt)
    /// - currentDistance = calculated from position to target
    /// </summary>
    /// <returns>True (indicates orthographic view is active)</returns>
    public bool SetFrontView()
    {
        if (mainCamera == null || target == null) return false;
        
        // === SWITCH TO ORTHOGRAPHIC PROJECTION ===
        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 2.5f; // Half-height of visible area
        
        // === POSITION CAMERA ON +X AXIS ===
        // Position: (viewDistance, 5, 0)
        // - X = viewDistance: On positive X axis (looking toward origin)
        // - Y = 5: Elevated to center typical shapes (shapes usually at Y > 0)
        // - Z = 0: Aligned with VP (no depth offset)
        Vector3 frontPosition = new Vector3(viewDistance, 5f, 0f);
        transform.position = frontPosition;
        
        // === SET ROTATION TO LOOK TOWARD ORIGIN ===
        // Rotation: (0°, -90°, 0°)
        // - X = 0°: No vertical tilt (horizon level)
        // - Y = -90°: Rotate to look from +X toward origin (negative X direction)
        // - Z = 0°: No roll (upright)
        transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        
        // === UPDATE INTERNAL STATE ===
        rotationX = -90f;         // Horizontal rotation to face origin
        rotationY = 0f;           // No vertical rotation (horizon level)
        currentDistance = Vector3.Distance(transform.position, target.position);
        
        return true; // Orthographic view
    }
    
    /// <summary>
    /// Toggles between Isometric View (orthographic) and Perspective View.
    /// 
    /// TOGGLE BEHAVIOR:
    /// - If currently in orthographic: Switch to perspective (ResetCamera)
    /// - If currently in perspective: Switch to isometric (set isometric view)
    /// 
    /// ISOMETRIC VIEW CONFIGURATION:
    /// - Position: (10, 10, -10) - Front-top-right diagonal position
    /// - Rotation: (35.264°, -45°, 0°) - Classic isometric angle
    /// - Projection: Orthographic with size 2.5 units
    /// 
    /// Engineering Context:
    /// Isometric projection shows all three axes (X, Y, Z) equally foreshortened.
    /// It's a type of axonometric projection commonly used in technical drawings.
    /// 
    /// Classic Isometric Angle (35.264°):
    /// - Derived from arcsin(tan(30°)) ≈ 35.264°
    /// - This angle ensures equal foreshortening of all three axes
    /// - Mathematical basis: cos(α) = cos(β) = cos(γ) where α, β, γ are angles between axes
    /// 
    /// Position Choice (10, 10, -10):
    /// - Equal distance on X and Y (creates 45° diagonal in XY plane)
    /// - Negative Z positions camera in front quadrant
    /// - Results in classic "front-top-right" isometric view
    /// - Shows front face, top face, and right face simultaneously
    /// 
    /// Rotation Breakdown:
    /// - Y-Rotation: -45° (horizontal diagonal, splits X and Z axes equally)
    /// - X-Rotation: 35.264° (classic isometric angle, elevates view)
    /// - Z-Rotation: 0° (no roll, keeps vertical axis vertical)
    /// 
    /// Coordinate System (On Screen):
    /// All three axes appear at 120° angles from each other:
    /// - Y-axis: Vertical (up-down on screen)
    /// - X-axis: Diagonal down-right (~30° from horizontal)
    /// - Z-axis: Diagonal down-left (~30° from horizontal)
    /// 
    /// Internal State Update:
    /// - rotationX = -45° (horizontal diagonal orientation)
    /// - rotationY = 35.264° (classic isometric elevation)
    /// - currentDistance = calculated from position to target
    /// </summary>
    /// <returns>True if switched to isometric (orthographic), false if switched to perspective</returns>
    public bool SetIsometricView()
    {
        if (mainCamera == null || target == null) return false;
        
        // === CHECK CURRENT PROJECTION MODE ===
        if (mainCamera.orthographic)
        {
            // === ALREADY IN ORTHOGRAPHIC → SWITCH TO PERSPECTIVE ===
            // User pressed isometric button while in orthographic preset view
            // Toggle back to perspective for free exploration
            ResetCamera();
            return false; // Now in perspective
        }
        else
        {
            // === CURRENTLY IN PERSPECTIVE → SWITCH TO ISOMETRIC ===
            
            // === SWITCH TO ORTHOGRAPHIC PROJECTION ===
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 2.5f; // Half-height of visible area
            
            // === POSITION CAMERA AT ISOMETRIC DIAGONAL ===
            // Position: (viewDistance, viewDistance, -viewDistance)
            // Typically: (10, 10, -10)
            // - X = 10: Right side (positive X)
            // - Y = 10: Above (positive Y)
            // - Z = -10: Front quadrant (negative Z)
            // Creates front-top-right isometric view
            Vector3 isometricPosition = new Vector3(viewDistance, viewDistance, -viewDistance);
            transform.position = isometricPosition;
            
            // === SET ROTATION TO CLASSIC ISOMETRIC ANGLE ===
            // Rotation: (35.264°, -45°, 0°)
            // - X = 35.264°: Classic isometric pitch (arcsin(tan(30°)))
            //   This angle ensures equal foreshortening of all axes
            // - Y = -45°: Diagonal yaw (splits X and Z axes equally)
            //   Results in 120° angles between axes on screen
            // - Z = 0°: No roll (keeps Y-axis vertical on screen)
            transform.rotation = Quaternion.Euler(35.264f, -45f, 0f);
            
            // === UPDATE INTERNAL STATE ===
            rotationX = -45f;         // Horizontal diagonal orientation
            rotationY = 35.264f;      // Classic isometric elevation angle
            currentDistance = Vector3.Distance(transform.position, target.position);
            
            return true; // Now in orthographic/isometric
        }
    }
}