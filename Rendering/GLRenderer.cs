using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using MinecraftBuildingGuide3D.Models;
using MinecraftBuildingGuide3D.Shapes;

namespace MinecraftBuildingGuide3D.Rendering
{
    public class GLRenderer : IDisposable
    {
        private ShaderProgram _blockShader, _gridShader, _outlineShader, _planeShader;
        private int _cubeVAO, _cubeVBO, _cubeEBO, _cubeLineEBO;
        private int _instanceVBO, _instanceCount;
        private int _planeVAO, _planeVBO, _planeEBO;
        private int _planeGridVAO, _planeGridVBO;
        private float _maxLineWidth = 1.0f;
        private int _gridVAO, _gridVBO, _gridVertexCount;
        private int _gridSizeX, _gridSizeY, _gridSizeZ;
        private bool _initialized = false, _disposed = false;

        public Camera Camera { get; private set; }

        // Rendering options
        public bool ShowGrid { get; set; } = true;
        public bool EnableLighting { get; set; } = true;
        public bool ShowBlockOutlines { get; set; } = true;
        public float OutlineWidth { get; set; } = 2.0f;
        public float AmbientStrength { get; set; } = 0.4f;
        public Vector3 LightDirection { get; set; } = Vector3.Normalize(new Vector3(-0.5f, -1f, -0.3f));
        public float GlobalAlpha { get; set; } = 1.0f;
        public Color4 OutlineColor { get; set; } = new Color4(0.0f, 0.0f, 0.0f, 1.0f);

        // View modes
        public bool Is2DMode { get; set; } = false;
        public int View2DLayer { get; set; } = 0;
        public float MaxLineWidth => _maxLineWidth;
        public int MaxVisibleLayer { get; set; } = int.MaxValue;
        public bool ShowAllLayers { get; set; } = true;

        // 2D view camera
        public float View2DZoom { get; set; } = 1.0f;
        public float View2DPanX { get; set; } = 0f;
        public float View2DPanZ { get; set; } = 0f;

        // Building mode
        public bool BuildingMode { get; set; } = false;
        public bool ShowXZPlane { get; set; } = false;
        public bool ShowXYPlane { get; set; } = false;
        public bool ShowYZPlane { get; set; } = false;
        public int PlaneY { get; set; } = 0;
        public int PlaneZ { get; set; } = 0;
        public int PlaneX { get; set; } = 0;

        // Highlight for building mode
        public Vector3? HighlightPosition { get; set; } = null;
        public Vector3? RemoveHighlightPosition { get; set; } = null;
        public bool ShowHighlight => BuildingMode && (HighlightPosition.HasValue || RemoveHighlightPosition.HasValue);

        // Shape bounds for axis display (set by MainForm after generation)
        public int ShapeSizeX { get; set; } = 0;
        public int ShapeSizeY { get; set; } = 0;
        public int ShapeSizeZ { get; set; } = 0;
        public bool IsOddX => ShapeSizeX > 0 && ShapeSizeX % 2 == 1;
        public bool IsOddY => ShapeSizeY > 0 && ShapeSizeY % 2 == 1;
        public bool IsOddZ => ShapeSizeZ > 0 && ShapeSizeZ % 2 == 1;

        // Plane shader source
        private const string PlaneVertexShader = @"
            #version 330 core
            layout (location = 0) in vec3 aPos;
            uniform mat4 model;
            uniform mat4 view;
            uniform mat4 projection;
            void main() {
                gl_Position = projection * view * model * vec4(aPos, 1.0);
            }";

        private const string PlaneFragmentShader = @"
            #version 330 core
            out vec4 FragColor;
            uniform vec4 planeColor;
            void main() {
                FragColor = planeColor;
            }";

        public void Initialize()
        {
            if (_initialized) return;
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.ClearColor(0.1f, 0.1f, 0.15f, 1.0f);

            _blockShader = new ShaderProgram(ShaderProgram.DefaultVertexShader, ShaderProgram.DefaultFragmentShader);
            _gridShader = new ShaderProgram(ShaderProgram.GridVertexShader, ShaderProgram.GridFragmentShader);
            _outlineShader = new ShaderProgram(ShaderProgram.OutlineVertexShader, ShaderProgram.OutlineFragmentShader);
            _planeShader = new ShaderProgram(PlaneVertexShader, PlaneFragmentShader);

            float[] range = new float[2];
            GL.GetFloat(GetPName.LineWidthRange, range);
            _maxLineWidth = range[1];

            CreateCubeMesh();
            CreatePlaneMesh();
            CreatePlaneGridMesh();
            _instanceVBO = GL.GenBuffer();
            Camera = new Camera { MaxDistance = 1000f };
            CreateGrid(512);
            _initialized = true;
        }

        private void CreateCubeMesh()
        {
            float[] v = {
                0,0,1, 0,0,1,  1,0,1, 0,0,1,  1,1,1, 0,0,1,  0,1,1, 0,0,1,
                1,0,0, 0,0,-1, 0,0,0, 0,0,-1, 0,1,0, 0,0,-1, 1,1,0, 0,0,-1,
                0,1,1, 0,1,0,  1,1,1, 0,1,0,  1,1,0, 0,1,0,  0,1,0, 0,1,0,
                0,0,0, 0,-1,0, 1,0,0, 0,-1,0, 1,0,1, 0,-1,0, 0,0,1, 0,-1,0,
                1,0,1, 1,0,0,  1,0,0, 1,0,0,  1,1,0, 1,0,0,  1,1,1, 1,0,0,
                0,0,0, -1,0,0, 0,0,1, -1,0,0, 0,1,1, -1,0,0, 0,1,0, -1,0,0,
            };
            uint[] i = { 0, 1, 2, 2, 3, 0, 4, 5, 6, 6, 7, 4, 8, 9, 10, 10, 11, 8, 12, 13, 14, 14, 15, 12, 16, 17, 18, 18, 19, 16, 20, 21, 22, 22, 23, 20 };
            uint[] li = { 12, 13, 13, 14, 14, 15, 15, 12, 8, 9, 9, 10, 10, 11, 11, 8, 15, 8, 14, 9, 13, 10, 12, 11 };

            _cubeVAO = GL.GenVertexArray(); _cubeVBO = GL.GenBuffer(); _cubeEBO = GL.GenBuffer(); _cubeLineEBO = GL.GenBuffer();
            GL.BindVertexArray(_cubeVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _cubeVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, v.Length * sizeof(float), v, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _cubeEBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, i.Length * sizeof(uint), i, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _cubeLineEBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, li.Length * sizeof(uint), li, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _cubeEBO);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 24, 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 24, 12);
            GL.EnableVertexAttribArray(1);
            GL.BindVertexArray(0);
        }

