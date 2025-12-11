using UnityEngine;

/// <summary>
/// Billboard component that makes a GameObject always face the main camera.
/// 
/// Purpose:
/// - Ensures text labels remain readable from any viewing angle
/// - Prevents text from appearing backwards or sideways during camera rotation
/// - Used by ProjectionDrawer for dimension labels on connector lines
/// 
/// Use Cases:
/// - Dimension labels (numerical measurements on lines)
/// - Floating UI elements in 3D space
/// - Sprites that should always face the player
/// - Debug text overlays in world space
/// 
/// Technical Approach:
/// Uses direct rotation matching instead of LookAt() to avoid mirroring artifacts.
/// 
/// LookAt() vs Rotation Matching:
/// - LookAt(camera): Can cause text to flip 180° when camera passes behind
/// - Rotation matching: Always maintains correct orientation, no flipping
/// 
/// Usage:
/// Simply attach this component to any GameObject with a TextMeshPro or other visual element.
/// No configuration needed - automatically finds and tracks the main camera.
/// </summary>
public class Billboard : MonoBehaviour
{
    /// <summary>
    /// Updates the billboard rotation every frame after all camera movements are complete.
    /// 
    /// Why LateUpdate()?
    /// - Update(): Called before camera movement (object rotates, then camera moves → misalignment)
    /// - LateUpdate(): Called AFTER all Update() and camera controller updates
    /// - Ensures billboard rotation is synchronized with final camera position
    /// - Prevents one-frame lag or jitter in label orientation
    /// 
    /// Frame Execution Order:
    /// 1. Update() - Game logic, user input
    /// 2. Camera movement (OrbitCameraController.Update())
    /// 3. LateUpdate() - Billboard rotation (this method)
    /// 4. Rendering - Billboard is correctly oriented for this frame
    /// 
    /// Performance:
    /// - Minimal overhead: Single rotation assignment per frame
    /// - No raycasting, no complex calculations
    /// - Scales well even with hundreds of labels (assuming reasonable label count)
    /// 
    /// Camera Null Check:
    /// Protects against edge cases:
    /// - Scene startup before camera is initialized
    /// - Camera destruction during scene transitions
    /// - Multiple camera setups where Camera.main might be null
    /// - Editor mode when camera isn't properly tagged
    /// </summary>
    void LateUpdate()
    {
        // === SAFETY CHECK ===
        // Verify main camera exists before attempting rotation
        // Camera.main finds the first camera tagged "MainCamera"
        if (Camera.main != null)
        {
            // === DIRECT ROTATION MATCHING ===
            // Copy camera's rotation directly to this object
            // 
            // Effect: Object appears to "stick" to camera orientation
            // - Camera rotates left → Object rotates left (relative to world)
            // - Camera tilts up → Object tilts up (relative to world)
            // - Net effect: Object always faces camera (from camera's perspective)
            // 
            // Why This Works:
            // If both camera and object have the same world-space rotation,
            // the object's local forward (+Z) aligns with camera's forward.
            // This means the object's "front face" always points at the camera.
            // 
            // Alternative Approach (NOT used):
            // transform.LookAt(Camera.main.transform.position);
            // Problem: Can cause text to flip upside-down or mirror when camera
            // crosses certain angles (gimbal lock-like artifacts)
            transform.rotation = Camera.main.transform.rotation;
        }
        // Note: If camera is null, object maintains last rotation
        // This is acceptable - prevents error spam in console
        // Label will become visible again when camera is restored
    }
}