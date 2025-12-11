using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages all UI controls and user interactions for the Engineering Graphics visualization tool.
/// Coordinates between UI elements (sliders, toggles, buttons) and the Visualizer system.
/// Handles bidirectional synchronization between input fields and sliders.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the main Visualizer component that generates and manages 3D shapes")]
    public Visualizer visualizer;
    
    [Tooltip("Reference to the OrbitCameraController for camera manipulation")]
    public OrbitCameraController orbitCameraController;
    
    [Header("Panel Controls")]
    [Tooltip("The main UI panel containing all shape controls (can be toggled on/off)")]
    public GameObject mainControlsPanel;
    
    [Tooltip("Button to show/hide the main controls panel")]
    public Button togglePanelButton;
    
    [Tooltip("Text display for the panel toggle button")]
    public TextMeshProUGUI togglePanelButtonText;
    
    [Header("Shape Controls")]
    [Tooltip("Dropdown menu for selecting shape type (Cube, Pyramids, Prisms, etc.)")]
    public TMP_Dropdown shapeDropdown;
    
    [Header("HP Angle Controls")]
    [Tooltip("Slider for horizontal plane (HP) inclination angle (0° - 90°)")]
    public Slider angleHPSlider;
    
    [Tooltip("Input field for precise HP angle entry")]
    public TMP_InputField angleHPInputField;
    
    [Header("VP Angle Controls")]
    [Tooltip("Slider for vertical plane (VP) inclination angle (0° - 90°)")]
    public Slider angleVPSlider;
    
    [Tooltip("Input field for precise VP angle entry")]
    public TMP_InputField angleVPInputField;
    
    [Header("Base Length Controls")]
    [Tooltip("Slider for shape base length/side length (0.5 - 5.0 units)")]
    public Slider baseLengthSlider;
    
    [Tooltip("Input field for precise base length entry")]
    public TMP_InputField baseLengthInputField;
    
    [Header("Height Controls")]
    [Tooltip("Slider for shape height (0.5 - 5.0 units). Locked to base length for Cube.")]
    public Slider heightSlider;
    
    [Tooltip("Input field for precise height entry")]
    public TMP_InputField heightInputField;
    
    [Header("Distance HP Controls")]
    [Tooltip("Slider for distance from Horizontal Plane - vertical offset (0.0 - 5.0 units)")]
    public Slider distHPSlider;
    
    [Tooltip("Input field for precise HP distance entry")]
    public TMP_InputField distHPInputField;
    
    [Header("Distance VP Controls")]
    [Tooltip("Slider for distance from Vertical Plane - horizontal offset (0.0 - 5.0 units)")]
    public Slider distVPSlider;
    
    [Tooltip("Input field for precise VP distance entry")]
    public TMP_InputField distVPInputField;
    
    [Header("Rotation Y Controls")]
    [Tooltip("Slider for manual Y-axis rotation (0° - 360°). Overrides orientation presets.")]
    public Slider rotationYSlider;
    
    [Tooltip("Input field for precise Y rotation entry")]
    public TMP_InputField rotationYInputField;
    
    [Header("Camera Controls")]
    [Tooltip("Button to reset camera and all shape parameters to defaults")]
    public Button resetButton;
    
    [Tooltip("Button to switch to top-down orthographic view")]
    public Button topViewButton;
    
    [Tooltip("Button to switch to front orthographic view")]
    public Button frontViewButton;
    
    [Tooltip("Button to toggle between isometric and perspective views")]
    public Button isometricViewButton;
    
    [Tooltip("Text display for isometric/perspective toggle button")]
    public TextMeshProUGUI isometricViewButtonText;
    
    [Header("Shape Visibility Controls")]
    [Tooltip("Button to toggle 3D shape visibility (projections remain visible)")]
    public Button toggleShapeButton;
    
    [Tooltip("Text display for shape visibility toggle button")]
    public TextMeshProUGUI toggleShapeButtonText;
    
    [Header("Connector Lines Controls")]
    [Tooltip("Button to toggle dotted connector lines between 3D shape and 2D projections")]
    public Button toggleConnectorsButton;
    
    [Tooltip("Text display for connector lines toggle button")]
    public TextMeshProUGUI toggleConnectorsButtonText;
    
    [Header("Face Inclination Controls")]
    [Tooltip("Toggle for HP face inclination mode (pyramid only). Mutually exclusive with VP mode.")]
    public Toggle faceInclinationHPToggle;
    
    [Tooltip("Toggle for VP face inclination mode (pyramid only). Mutually exclusive with HP mode.")]
    public Toggle faceInclinationVPToggle;
    
    [Header("Orientation Controls")]
    [Tooltip("Toggle for preset orientation angles (corner vs edge presentation)")]
    public Toggle orientationToggle;
    
    [Tooltip("Text label for orientation toggle (changes based on shape type)")]
    public TextMeshProUGUI orientationToggleLabel;
    
    /// <summary>
    /// Initializes all UI controls and registers event listeners.
    /// Sets initial values from ShapeData and establishes bidirectional sync between sliders and input fields.
    /// </summary>
    void Start()
    {
        // === VALIDATE REFERENCES ===
        if (visualizer == null)
        {
            return;
        }
        
        if (visualizer.shapeData == null)
        {
            return;
        }
        
        // === INITIALIZE SHAPE DROPDOWN ===
        if (shapeDropdown != null)
        {
            shapeDropdown.value = (int)visualizer.shapeData.shape;
        }
        
        // === INITIALIZE SLIDERS FROM SHAPEDATA ===
        if (angleHPSlider != null)
        {
            angleHPSlider.value = visualizer.shapeData.angleHP;
        }
        
        if (angleVPSlider != null)
        {
            angleVPSlider.value = visualizer.shapeData.angleVP;
        }
        
        if (baseLengthSlider != null)
        {
            baseLengthSlider.value = visualizer.shapeData.baseLength;
        }
        
        if (heightSlider != null)
        {
            heightSlider.value = visualizer.shapeData.height;
        }
        
        if (distHPSlider != null)
        {
            distHPSlider.value = visualizer.shapeData.distHP;
        }
        
        if (distVPSlider != null)
        {
            distVPSlider.value = visualizer.shapeData.distVP;
        }
        
        if (rotationYSlider != null)
        {
            rotationYSlider.value = visualizer.shapeData.rotationY;
        }
        
        // === INITIALIZE INPUT FIELDS FROM SHAPEDATA ===
        if (angleHPInputField != null)
        {
            angleHPInputField.text = visualizer.shapeData.angleHP.ToString("F1");
        }
        
        if (angleVPInputField != null)
        {
            angleVPInputField.text = visualizer.shapeData.angleVP.ToString("F1");
        }
        
        if (baseLengthInputField != null)
        {
            baseLengthInputField.text = visualizer.shapeData.baseLength.ToString("F1");
        }
        
        if (heightInputField != null)
        {
            heightInputField.text = visualizer.shapeData.height.ToString("F1");
        }
        
        if (distHPInputField != null)
        {
            distHPInputField.text = visualizer.shapeData.distHP.ToString("F1");
        }
        
        if (distVPInputField != null)
        {
            distVPInputField.text = visualizer.shapeData.distVP.ToString("F1");
        }
        
        if (rotationYInputField != null)
        {
            rotationYInputField.text = visualizer.shapeData.rotationY.ToString("F1");
        }
        
        // === INITIALIZE TOGGLES FROM VISUALIZER STATE ===
        if (faceInclinationHPToggle != null)
        {
            faceInclinationHPToggle.isOn = visualizer.useFaceInclinationHP;
        }
        
        if (faceInclinationVPToggle != null)
        {
            faceInclinationVPToggle.isOn = visualizer.useFaceInclinationVP;
        }
        
        if (orientationToggle != null)
        {
            orientationToggle.isOn = visualizer.isDiamondOrientation;
            
            // Set initial interactable state (disabled during face inclination)
            bool faceInclinationActive = visualizer.IsFaceInclinationActive();
            orientationToggle.interactable = !faceInclinationActive;
            
            // Update label based on shape type
            UpdateOrientationLabel();
        }
        
        // === INITIALIZE BUTTON TEXTS ===
        if (togglePanelButtonText != null)
        {
            togglePanelButtonText.text = "Hide\nUI";
        }
        
        if (isometricViewButtonText != null)
        {
            isometricViewButtonText.text = "Switch to Isometric";
        }
        
        if (toggleShapeButtonText != null)
        {
            toggleShapeButtonText.text = "Hide\nShape";
        }
        
        if (toggleConnectorsButtonText != null)
        {
            toggleConnectorsButtonText.text = "Hide\nConnectors";
        }
        
        // === REGISTER EVENT LISTENERS ===
        
        // Panel controls
        if (togglePanelButton != null)
        {
            togglePanelButton.onClick.AddListener(OnTogglePanel);
        }
        
        // Shape dropdown
        if (shapeDropdown != null)
        {
            shapeDropdown.onValueChanged.AddListener(OnShapeChanged);
        }
        
        // Slider listeners
        if (angleHPSlider != null)
        {
            angleHPSlider.onValueChanged.AddListener(OnAngleHPChanged);
        }
        
        if (angleVPSlider != null)
        {
            angleVPSlider.onValueChanged.AddListener(OnAngleVPChanged);
        }
        
        if (baseLengthSlider != null)
        {
            baseLengthSlider.onValueChanged.AddListener(OnBaseLengthChanged);
        }
        
        if (heightSlider != null)
        {
            heightSlider.onValueChanged.AddListener(OnHeightChanged);
        }
        
        if (distHPSlider != null)
        {
            distHPSlider.onValueChanged.AddListener(OnDistHPChanged);
        }
        
        if (distVPSlider != null)
        {
            distVPSlider.onValueChanged.AddListener(OnDistVPChanged);
        }
        
        if (rotationYSlider != null)
        {
            rotationYSlider.onValueChanged.AddListener(OnRotationYChanged);
        }
        
        // Input field listeners
        if (angleHPInputField != null)
        {
            angleHPInputField.onEndEdit.AddListener(OnAngleHPInputChanged);
        }
        
        if (angleVPInputField != null)
        {
            angleVPInputField.onEndEdit.AddListener(OnAngleVPInputChanged);
        }
        
        if (baseLengthInputField != null)
        {
            baseLengthInputField.onEndEdit.AddListener(OnBaseLengthInputChanged);
        }
        
        if (heightInputField != null)
        {
            heightInputField.onEndEdit.AddListener(OnHeightInputChanged);
        }
        
        if (distHPInputField != null)
        {
            distHPInputField.onEndEdit.AddListener(OnDistHPInputChanged);
        }
        
        if (distVPInputField != null)
        {
            distVPInputField.onEndEdit.AddListener(OnDistVPInputChanged);
        }
        
        if (rotationYInputField != null)
        {
            rotationYInputField.onEndEdit.AddListener(OnRotationYInputChanged);
        }
        
        // Camera button listeners
        if (resetButton != null && orbitCameraController != null)
        {
            resetButton.onClick.AddListener(OnResetView);
        }
        
        if (topViewButton != null && orbitCameraController != null)
        {
            topViewButton.onClick.AddListener(OnToggleIsometricView);
        }
        
        if (frontViewButton != null && orbitCameraController != null)
        {
            frontViewButton.onClick.AddListener(OnToggleIsometricView);
        }
        
        if (isometricViewButton != null && orbitCameraController != null)
        {
            isometricViewButton.onClick.AddListener(OnToggleIsometricView);
        }
        
        // Visibility button listeners
        if (toggleShapeButton != null)
        {
            toggleShapeButton.onClick.AddListener(OnToggleShapeClicked);
        }
        
        if (toggleConnectorsButton != null)
        {
            toggleConnectorsButton.onClick.AddListener(OnToggleConnectorsClicked);
        }
        
        // Toggle listeners
        if (faceInclinationHPToggle != null)
        {
            faceInclinationHPToggle.onValueChanged.AddListener(OnFaceInclinationHPToggled);
        }
        
        if (faceInclinationVPToggle != null)
        {
            faceInclinationVPToggle.onValueChanged.AddListener(OnFaceInclinationVPToggled);
        }
        
        if (orientationToggle != null)
        {
            orientationToggle.onValueChanged.AddListener(OnOrientationToggled);
        }
    }
    
    // ========================================
    // PANEL CONTROLS
    // ========================================
    
    /// <summary>
    /// Toggles the visibility of the main controls panel.
    /// Useful for taking clean screenshots or reducing UI clutter.
    /// </summary>
    public void OnTogglePanel()
    {
        if (mainControlsPanel == null) return;
        
        bool newState = !mainControlsPanel.activeSelf;
        mainControlsPanel.SetActive(newState);
        
        if (togglePanelButtonText != null)
        {
            togglePanelButtonText.text = newState ? "Hide\nUI" : "Show\nUI";
        }
    }
    
    // ========================================
    // SHAPE SELECTION
    // ========================================
    
    /// <summary>
    /// Handles shape dropdown selection changes.
    /// Updates ShapeData, manages toggle availability (pyramids vs prisms),
    /// handles Cube-specific height locking, and updates orientation labels.
    /// </summary>
    /// <param name="index">Dropdown index corresponding to ShapeData.ShapeType enum</param>
    public void OnShapeChanged(int index)
    {
        if (visualizer == null || visualizer.shapeData == null) return;
        
        // Update shape type in ShapeData
        visualizer.shapeData.shape = (ShapeData.ShapeType)index;
        
        // Check if new shape supports face inclination (pyramids only)
        bool supportsSlant = visualizer.IsPyramidType(visualizer.shapeData.shape);
        
        // === MANAGE FACE INCLINATION TOGGLES ===
        if (!supportsSlant)
        {
            // Disable face inclination for non-pyramid shapes
            visualizer.DisableFaceInclination();
            
            if (faceInclinationHPToggle != null)
            {
                faceInclinationHPToggle.isOn = false;
                faceInclinationHPToggle.interactable = false;
            }
            
            if (faceInclinationVPToggle != null)
            {
                faceInclinationVPToggle.isOn = false;
                faceInclinationVPToggle.interactable = false;
            }
        }
        else
        {
            // Enable face inclination toggles for pyramids
            if (faceInclinationHPToggle != null)
            {
                faceInclinationHPToggle.interactable = true;
            }
            
            if (faceInclinationVPToggle != null)
            {
                faceInclinationVPToggle.interactable = true;
            }
        }
        
        // === MANAGE ORIENTATION TOGGLE ===
        if (orientationToggle != null)
        {
            bool faceInclinationActive = visualizer.IsFaceInclinationActive();
            
            if (supportsSlant)
            {
                // Pyramids: Enable only if face inclination is OFF
                orientationToggle.interactable = !faceInclinationActive;
                
                if (!faceInclinationActive)
                {
                    orientationToggle.SetIsOnWithoutNotify(visualizer.isDiamondOrientation);
                }
                else
                {
                    // Reset when face inclination is active
                    orientationToggle.SetIsOnWithoutNotify(false);
                    visualizer.isDiamondOrientation = false;
                }
            }
            else
            {
                // Prisms/Cube/Cylinder: Always enable (no face inclination conflict)
                orientationToggle.interactable = true;
                orientationToggle.SetIsOnWithoutNotify(visualizer.isDiamondOrientation);
            }
            
            // Update label text based on new shape
            UpdateOrientationLabel();
        }
        
        // === HANDLE CUBE-SPECIFIC CONSTRAINTS ===
        if (visualizer.shapeData.shape == ShapeData.ShapeType.Cube)
        {
            // For Cube, height must always equal base length
            visualizer.shapeData.height = visualizer.shapeData.baseLength;
            
            if (heightSlider != null)
            {
                heightSlider.value = visualizer.shapeData.baseLength;
                heightSlider.interactable = false; // Lock the slider
            }
            
            if (heightInputField != null)
            {
                heightInputField.text = visualizer.shapeData.baseLength.ToString("F1");
            }
        }
        else
        {
            // For all other shapes, enable height slider
            if (heightSlider != null)
            {
                heightSlider.interactable = true;
            }
        }
        
        // Regenerate shape with new type
        visualizer.UpdateVisualization();
    }
    
    // ========================================
    // FACE INCLINATION TOGGLES
    // ========================================
    
    /// <summary>
    /// Handles HP (Horizontal Plane) face inclination toggle.
    /// Implements mutual exclusion with VP toggle and forces standard orientation.
    /// Only available for pyramid-type shapes.
    /// </summary>
    /// <param name="isOn">New toggle state</param>
    public void OnFaceInclinationHPToggled(bool isOn)
    {
        if (visualizer == null) return;
        
        if (isOn)
        {
            // Attempt to enable HP face inclination
            bool success = visualizer.EnableFaceInclinationHP();
            
            if (success)
            {
                // Disable VP toggle (mutual exclusion)
                if (faceInclinationVPToggle != null)
                {
                    faceInclinationVPToggle.SetIsOnWithoutNotify(false);
                }
                
                // Force orientation toggle OFF (face inclination uses calculated angles)
                if (orientationToggle != null)
                {
                    orientationToggle.SetIsOnWithoutNotify(false);
                    UpdateOrientationLabel();
                    orientationToggle.interactable = false; // Disable during face inclination
                }
            }
            else
            {
                // Failed (not a pyramid) - revert toggle
                if (faceInclinationHPToggle != null)
                {
                    faceInclinationHPToggle.SetIsOnWithoutNotify(false);
                }
            }
        }
        else
        {
            // Disable HP face inclination
            visualizer.useFaceInclinationHP = false;
            
            // Re-enable orientation toggle if no face inclination is active
            if (!visualizer.IsFaceInclinationActive() && orientationToggle != null)
            {
                orientationToggle.interactable = true;
            }
        }
        
        visualizer.UpdateVisualization();
    }
    
    /// <summary>
    /// Handles VP (Vertical Plane) face inclination toggle.
    /// Implements mutual exclusion with HP toggle and forces standard orientation.
    /// Uses multi-axis rotation (X, Y, Z) for correct VP presentation.
    /// Only available for pyramid-type shapes.
    /// </summary>
    /// <param name="isOn">New toggle state</param>
    public void OnFaceInclinationVPToggled(bool isOn)
    {
        if (visualizer == null) return;
        
        if (isOn)
        {
            // Attempt to enable VP face inclination
            bool success = visualizer.EnableFaceInclinationVP();
            
            if (success)
            {
                // Disable HP toggle (mutual exclusion)
                if (faceInclinationHPToggle != null)
                {
                    faceInclinationHPToggle.SetIsOnWithoutNotify(false);
                }
                
                // Force orientation toggle OFF
                if (orientationToggle != null)
                {
                    orientationToggle.SetIsOnWithoutNotify(false);
                    UpdateOrientationLabel();
                    orientationToggle.interactable = false; // Disable during face inclination
                }
            }
            else
            {
                // Failed (not a pyramid) - revert toggle
                if (faceInclinationVPToggle != null)
                {
                    faceInclinationVPToggle.SetIsOnWithoutNotify(false);
                }
            }
        }
        else
        {
            // Disable VP face inclination
            visualizer.useFaceInclinationVP = false;
            
            // Re-enable orientation toggle if no face inclination is active
            if (!visualizer.IsFaceInclinationActive() && orientationToggle != null)
            {
                orientationToggle.interactable = true;
            }
        }
        
        visualizer.UpdateVisualization();
    }
    
    // ========================================
    // ORIENTATION TOGGLE
    // ========================================
    
    /// <summary>
    /// Handles orientation toggle changes (corner vs edge presentation).
    /// Applies shape-specific preset rotation angles (30°, 45°, 54°, 180°).
    /// Disabled during face inclination mode.
    /// </summary>
    /// <param name="isOn">New toggle state</param>
    public void OnOrientationToggled(bool isOn)
    {
        if (visualizer == null) return;
        
        // Prevent orientation change during face inclination
        if (visualizer.IsFaceInclinationActive())
        {
            if (orientationToggle != null)
            {
                orientationToggle.SetIsOnWithoutNotify(false);
            }
            return;
        }
        
        visualizer.isDiamondOrientation = isOn;
        UpdateOrientationLabel();
        visualizer.UpdateVisualization();
    }
    
    /// <summary>
    /// Updates the orientation toggle label text based on current shape type.
    /// Provides intuitive labels like "Orient to Corner" or "Orient to Edge".
    /// </summary>
    private void UpdateOrientationLabel()
    {
        if (orientationToggleLabel == null || visualizer == null) return;
        
        bool isPyramid = visualizer.IsPyramidType(visualizer.shapeData.shape);
        
        if (isPyramid)
        {
            // Hexagonal pyramid is special: default is corner-facing
            if (visualizer.shapeData.shape == ShapeData.ShapeType.HexagonalPyramid)
            {
                orientationToggleLabel.text = "Orient to Edge";
            }
            else
            {
                // All other pyramids: default is edge-facing
                orientationToggleLabel.text = "Orient to Corner";
            }
        }
        else
        {
            // Prisms and other shapes: uniform "Orient to Corner"
            orientationToggleLabel.text = "Orient to Corner";
        }
    }
    
    // ========================================
    // SLIDER HANDLERS (Update ShapeData + Input Fields)
    // ========================================
    
    /// <summary>
    /// Handles HP angle slider changes. Updates ShapeData and input field.
    /// </summary>
    public void OnAngleHPChanged(float value)
    {
        if (visualizer == null || visualizer.shapeData == null) return;
        
        visualizer.shapeData.angleHP = value;
        
        if (angleHPInputField != null)
        {
            angleHPInputField.text = value.ToString("F1");
        }
        
        visualizer.UpdateVisualization();
    }
    
    /// <summary>
    /// Handles VP angle slider changes. Updates ShapeData and input field.
    /// </summary>
    public void OnAngleVPChanged(float value)
    {
        if (visualizer == null || visualizer.shapeData == null) return;
        
        visualizer.shapeData.angleVP = value;
        
        if (angleVPInputField != null)
        {
            angleVPInputField.text = value.ToString("F1");
        }
        
        visualizer.UpdateVisualization();
    }
    
    /// <summary>
    /// Handles base length slider changes. Updates ShapeData and input field.
    /// For Cube, automatically updates height to maintain 1:1 ratio.
    /// </summary>
    public void OnBaseLengthChanged(float value)
    {
        if (visualizer == null || visualizer.shapeData == null) return;
        
        visualizer.shapeData.baseLength = value;
        
        if (baseLengthInputField != null)
        {
            baseLengthInputField.text = value.ToString("F1");
        }
        
        // Cube-specific: synchronize height with base length
        if (visualizer.shapeData.shape == ShapeData.ShapeType.Cube)
        {
            visualizer.shapeData.height = value;
            
            if (heightSlider != null)
            {
                heightSlider.value = value;
            }
            
            if (heightInputField != null)
            {
                heightInputField.text = value.ToString("F1");
            }
        }
        
        visualizer.UpdateVisualization();
    }
    
    /// <summary>
    /// Handles height slider changes. Updates ShapeData and input field.
    /// </summary>
    public void OnHeightChanged(float value)
    {
        if (visualizer == null || visualizer.shapeData == null) return;
        
        visualizer.shapeData.height = value;
        
        if (heightInputField != null)
        {
            heightInputField.text = value.ToString("F1");
        }
        
        visualizer.UpdateVisualization();
    }
    
    /// <summary>
    /// Handles HP distance slider changes (vertical offset from HP).
    /// </summary>
    public void OnDistHPChanged(float value)
    {
        if (visualizer == null || visualizer.shapeData == null) return;
        
        visualizer.shapeData.distHP = value;
        
        if (distHPInputField != null)
        {
            distHPInputField.text = value.ToString("F1");
        }
        
        visualizer.UpdateVisualization();
    }
    
    /// <summary>
    /// Handles VP distance slider changes (horizontal offset from VP).
    /// </summary>
    public void OnDistVPChanged(float value)
    {
        if (visualizer == null || visualizer.shapeData == null) return;
        
        visualizer.shapeData.distVP = value;
        
        if (distVPInputField != null)
        {
            distVPInputField.text = value.ToString("F1");
        }
        
        visualizer.UpdateVisualization();
    }
    
    /// <summary>
    /// Handles manual Y-axis rotation slider changes.
    /// Automatically disables orientation toggle to prevent conflicts.
    /// Manual rotation takes lowest priority in the rotation system.
    /// </summary>
    public void OnRotationYChanged(float value)
    {
        if (visualizer == null || visualizer.shapeData == null) return;
        
        visualizer.shapeData.rotationY = value;
        
        if (rotationYInputField != null)
        {
            rotationYInputField.text = value.ToString("F1");
        }
        
        // Disable orientation toggle if active (manual rotation overrides presets)
        if (orientationToggle != null && visualizer.isDiamondOrientation)
        {
            orientationToggle.SetIsOnWithoutNotify(false);
            visualizer.isDiamondOrientation = false;
        }
        
        visualizer.UpdateVisualization();
    }
    
    // ========================================
    // INPUT FIELD HANDLERS (Update Sliders)
    // ========================================
    
    /// <summary>
    /// Handles HP angle input field text entry.
    /// Parses text, clamps to slider range, and updates slider (which triggers ShapeData update).
    /// </summary>
    public void OnAngleHPInputChanged(string text)
    {
        if (angleHPSlider == null || visualizer == null || visualizer.shapeData == null) return;
        
        if (float.TryParse(text, out float inputValue))
        {
            float clampedValue = Mathf.Clamp(inputValue, angleHPSlider.minValue, angleHPSlider.maxValue);
            angleHPSlider.value = clampedValue;
        }
        else
        {
            // Invalid input - reset to current value
            if (angleHPInputField != null)
            {
                angleHPInputField.text = angleHPSlider.value.ToString("F1");
            }
        }
    }
    
    /// <summary>
    /// Handles VP angle input field text entry.
    /// </summary>
    public void OnAngleVPInputChanged(string text)
    {
        if (angleVPSlider == null || visualizer == null || visualizer.shapeData == null) return;
        
        if (float.TryParse(text, out float inputValue))
        {
            float clampedValue = Mathf.Clamp(inputValue, angleVPSlider.minValue, angleVPSlider.maxValue);
            angleVPSlider.value = clampedValue;
        }
        else
        {
            if (angleVPInputField != null)
            {
                angleVPInputField.text = angleVPSlider.value.ToString("F1");
            }
        }
    }
    
    /// <summary>
    /// Handles base length input field text entry.
    /// </summary>
    public void OnBaseLengthInputChanged(string text)
    {
        if (baseLengthSlider == null || visualizer == null || visualizer.shapeData == null) return;
        
        if (float.TryParse(text, out float inputValue))
        {
            float clampedValue = Mathf.Clamp(inputValue, baseLengthSlider.minValue, baseLengthSlider.maxValue);
            baseLengthSlider.value = clampedValue;
        }
        else
        {
            if (baseLengthInputField != null)
            {
                baseLengthInputField.text = baseLengthSlider.value.ToString("F1");
            }
        }
    }
    
    /// <summary>
    /// Handles height input field text entry.
    /// </summary>
    public void OnHeightInputChanged(string text)
    {
        if (heightSlider == null || visualizer == null || visualizer.shapeData == null) return;
        
        if (float.TryParse(text, out float inputValue))
        {
            float clampedValue = Mathf.Clamp(inputValue, heightSlider.minValue, heightSlider.maxValue);
            heightSlider.value = clampedValue;
        }
        else
        {
            if (heightInputField != null)
            {
                heightInputField.text = heightSlider.value.ToString("F1");
            }
        }
    }
    
    /// <summary>
    /// Handles HP distance input field text entry.
    /// </summary>
    public void OnDistHPInputChanged(string text)
    {
        if (distHPSlider == null || visualizer == null || visualizer.shapeData == null) return;
        
        if (float.TryParse(text, out float inputValue))
        {
            float clampedValue = Mathf.Clamp(inputValue, distHPSlider.minValue, distHPSlider.maxValue);
            distHPSlider.value = clampedValue;
        }
        else
        {
            if (distHPInputField != null)
            {
                distHPInputField.text = distHPSlider.value.ToString("F1");
            }
        }
    }
    
    /// <summary>
    /// Handles VP distance input field text entry.
    /// </summary>
    public void OnDistVPInputChanged(string text)
    {
        if (distVPSlider == null || visualizer == null || visualizer.shapeData == null) return;
        
        if (float.TryParse(text, out float inputValue))
        {
            float clampedValue = Mathf.Clamp(inputValue, distVPSlider.minValue, distVPSlider.maxValue);
            distVPSlider.value = clampedValue;
        }
        else
        {
            if (distVPInputField != null)
            {
                distVPInputField.text = distVPSlider.value.ToString("F1");
            }
        }
    }
    
    /// <summary>
    /// Handles rotation Y input field text entry.
    /// </summary>
    public void OnRotationYInputChanged(string text)
    {
        if (rotationYSlider == null || visualizer == null || visualizer.shapeData == null) return;
        
        if (float.TryParse(text, out float inputValue))
        {
            float clampedValue = Mathf.Clamp(inputValue, rotationYSlider.minValue, rotationYSlider.maxValue);
            rotationYSlider.value = clampedValue;
        }
        else
        {
            if (rotationYInputField != null)
            {
                rotationYInputField.text = rotationYSlider.value.ToString("F1");
            }
        }
    }
    
    // ========================================
    // CAMERA CONTROLS
    // ========================================
    
    /// <summary>
    /// Resets camera to default perspective view and resets ALL shape parameters to defaults.
    /// 
    /// Default Values:
    /// - Angles: 0°
    /// - Dimensions: 2.0 units
    /// - Distances: 2.0 units
    /// - Rotation Y: 0°
    /// - All toggles: OFF
    /// </summary>
    public void OnResetView()
    {
        // Reset camera
        if (orbitCameraController != null)
        {
            orbitCameraController.ResetCamera();
            
            if (isometricViewButtonText != null)
            {
                isometricViewButtonText.text = "Switch to Isometric";
            }
        }
        
        // Reset all sliders to default values
        if (angleHPSlider != null) angleHPSlider.value = 0f;
        if (angleVPSlider != null) angleVPSlider.value = 0f;
        if (baseLengthSlider != null) baseLengthSlider.value = 2.0f;
        if (heightSlider != null) heightSlider.value = 2.0f;
        if (distHPSlider != null) distHPSlider.value = 2.0f;
        if (distVPSlider != null) distVPSlider.value = 2.0f;
        if (rotationYSlider != null) rotationYSlider.value = 0f;
        
        // Reset all toggles
        if (faceInclinationHPToggle != null) faceInclinationHPToggle.isOn = false;
        if (faceInclinationVPToggle != null) faceInclinationVPToggle.isOn = false;
        if (orientationToggle != null) orientationToggle.isOn = false;
    }
    
    /// <summary>
    /// Handles camera view button clicks (Top, Front, Isometric).
    /// Intelligently determines which button was clicked and sets appropriate camera view.
    /// Updates isometric button text to reflect current projection mode.
    /// </summary>
    public void OnToggleIsometricView()
    {
        if (orbitCameraController == null) return;
        
        // Detect which button was clicked
        string buttonName = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject?.name ?? "Unknown";
        
        bool isOrthographic = false;
        
        if (buttonName.Contains("Top") || buttonName.ToLower().Contains("top"))
        {
            isOrthographic = orbitCameraController.SetTopView();
        }
        else if (buttonName.Contains("Front") || buttonName.ToLower().Contains("front"))
        {
            isOrthographic = orbitCameraController.SetFrontView();
        }
        else
        {
            // Isometric button - toggles between isometric and perspective
            isOrthographic = orbitCameraController.SetIsometricView();
        }
        
        // Update button text based on camera projection mode
        if (isometricViewButtonText != null)
        {
            isometricViewButtonText.text = isOrthographic ? "Switch to Perspective" : "Switch to Isometric";
        }
    }
    
    // ========================================
    // VISIBILITY CONTROLS
    // ========================================
    
    /// <summary>
    /// Toggles the visibility of the 3D shape (projections remain visible).
    /// Useful for focusing on 2D projections only.
    /// </summary>
    public void OnToggleShapeClicked()
    {
        if (visualizer == null) return;
        
        bool isShapeVisible = visualizer.ToggleShapeVisibility();
        
        if (toggleShapeButtonText != null)
        {
            toggleShapeButtonText.text = isShapeVisible ? "Hide\nShape" : "Show\nShape";
        }
    }
    
    /// <summary>
    /// Toggles the visibility of dotted connector lines between 3D shape and 2D projections.
    /// Useful for reducing visual clutter.
    /// </summary>
    public void OnToggleConnectorsClicked()
    {
        if (visualizer == null) return;
        
        bool areConnectorsVisible = visualizer.ToggleConnectorLinesVisibility();
        
        if (toggleConnectorsButtonText != null)
        {
            toggleConnectorsButtonText.text = areConnectorsVisible ? "Hide\nConnectors" : "Show\nConnectors";
        }
    }
    
    // ========================================
    // CLEANUP
    // ========================================
    
    /// <summary>
    /// Removes all event listeners to prevent memory leaks.
    /// Called automatically when the GameObject is destroyed.
    /// </summary>
    void OnDestroy()
    {
        // Panel controls
        if (togglePanelButton != null)
        {
            togglePanelButton.onClick.RemoveListener(OnTogglePanel);
        }
        
        // Shape dropdown
        if (shapeDropdown != null)
        {
            shapeDropdown.onValueChanged.RemoveListener(OnShapeChanged);
        }
        
        // Sliders
        if (angleHPSlider != null) angleHPSlider.onValueChanged.RemoveListener(OnAngleHPChanged);
        if (angleVPSlider != null) angleVPSlider.onValueChanged.RemoveListener(OnAngleVPChanged);
        if (baseLengthSlider != null) baseLengthSlider.onValueChanged.RemoveListener(OnBaseLengthChanged);
        if (heightSlider != null) heightSlider.onValueChanged.RemoveListener(OnHeightChanged);
        if (distHPSlider != null) distHPSlider.onValueChanged.RemoveListener(OnDistHPChanged);
        if (distVPSlider != null) distVPSlider.onValueChanged.RemoveListener(OnDistVPChanged);
        if (rotationYSlider != null) rotationYSlider.onValueChanged.RemoveListener(OnRotationYChanged);
        
        // Input fields
        if (angleHPInputField != null) angleHPInputField.onEndEdit.RemoveListener(OnAngleHPInputChanged);
        if (angleVPInputField != null) angleVPInputField.onEndEdit.RemoveListener(OnAngleVPInputChanged);
        if (baseLengthInputField != null) baseLengthInputField.onEndEdit.RemoveListener(OnBaseLengthInputChanged);
        if (heightInputField != null) heightInputField.onEndEdit.RemoveListener(OnHeightInputChanged);
        if (distHPInputField != null) distHPInputField.onEndEdit.RemoveListener(OnDistHPInputChanged);
        if (distVPInputField != null) distVPInputField.onEndEdit.RemoveListener(OnDistVPInputChanged);
        if (rotationYInputField != null) rotationYInputField.onEndEdit.RemoveListener(OnRotationYInputChanged);
        
        // Camera buttons
        if (resetButton != null) resetButton.onClick.RemoveListener(OnResetView);
        if (topViewButton != null) topViewButton.onClick.RemoveListener(OnToggleIsometricView);
        if (frontViewButton != null) frontViewButton.onClick.RemoveListener(OnToggleIsometricView);
        if (isometricViewButton != null) isometricViewButton.onClick.RemoveListener(OnToggleIsometricView);
        
        // Visibility buttons
        if (toggleShapeButton != null) toggleShapeButton.onClick.RemoveListener(OnToggleShapeClicked);
        if (toggleConnectorsButton != null) toggleConnectorsButton.onClick.RemoveListener(OnToggleConnectorsClicked);
        
        // Toggles
        if (faceInclinationHPToggle != null) faceInclinationHPToggle.onValueChanged.RemoveListener(OnFaceInclinationHPToggled);
        if (faceInclinationVPToggle != null) faceInclinationVPToggle.onValueChanged.RemoveListener(OnFaceInclinationVPToggled);
        if (orientationToggle != null) orientationToggle.onValueChanged.RemoveListener(OnOrientationToggled);
    }
}