        private void CreatePlaneMesh()
        {
            float s = 200f;
            float[] v = { -s, 0, -s, s, 0, -s, s, 0, s, -s, 0, s };
            uint[] indices = { 0, 1, 2, 2, 3, 0 };

            _planeVAO = GL.GenVertexArray();
            _planeVBO = GL.GenBuffer();
            _planeEBO = GL.GenBuffer();

            GL.BindVertexArray(_planeVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _planeVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, v.Length * sizeof(float), v, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _planeEBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 12, 0);
            GL.EnableVertexAttribArray(0);
            GL.BindVertexArray(0);
        }

        private void CreatePlaneGridMesh()
        {
            // Create a grid of lines for the plane (centered at origin, in XZ plane)
            var verts = new List<float>();
            int gridSize = 100;
            float extent = gridSize;

            for (int i = -gridSize; i <= gridSize; i++)
            {
                // Lines along X axis
                verts.AddRange(new float[] { -extent, 0, i, extent, 0, i });
                // Lines along Z axis
                verts.AddRange(new float[] { i, 0, -extent, i, 0, extent });
            }

            _planeGridVAO = GL.GenVertexArray();
            _planeGridVBO = GL.GenBuffer();

            GL.BindVertexArray(_planeGridVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _planeGridVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, verts.Count * sizeof(float), verts.ToArray(), BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 12, 0);
            GL.EnableVertexAttribArray(0);
            GL.BindVertexArray(0);
        }

        private void CreateGrid(int size)
        {
            var verts = new List<float>();
            float h = size / 2f;
            for (int j = -size / 2; j <= size / 2; j++) { verts.AddRange(new[] { (float)j, 0, -h, (float)j, 0, h, -h, 0, (float)j, h, 0, (float)j }); }
            _gridVertexCount = verts.Count / 3;
            if (_gridVAO == 0) _gridVAO = GL.GenVertexArray();
            if (_gridVBO == 0) _gridVBO = GL.GenBuffer();
            GL.BindVertexArray(_gridVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _gridVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, verts.Count * sizeof(float), verts.ToArray(), BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 12, 0);
            GL.EnableVertexAttribArray(0);
            GL.BindVertexArray(0);
        }

