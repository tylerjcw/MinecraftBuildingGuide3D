using MinecraftBuildingGuide3D.Models;
using MinecraftBuildingGuide3D.Rendering;
using MinecraftBuildingGuide3D.Shapes;
using OpenTK.GLControl;
using OpenTK.Mathematics;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MinecraftBuildingGuide3D
{
    public enum BuildMode { Off, Add, Remove }

    public partial class MainForm : Form
    {
        private GLControl _glControl;
        private GLRenderer _renderer;
        private VoxelGrid _voxelGrid;
        private ShapeInfo _currentShapeInfo;
        private Point _lastMousePos;
        private bool _isLeftDragging, _isRightDragging;
        private BuildMode _buildMode = BuildMode.Off;
        private (int minX, int minY, int minZ, int maxX, int maxY, int maxZ) _currentBounds;

        public MainForm()
        {
            InitializeComponent();
            _voxelGrid = new VoxelGrid(256);
            this.Load += MainForm_Load;
            this.FormClosing += MainForm_FormClosing;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            _glControl = new GLControl() { Dock = DockStyle.Fill };
            _glControl.Load += GlControl_Load;
            _glControl.Paint += GlControl_Paint;
            _glControl.Resize += GlControl_Resize;
            _glControl.MouseDown += GlControl_MouseDown;
            _glControl.MouseUp += GlControl_MouseUp;
            _glControl.MouseMove += GlControl_MouseMove;
            _glControl.MouseWheel += GlControl_MouseWheel;
            _glPanel.Controls.Add(_glControl);
            UpdateParameterFields();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e) => _renderer?.Dispose();

        private void GlControl_Load(object sender, EventArgs e)
        {
            _glControl.MakeCurrent();
            _renderer = new GLRenderer();
            _renderer.Initialize();
            _lblStatus.Text = $"   Left-drag: Rotate  |  Right-drag: Pan  |  Scroll: Zoom  |  Max Line Width: {_renderer.MaxLineWidth:F0}px";
            GenerateCurrentShape();
        }

        private void GlControl_Paint(object sender, PaintEventArgs e)
        {
            _glControl.MakeCurrent();
            _renderer?.Render(_glControl.Width, _glControl.Height);
            _glControl.SwapBuffers();
        }

        private void GlControl_Resize(object sender, EventArgs e)
        {
            if (_glControl.Width > 0 && _glControl.Height > 0)
            {
                _glControl.MakeCurrent();
                _renderer?.Resize(_glControl.Width, _glControl.Height);
                _glControl.Invalidate();
            }
        }

        #region Mouse Handling

        private void GlControl_MouseDown(object sender, MouseEventArgs e)
        {
            _lastMousePos = e.Location;

            if (_buildMode != BuildMode.Off && e.Button == MouseButtons.Left)
            {
                if (_buildMode == BuildMode.Add) PlaceBlock(e.X, e.Y);
                else if (_buildMode == BuildMode.Remove) RemoveBlock(e.X, e.Y);
                return;
            }

            if (e.Button == MouseButtons.Left) _isLeftDragging = true;
            else if (e.Button == MouseButtons.Right) _isRightDragging = true;
            else if (e.Button == MouseButtons.Middle) ResetCameraToShape();
        }

        private void GlControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) _isLeftDragging = false;
            else if (e.Button == MouseButtons.Right) _isRightDragging = false;
        }

        private void GlControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (_renderer == null) return;

            if (_buildMode != BuildMode.Off) UpdateBuildPreview(e.X, e.Y);

            int dx = e.X - _lastMousePos.X, dy = e.Y - _lastMousePos.Y;
            _lastMousePos = e.Location;

            if (_renderer.Is2DMode)
            {
                if (_isLeftDragging || _isRightDragging)
                {
                    _renderer.Pan2D(dx, dy);
                    _glControl.Invalidate();
                }
                return;
            }

            if (_isLeftDragging) { _renderer.Camera.Rotate(-dx, -dy); _glControl.Invalidate(); }
            else if (_isRightDragging) { _renderer.Camera.Pan(-dx, dy); _glControl.Invalidate(); }
        }

        private void GlControl_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_renderer == null) return;

            if (_renderer.Is2DMode)
            {
                _renderer.Zoom2D(e.Delta / 120f);
                _glControl.Invalidate();
                return;
            }

            _renderer.Camera.Zoom(e.Delta / 120f * 5f);
            _glControl.Invalidate();
        }

        #endregion

        #region Building Mode

        private void PlaceBlock(int mouseX, int mouseY)
        {
            if (_renderer.Is2DMode)
            {
                // 2D mode: place at current layer Y
                var gridPos = ScreenToGrid2D(mouseX, mouseY);
                if (gridPos.HasValue)
                {
                    int x = gridPos.Value.x;
                    int y = _renderer.View2DLayer;
                    int z = gridPos.Value.z;

                    if (x >= 0 && x < _voxelGrid.SizeX && y >= 0 && y < _voxelGrid.SizeY && z >= 0 && z < _voxelGrid.SizeZ)
                    {
                        _voxelGrid.SetBlock(x, y, z, BlockType.Solid);
                        _renderer.UpdateFromGrid(_voxelGrid);
                        UpdateShapeInfoOnly();
                        _glControl.Invalidate();
                    }
                }
            }
            else
            {
                // 3D mode: raycast to find placement position
                var (pos, _) = _renderer.RaycastBlock(mouseX, mouseY, _glControl.Width, _glControl.Height, _voxelGrid);
                if (pos.HasValue)
                {
                    int x = (int)pos.Value.X + _voxelGrid.CenterX;
                    int y = (int)pos.Value.Y;
                    int z = (int)pos.Value.Z + _voxelGrid.CenterZ;

                    if (x >= 0 && x < _voxelGrid.SizeX && y >= 0 && y < _voxelGrid.SizeY && z >= 0 && z < _voxelGrid.SizeZ)
                    {
                        _voxelGrid.SetBlock(x, y, z, BlockType.Solid);
                        _renderer.UpdateFromGrid(_voxelGrid);
                        UpdateShapeInfoOnly();
                        _glControl.Invalidate();
                    }
                }
            }
        }

        private void RemoveBlock(int mouseX, int mouseY)
        {
            if (_renderer == null) return;

            if (_renderer.Is2DMode)
            {
                // 2D mode: remove at current layer Y
                var gridPos = ScreenToGrid2D(mouseX, mouseY);
                if (gridPos.HasValue)
                {
                    int x = gridPos.Value.x;
                    int y = _renderer.View2DLayer;
                    int z = gridPos.Value.z;

                    if (x >= 0 && x < _voxelGrid.SizeX && y >= 0 && y < _voxelGrid.SizeY && z >= 0 && z < _voxelGrid.SizeZ)
                    {
                        if (_voxelGrid.GetBlock(x, y, z) != BlockType.Empty)
                        {
                            _voxelGrid.SetBlock(x, y, z, BlockType.Empty);
                            _renderer.UpdateFromGrid(_voxelGrid);
                            UpdateShapeInfoOnly();
                            _glControl.Invalidate();
                        }
                    }
                }
            }
            else
            {
                // 3D mode: raycast to find block to remove
                float ndcX = (2f * mouseX / _glControl.Width) - 1f;
                float ndcY = 1f - (2f * mouseY / _glControl.Height);

                Matrix4 invProj = _renderer.Camera.ProjectionMatrix.Inverted();
                Matrix4 invView = _renderer.Camera.ViewMatrix.Inverted();

                Vector4 rayClip = new Vector4(ndcX, ndcY, -1f, 1f);
                Vector4 rayEye = rayClip * invProj;
                rayEye = new Vector4(rayEye.X, rayEye.Y, -1f, 0f);
                Vector4 rayWorld = rayEye * invView;
                Vector3 rayDir = Vector3.Normalize(new Vector3(rayWorld.X, rayWorld.Y, rayWorld.Z));
                Vector3 rayOrigin = _renderer.Camera.Position;

                for (float t = 0; t < 500f; t += 0.1f)
                {
                    Vector3 pos = rayOrigin + rayDir * t;
                    int gx = (int)Math.Floor(pos.X + _voxelGrid.CenterX);
                    int gy = (int)Math.Floor(pos.Y);
                    int gz = (int)Math.Floor(pos.Z + _voxelGrid.CenterZ);

                    if (gx >= 0 && gx < _voxelGrid.SizeX && gy >= 0 && gy < _voxelGrid.SizeY && gz >= 0 && gz < _voxelGrid.SizeZ)
                    {
                        if (_voxelGrid.GetBlock(gx, gy, gz) != BlockType.Empty)
                        {
                            _voxelGrid.SetBlock(gx, gy, gz, BlockType.Empty);
                            _renderer.UpdateFromGrid(_voxelGrid);
                            UpdateShapeInfoOnly();
                            _glControl.Invalidate();
                            return;
                        }
                    }
                }
            }
        }

        private (int x, int z)? ScreenToGrid2D(int mouseX, int mouseY)
        {
            // Convert screen coordinates to grid coordinates in 2D mode
            float aspect = (float)_glControl.Width / _glControl.Height;

            // Match the renderer's orthographic projection calculation
            float baseSize = Math.Max(_voxelGrid.SizeX, _voxelGrid.SizeZ) * 0.7f;
            float size = baseSize / _renderer.View2DZoom;

            // NDC coordinates
            float ndcX = (2f * mouseX / _glControl.Width) - 1f;
            float ndcY = 1f - (2f * mouseY / _glControl.Height);

            // World coordinates (in 2D top-down view)
            // Orthographic: width = size * aspect, height = size
            // Note: Screen up = -Z world (camera up vector is -UnitZ)
            float worldX = ndcX * (size * aspect / 2f) + _renderer.View2DPanX;
            float worldZ = -ndcY * (size / 2f) + _renderer.View2DPanZ;  // Negated for correct mapping

            // Convert to grid coordinates
            int gx = (int)Math.Floor(worldX + _voxelGrid.CenterX);
            int gz = (int)Math.Floor(worldZ + _voxelGrid.CenterZ);

            return (gx, gz);
        }

        private void UpdateBuildPreview(int mouseX, int mouseY)
        {
            _renderer.HighlightPosition = null;
            _renderer.RemoveHighlightPosition = null;

            if (_renderer.Is2DMode)
            {
                // 2D mode preview
                var gridPos = ScreenToGrid2D(mouseX, mouseY);
                if (gridPos.HasValue)
                {
                    int x = gridPos.Value.x;
                    int y = _renderer.View2DLayer;
                    int z = gridPos.Value.z;

                    if (x >= 0 && x < _voxelGrid.SizeX && y >= 0 && y < _voxelGrid.SizeY && z >= 0 && z < _voxelGrid.SizeZ)
                    {
                        Vector3 worldPos = new Vector3(x - _voxelGrid.CenterX, y, z - _voxelGrid.CenterZ);

                        if (_buildMode == BuildMode.Add)
                        {
                            // Show where block will be placed
                            _renderer.HighlightPosition = worldPos;
                        }
                        else if (_buildMode == BuildMode.Remove)
                        {
                            // Only show if there's a block to remove
                            if (_voxelGrid.GetBlock(x, y, z) != BlockType.Empty)
                                _renderer.RemoveHighlightPosition = worldPos;
                        }
                    }
                }
            }
            else
            {
                // 3D mode preview
                if (_buildMode == BuildMode.Add)
                {
                    var (pos, _) = _renderer.RaycastBlock(mouseX, mouseY, _glControl.Width, _glControl.Height, _voxelGrid);
                    _renderer.HighlightPosition = pos;
                }
                else if (_buildMode == BuildMode.Remove)
                {
                    // Raycast to find block under cursor for removal preview
                    var removePos = RaycastFirstBlock(mouseX, mouseY);
                    _renderer.RemoveHighlightPosition = removePos;
                }
            }
            _glControl.Invalidate();
        }

        private Vector3? RaycastFirstBlock(int mouseX, int mouseY)
        {
            if (_renderer == null) return null;

            float ndcX = (2f * mouseX / _glControl.Width) - 1f;
            float ndcY = 1f - (2f * mouseY / _glControl.Height);

            Matrix4 invProj = _renderer.Camera.ProjectionMatrix.Inverted();
            Matrix4 invView = _renderer.Camera.ViewMatrix.Inverted();

            Vector4 rayClip = new Vector4(ndcX, ndcY, -1f, 1f);
            Vector4 rayEye = rayClip * invProj;
            rayEye = new Vector4(rayEye.X, rayEye.Y, -1f, 0f);
            Vector4 rayWorld = rayEye * invView;
            Vector3 rayDir = Vector3.Normalize(new Vector3(rayWorld.X, rayWorld.Y, rayWorld.Z));
            Vector3 rayOrigin = _renderer.Camera.Position;

            for (float t = 0; t < 500f; t += 0.1f)
            {
                Vector3 pos = rayOrigin + rayDir * t;
                int gx = (int)Math.Floor(pos.X + _voxelGrid.CenterX);
                int gy = (int)Math.Floor(pos.Y);
                int gz = (int)Math.Floor(pos.Z + _voxelGrid.CenterZ);

                if (gx >= 0 && gx < _voxelGrid.SizeX && gy >= 0 && gy < _voxelGrid.SizeY && gz >= 0 && gz < _voxelGrid.SizeZ)
                {
                    if (_voxelGrid.GetBlock(gx, gy, gz) != BlockType.Empty)
                    {
                        return new Vector3(gx - _voxelGrid.CenterX, gy, gz - _voxelGrid.CenterZ);
                    }
                }
            }
            return null;
        }

        private void RbBuildMode_CheckedChanged(object sender, EventArgs e)
        {
            if (_rbBuildOff.Checked) _buildMode = BuildMode.Off;
            else if (_rbBuildAdd.Checked) _buildMode = BuildMode.Add;
            else if (_rbBuildRemove.Checked) _buildMode = BuildMode.Remove;

            if (_renderer != null)
            {
                _renderer.BuildingMode = (_buildMode != BuildMode.Off);
                _renderer.HighlightPosition = null;
                _renderer.RemoveHighlightPosition = null;
                _glControl?.Invalidate();
                UpdateStatusBar();
            }
        }

        private void ChkPlane_CheckedChanged(object sender, EventArgs e)
        {
            if (_renderer == null || _syncingPlanes) return;
            _syncingPlanes = true;

            _renderer.ShowXZPlane = _chkPlaneXZ.Checked;
            _renderer.ShowXYPlane = _chkPlaneXY.Checked;
            _renderer.ShowYZPlane = _chkPlaneYZ.Checked;

            // Sync to View tab
            _chkPlaneXZView.Checked = _chkPlaneXZ.Checked;
            _chkPlaneXYView.Checked = _chkPlaneXY.Checked;
            _chkPlaneYZView.Checked = _chkPlaneYZ.Checked;

            _syncingPlanes = false;
            _glControl?.Invalidate();
        }

        private void ChkPlaneView_CheckedChanged(object sender, EventArgs e)
        {
            if (_renderer == null || _syncingPlanes) return;
            _syncingPlanes = true;

            _renderer.ShowXZPlane = _chkPlaneXZView.Checked;
            _renderer.ShowXYPlane = _chkPlaneXYView.Checked;
            _renderer.ShowYZPlane = _chkPlaneYZView.Checked;

            // Sync to Build tab
            _chkPlaneXZ.Checked = _chkPlaneXZView.Checked;
            _chkPlaneXY.Checked = _chkPlaneXYView.Checked;
            _chkPlaneYZ.Checked = _chkPlaneYZView.Checked;

            _syncingPlanes = false;
            _glControl?.Invalidate();
        }

        private void SyncPlaneY()
        {
            if (_renderer == null || _syncingPlanes) return;
            _syncingPlanes = true;
            _renderer.PlaneY = (int)_numPlaneYView.Value;
            _numPlaneY.Value = _numPlaneYView.Value;
            _syncingPlanes = false;
            _glControl?.Invalidate();
        }

        private void SyncPlaneZ()
        {
            if (_renderer == null || _syncingPlanes) return;
            _syncingPlanes = true;
            _renderer.PlaneZ = (int)_numPlaneZView.Value;
            _numPlaneZ.Value = _numPlaneZView.Value;
            _syncingPlanes = false;
            _glControl?.Invalidate();
        }

        private void SyncPlaneX()
        {
            if (_renderer == null || _syncingPlanes) return;
            _syncingPlanes = true;
            _renderer.PlaneX = (int)_numPlaneXView.Value;
            _numPlaneX.Value = _numPlaneXView.Value;
            _syncingPlanes = false;
            _glControl?.Invalidate();
        }

        private void UpdateStatusBar()
        {
            if (_renderer == null) return;

            if (_renderer.Is2DMode)
            {
                if (_buildMode == BuildMode.Add)
                    _lblStatus.Text = $"   2D ADD MODE: Left-click to place block at Y={_renderer.View2DLayer}  |  Drag to pan  |  Scroll to zoom";
                else if (_buildMode == BuildMode.Remove)
                    _lblStatus.Text = $"   2D REMOVE MODE: Left-click to remove block at Y={_renderer.View2DLayer}  |  Drag to pan  |  Scroll to zoom";
                else
                    _lblStatus.Text = "   2D MODE: Drag to pan  |  Scroll to zoom  |  Use Layer control to change Y level";
            }
            else if (_buildMode == BuildMode.Add)
                _lblStatus.Text = "   ADD MODE: Left-click to place block  |  Camera: Left-drag rotate, Right-drag pan, Scroll zoom";
            else if (_buildMode == BuildMode.Remove)
                _lblStatus.Text = "   REMOVE MODE: Left-click to remove block  |  Camera: Left-drag rotate, Right-drag pan, Scroll zoom";
            else
                _lblStatus.Text = $"   Left-drag: Rotate  |  Right-drag: Pan  |  Scroll: Zoom  |  Max Line Width: {_renderer?.MaxLineWidth:F0}px";
        }

        #endregion

        #region Camera Controls

        private void ResetCameraToShape()
        {
            if (_renderer == null) return;
            FocusCameraOnBounds(_currentBounds);
            _glControl?.Invalidate();
        }

        private void SetCameraView(CameraView view)
        {
            if (_renderer == null) return;
            _renderer.Camera.SetView(view);
            var bounds = _currentBounds;
            float centerX = (bounds.minX + bounds.maxX) / 2f - _voxelGrid.CenterX;
            float centerY = (bounds.minY + bounds.maxY) / 2f;
            float centerZ = (bounds.minZ + bounds.maxZ) / 2f - _voxelGrid.CenterZ;
            _renderer.Camera.Target = new Vector3(centerX, centerY, centerZ);
            _glControl?.Invalidate();
        }

        private void FocusCameraOnBounds((int minX, int minY, int minZ, int maxX, int maxY, int maxZ) bounds)
        {
            if (_renderer == null) return;

            float centerX = (bounds.minX + bounds.maxX) / 2f - _voxelGrid.CenterX;
            float centerY = (bounds.minY + bounds.maxY) / 2f;
            float centerZ = (bounds.minZ + bounds.maxZ) / 2f - _voxelGrid.CenterZ;

            float sizeX = bounds.maxX - bounds.minX + 1;
            float sizeY = bounds.maxY - bounds.minY + 1;
            float sizeZ = bounds.maxZ - bounds.minZ + 1;
            float maxSize = Math.Max(sizeX, Math.Max(sizeY, sizeZ));

            _renderer.Camera.Target = new Vector3(centerX, centerY, centerZ);

            float fovRad = MathHelper.DegreesToRadians(_renderer.Camera.FieldOfView);
            float distance = (maxSize * 1.2f) / (2f * MathF.Tan(fovRad / 2f));
            distance = Math.Max(distance, maxSize * 1.5f);
            _renderer.Camera.Distance = Math.Clamp(distance, _renderer.Camera.MinDistance, _renderer.Camera.MaxDistance);

            _renderer.Camera.Yaw = 45f;
            _renderer.Camera.Pitch = 30f;
        }

        #endregion

        #region View Controls

        private void TrkLayer_ValueChanged(object sender, EventArgs e)
        {
            if (_renderer == null) return;
            if (_renderer.Is2DMode) _renderer.View2DLayer = _trkLayer.Value;
            else _renderer.MaxVisibleLayer = _trkLayer.Value;
            _renderer.UpdateFromGrid(_voxelGrid);
            UpdateStatusBar();
            _glControl?.Invalidate();
        }

        private void ChkAllLayers_CheckedChanged(object sender, EventArgs e)
        {
            if (_renderer == null) return;
            _renderer.ShowAllLayers = _chkAllLayers.Checked;
            _trkLayer.Enabled = !_chkAllLayers.Checked || _renderer.Is2DMode;
            _numLayer.Enabled = _trkLayer.Enabled;
            _renderer.UpdateFromGrid(_voxelGrid);
            _glControl?.Invalidate();
        }

        private void Chk2D_CheckedChanged(object sender, EventArgs e)
        {
            if (_renderer == null) return;
            _renderer.Is2DMode = _chk2D.Checked;
            _trkLayer.Enabled = _chk2D.Checked || !_chkAllLayers.Checked;
            _numLayer.Enabled = _trkLayer.Enabled;

            if (_chk2D.Checked)
            {
                _renderer.Reset2DView();
                _renderer.View2DLayer = _trkLayer.Value;  // Sync layer with slider
            }

            UpdateStatusBar();
            _renderer.UpdateFromGrid(_voxelGrid);
            _glControl?.Invalidate();
        }

        #endregion

        #region Shape Generation

        private void BtnGenerate_Click(object sender, EventArgs e) => GenerateCurrentShape();

        private void UpdateParameterFields()
        {
            string shape = _cmbShape.SelectedItem?.ToString() ?? "Sphere";
            HideAllExtraParams();
            _cmbArchStyle.Visible = false;

            switch (shape)
            {
                case "Sphere":
                    _lblP1.Text = "Radius:"; _numP1.Value = 20;
                    break;

                case "Dome":
                    _lblP1.Text = "Radius:"; _lblP2.Text = "Height %:"; _lblP3.Text = "Base %:";
                    _numP1.Value = 20; _numP2.Value = 100; _numP3.Value = 100;
                    _numP2.Visible = _lblP2.Visible = _numP3.Visible = _lblP3.Visible = true;
                    break;

                case "Onion Dome":
                    _lblP1.Text = "Radius:"; _lblP2.Text = "Height:";
                    _numP1.Value = 12; _numP2.Value = 30;
                    _numP2.Visible = _lblP2.Visible = true;
                    break;

                case "Wizard Tower Roof":
                    _lblP1.Text = "Radius:"; _lblP2.Text = "Height:";
                    _numP1.Value = 10; _numP2.Value = 35;
                    _numP2.Visible = _lblP2.Visible = true;
                    break;

                case "Cylinder":
                    _lblP1.Text = "Radius:"; _lblP2.Text = "Height:";
                    _numP1.Value = 15; _numP2.Value = 40;
                    _numP2.Visible = _lblP2.Visible = true;
                    break;

                case "Cone":
                    _lblP1.Text = "Radius:"; _lblP2.Text = "Height:"; _lblP3.Text = "Step H:";
                    _numP1.Value = 15; _numP2.Value = 40; _numP3.Value = 1;
                    _numP2.Visible = _lblP2.Visible = _numP3.Visible = _lblP3.Visible = true;
                    break;

                case "Pyramid":
                    _lblP1.Text = "Base:"; _lblP2.Text = "Height:"; _lblP3.Text = "Step H:";
                    _numP1.Value = 30; _numP2.Value = 25; _numP3.Value = 1;
                    _numP2.Visible = _lblP2.Visible = _numP3.Visible = _lblP3.Visible = true;
                    break;

                case "Torus":
                    _lblP1.Text = "Major R:"; _lblP2.Text = "Minor R:";
                    _numP1.Value = 20; _numP2.Value = 6;
                    _numP2.Visible = _lblP2.Visible = true;
                    break;

                case "Ellipsoid":
                    _lblP1.Text = "X Rad:"; _lblP2.Text = "Y Rad:"; _lblP3.Text = "Z Rad:";
                    _numP1.Value = 25; _numP2.Value = 15; _numP3.Value = 20;
                    _numP2.Visible = _numP3.Visible = _lblP2.Visible = _lblP3.Visible = true;
                    break;

                case "Cuboid":
                    _lblP1.Text = "Width:"; _lblP2.Text = "Height:"; _lblP3.Text = "Depth:";
                    _numP1.Value = 30; _numP2.Value = 20; _numP3.Value = 30;
                    _numP2.Visible = _numP3.Visible = _lblP2.Visible = _lblP3.Visible = true;
                    break;

                case "Arch":
                    _lblP1.Text = "Width:"; _lblP2.Text = "Height:"; _lblP3.Text = "Depth:";
                    _numP1.Value = 24; _numP2.Value = 30; _numP3.Value = 6;
                    _numP2.Visible = _numP3.Visible = _lblP2.Visible = _lblP3.Visible = true;
                    _cmbArchStyle.Visible = true;
                    break;

                case "Spiral Staircase":
                    _lblP1.Text = "Radius:"; _lblP2.Text = "Height:"; _lblP3.Text = "Steps/R:"; _lblP4.Text = "Pillar:";
                    _numP1.Value = 10; _numP2.Value = 48; _numP3.Value = 16; _numP4.Value = 1;
                    _numP2.Visible = _numP3.Visible = _numP4.Visible = true;
                    _lblP2.Visible = _lblP3.Visible = _lblP4.Visible = true;
                    break;

                case "Helix":
                    _lblP1.Text = "Radius:"; _lblP2.Text = "Height:"; _lblP3.Text = "Rots:"; _lblP4.Text = "Strands:";
                    _numP1.Value = 15; _numP2.Value = 60; _numP3.Value = 4; _numP4.Value = 1;
                    _numP4.Maximum = 8;
                    _numP2.Visible = _numP3.Visible = _numP4.Visible = true;
                    _lblP2.Visible = _lblP3.Visible = _lblP4.Visible = true;
                    break;

                case "Bridge":
                    _lblP1.Text = "Length:"; _lblP2.Text = "Height:"; _lblP3.Text = "Width:";
                    _lblP4.Text = "Arches:"; _lblP5.Text = "Levels:";
                    _numP1.Value = 60; _numP2.Value = 25; _numP3.Value = 5; _numP4.Value = 4; _numP5.Value = 2;
                    _numP4.Maximum = 10; _numP5.Maximum = 4;
                    _numP2.Visible = _numP3.Visible = _numP4.Visible = _numP5.Visible = true;
                    _lblP2.Visible = _lblP3.Visible = _lblP4.Visible = _lblP5.Visible = true;
                    _cmbArchStyle.Visible = true;
                    break;

                case "Norman Wall":
                    _lblP1.Text = "Length:"; _lblP2.Text = "Height:"; _lblP3.Text = "Thick:"; _lblP4.Text = "Crenel:";
                    _numP1.Value = 60; _numP2.Value = 20; _numP3.Value = 3; _numP4.Value = 2;
                    _numP2.Visible = _numP3.Visible = _numP4.Visible = true;
                    _lblP2.Visible = _lblP3.Visible = _lblP4.Visible = true;
                    break;

                case "Norman Tower":
                    _lblP1.Text = "Radius:"; _lblP2.Text = "Height:"; _lblP3.Text = "Floors:";
                    _numP1.Value = 12; _numP2.Value = 40; _numP3.Value = 4;
                    _numP2.Visible = _numP3.Visible = _lblP2.Visible = _lblP3.Visible = true;
                    break;

                case "Paraboloid":
                    _lblP1.Text = "Radius:"; _lblP2.Text = "Height:";
                    _numP1.Value = 18; _numP2.Value = 30;
                    _numP2.Visible = _lblP2.Visible = true;
                    break;

                case "Hyperboloid":
                    _lblP1.Text = "Top R:"; _lblP2.Text = "Waist:"; _lblP3.Text = "Height:";
                    _numP1.Value = 18; _numP2.Value = 10; _numP3.Value = 40;
                    _numP2.Visible = _numP3.Visible = _lblP2.Visible = _lblP3.Visible = true;
                    break;

                case "Point-to-Point Line":
                    _lblP1.Text = "Width:"; _lblP2.Text = "Height:";
                    _numP1.Value = 1; _numP1.Maximum = 20;
                    _numP2.Value = 1; _numP2.Maximum = 50;
                    _numP2.Visible = _lblP2.Visible = true;
                    _lblPoints.Visible = _txtPoints.Visible = _chkClosedLoop.Visible = true;
                    _lblPreset.Visible = _cmbPreset.Visible = true;
                    _cmbPreset.SelectedIndex = 0;
                    if (string.IsNullOrWhiteSpace(_txtPoints.Text))
                        _txtPoints.Text = "0, 0, 0\n10, 5, 0\n20, 0, 10\n10, 5, 20\n0, 0, 10";
                    break;

                case "Spline Curve":
                    _lblP1.Text = "Width:"; _lblP2.Text = "Height:"; _lblP3.Text = "Smooth:";
                    _numP1.Value = 1; _numP1.Maximum = 20;
                    _numP2.Value = 1; _numP2.Maximum = 50;
                    _numP3.Value = 10; _numP3.Maximum = 30; _numP3.Minimum = 2;
                    _numP2.Visible = _lblP2.Visible = _numP3.Visible = _lblP3.Visible = true;
                    _lblPoints.Visible = _txtPoints.Visible = _chkClosedLoop.Visible = true;
                    _lblPreset.Visible = _cmbPreset.Visible = true;
                    _cmbPreset.SelectedIndex = 0;
                    if (string.IsNullOrWhiteSpace(_txtPoints.Text))
                        _txtPoints.Text = "0, 0, 0\n10, 10, 0\n20, 0, 10\n10, -10, 20\n0, 0, 10";
                    break;
            }
        }

        private void CmbPreset_SelectedIndexChanged(object sender, EventArgs e)
        {
            string preset = _cmbPreset.SelectedItem?.ToString() ?? "Custom";
            if (preset == "Custom") return;

            int radius = 15;  // Default size for presets
            int points = 16;  // Number of points for curved shapes
            var sb = new System.Text.StringBuilder();

            switch (preset)
            {
                case "Circle":
                    _chkClosedLoop.Checked = true;
                    for (int i = 0; i < points; i++)
                    {
                        double angle = 2 * Math.PI * i / points;
                        int x = (int)Math.Round(radius * Math.Cos(angle));
                        int z = (int)Math.Round(radius * Math.Sin(angle));
                        sb.AppendLine($"{x}, 0, {z}");
                    }
                    break;

                case "Square":
                    _chkClosedLoop.Checked = true;
                    sb.AppendLine($"-{radius}, 0, -{radius}");
                    sb.AppendLine($"{radius}, 0, -{radius}");
                    sb.AppendLine($"{radius}, 0, {radius}");
                    sb.AppendLine($"-{radius}, 0, {radius}");
                    break;

                case "Triangle":
                    _chkClosedLoop.Checked = true;
                    sb.AppendLine($"0, 0, -{radius}");
                    sb.AppendLine($"{(int)(radius * 0.866)}, 0, {radius / 2}");
                    sb.AppendLine($"-{(int)(radius * 0.866)}, 0, {radius / 2}");
                    break;

                case "Star":
                    _chkClosedLoop.Checked = true;
                    int innerR = radius / 2;
                    for (int i = 0; i < 10; i++)
                    {
                        double angle = Math.PI / 2 + 2 * Math.PI * i / 10;
                        int r = (i % 2 == 0) ? radius : innerR;
                        int x = (int)Math.Round(r * Math.Cos(angle));
                        int z = (int)Math.Round(r * Math.Sin(angle));
                        sb.AppendLine($"{x}, 0, {z}");
                    }
                    break;

                case "Spiral":
                    _chkClosedLoop.Checked = false;
                    int spiralPoints = 32;
                    double rotations = 2.5;
                    for (int i = 0; i < spiralPoints; i++)
                    {
                        double t = (double)i / (spiralPoints - 1);
                        double angle = 2 * Math.PI * rotations * t;
                        double r = radius * t;
                        int x = (int)Math.Round(r * Math.Cos(angle));
                        int z = (int)Math.Round(r * Math.Sin(angle));
                        int y = (int)(t * radius);  // Rise as it spirals
                        sb.AppendLine($"{x}, {y}, {z}");
                    }
                    break;

                case "Wave":
                    _chkClosedLoop.Checked = false;
                    int wavePoints = 20;
                    for (int i = 0; i < wavePoints; i++)
                    {
                        int x = -radius + (2 * radius * i / (wavePoints - 1));
                        int y = (int)(8 * Math.Sin(2 * Math.PI * i / (wavePoints / 2)));
                        sb.AppendLine($"{x}, {y}, 0");
                    }
                    break;

                case "Zigzag":
                    _chkClosedLoop.Checked = false;
                    int zigPoints = 8;
                    for (int i = 0; i < zigPoints; i++)
                    {
                        int x = -radius + (2 * radius * i / (zigPoints - 1));
                        int z = (i % 2 == 0) ? -5 : 5;
                        sb.AppendLine($"{x}, 0, {z}");
                    }
                    break;

                case "Heart":
                    _chkClosedLoop.Checked = true;
                    int heartPoints = 24;
                    for (int i = 0; i < heartPoints; i++)
                    {
                        double t = 2 * Math.PI * i / heartPoints;
                        // Heart curve parametric equations
                        double x = 16 * Math.Pow(Math.Sin(t), 3);
                        double z = 13 * Math.Cos(t) - 5 * Math.Cos(2 * t) - 2 * Math.Cos(3 * t) - Math.Cos(4 * t);
                        sb.AppendLine($"{(int)(x * radius / 16)}, 0, {(int)(-z * radius / 16)}");
                    }
                    break;

                case "Figure-8":
                    _chkClosedLoop.Checked = true;
                    int f8Points = 24;
                    for (int i = 0; i < f8Points; i++)
                    {
                        double t = 2 * Math.PI * i / f8Points;
                        // Lemniscate of Bernoulli
                        double denom = 1 + Math.Sin(t) * Math.Sin(t);
                        double x = radius * Math.Cos(t) / denom;
                        double z = radius * Math.Sin(t) * Math.Cos(t) / denom;
                        sb.AppendLine($"{(int)x}, 0, {(int)z}");
                    }
                    break;
            }

            _txtPoints.Text = sb.ToString().TrimEnd();
        }

        private void GenerateCurrentShape()
        {
            if (_renderer == null) return;

            string shape = _cmbShape.SelectedItem?.ToString() ?? "Sphere";
            bool hollow = _chkHollow.Checked;
            int thickness = (int)_numThk.Value;
            int p1 = (int)_numP1.Value, p2 = (int)_numP2.Value;
            int p3 = (int)_numP3.Value, p4 = (int)_numP4.Value;
            int p5 = (int)_numP5.Value, p6 = (int)_numP6.Value;

            EdgeMode edgeMode = _cmbEdge.SelectedIndex switch
            {
                1 => EdgeMode.HeavyInside,
                2 => EdgeMode.HeavyOutside,
                _ => EdgeMode.Normal
            };

            ArchStyle archStyle = _cmbArchStyle.SelectedIndex switch
            {
                1 => ArchStyle.Pointed,
                2 => ArchStyle.Segmental,
                _ => ArchStyle.Round
            };

            int maxDim = Math.Max(p1, Math.Max(p2, p3)) * 3 + 20;
            if (_voxelGrid.SizeX < maxDim)
                _voxelGrid = new VoxelGrid(Math.Min(maxDim, 512));

            _currentShapeInfo = shape switch
            {
                "Sphere" => ShapeGenerator.GenerateSphere(_voxelGrid, p1, hollow, thickness),
                "Dome" => ShapeGenerator.GenerateDome(_voxelGrid, p1, hollow, thickness, p2 / 100.0, p3 / 100.0),
                "Onion Dome" => ShapeGenerator.GenerateOnionDome(_voxelGrid, p1, p2, thickness),
                "Wizard Tower Roof" => ShapeGenerator.GenerateWizardTowerRoof(_voxelGrid, p1, p2, thickness),
                "Cylinder" => ShapeGenerator.GenerateCylinder(_voxelGrid, p1, p2, hollow, thickness, edgeMode),
                "Cone" => ShapeGenerator.GenerateCone(_voxelGrid, p1, p2, hollow, thickness, p3),
                "Pyramid" => ShapeGenerator.GeneratePyramid(_voxelGrid, p1, p2, hollow, thickness, p3),
                "Torus" => ShapeGenerator.GenerateTorus(_voxelGrid, p1, p2),
                "Ellipsoid" => ShapeGenerator.GenerateEllipsoid(_voxelGrid, p1, p2, p3, hollow, thickness),
                "Cuboid" => ShapeGenerator.GenerateCuboid(_voxelGrid, p1, p2, p3, hollow, thickness),
                "Arch" => ShapeGenerator.GenerateArch(_voxelGrid, p1, p2, p3, thickness, archStyle),
                "Spiral Staircase" => ShapeGenerator.GenerateSpiralStaircase(_voxelGrid, p1, p2, p3, p4),
                "Helix" => ShapeGenerator.GenerateHelix(_voxelGrid, p1, p2, thickness, p3, p4),
                "Bridge" => ShapeGenerator.GenerateBridge(_voxelGrid, p1, p2, p3, p4, p5, archStyle),
                "Norman Wall" => ShapeGenerator.GenerateWall(_voxelGrid, p1, p2, p3, p4, 2, 3, true, 8),
                "Norman Tower" => ShapeGenerator.GenerateTower(_voxelGrid, p1, p2, p3, true, edgeMode),
                "Paraboloid" => ShapeGenerator.GenerateParaboloid(_voxelGrid, p1, p2, hollow, thickness),
                "Hyperboloid" => ShapeGenerator.GenerateHyperboloid(_voxelGrid, p1, p2, p3, hollow, thickness),
                "Point-to-Point Line" => ShapeGenerator.GeneratePointToPointLine(_voxelGrid,
                    ShapeGenerator.ParsePoints(_txtPoints.Text), p1, p2, _chkClosedLoop.Checked),
                "Spline Curve" => ShapeGenerator.GenerateSplineLine(_voxelGrid,
                    ShapeGenerator.ParsePoints(_txtPoints.Text), p1, p2, _chkClosedLoop.Checked, p3),
                _ => new ShapeInfo("Unknown", new())
            };

            _currentBounds = _voxelGrid.GetBoundingBox();

            // Set layer slider range to the shape's Y bounds
            _trkLayer.Minimum = _currentBounds.minY;
            _trkLayer.Maximum = Math.Max(_currentBounds.minY + 1, _currentBounds.maxY);
            _numLayer.Minimum = _trkLayer.Minimum;
            _numLayer.Maximum = _trkLayer.Maximum;

            // Set layer to middle of shape
            int midY = (_currentBounds.minY + _currentBounds.maxY) / 2;
            _trkLayer.Value = Math.Max(_trkLayer.Minimum, Math.Min(_trkLayer.Maximum, midY));
            _numLayer.Value = _trkLayer.Value;
            if (_renderer.Is2DMode) _renderer.View2DLayer = _trkLayer.Value;

            // Set shape dimensions for axis display (odd/even detection)
            _renderer.ShapeSizeX = _currentBounds.maxX - _currentBounds.minX + 1;
            _renderer.ShapeSizeY = _currentBounds.maxY - _currentBounds.minY + 1;
            _renderer.ShapeSizeZ = _currentBounds.maxZ - _currentBounds.minZ + 1;

            if (_chkAutoZoom.Checked) FocusCameraOnBounds(_currentBounds);

            UpdateShapeInfoDisplay();
            _renderer.UpdateFromGrid(_voxelGrid);
            _glControl?.Invalidate();
        }

        private void UpdateShapeInfoOnly()
        {
            _currentBounds = _voxelGrid.GetBoundingBox();

            // Update shape dimensions for axis display
            _renderer.ShapeSizeX = _currentBounds.maxX - _currentBounds.minX + 1;
            _renderer.ShapeSizeY = _currentBounds.maxY - _currentBounds.minY + 1;
            _renderer.ShapeSizeZ = _currentBounds.maxZ - _currentBounds.minZ + 1;

            UpdateShapeInfoDisplay();
        }

        private void UpdateShapeInfoDisplay()
        {
            var bounds = _currentBounds;
            int lenX = bounds.maxX - bounds.minX + 1;
            int lenY = bounds.maxY - bounds.minY + 1;
            int lenZ = bounds.maxZ - bounds.minZ + 1;

            var sb = new System.Text.StringBuilder();

            if (_currentShapeInfo != null)
            {
                sb.AppendLine($"Shape: {_currentShapeInfo.Name}");
                foreach (var kvp in _currentShapeInfo.Properties)
                    sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
            }

            sb.AppendLine();
            sb.AppendLine($"Size: {lenX} × {lenZ} × {lenY}  (L×W×H)");
            sb.AppendLine($"Blocks: {_voxelGrid.BlockCount:N0}");
            sb.AppendLine();

            // Minecraft-specific info
            int stacks = _voxelGrid.BlockCount / 64;
            int remainder = _voxelGrid.BlockCount % 64;
            sb.AppendLine($"Stacks: {stacks} + {remainder}");
            sb.AppendLine($"Shulker Boxes: {(stacks + 26) / 27}");

            _lblInfo.Text = sb.ToString();
        }

        #endregion

        #region Dialogs

        private void ShowHelpDialog()
        {
            var dialog = new Form
            {
                Text = "Minecraft Building Guide 3D - Help Documentation",
                Size = new Size(1200, 850),
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.FromArgb(26, 26, 36),
                Icon = this.Icon
            };

            var browser = new WebBrowser
            {
                Dock = DockStyle.Fill,
                IsWebBrowserContextMenuEnabled = false,
                WebBrowserShortcutsEnabled = false
            };

            // Try to load from file, fall back to embedded HTML
            string helpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Help.html");
            if (File.Exists(helpPath))
            {
                browser.Navigate(helpPath);
            }
            else
            {
                // Generate help HTML inline if file not found
                browser.DocumentText = GenerateHelpHtml();
            }

            dialog.Controls.Add(browser);
            dialog.Show();
        }

        private string GenerateHelpHtml()
        {
            return @"<!DOCTYPE html>
<html><head><meta charset='UTF-8'>
<style>
body { font-family: 'Segoe UI', sans-serif; background: #1a1a24; color: #e0e0e8; padding: 40px; line-height: 1.6; }
h1 { color: #4a90d9; border-bottom: 2px solid #4a90d9; padding-bottom: 10px; }
h2 { color: #5cb85c; margin-top: 30px; }
h3 { color: #f0ad4e; }
table { width: 100%; border-collapse: collapse; margin: 15px 0; background: #22222e; }
th, td { padding: 10px 15px; text-align: left; border-bottom: 1px solid #3a3a4a; }
th { background: #2a2a38; color: #4a90d9; }
.tip { background: #2a2a38; border-left: 4px solid #5cb85c; padding: 15px; margin: 15px 0; }
code { background: #2a2a38; padding: 2px 6px; color: #f0ad4e; }
kbd { background: #2a2a38; border: 1px solid #3a3a4a; padding: 2px 6px; border-radius: 3px; }
</style></head><body>
<h1>Minecraft Building Guide 3D</h1>
<p style='color:#a0a0b0'>Version 0.5.0 (Pre-Alpha) | Developer: Komrad Toast</p>

<h2>Quick Start</h2>
<ol>
<li>Select a shape from the dropdown</li>
<li>Adjust parameters (radius, height, etc.)</li>
<li>Click Generate</li>
<li>Use 2D Layer View for building</li>
</ol>

<h2>Camera Controls</h2>
<table>
<tr><th>Action</th><th>Control</th></tr>
<tr><td>Rotate</td><td><kbd>Left Mouse</kbd> + Drag</td></tr>
<tr><td>Pan</td><td><kbd>Right Mouse</kbd> + Drag</td></tr>
<tr><td>Zoom</td><td><kbd>Scroll Wheel</kbd></td></tr>
<tr><td>Reset</td><td><kbd>Middle Click</kbd></td></tr>
</table>

<h2>2D Layer View</h2>
<p>Enable in View tab. Use Layer slider to view each Y level for building in Minecraft.</p>
<table>
<tr><th>Action</th><th>Control</th></tr>
<tr><td>Pan</td><td>Drag (left or right mouse)</td></tr>
<tr><td>Zoom</td><td>Scroll wheel</td></tr>
</table>

<h2>Building Mode</h2>
<ul>
<li><strong style='color:#5cb85c'>Add Mode:</strong> Left-click to place blocks</li>
<li><strong style='color:#d9534f'>Remove Mode:</strong> Left-click to delete blocks</li>
</ul>
<p>Enable Guide Planes to build in empty space. Planes only show when camera is on the correct side.</p>

<h2>Shapes Available</h2>
<p><strong>Basic:</strong> Sphere, Cylinder, Cone, Pyramid, Cuboid, Ellipsoid, Torus</p>
<p><strong>Domes:</strong> Dome (adjustable), Onion Dome, Wizard Tower Roof</p>
<p><strong>Architecture:</strong> Arch, Bridge (Aqueduct), Norman Wall, Norman Tower</p>
<p><strong>Complex:</strong> Paraboloid, Hyperboloid, Spiral Staircase, Helix</p>

<h2>Shape Info</h2>
<ul>
<li><strong>Size (L×W×H):</strong> Minecraft coordinate dimensions</li>
<li><strong>Blocks:</strong> Total block count</li>
<li><strong>Stacks:</strong> Number of 64-block stacks</li>
<li><strong>Shulker Boxes:</strong> Storage needed (27 stacks each)</li>
</ul>

<div class='tip'>
<strong>💡 Tip:</strong> Use the Stacks count to gather materials before building!
</div>

<h2>Options</h2>
<p><strong>Hollow:</strong> Creates shell instead of solid. Adjust Wall thickness.</p>
<p><strong>Edge Mode:</strong> Heavy Inside/Outside fills diagonal gaps in circles.</p>
<p><strong>Arch Style:</strong> Round (Roman), Pointed (Gothic), Segmental (flat)</p>
<p><strong>Step Height:</strong> For cones/pyramids - controls terrace height.</p>

<footer style='margin-top:40px; color:#666; font-size:12px'>
© 2026 Komrad Toast | MIT License | Not affiliated with Mojang
</footer>
</body></html>";
        }

        private void ShowAboutDialog()
        {
            string aboutText = @"
  Minecraft Building Guide 3D
  ════════════════════════════
  
  Version: 0.5.0 (Pre-Alpha)
  
  A 3D voxel shape generator for
  planning Minecraft builds.
  
  ────────────────────────────
  
  Developer: Komrad Toast
  
  Source: GitHub (coming soon)
  
  ────────────────────────────
  
  MIT License
  Copyright (c) 2026 Komrad Toast
  
  Permission is hereby granted,
  free of charge, to use, copy,
  modify, merge, publish, and
  distribute this software.
  
  THE SOFTWARE IS PROVIDED
  ""AS IS"", WITHOUT WARRANTY
  OF ANY KIND.
";
            ShowTextDialog("About", aboutText, 380, 480);
        }

        private void ShowTextDialog(string title, string text, int width, int height)
        {
            var dialog = new Form
            {
                Text = title,
                Size = new Size(width, height),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(28, 28, 38),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var textBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Text = text,
                ReadOnly = true,
                BackColor = Color.FromArgb(28, 28, 38),
                ForeColor = Color.FromArgb(220, 220, 230),
                Font = new Font("Consolas", 9.5f),
                BorderStyle = BorderStyle.None
            };

            var btnClose = new Button
            {
                Text = "Close",
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.FromArgb(50, 60, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnClose.FlatAppearance.BorderSize = 0;

            dialog.Controls.Add(textBox);
            dialog.Controls.Add(btnClose);
            dialog.ShowDialog(this);
        }

        #endregion
    }
}