Engineering Graphics: Solids Projection Visualizer
A comprehensive WebGL tool designed to assist students in understanding the projections of solids in Engineering Graphics. This project allows users to visualize complex 3D geometry and their corresponding 2D orthographic projections (Top and Front views) in real-time.

Developed as part of my internship at [Company/Organization Name].

Key Features
Dynamic Shape Generation: Procedurally generates Cubes, Prisms (Triangular, Square, Pentagonal, Hexagonal), Pyramids, Cylinders, and Cones.

Complex Geometry Logic: Implements mathematical formulas to handle "Slant Face" inclination, using Apothem calculations and compound rotation matrices to tilt non-square shapes perfectly against horizontal and vertical planes.

Real-Time Projections: Uses a custom MeshAnalyzer with spatial vertex welding to calculate and draw hidden, visible, and silhouette lines for projections dynamically.

Interactive Controls:

Mutual Exclusion Toggles: Prevents logical errors by locking conflicting rotation modes.

Auto-Orientation: Automatically rotates shapes (Square vs. Diamond/Corner position) based on user selection.

Manual Overrides: Fine-tune positioning with HP/VP distance sliders and manual Y-axis rotation.

Web-Optimized: Built with Unity WebGL for direct browser access without installation.

Technical Highlights
Engine: Unity 6 (C#)

Mesh Analysis: Custom algorithms for edge detection and hidden line removal based on face normals and spatial hashing.

Math: Implementation of Euler angles and trigonometric corrections for asymmetrical polygon rotations.