        public void UpdateFromGrid(VoxelGrid grid)
        {
            _gridSizeX = grid.SizeX; _gridSizeY = grid.SizeY; _gridSizeZ = grid.SizeZ;
            var data = new List<float>();
            IEnumerable<Block> blocks = Is2DMode ? grid.GetBlocksAtLayer(View2DLayer) : (ShowAllLayers ? grid.GetAllBlocks() : grid.GetBlocksUpToLayer(MaxVisibleLayer));

            foreach (var b in blocks)
            {
                data.Add(b.X - grid.CenterX);
                data.Add(Is2DMode ? 0 : b.Y);
                data.Add(b.Z - grid.CenterZ);
                var c = BlockColors.GetColor(b.Type);
                if (!ShowAllLayers && !Is2DMode && b.Y == MaxVisibleLayer) c = BlockColors.Layer;
                data.Add(c.R); data.Add(c.G); data.Add(c.B); data.Add(c.A);
            }

            _instanceCount = data.Count / 7;
            GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, data.Count * sizeof(float), data.ToArray(), BufferUsageHint.DynamicDraw);
            GL.BindVertexArray(_cubeVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVBO);
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 28, 0);
            GL.EnableVertexAttribArray(2); GL.VertexAttribDivisor(2, 1);
            GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, 28, 12);
            GL.EnableVertexAttribArray(3); GL.VertexAttribDivisor(3, 1);
            GL.BindVertexArray(0);
        }

        public void Render(int w, int h)
        {
            if (!_initialized) return;
            GL.Viewport(0, 0, w, h);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Camera.AspectRatio = (float)w / h;

            Matrix4 view, proj;
            if (Is2DMode)
            {
                // 2D orthographic view with zoom and pan
                float baseSize = Math.Max(_gridSizeX, _gridSizeZ) * 0.7f;
                float size = baseSize / View2DZoom;
                proj = Matrix4.CreateOrthographic(size * w / h, size, 0.1f, 500f);
                view = Matrix4.LookAt(
                    new Vector3(View2DPanX, 100, View2DPanZ),
                    new Vector3(View2DPanX, 0, View2DPanZ),
                    -Vector3.UnitZ);
            }
            else { view = Camera.ViewMatrix; proj = Camera.ProjectionMatrix; }

            // Draw construction planes with grids
            if (ShowXZPlane || ShowXYPlane || ShowYZPlane)
            {
                DrawGuidePlanes(view, proj);
            }

            // Draw floor grid (3D mode)
            if (ShowGrid && !Is2DMode)
            {
                _gridShader.Use();
                _gridShader.SetMatrix4("view", view);
                _gridShader.SetMatrix4("projection", proj);
                _gridShader.SetColor4("gridColor", new Color4(0.25f, 0.25f, 0.3f, 0.3f));
                GL.BindVertexArray(_gridVAO);
                GL.DrawArrays(PrimitiveType.Lines, 0, _gridVertexCount);
            }

            // Draw 2D grid (2D mode) - at current layer
            if (ShowGrid && Is2DMode)
            {
                Draw2DGrid(view, proj);
            }

            // Draw blocks
            if (_instanceCount > 0)
            {
                if (Is2DMode) GL.Disable(EnableCap.CullFace);
                _blockShader.Use();
                _blockShader.SetMatrix4("model", Matrix4.Identity);
                _blockShader.SetMatrix4("view", view);
                _blockShader.SetMatrix4("projection", proj);
                _blockShader.SetVector3("lightDir", LightDirection);
                _blockShader.SetVector3("viewPos", Is2DMode ? new Vector3(View2DPanX, 100, View2DPanZ) : Camera.Position);
                _blockShader.SetFloat("ambientStrength", Is2DMode ? 1f : AmbientStrength);
                _blockShader.SetInt("enableLighting", Is2DMode ? 0 : (EnableLighting ? 1 : 0));
                _blockShader.SetFloat("globalAlpha", GlobalAlpha);
                GL.BindVertexArray(_cubeVAO);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _cubeEBO);
                GL.DrawElementsInstanced(PrimitiveType.Triangles, 36, DrawElementsType.UnsignedInt, IntPtr.Zero, _instanceCount);

                if (ShowBlockOutlines)
                {
                    _outlineShader.Use();
                    _outlineShader.SetMatrix4("model", Matrix4.Identity);
                    _outlineShader.SetMatrix4("view", view);
                    _outlineShader.SetMatrix4("projection", proj);
                    _outlineShader.SetColor4("outlineColor", OutlineColor);
                    GL.LineWidth(Math.Min(OutlineWidth, _maxLineWidth));
                    GL.DepthFunc(DepthFunction.Lequal);
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, _cubeLineEBO);
                    GL.DrawElementsInstanced(PrimitiveType.Lines, 24, DrawElementsType.UnsignedInt, IntPtr.Zero, _instanceCount);
                    GL.DepthFunc(DepthFunction.Less);
                    GL.LineWidth(1f);
                }
                if (Is2DMode) GL.Enable(EnableCap.CullFace);
            }

            // Draw highlight block (building mode preview)
            if (HighlightPosition.HasValue)
            {
                // Add mode - cyan/blue highlight
                DrawHighlightBlock(view, proj, HighlightPosition.Value,
                    new Color4(0.3f, 0.7f, 1.0f, 0.6f), new Color4(0.2f, 0.8f, 1.0f, 1.0f));
            }
            if (RemoveHighlightPosition.HasValue)
            {
                // Remove mode - red highlight
                DrawHighlightBlock(view, proj, RemoveHighlightPosition.Value,
                    new Color4(1.0f, 0.3f, 0.3f, 0.6f), new Color4(1.0f, 0.2f, 0.2f, 1.0f));
            }

            // Draw orientation gizmo in bottom-left corner
            if (!Is2DMode)
            {
                DrawOrientationGizmo(w, h, view);
            }

            GL.BindVertexArray(0);
        }

        private void DrawOrientationGizmo(int viewportWidth, int viewportHeight, Matrix4 cameraView)
        {
            // Draw a small orientation indicator in the bottom-left corner
            int gizmoSize = 80;
            int margin = 10;

            // Set up a small viewport in the bottom-left corner
            GL.Viewport(margin, margin, gizmoSize, gizmoSize);

            // Create orthographic projection for the gizmo
            float orthoSize = 2.5f;
            Matrix4 gizmoProj = Matrix4.CreateOrthographic(orthoSize, orthoSize, 0.1f, 100f);

            // Extract just the rotation from the camera view (remove translation)
            Matrix4 rotationOnly = cameraView.ClearTranslation();
            // Create a view matrix that looks at origin from a fixed distance, using camera rotation
            Matrix4 gizmoView = Matrix4.CreateTranslation(0, 0, -5f) * rotationOnly;

            // Clear only depth for this viewport area
            GL.Enable(EnableCap.DepthTest);
            GL.Clear(ClearBufferMask.DepthBufferBit);

            _planeShader.Use();
            _planeShader.SetMatrix4("view", gizmoView);
            _planeShader.SetMatrix4("projection", gizmoProj);
            _planeShader.SetMatrix4("model", Matrix4.Identity);

            // Draw axis lines
            float axisLength = 1.0f;
            float[] axisData = {
                // X axis (red) - points right
                0, 0, 0,  axisLength, 0, 0,
                // Y axis (green) - points up  
                0, 0, 0,  0, axisLength, 0,
                // Z axis (blue) - points toward viewer
                0, 0, 0,  0, 0, axisLength
            };

            int axisVBO = GL.GenBuffer();
            int axisVAO = GL.GenVertexArray();

            GL.BindVertexArray(axisVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, axisVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, axisData.Length * sizeof(float), axisData, BufferUsageHint.StreamDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.LineWidth(Math.Min(3f, _maxLineWidth));

            // Draw X axis (red)
            _planeShader.SetVector4("planeColor", new Vector4(1f, 0.3f, 0.3f, 1f));
            GL.DrawArrays(PrimitiveType.Lines, 0, 2);

            // Draw Y axis (green)
            _planeShader.SetVector4("planeColor", new Vector4(0.3f, 1f, 0.3f, 1f));
            GL.DrawArrays(PrimitiveType.Lines, 2, 2);

            // Draw Z axis (blue)
            _planeShader.SetVector4("planeColor", new Vector4(0.3f, 0.5f, 1f, 1f));
            GL.DrawArrays(PrimitiveType.Lines, 4, 2);

            // Draw small cubes at axis ends to make them more visible
            float cubeSize = 0.15f;
            DrawGizmoCube(gizmoView, gizmoProj, new Vector3(axisLength, 0, 0), cubeSize, new Color4(1f, 0.3f, 0.3f, 1f));
            DrawGizmoCube(gizmoView, gizmoProj, new Vector3(0, axisLength, 0), cubeSize, new Color4(0.3f, 1f, 0.3f, 1f));
            DrawGizmoCube(gizmoView, gizmoProj, new Vector3(0, 0, axisLength), cubeSize, new Color4(0.3f, 0.5f, 1f, 1f));

            // Draw axis labels
            DrawGizmoLabel(gizmoView, gizmoProj, new Vector3(axisLength + 0.3f, 0, 0), "X", new Vector4(1f, 0.4f, 0.4f, 1f));
            DrawGizmoLabel(gizmoView, gizmoProj, new Vector3(0, axisLength + 0.3f, 0), "Y", new Vector4(0.4f, 1f, 0.4f, 1f));
            DrawGizmoLabel(gizmoView, gizmoProj, new Vector3(0, 0, axisLength + 0.3f), "Z", new Vector4(0.4f, 0.6f, 1f, 1f));

            GL.LineWidth(1f);
            GL.DeleteBuffer(axisVBO);
            GL.DeleteVertexArray(axisVAO);

            // Restore full viewport
            GL.Viewport(0, 0, viewportWidth, viewportHeight);
        }

        private void DrawGizmoCube(Matrix4 view, Matrix4 proj, Vector3 position, float size, Color4 color)
        {
            _blockShader.Use();
            _blockShader.SetMatrix4("view", view);
            _blockShader.SetMatrix4("projection", proj);

            // Create a small cube at the position
            Matrix4 model = Matrix4.CreateScale(size) * Matrix4.CreateTranslation(position);
            _blockShader.SetMatrix4("model", model);
            _blockShader.SetInt("useLighting", 0);
            _blockShader.SetFloat("globalAlpha", 1f);

            // Draw using the cube geometry but with a solid color
            // We'll use instanced drawing with a single instance
            float[] instanceData = { 0, 0, 0, color.R, color.G, color.B, color.A };

            GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, instanceData.Length * sizeof(float), instanceData, BufferUsageHint.StreamDraw);

            GL.BindVertexArray(_cubeVAO);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _cubeEBO);
            GL.DrawElementsInstanced(PrimitiveType.Triangles, 36, DrawElementsType.UnsignedInt, IntPtr.Zero, 1);
        }

        private void DrawGizmoLabel(Matrix4 view, Matrix4 proj, Vector3 position, string label, Vector4 color)
        {
            // Draw a simple letter using line segments
            _planeShader.Use();
            _planeShader.SetMatrix4("view", view);
            _planeShader.SetMatrix4("projection", proj);
            _planeShader.SetVector4("planeColor", color);

            float s = 0.15f; // Letter size
            var lines = new List<float>();

            switch (label)
            {
                case "X":
                    lines.AddRange(new float[] {
                        position.X - s, position.Y + s, position.Z,
                        position.X + s, position.Y - s, position.Z,
                        position.X - s, position.Y - s, position.Z,
                        position.X + s, position.Y + s, position.Z
                    });
                    break;
                case "Y":
                    lines.AddRange(new float[] {
                        position.X - s, position.Y + s, position.Z,
                        position.X, position.Y, position.Z,
                        position.X + s, position.Y + s, position.Z,
                        position.X, position.Y, position.Z,
                        position.X, position.Y, position.Z,
                        position.X, position.Y - s, position.Z
                    });
                    break;
                case "Z":
                    lines.AddRange(new float[] {
                        position.X - s, position.Y + s, position.Z,
                        position.X + s, position.Y + s, position.Z,
                        position.X + s, position.Y + s, position.Z,
                        position.X - s, position.Y - s, position.Z,
                        position.X - s, position.Y - s, position.Z,
                        position.X + s, position.Y - s, position.Z
                    });
                    break;
            }

            if (lines.Count > 0)
            {
                float[] lineData = lines.ToArray();
                int vbo = GL.GenBuffer();
                int vao = GL.GenVertexArray();

                GL.BindVertexArray(vao);
                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
                GL.BufferData(BufferTarget.ArrayBuffer, lineData.Length * sizeof(float), lineData, BufferUsageHint.StreamDraw);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
                GL.EnableVertexAttribArray(0);

                _planeShader.SetMatrix4("model", Matrix4.Identity);
                GL.LineWidth(Math.Min(2f, _maxLineWidth));
                GL.DrawArrays(PrimitiveType.Lines, 0, lineData.Length / 3);

                GL.DeleteBuffer(vbo);
                GL.DeleteVertexArray(vao);
            }
        }

        private void DrawGuidePlanes(Matrix4 view, Matrix4 proj)
        {
            GL.Disable(EnableCap.CullFace);

            _planeShader.Use();
            _planeShader.SetMatrix4("view", view);
            _planeShader.SetMatrix4("projection", proj);

            int gridLineCount = 201 * 4;

            // First pass: Draw grid lines WITH depth testing (shows in front of objects, clips behind)
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);

            // XZ Plane (Blue) - Horizontal floor plane
            if (ShowXZPlane)
            {
                var model = Matrix4.CreateTranslation(0, PlaneY + 0.01f, 0);
                _planeShader.SetMatrix4("model", model);
                _planeShader.SetVector4("planeColor", new Vector4(0.3f, 0.5f, 0.9f, 0.9f));
                GL.LineWidth(Math.Min(2f, _maxLineWidth));
                GL.BindVertexArray(_planeGridVAO);
                GL.DrawArrays(PrimitiveType.Lines, 0, gridLineCount);
            }

            // XY Plane (Red) - Vertical at Z
            if (ShowXYPlane)
            {
                var model = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(90)) *
                            Matrix4.CreateTranslation(0, 0, PlaneZ + 0.01f);
                _planeShader.SetMatrix4("model", model);
                _planeShader.SetVector4("planeColor", new Vector4(0.9f, 0.3f, 0.3f, 0.9f));
                GL.LineWidth(Math.Min(2f, _maxLineWidth));
                GL.BindVertexArray(_planeGridVAO);
                GL.DrawArrays(PrimitiveType.Lines, 0, gridLineCount);
            }

            // YZ Plane (Green) - Vertical at X
            if (ShowYZPlane)
            {
                var model = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(90)) *
                            Matrix4.CreateTranslation(PlaneX + 0.01f, 0, 0);
                _planeShader.SetMatrix4("model", model);
                _planeShader.SetVector4("planeColor", new Vector4(0.3f, 0.9f, 0.3f, 0.9f));
                GL.LineWidth(Math.Min(2f, _maxLineWidth));
                GL.BindVertexArray(_planeGridVAO);
                GL.DrawArrays(PrimitiveType.Lines, 0, gridLineCount);
            }

            // Second pass: Draw through objects (dimmer, shows where plane passes through)
            GL.DepthFunc(DepthFunction.Greater);  // Only draw where occluded by objects

            if (ShowXZPlane)
            {
                var model = Matrix4.CreateTranslation(0, PlaneY + 0.01f, 0);
                _planeShader.SetMatrix4("model", model);
                _planeShader.SetVector4("planeColor", new Vector4(0.3f, 0.5f, 0.9f, 0.35f));  // Dimmer
                GL.LineWidth(Math.Min(1.5f, _maxLineWidth));
                GL.BindVertexArray(_planeGridVAO);
                GL.DrawArrays(PrimitiveType.Lines, 0, gridLineCount);
            }

            if (ShowXYPlane)
            {
                var model = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(90)) *
                            Matrix4.CreateTranslation(0, 0, PlaneZ + 0.01f);
                _planeShader.SetMatrix4("model", model);
                _planeShader.SetVector4("planeColor", new Vector4(0.9f, 0.3f, 0.3f, 0.35f));
                GL.LineWidth(Math.Min(1.5f, _maxLineWidth));
                GL.BindVertexArray(_planeGridVAO);
                GL.DrawArrays(PrimitiveType.Lines, 0, gridLineCount);
            }

            if (ShowYZPlane)
            {
                var model = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(90)) *
                            Matrix4.CreateTranslation(PlaneX + 0.01f, 0, 0);
                _planeShader.SetMatrix4("model", model);
                _planeShader.SetVector4("planeColor", new Vector4(0.3f, 0.9f, 0.3f, 0.35f));
                GL.LineWidth(Math.Min(1.5f, _maxLineWidth));
                GL.BindVertexArray(_planeGridVAO);
                GL.DrawArrays(PrimitiveType.Lines, 0, gridLineCount);
            }

            GL.DepthFunc(DepthFunction.Less);  // Restore default
            GL.LineWidth(1f);
            GL.Enable(EnableCap.CullFace);
        }

        private void Draw2DGrid(Matrix4 view, Matrix4 proj)
        {
            // Draw a grid at Y=0 where 2D blocks are rendered
            _planeShader.Use();
            _planeShader.SetMatrix4("view", view);
            _planeShader.SetMatrix4("projection", proj);

            int gridSize = Math.Max(_gridSizeX, _gridSizeZ);
            int halfGrid = gridSize / 2;
            float y = 0.01f;  // Slightly above Y=0 where blocks are rendered in 2D mode

            // Generate grid lines dynamically
            var gridLines = new List<float>();

            // Vertical lines (along Z axis)
            for (int x = -halfGrid; x <= halfGrid; x++)
            {
                gridLines.Add(x); gridLines.Add(y); gridLines.Add(-halfGrid);
                gridLines.Add(x); gridLines.Add(y); gridLines.Add(halfGrid);
            }

            // Horizontal lines (along X axis)
            for (int z = -halfGrid; z <= halfGrid; z++)
            {
                gridLines.Add(-halfGrid); gridLines.Add(y); gridLines.Add(z);
                gridLines.Add(halfGrid); gridLines.Add(y); gridLines.Add(z);
            }

            float[] gridData = gridLines.ToArray();

            int tempVBO = GL.GenBuffer();
            int tempVAO = GL.GenVertexArray();

            GL.BindVertexArray(tempVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, tempVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, gridData.Length * sizeof(float), gridData, BufferUsageHint.StreamDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // Draw minor grid lines
            var model = Matrix4.Identity;
            _planeShader.SetMatrix4("model", model);
            _planeShader.SetVector4("planeColor", new Vector4(0.35f, 0.35f, 0.4f, 0.5f));
            GL.LineWidth(1f);
            GL.DrawArrays(PrimitiveType.Lines, 0, gridData.Length / 3);

            // Draw major grid lines (every 10 blocks) - thicker
            var majorLines = new List<float>();
            for (int x = -halfGrid; x <= halfGrid; x += 10)
            {
                majorLines.Add(x); majorLines.Add(y + 0.01f); majorLines.Add(-halfGrid);
                majorLines.Add(x); majorLines.Add(y + 0.01f); majorLines.Add(halfGrid);
            }
            for (int z = -halfGrid; z <= halfGrid; z += 10)
            {
                majorLines.Add(-halfGrid); majorLines.Add(y + 0.01f); majorLines.Add(z);
                majorLines.Add(halfGrid); majorLines.Add(y + 0.01f); majorLines.Add(z);
            }

            float[] majorData = majorLines.ToArray();
            GL.BufferData(BufferTarget.ArrayBuffer, majorData.Length * sizeof(float), majorData, BufferUsageHint.StreamDraw);
            _planeShader.SetVector4("planeColor", new Vector4(0.5f, 0.5f, 0.55f, 0.7f));
            GL.LineWidth(Math.Min(1.5f, _maxLineWidth));
            GL.DrawArrays(PrimitiveType.Lines, 0, majorData.Length / 3);

            // Draw X axis lines (red) - horizontal lines along X
            // When Z dimension is odd, there's a center row of blocks at Z=0 (occupies Z=0 to Z=1)
            // So we draw lines at Z=0 and Z=1 to bracket it
            var axisLines = new List<float>();
            if (IsOddZ)
            {
                // Two lines bracketing the center row (block at Z=0 occupies Z=0 to Z=1)
                axisLines.Add(-halfGrid); axisLines.Add(y + 0.02f); axisLines.Add(0f);
                axisLines.Add(halfGrid); axisLines.Add(y + 0.02f); axisLines.Add(0f);
                axisLines.Add(-halfGrid); axisLines.Add(y + 0.02f); axisLines.Add(1f);
                axisLines.Add(halfGrid); axisLines.Add(y + 0.02f); axisLines.Add(1f);
            }
            else
            {
                // Single line at Z=0 (center is between blocks)
                axisLines.Add(-halfGrid); axisLines.Add(y + 0.02f); axisLines.Add(0);
                axisLines.Add(halfGrid); axisLines.Add(y + 0.02f); axisLines.Add(0);
            }

            float[] xAxisData = axisLines.ToArray();
            GL.BufferData(BufferTarget.ArrayBuffer, xAxisData.Length * sizeof(float), xAxisData, BufferUsageHint.StreamDraw);
            _planeShader.SetVector4("planeColor", new Vector4(0.9f, 0.35f, 0.35f, 0.9f));
            GL.LineWidth(Math.Min(2f, _maxLineWidth));
            GL.DrawArrays(PrimitiveType.Lines, 0, xAxisData.Length / 3);

            // Draw Z axis lines (blue) - vertical lines along Z
            // When X dimension is odd, there's a center column of blocks at X=0 (occupies X=0 to X=1)
            // So we draw lines at X=0 and X=1 to bracket it
            axisLines.Clear();
            if (IsOddX)
            {
                // Two lines bracketing the center column (block at X=0 occupies X=0 to X=1)
                axisLines.Add(0f); axisLines.Add(y + 0.02f); axisLines.Add(-halfGrid);
                axisLines.Add(0f); axisLines.Add(y + 0.02f); axisLines.Add(halfGrid);
                axisLines.Add(1f); axisLines.Add(y + 0.02f); axisLines.Add(-halfGrid);
                axisLines.Add(1f); axisLines.Add(y + 0.02f); axisLines.Add(halfGrid);
            }
            else
            {
                // Single line at X=0 (center is between blocks)
                axisLines.Add(0); axisLines.Add(y + 0.02f); axisLines.Add(-halfGrid);
                axisLines.Add(0); axisLines.Add(y + 0.02f); axisLines.Add(halfGrid);
            }

            float[] zAxisData = axisLines.ToArray();
            GL.BufferData(BufferTarget.ArrayBuffer, zAxisData.Length * sizeof(float), zAxisData, BufferUsageHint.StreamDraw);
            _planeShader.SetVector4("planeColor", new Vector4(0.35f, 0.55f, 0.95f, 0.9f));
            GL.DrawArrays(PrimitiveType.Lines, 0, zAxisData.Length / 3);

            GL.LineWidth(1f);
            GL.DeleteBuffer(tempVBO);
            GL.DeleteVertexArray(tempVAO);

            // Draw axis numbers when zoomed in enough
            if (View2DZoom >= 1.5f)
            {
                DrawAxisNumbers(view, proj, halfGrid);
            }
        }

        private void DrawAxisNumbers(Matrix4 view, Matrix4 proj, int halfGrid)
        {
            float y = 0.05f;  // Slightly above Y=0 where blocks are rendered in 2D mode

            // Determine step based on zoom level
            int step = View2DZoom >= 4f ? 1 : (View2DZoom >= 2.5f ? 5 : 10);

            // Calculate visible range based on pan and zoom
            float baseSize = Math.Max(_gridSizeX, _gridSizeZ) * 0.7f;
            float viewSize = baseSize / View2DZoom;
            int minVisible = (int)Math.Floor(View2DPanX - viewSize);
            int maxVisible = (int)Math.Ceiling(View2DPanX + viewSize);
            int minVisibleZ = (int)Math.Floor(View2DPanZ - viewSize);
            int maxVisibleZ = (int)Math.Ceiling(View2DPanZ + viewSize);

            // Collect all number line segments into one batch
            var xAxisLines = new List<float>();
            var zAxisLines = new List<float>();

            // Size of numbers - scale with zoom so they stay readable
            float digitScale = 0.6f / View2DZoom + 0.3f;  // Ranges from ~0.7 at zoom 1.5 to ~0.45 at zoom 4

            // Draw X axis numbers (along the X axis, below Z axis) - RED
            // Numbers should be at grid line positions (integers), not block centers
            float zOffset = IsOddZ ? 2.0f : 1.5f;  // Position below the axis line(s)
            for (int x = ((minVisible / step) - 1) * step; x <= maxVisible; x += step)
            {
                if (x == 0) continue;
                if (x < -halfGrid || x > halfGrid) continue;

                AddVectorNumberLines(xAxisLines, x, y, (float)x, zOffset, digitScale);
            }

            // Draw Z axis numbers (along the Z axis, left of X axis) - BLUE
            float xOffset = IsOddX ? -1.5f : -1.0f;  // Position left of the axis line(s)
            for (int z = ((minVisibleZ / step) - 1) * step; z <= maxVisibleZ; z += step)
            {
                if (z == 0) continue;
                if (z < -halfGrid || z > halfGrid) continue;

                AddVectorNumberLines(zAxisLines, z, y, xOffset, (float)z, digitScale);
            }

            // Now draw all the collected lines
            _planeShader.Use();
            _planeShader.SetMatrix4("view", view);
            _planeShader.SetMatrix4("projection", proj);
            _planeShader.SetMatrix4("model", Matrix4.Identity);
            GL.Disable(EnableCap.DepthTest);
            GL.LineWidth(Math.Min(2f, _maxLineWidth));

            // Draw X axis numbers (red)
            if (xAxisLines.Count > 0)
            {
                float[] lineData = xAxisLines.ToArray();
                int tempVBO = GL.GenBuffer();
                int tempVAO = GL.GenVertexArray();

                GL.BindVertexArray(tempVAO);
                GL.BindBuffer(BufferTarget.ArrayBuffer, tempVBO);
                GL.BufferData(BufferTarget.ArrayBuffer, lineData.Length * sizeof(float), lineData, BufferUsageHint.StreamDraw);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
                GL.EnableVertexAttribArray(0);

                _planeShader.SetVector4("planeColor", new Vector4(0.95f, 0.4f, 0.4f, 1.0f));
                GL.DrawArrays(PrimitiveType.Lines, 0, lineData.Length / 3);

                GL.DeleteBuffer(tempVBO);
                GL.DeleteVertexArray(tempVAO);
            }

            // Draw Z axis numbers (blue)
            if (zAxisLines.Count > 0)
            {
                float[] lineData = zAxisLines.ToArray();
                int tempVBO = GL.GenBuffer();
                int tempVAO = GL.GenVertexArray();

                GL.BindVertexArray(tempVAO);
                GL.BindBuffer(BufferTarget.ArrayBuffer, tempVBO);
                GL.BufferData(BufferTarget.ArrayBuffer, lineData.Length * sizeof(float), lineData, BufferUsageHint.StreamDraw);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
                GL.EnableVertexAttribArray(0);

                _planeShader.SetVector4("planeColor", new Vector4(0.4f, 0.6f, 1.0f, 1.0f));
                GL.DrawArrays(PrimitiveType.Lines, 0, lineData.Length / 3);

                GL.DeleteBuffer(tempVBO);
                GL.DeleteVertexArray(tempVAO);
            }

            GL.LineWidth(1f);
            GL.Enable(EnableCap.DepthTest);
        }

        // Simple 7-segment style digit definitions (as line segments)
        // Each digit is defined as pairs of points (x1,y1, x2,y2) normalized to 0-1 range
        // y=0 is top, y=1 is bottom
        private static readonly Dictionary<char, float[]> _digitSegments = new Dictionary<char, float[]>
        {
            ['0'] = new float[] { 0, 0, 1, 0, 1, 0, 1, 1, 1, 1, 0, 1, 0, 1, 0, 0 },
            ['1'] = new float[] { 0.5f, 0, 0.5f, 1 },
            ['2'] = new float[] { 0, 0, 1, 0, 1, 0, 1, 0.5f, 1, 0.5f, 0, 0.5f, 0, 0.5f, 0, 1, 0, 1, 1, 1 },
            ['3'] = new float[] { 0, 0, 1, 0, 1, 0, 1, 1, 1, 1, 0, 1, 0, 0.5f, 1, 0.5f },
            ['4'] = new float[] { 0, 0, 0, 0.5f, 0, 0.5f, 1, 0.5f, 1, 0, 1, 1 },
            ['5'] = new float[] { 1, 0, 0, 0, 0, 0, 0, 0.5f, 0, 0.5f, 1, 0.5f, 1, 0.5f, 1, 1, 1, 1, 0, 1 },
            ['6'] = new float[] { 1, 0, 0, 0, 0, 0, 0, 1, 0, 1, 1, 1, 1, 1, 1, 0.5f, 1, 0.5f, 0, 0.5f },
            ['7'] = new float[] { 0, 0, 1, 0, 1, 0, 1, 1 },
            ['8'] = new float[] { 0, 0, 1, 0, 1, 0, 1, 1, 1, 1, 0, 1, 0, 1, 0, 0, 0, 0.5f, 1, 0.5f },
            ['9'] = new float[] { 0, 0.5f, 1, 0.5f, 1, 0.5f, 1, 1, 1, 1, 0, 1, 0, 1, 0, 0, 0, 0, 1, 0 },
            ['-'] = new float[] { 0.1f, 0.5f, 0.9f, 0.5f },
        };

        private void AddVectorNumberLines(List<float> lines, int number, float y, float worldX, float worldZ, float scale)
        {
            string text = number.ToString();
            float charWidth = scale * 0.6f;
            float charHeight = scale * 0.9f;
            float spacing = scale * 0.2f;

            // Total width for centering
            float totalWidth = text.Length * charWidth + (text.Length - 1) * spacing;
            float startX = worldX - totalWidth / 2f;

            foreach (char c in text)
            {
                if (!_digitSegments.ContainsKey(c))
                {
                    startX += charWidth + spacing;
                    continue;
                }

                float[] segs = _digitSegments[c];
                for (int i = 0; i < segs.Length; i += 4)
                {
                    // segs are normalized 0-1, where y increases downward in the digit
                    float x1 = startX + segs[i] * charWidth;
                    float z1 = worldZ + segs[i + 1] * charHeight;  // +Z is down on screen
                    float x2 = startX + segs[i + 2] * charWidth;
                    float z2 = worldZ + segs[i + 3] * charHeight;

                    lines.Add(x1); lines.Add(y); lines.Add(z1);
                    lines.Add(x2); lines.Add(y); lines.Add(z2);
                }

                startX += charWidth + spacing;
            }
        }


        private void DrawHighlightBlock(Matrix4 view, Matrix4 proj, Vector3 pos, Color4 fillColor, Color4 outlineColor)
        {
            _blockShader.Use();
            _blockShader.SetMatrix4("model", Matrix4.CreateTranslation(pos));
            _blockShader.SetMatrix4("view", view);
            _blockShader.SetMatrix4("projection", proj);
            _blockShader.SetFloat("ambientStrength", 1f);
            _blockShader.SetInt("enableLighting", 0);
            _blockShader.SetFloat("globalAlpha", 0.5f);

            float[] highlightData = { 0, 0, 0, fillColor.R, fillColor.G, fillColor.B, fillColor.A };
            int tempVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, tempVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, highlightData.Length * sizeof(float), highlightData, BufferUsageHint.StreamDraw);

            GL.BindVertexArray(_cubeVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, tempVBO);
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 28, 0);
            GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, 28, 12);

            GL.Disable(EnableCap.CullFace);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _cubeEBO);
            GL.DrawElements(PrimitiveType.Triangles, 36, DrawElementsType.UnsignedInt, 0);

            _outlineShader.Use();
            _outlineShader.SetMatrix4("model", Matrix4.CreateTranslation(pos));
            _outlineShader.SetMatrix4("view", view);
            _outlineShader.SetMatrix4("projection", proj);
            _outlineShader.SetColor4("outlineColor", outlineColor);
            GL.LineWidth(Math.Min(3f, _maxLineWidth));
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _cubeLineEBO);
            GL.DrawElements(PrimitiveType.Lines, 24, DrawElementsType.UnsignedInt, 0);
            GL.LineWidth(1f);
            GL.Enable(EnableCap.CullFace);

            GL.DeleteBuffer(tempVBO);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVBO);
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 28, 0);
            GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, 28, 12);
        }

        public (Vector3? blockPos, Vector3? faceNormal) RaycastBlock(int mouseX, int mouseY, int screenW, int screenH, VoxelGrid grid)
        {
            if (Is2DMode) return (null, null);

            float ndcX = (2f * mouseX / screenW) - 1f;
            float ndcY = 1f - (2f * mouseY / screenH);

            Matrix4 invProj = Camera.ProjectionMatrix.Inverted();
            Matrix4 invView = Camera.ViewMatrix.Inverted();

            Vector4 rayClip = new Vector4(ndcX, ndcY, -1f, 1f);
            Vector4 rayEye = rayClip * invProj;
            rayEye = new Vector4(rayEye.X, rayEye.Y, -1f, 0f);
            Vector4 rayWorld = rayEye * invView;
            Vector3 rayDir = Vector3.Normalize(new Vector3(rayWorld.X, rayWorld.Y, rayWorld.Z));
            Vector3 rayOrigin = Camera.Position;

            float maxDist = 500f;
            float step = 0.1f;
            Vector3? lastEmpty = null;

            for (float t = 0; t < maxDist; t += step)
            {
                Vector3 pos = rayOrigin + rayDir * t;
                int gx = (int)Math.Floor(pos.X + grid.CenterX);
                int gy = (int)Math.Floor(pos.Y);
                int gz = (int)Math.Floor(pos.Z + grid.CenterZ);

                if (gx >= 0 && gx < grid.SizeX && gy >= 0 && gy < grid.SizeY && gz >= 0 && gz < grid.SizeZ)
                {
                    if (grid.GetBlock(gx, gy, gz) != BlockType.Empty)
                    {
                        if (lastEmpty.HasValue)
                        {
                            Vector3 faceNormal = Vector3.Normalize(lastEmpty.Value - new Vector3(gx - grid.CenterX, gy, gz - grid.CenterZ));
                            return (lastEmpty, faceNormal);
                        }
                        return (new Vector3(gx - grid.CenterX, gy, gz - grid.CenterZ), null);
                    }
                    lastEmpty = new Vector3(gx - grid.CenterX, gy, gz - grid.CenterZ);
                }

                // Check plane intersections
                if (ShowXZPlane && Math.Abs(pos.Y - PlaneY) < 0.5f)
                {
                    int px = (int)Math.Floor(pos.X);
                    int pz = (int)Math.Floor(pos.Z);
                    return (new Vector3(px, PlaneY, pz), Vector3.UnitY);
                }
                if (ShowXYPlane && Math.Abs(pos.Z - PlaneZ) < 0.5f)
                {
                    int px = (int)Math.Floor(pos.X);
                    int py = (int)Math.Floor(pos.Y);
                    if (py >= 0) return (new Vector3(px, py, PlaneZ), -Vector3.UnitZ);
                }
                if (ShowYZPlane && Math.Abs(pos.X - PlaneX) < 0.5f)
                {
                    int py = (int)Math.Floor(pos.Y);
                    int pz = (int)Math.Floor(pos.Z);
                    if (py >= 0) return (new Vector3(PlaneX, py, pz), -Vector3.UnitX);
                }
            }

            return (null, null);
        }

        // 2D mode camera controls
        public void Zoom2D(float delta)
        {
            View2DZoom *= (1f + delta * 0.1f);
            View2DZoom = Math.Clamp(View2DZoom, 0.1f, 10f);
        }

        public void Pan2D(float dx, float dz)
        {
            float scale = 0.5f / View2DZoom;
            View2DPanX -= dx * scale;
            View2DPanZ -= dz * scale;
        }

        public void Reset2DView()
        {
            View2DZoom = 1.0f;
            View2DPanX = 0f;
            View2DPanZ = 0f;
        }

        public void Resize(int w, int h) { GL.Viewport(0, 0, w, h); Camera.AspectRatio = (float)w / h; }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing) { _blockShader?.Dispose(); _gridShader?.Dispose(); _outlineShader?.Dispose(); _planeShader?.Dispose(); }
            if (_cubeVAO != 0) GL.DeleteVertexArray(_cubeVAO);
            if (_cubeVBO != 0) GL.DeleteBuffer(_cubeVBO);
            if (_cubeEBO != 0) GL.DeleteBuffer(_cubeEBO);
            if (_cubeLineEBO != 0) GL.DeleteBuffer(_cubeLineEBO);
            if (_instanceVBO != 0) GL.DeleteBuffer(_instanceVBO);
            if (_gridVAO != 0) GL.DeleteVertexArray(_gridVAO);
            if (_gridVBO != 0) GL.DeleteBuffer(_gridVBO);
            if (_planeVAO != 0) GL.DeleteVertexArray(_planeVAO);
            if (_planeVBO != 0) GL.DeleteBuffer(_planeVBO);
            if (_planeEBO != 0) GL.DeleteBuffer(_planeEBO);
            if (_planeGridVAO != 0) GL.DeleteVertexArray(_planeGridVAO);
            if (_planeGridVBO != 0) GL.DeleteBuffer(_planeGridVBO);
            _disposed = true;
        }
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
    }
}