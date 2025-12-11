namespace MinecraftBuildingGuide3D
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _renderer?.Dispose(); components?.Dispose(); }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.Text = "Minecraft Building Guide 3D";
            this.Size = new System.Drawing.Size(1600, 1000);
            this.MinimumSize = new System.Drawing.Size(1300, 750);
            this.BackColor = System.Drawing.Color.FromArgb(18, 18, 24);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            var mainColor = System.Drawing.Color.FromArgb(24, 24, 32);
            var panelColor = System.Drawing.Color.FromArgb(28, 28, 38);
            var textColor = System.Drawing.Color.FromArgb(220, 220, 230);
            var dimTextColor = System.Drawing.Color.FromArgb(140, 140, 160);
            var instructionColor = System.Drawing.Color.FromArgb(100, 120, 140);

            // Menu strip with custom dark theme renderer
            _menuStrip = new System.Windows.Forms.MenuStrip
            {
                BackColor = System.Drawing.Color.FromArgb(22, 22, 30),
                ForeColor = textColor,
                Renderer = new DarkMenuRenderer()
            };
            var helpMenu = new System.Windows.Forms.ToolStripMenuItem("Help");
            var helpItem = new System.Windows.Forms.ToolStripMenuItem("Instructions...");
            helpItem.Click += (s, e) => ShowHelpDialog();
            var aboutItem = new System.Windows.Forms.ToolStripMenuItem("About...");
            aboutItem.Click += (s, e) => ShowAboutDialog();
            helpMenu.DropDownItems.Add(helpItem);
            helpMenu.DropDownItems.Add(new System.Windows.Forms.ToolStripSeparator());
            helpMenu.DropDownItems.Add(aboutItem);
            _menuStrip.Items.Add(helpMenu);

            // Left control panel
            _controlPanel = new System.Windows.Forms.Panel
            {
                Dock = System.Windows.Forms.DockStyle.Left,
                Width = 360,
                BackColor = mainColor
            };

            // Tab buttons
            _tabButtonPanel = new System.Windows.Forms.Panel
            {
                Dock = System.Windows.Forms.DockStyle.Top,
                Height = 40,
                BackColor = System.Drawing.Color.FromArgb(20, 20, 28)
            };

            _btnTabShapes = CreateTabButton("Shapes", 0, true);
            _btnTabView = CreateTabButton("View", 1, false);
            _btnTabBuild = CreateTabButton("Build", 2, false);
            _btnTabShapes.Click += (s, e) => SwitchTab(0);
            _btnTabView.Click += (s, e) => SwitchTab(1);
            _btnTabBuild.Click += (s, e) => SwitchTab(2);
            _tabButtonPanel.Controls.AddRange(new System.Windows.Forms.Control[] { _btnTabShapes, _btnTabView, _btnTabBuild });

            // Tab panels
            _panelShapes = CreateTabPanel(panelColor);
            _panelView = CreateTabPanel(panelColor);
            _panelBuild = CreateTabPanel(panelColor);
            _panelView.Visible = false;
            _panelBuild.Visible = false;

            // ===== SHAPES TAB =====
            int y = 10;

            AddLabel(_panelShapes, "SHAPE TYPE", 12, y, dimTextColor, 9, true); y += 20;
            _cmbShape = AddCombo(_panelShapes, 12, y, 328, new[] {
                "Sphere", "Dome", "Onion Dome", "Wizard Tower Roof",
                "Cylinder", "Cone", "Pyramid", "Torus", "Ellipsoid", "Cuboid",
                "Arch", "Spiral Staircase", "Helix",
                "Bridge", "Norman Wall", "Norman Tower",
                "Paraboloid", "Hyperboloid",
                "Point-to-Point Line", "Spline Curve"
            });
            _cmbShape.SelectedIndexChanged += (s, e) => UpdateParameterFields();
            y += 32;

            AddLabel(_panelShapes, "PARAMETERS", 12, y, dimTextColor, 9, true); y += 20;

            // Parameter rows - 6 parameters max
            _lblP1 = AddLabel(_panelShapes, "Radius:", 12, y + 3, textColor);
            _numP1 = AddNum(_panelShapes, 85, y, 1, 500, 20, 65);
            _lblP2 = AddLabel(_panelShapes, "", 165, y + 3, textColor);
            _numP2 = AddNum(_panelShapes, 235, y, 1, 500, 30, 65);
            y += 30;

            _lblP3 = AddLabel(_panelShapes, "", 12, y + 3, textColor);
            _numP3 = AddNum(_panelShapes, 85, y, 1, 500, 20, 65);
            _lblP4 = AddLabel(_panelShapes, "", 165, y + 3, textColor);
            _numP4 = AddNum(_panelShapes, 235, y, 1, 500, 20, 65);
            y += 30;

            _lblP5 = AddLabel(_panelShapes, "", 12, y + 3, textColor);
            _numP5 = AddNum(_panelShapes, 85, y, 1, 50, 1, 65);
            _lblP6 = AddLabel(_panelShapes, "", 165, y + 3, textColor);
            _numP6 = AddNum(_panelShapes, 235, y, 1, 50, 1, 65);
            y += 32;

            // Options row
            _chkHollow = AddChk(_panelShapes, "Hollow", 12, y, 70, textColor);
            _lblThk = AddLabel(_panelShapes, "Thickness:", 85, y + 3, textColor);
            _numThk = AddNum(_panelShapes, 155, y, 1, 50, 2, 50);
            y += 28;

            AddLabel(_panelShapes, "Edge:", 12, y + 3, textColor);
            _cmbEdge = AddCombo(_panelShapes, 55, y, 100, new[] { "Normal", "Heavy In", "Heavy Out" });
            AddLabel(_panelShapes, "Arch:", 165, y + 3, textColor);
            _cmbArchStyle = AddCombo(_panelShapes, 205, y, 95, new[] { "Round", "Pointed", "Segment" });
            y += 34;

            // Points entry for line shapes (initially hidden)
            _lblPoints = AddLabel(_panelShapes, "Points (x,y,z per line):", 12, y + 3, textColor);
            _lblPoints.Visible = false;

            // Presets dropdown
            _lblPreset = AddLabel(_panelShapes, "Preset:", 180, y + 3, textColor);
            _lblPreset.Visible = false;
            _cmbPreset = AddCombo(_panelShapes, 225, y, 115, new[] {
                "Custom", "Circle", "Square", "Triangle", "Star", "Spiral", "Wave", "Zigzag", "Heart", "Figure-8"
            });
            _cmbPreset.Visible = false;
            _cmbPreset.SelectedIndexChanged += CmbPreset_SelectedIndexChanged;
            y += 22;

            _txtPoints = new System.Windows.Forms.TextBox
            {
                Location = new System.Drawing.Point(12, y),
                Size = new System.Drawing.Size(328, 80),
                Multiline = true,
                ScrollBars = System.Windows.Forms.ScrollBars.Vertical,
                BackColor = System.Drawing.Color.FromArgb(40, 40, 55),
                ForeColor = System.Drawing.Color.FromArgb(220, 220, 230),
                BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle,
                Font = new System.Drawing.Font("Consolas", 9f),
                Visible = false
            };
            _panelShapes.Controls.Add(_txtPoints);
            y += 85;

            _chkClosedLoop = AddChk(_panelShapes, "Closed Loop", 12, y, 100, textColor);
            _chkClosedLoop.Visible = false;
            y += 30;

            // Generate button
            _btnGenerate = new System.Windows.Forms.Button
            {
                Text = "⚡ GENERATE",
                Location = new System.Drawing.Point(12, y),
                Size = new System.Drawing.Size(160, 40),
                BackColor = System.Drawing.Color.FromArgb(45, 100, 55),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = System.Windows.Forms.FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 11, System.Drawing.FontStyle.Bold),
                Cursor = System.Windows.Forms.Cursors.Hand
            };
            _btnGenerate.FlatAppearance.BorderSize = 0;
            _btnGenerate.Click += BtnGenerate_Click;
            _panelShapes.Controls.Add(_btnGenerate);

            _chkAutoZoom = AddChk(_panelShapes, "Auto-Fit View", 185, y + 10, 110, textColor);
            _chkAutoZoom.Checked = true;
            y += 50;

            AddSeparator(_panelShapes, 12, y, 328); y += 10;

            AddLabel(_panelShapes, "SHAPE INFO", 12, y, dimTextColor, 9, true); y += 20;
            _lblInfo = new System.Windows.Forms.Label
            {
                Location = new System.Drawing.Point(12, y),
                Size = new System.Drawing.Size(328, 150),
                ForeColor = System.Drawing.Color.FromArgb(180, 200, 220),
                Font = new System.Drawing.Font("Consolas", 9.5f),
                Text = "Generate a shape to see info..."
            };
            _panelShapes.Controls.Add(_lblInfo);

            // ===== VIEW TAB =====
            y = 10;

            AddLabel(_panelView, "CAMERA VIEWS", 12, y, dimTextColor, 9, true); y += 22;

            // Camera button grid (4x2)
            var btnW = 78; var btnH = 28; var btnGap = 4;
            _btnReset = CreateCamBtn("Reset", 12, y, btnW, btnH);
            _btnTop = CreateCamBtn("Top", 12 + btnW + btnGap, y, btnW, btnH);
            _btnFront = CreateCamBtn("Front", 12 + (btnW + btnGap) * 2, y, btnW, btnH);
            _btnCorner = CreateCamBtn("Corner", 12 + (btnW + btnGap) * 3, y, btnW, btnH);
            _panelView.Controls.AddRange(new System.Windows.Forms.Control[] { _btnReset, _btnTop, _btnFront, _btnCorner });
            y += btnH + btnGap;

            _btnLeft = CreateCamBtn("Left", 12, y, btnW, btnH);
            _btnRight = CreateCamBtn("Right", 12 + btnW + btnGap, y, btnW, btnH);
            _btnBack = CreateCamBtn("Back", 12 + (btnW + btnGap) * 2, y, btnW, btnH);
            _btnBottom = CreateCamBtn("Bottom", 12 + (btnW + btnGap) * 3, y, btnW, btnH);
            _panelView.Controls.AddRange(new System.Windows.Forms.Control[] { _btnLeft, _btnRight, _btnBack, _btnBottom });

            _btnReset.Click += (s, e) => ResetCameraToShape();
            _btnTop.Click += (s, e) => SetCameraView(Rendering.CameraView.Top);
            _btnFront.Click += (s, e) => SetCameraView(Rendering.CameraView.Front);
            _btnCorner.Click += (s, e) => { _renderer?.Camera.SetView(Rendering.CameraView.Isometric); _renderer.Camera.Yaw = 35; _renderer.Camera.Pitch = 25; _glControl?.Invalidate(); };
            _btnLeft.Click += (s, e) => SetCameraView(Rendering.CameraView.Left);
            _btnRight.Click += (s, e) => SetCameraView(Rendering.CameraView.Right);
            _btnBack.Click += (s, e) => SetCameraView(Rendering.CameraView.Back);
            _btnBottom.Click += (s, e) => SetCameraView(Rendering.CameraView.Bottom);
            y += btnH + 12;

            AddLabel(_panelView, "VIEW MODE", 12, y, dimTextColor, 9, true); y += 22;

            _chk2D = AddChk(_panelView, "2D Layer View", 12, y, 130, textColor);
            _chk2D.CheckedChanged += Chk2D_CheckedChanged;
            _chkAllLayers = AddChk(_panelView, "Show All Layers", 155, y, 140, textColor);
            _chkAllLayers.Checked = true;
            _chkAllLayers.CheckedChanged += ChkAllLayers_CheckedChanged;
            y += 26;

            // Layer control with both slider and numeric
            AddLabel(_panelView, "Layer (Y):", 12, y + 3, textColor);
            _numLayer = AddNum(_panelView, 85, y, 0, 256, 0, 55);
            _trkLayer = AddTrack(_panelView, 145, y - 5, 180, 0, 256, 0);
            _numLayer.ValueChanged += (s, e) => { if (!_syncingLayer) { _syncingLayer = true; _trkLayer.Value = (int)_numLayer.Value; TrkLayer_ValueChanged(s, e); _syncingLayer = false; } };
            _trkLayer.ValueChanged += (s, e) => { if (!_syncingLayer) { _syncingLayer = true; _numLayer.Value = _trkLayer.Value; TrkLayer_ValueChanged(s, e); _syncingLayer = false; } };
            y += 45;

            AddSeparator(_panelView, 12, y, 328); y += 10;

            AddLabel(_panelView, "RENDERING", 12, y, dimTextColor, 9, true); y += 22;

            _chkGrid = AddChk(_panelView, "Grid", 12, y, 55, textColor); _chkGrid.Checked = true;
            _chkGrid.CheckedChanged += (s, e) => { if (_renderer != null) { _renderer.ShowGrid = _chkGrid.Checked; _glControl?.Invalidate(); } };
            _chkLight = AddChk(_panelView, "Lighting", 75, y, 80, textColor); _chkLight.Checked = true;
            _chkLight.CheckedChanged += (s, e) => { if (_renderer != null) { _renderer.EnableLighting = _chkLight.Checked; _glControl?.Invalidate(); } };
            _chkOutline = AddChk(_panelView, "Outlines", 165, y, 80, textColor); _chkOutline.Checked = true;
            _chkOutline.CheckedChanged += (s, e) => { if (_renderer != null) { _renderer.ShowBlockOutlines = _chkOutline.Checked; _glControl?.Invalidate(); } };
            y += 28;

            AddLabel(_panelView, "Border:", 12, y + 3, textColor);
            _numBorder = AddNum(_panelView, 65, y, 1, 15, 2, 45);
            _trkBorder = AddTrack(_panelView, 115, y - 5, 130, 1, 15, 2);
            _lblBorderVal = AddLabel(_panelView, "2 px", 250, y + 3, textColor);
            _numBorder.ValueChanged += (s, e) => { _trkBorder.Value = (int)_numBorder.Value; UpdateBorder(); };
            _trkBorder.ValueChanged += (s, e) => { _numBorder.Value = _trkBorder.Value; UpdateBorder(); };
            y += 40;

            AddLabel(_panelView, "Alpha:", 12, y + 3, textColor);
            _numAlpha = AddNum(_panelView, 65, y, 10, 100, 100, 45);
            _trkAlpha = AddTrack(_panelView, 115, y - 5, 130, 10, 100, 100);
            _lblAlphaVal = AddLabel(_panelView, "100%", 250, y + 3, textColor);
            _numAlpha.ValueChanged += (s, e) => { _trkAlpha.Value = (int)_numAlpha.Value; UpdateAlpha(); };
            _trkAlpha.ValueChanged += (s, e) => { _numAlpha.Value = _trkAlpha.Value; UpdateAlpha(); };
            y += 45;

            AddSeparator(_panelView, 12, y, 328); y += 10;

            AddLabel(_panelView, "REFERENCE PLANES", 12, y, dimTextColor, 9, true); y += 22;

            _chkPlaneXZView = AddChk(_panelView, "XZ (Blue)", 12, y, 90, textColor);
            _chkPlaneXZView.CheckedChanged += ChkPlaneView_CheckedChanged;
            AddLabel(_panelView, "Y:", 115, y + 3, textColor);
            _numPlaneYView = AddNum(_panelView, 135, y, -128, 256, 0, 55);
            _numPlaneYView.ValueChanged += (s, e) => { SyncPlaneY(); };
            y += 26;

            _chkPlaneXYView = AddChk(_panelView, "XY (Red)", 12, y, 90, textColor);
            _chkPlaneXYView.CheckedChanged += ChkPlaneView_CheckedChanged;
            AddLabel(_panelView, "Z:", 115, y + 3, textColor);
            _numPlaneZView = AddNum(_panelView, 135, y, -256, 256, 0, 55);
            _numPlaneZView.ValueChanged += (s, e) => { SyncPlaneZ(); };
            y += 26;

            _chkPlaneYZView = AddChk(_panelView, "YZ (Green)", 12, y, 100, textColor);
            _chkPlaneYZView.CheckedChanged += ChkPlaneView_CheckedChanged;
            AddLabel(_panelView, "X:", 115, y + 3, textColor);
            _numPlaneXView = AddNum(_panelView, 135, y, -256, 256, 0, 55);
            _numPlaneXView.ValueChanged += (s, e) => { SyncPlaneX(); };

            // ===== BUILD TAB =====
            y = 10;

            AddLabel(_panelBuild, "BUILD MODE", 12, y, dimTextColor, 9, true); y += 22;

            _rbBuildOff = AddRadio(_panelBuild, "Off", 12, y, 55, textColor, true);
            _rbBuildOff.CheckedChanged += RbBuildMode_CheckedChanged;

            _rbBuildAdd = AddRadio(_panelBuild, "➕ Add", 70, y, 85, System.Drawing.Color.FromArgb(100, 200, 120), false, System.Drawing.FontStyle.Bold);
            _rbBuildAdd.CheckedChanged += RbBuildMode_CheckedChanged;

            _rbBuildRemove = AddRadio(_panelBuild, "➖ Remove", 160, y, 110, System.Drawing.Color.FromArgb(220, 100, 100), false, System.Drawing.FontStyle.Bold);
            _rbBuildRemove.CheckedChanged += RbBuildMode_CheckedChanged;
            y += 35;

            AddSeparator(_panelBuild, 12, y, 328); y += 12;

            AddLabel(_panelBuild, "GUIDE PLANES", 12, y, dimTextColor, 9, true); y += 22;

            _chkPlaneXZ = AddChk(_panelBuild, "XZ (Blue)", 12, y, 90, textColor);
            _chkPlaneXZ.CheckedChanged += ChkPlane_CheckedChanged;
            AddLabel(_panelBuild, "Y:", 115, y + 3, textColor);
            _numPlaneY = AddNum(_panelBuild, 135, y, -128, 256, 0, 55);
            _numPlaneY.ValueChanged += (s, e) => { if (_renderer != null && !_syncingPlanes) { _syncingPlanes = true; _renderer.PlaneY = (int)_numPlaneY.Value; _numPlaneYView.Value = _numPlaneY.Value; _syncingPlanes = false; _glControl?.Invalidate(); } };
            y += 28;

            _chkPlaneXY = AddChk(_panelBuild, "XY (Red)", 12, y, 90, textColor);
            _chkPlaneXY.CheckedChanged += ChkPlane_CheckedChanged;
            AddLabel(_panelBuild, "Z:", 115, y + 3, textColor);
            _numPlaneZ = AddNum(_panelBuild, 135, y, -256, 256, 0, 55);
            _numPlaneZ.ValueChanged += (s, e) => { if (_renderer != null && !_syncingPlanes) { _syncingPlanes = true; _renderer.PlaneZ = (int)_numPlaneZ.Value; _numPlaneZView.Value = _numPlaneZ.Value; _syncingPlanes = false; _glControl?.Invalidate(); } };
            y += 28;

            _chkPlaneYZ = AddChk(_panelBuild, "YZ (Green)", 12, y, 100, textColor);
            _chkPlaneYZ.CheckedChanged += ChkPlane_CheckedChanged;
            AddLabel(_panelBuild, "X:", 115, y + 3, textColor);
            _numPlaneX = AddNum(_panelBuild, 135, y, -256, 256, 0, 55);
            _numPlaneX.ValueChanged += (s, e) => { if (_renderer != null && !_syncingPlanes) { _syncingPlanes = true; _renderer.PlaneX = (int)_numPlaneX.Value; _numPlaneXView.Value = _numPlaneX.Value; _syncingPlanes = false; _glControl?.Invalidate(); } };
            y += 38;

            AddSeparator(_panelBuild, 12, y, 328); y += 12;

            AddLabel(_panelBuild, "INSTRUCTIONS", 12, y, dimTextColor, 9, true); y += 18;
            var lblInstr = new System.Windows.Forms.Label
            {
                Location = new System.Drawing.Point(12, y),
                Size = new System.Drawing.Size(328, 130),
                ForeColor = instructionColor,
                Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Italic),
                Text = "• Select 'Add' or 'Remove' mode\n• Left-click on block face to add/remove\n• Works in both 3D and 2D Layer views\n• In 2D: Builds at current layer (Y level)\n• Enable guide planes to build in empty space\n• Planes show through objects when intersecting"
            };
            _panelBuild.Controls.Add(lblInstr);

            // Assemble
            _controlPanel.Controls.Add(_panelShapes);
            _controlPanel.Controls.Add(_panelView);
            _controlPanel.Controls.Add(_panelBuild);
            _controlPanel.Controls.Add(_tabButtonPanel);

            _glPanel = new System.Windows.Forms.Panel
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                BackColor = System.Drawing.Color.FromArgb(15, 15, 20)
            };

            _lblStatus = new System.Windows.Forms.Label
            {
                Dock = System.Windows.Forms.DockStyle.Bottom,
                Height = 26,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                ForeColor = dimTextColor,
                BackColor = System.Drawing.Color.FromArgb(16, 16, 22),
                Text = "   Left-drag: Rotate  |  Right-drag: Pan  |  Scroll: Zoom  |  Middle-click: Reset",
                Font = new System.Drawing.Font("Segoe UI", 9)
            };

            this.MainMenuStrip = _menuStrip;
            this.Controls.Add(_glPanel);
            this.Controls.Add(_controlPanel);
            this.Controls.Add(_lblStatus);
            this.Controls.Add(_menuStrip);

            HideAllExtraParams();
        }

        private void UpdateBorder() { if (_renderer != null) { _renderer.OutlineWidth = _trkBorder.Value; _lblBorderVal.Text = $"{_trkBorder.Value} px"; _glControl?.Invalidate(); } }
        private void UpdateAlpha() { if (_renderer != null) { _renderer.GlobalAlpha = _trkAlpha.Value / 100f; _lblAlphaVal.Text = $"{_trkAlpha.Value}%"; _glControl?.Invalidate(); } }

        private void AddSeparator(System.Windows.Forms.Control parent, int x, int y, int w)
        {
            parent.Controls.Add(new System.Windows.Forms.Panel
            {
                Location = new System.Drawing.Point(x, y),
                Size = new System.Drawing.Size(w, 1),
                BackColor = System.Drawing.Color.FromArgb(50, 50, 65)
            });
        }

        private System.Windows.Forms.Button CreateTabButton(string text, int index, bool active)
        {
            var btn = new System.Windows.Forms.Button
            {
                Text = text,
                Location = new System.Drawing.Point(index * 118 + 4, 5),
                Size = new System.Drawing.Size(114, 30),
                FlatStyle = System.Windows.Forms.FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 10, active ? System.Drawing.FontStyle.Bold : System.Drawing.FontStyle.Regular),
                BackColor = active ? System.Drawing.Color.FromArgb(45, 45, 60) : System.Drawing.Color.FromArgb(28, 28, 38),
                ForeColor = active ? System.Drawing.Color.White : System.Drawing.Color.FromArgb(150, 150, 170),
                Cursor = System.Windows.Forms.Cursors.Hand,
                Tag = index
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private System.Windows.Forms.Button CreateCamBtn(string text, int x, int y, int w, int h)
        {
            var btn = new System.Windows.Forms.Button
            {
                Text = text,
                Location = new System.Drawing.Point(x, y),
                Size = new System.Drawing.Size(w, h),
                BackColor = System.Drawing.Color.FromArgb(40, 45, 58),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = System.Windows.Forms.FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 9),
                Cursor = System.Windows.Forms.Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(55, 60, 75);
            return btn;
        }

        private System.Windows.Forms.Panel CreateTabPanel(System.Drawing.Color color)
        {
            return new System.Windows.Forms.Panel { Dock = System.Windows.Forms.DockStyle.Fill, BackColor = color, AutoScroll = true };
        }

        private void SwitchTab(int index)
        {
            _panelShapes.Visible = (index == 0);
            _panelView.Visible = (index == 1);
            _panelBuild.Visible = (index == 2);
            foreach (System.Windows.Forms.Control c in _tabButtonPanel.Controls)
                if (c is System.Windows.Forms.Button btn && btn.Tag is int i)
                {
                    btn.BackColor = (i == index) ? System.Drawing.Color.FromArgb(45, 45, 60) : System.Drawing.Color.FromArgb(28, 28, 38);
                    btn.ForeColor = (i == index) ? System.Drawing.Color.White : System.Drawing.Color.FromArgb(150, 150, 170);
                    btn.Font = new System.Drawing.Font("Segoe UI", 10, (i == index) ? System.Drawing.FontStyle.Bold : System.Drawing.FontStyle.Regular);
                }
        }

        private void HideAllExtraParams()
        {
            _numP2.Visible = _numP3.Visible = _numP4.Visible = _numP5.Visible = _numP6.Visible = false;
            _lblP2.Visible = _lblP3.Visible = _lblP4.Visible = _lblP5.Visible = _lblP6.Visible = false;
            _lblPoints.Visible = _txtPoints.Visible = _chkClosedLoop.Visible = false;
            _lblPreset.Visible = _cmbPreset.Visible = false;
        }

        private System.Windows.Forms.Label AddLabel(System.Windows.Forms.Control p, string t, int x, int y, System.Drawing.Color c, float sz = 9.5f, bool b = false)
        {
            var l = new System.Windows.Forms.Label { Text = t, Location = new System.Drawing.Point(x, y), AutoSize = true, ForeColor = c, Font = new System.Drawing.Font("Segoe UI", sz, b ? System.Drawing.FontStyle.Bold : System.Drawing.FontStyle.Regular) };
            p.Controls.Add(l); return l;
        }

        private DarkNumericUpDown AddNum(System.Windows.Forms.Control p, int x, int y, int min, int max, int val, int w = 65)
        {
            var n = new DarkNumericUpDown { Location = new System.Drawing.Point(x, y), Size = new System.Drawing.Size(w, 26), Minimum = min, Maximum = max };
            n.Value = System.Math.Max(min, System.Math.Min(max, val));
            p.Controls.Add(n); return n;
        }

        private DarkComboBox AddCombo(System.Windows.Forms.Control p, int x, int y, int w, string[] items)
        {
            var c = new DarkComboBox { Location = new System.Drawing.Point(x, y), Size = new System.Drawing.Size(w, 26) };
            c.Items.AddRange(items); c.SelectedIndex = 0;
            p.Controls.Add(c); return c;
        }

        private DarkCheckBox AddChk(System.Windows.Forms.Control p, string t, int x, int y, int w, System.Drawing.Color c)
        {
            var ch = new DarkCheckBox { Text = t, Location = new System.Drawing.Point(x, y), Size = new System.Drawing.Size(w, 24), ForeColor = c };
            p.Controls.Add(ch); return ch;
        }

        private DarkTrackBar AddTrack(System.Windows.Forms.Control p, int x, int y, int w, int min, int max, int val)
        {
            var t = new DarkTrackBar { Location = new System.Drawing.Point(x, y), Size = new System.Drawing.Size(w, 26), Minimum = min, Maximum = max, Value = val };
            p.Controls.Add(t); return t;
        }

        private DarkRadioButton AddRadio(System.Windows.Forms.Control p, string t, int x, int y, int w, System.Drawing.Color c, bool isChecked = false, System.Drawing.FontStyle style = System.Drawing.FontStyle.Regular)
        {
            var r = new DarkRadioButton { Text = t, Location = new System.Drawing.Point(x, y), Size = new System.Drawing.Size(w, 24), ForeColor = c, Font = new System.Drawing.Font("Segoe UI", 10, style), Checked = isChecked };
            p.Controls.Add(r); return r;
        }

        // Controls
        private bool _syncingLayer = false;
        private bool _syncingPlanes = false;
        private System.Windows.Forms.MenuStrip _menuStrip;
        private System.Windows.Forms.Panel _controlPanel, _glPanel, _tabButtonPanel;
        private System.Windows.Forms.Panel _panelShapes, _panelView, _panelBuild;
        private System.Windows.Forms.Button _btnTabShapes, _btnTabView, _btnTabBuild;
        private System.Windows.Forms.Label _lblStatus, _lblInfo;
        private System.Windows.Forms.Label _lblP1, _lblP2, _lblP3, _lblP4, _lblP5, _lblP6, _lblThk;
        private System.Windows.Forms.Label _lblBorderVal, _lblAlphaVal;
        private System.Windows.Forms.Label _lblPoints, _lblPreset;
        private System.Windows.Forms.TextBox _txtPoints;
        private DarkComboBox _cmbShape, _cmbEdge, _cmbArchStyle, _cmbPreset;
        private DarkNumericUpDown _numP1, _numP2, _numP3, _numP4, _numP5, _numP6, _numThk;
        private DarkNumericUpDown _numPlaneX, _numPlaneY, _numPlaneZ;
        private DarkNumericUpDown _numPlaneXView, _numPlaneYView, _numPlaneZView;
        private DarkNumericUpDown _numLayer, _numBorder, _numAlpha;
        private DarkCheckBox _chkHollow, _chkAutoZoom, _chkClosedLoop;
        private DarkCheckBox _chk2D, _chkAllLayers, _chkGrid, _chkLight, _chkOutline;
        private DarkCheckBox _chkPlaneXZ, _chkPlaneXY, _chkPlaneYZ;
        private DarkCheckBox _chkPlaneXZView, _chkPlaneXYView, _chkPlaneYZView;
        private DarkRadioButton _rbBuildOff, _rbBuildAdd, _rbBuildRemove;
        private System.Windows.Forms.Button _btnGenerate;
        private System.Windows.Forms.Button _btnReset, _btnTop, _btnFront, _btnCorner, _btnLeft, _btnRight, _btnBack, _btnBottom;
        private DarkTrackBar _trkLayer, _trkBorder, _trkAlpha;
    }

    // Custom menu renderer for dark theme
    public class DarkMenuRenderer : System.Windows.Forms.ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkColorTable()) { }

        protected override void OnRenderMenuItemBackground(System.Windows.Forms.ToolStripItemRenderEventArgs e)
        {
            var item = e.Item;
            var g = e.Graphics;
            var rect = new System.Drawing.Rectangle(System.Drawing.Point.Empty, item.Size);

            if (item.Selected || item.Pressed)
            {
                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(50, 60, 80)))
                    g.FillRectangle(brush, rect);
            }
            else
            {
                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(28, 28, 38)))
                    g.FillRectangle(brush, rect);
            }
        }

        protected override void OnRenderItemText(System.Windows.Forms.ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = System.Drawing.Color.FromArgb(220, 220, 230);
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(System.Windows.Forms.ToolStripSeparatorRenderEventArgs e)
        {
            var g = e.Graphics;
            var rect = new System.Drawing.Rectangle(System.Drawing.Point.Empty, e.Item.Size);
            using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(50, 50, 65)))
                g.DrawLine(pen, rect.Left + 5, rect.Height / 2, rect.Right - 5, rect.Height / 2);
        }
    }

    public class DarkColorTable : System.Windows.Forms.ProfessionalColorTable
    {
        public override System.Drawing.Color MenuBorder => System.Drawing.Color.FromArgb(50, 50, 65);
        public override System.Drawing.Color MenuItemBorder => System.Drawing.Color.FromArgb(50, 60, 80);
        public override System.Drawing.Color MenuItemSelected => System.Drawing.Color.FromArgb(50, 60, 80);
        public override System.Drawing.Color MenuStripGradientBegin => System.Drawing.Color.FromArgb(22, 22, 30);
        public override System.Drawing.Color MenuStripGradientEnd => System.Drawing.Color.FromArgb(22, 22, 30);
        public override System.Drawing.Color MenuItemSelectedGradientBegin => System.Drawing.Color.FromArgb(50, 60, 80);
        public override System.Drawing.Color MenuItemSelectedGradientEnd => System.Drawing.Color.FromArgb(50, 60, 80);
        public override System.Drawing.Color MenuItemPressedGradientBegin => System.Drawing.Color.FromArgb(40, 50, 70);
        public override System.Drawing.Color MenuItemPressedGradientEnd => System.Drawing.Color.FromArgb(40, 50, 70);
        public override System.Drawing.Color ToolStripDropDownBackground => System.Drawing.Color.FromArgb(28, 28, 38);
        public override System.Drawing.Color ImageMarginGradientBegin => System.Drawing.Color.FromArgb(28, 28, 38);
        public override System.Drawing.Color ImageMarginGradientMiddle => System.Drawing.Color.FromArgb(28, 28, 38);
        public override System.Drawing.Color ImageMarginGradientEnd => System.Drawing.Color.FromArgb(28, 28, 38);
        public override System.Drawing.Color SeparatorDark => System.Drawing.Color.FromArgb(50, 50, 65);
        public override System.Drawing.Color SeparatorLight => System.Drawing.Color.FromArgb(50, 50, 65);
    }
}