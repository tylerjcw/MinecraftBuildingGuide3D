using System;
using System.Collections.Generic;
using MinecraftBuildingGuide3D.Models;

namespace MinecraftBuildingGuide3D.Shapes
{
    public enum EdgeMode { Normal, HeavyInside, HeavyOutside }
    public enum ArchStyle { Round, Pointed, Segmental }

    public class ShapeInfo
    {
        public string Name { get; }
        public Dictionary<string, string> Properties { get; }
        public ShapeInfo(string name, Dictionary<string, string> props) { Name = name; Properties = props; }
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Shape: {Name}");
            foreach (var kvp in Properties) sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
            return sb.ToString();
        }
    }

    public static class ShapeGenerator
    {
        private static HashSet<(int, int, int)> _placed = new();

        private static void StartShape() => _placed.Clear();

        private static void Place(VoxelGrid grid, int x, int y, int z, BlockType type = BlockType.Solid)
        {
            if (!_placed.Contains((x, y, z)))
            {
                _placed.Add((x, y, z));
                grid.SetBlock(x, y, z, type);
            }
        }

        #region Basic Shapes

        public static ShapeInfo GenerateSphere(VoxelGrid grid, int radius, bool hollow = false, int thickness = 1)
        {
            grid.Clear(); StartShape();
            int cx = grid.CenterX, cz = grid.CenterZ;
            int cy = grid.CenterY;

            // For radius R, we want diameter = 2*R blocks
            // Use offset of 0.5 to center the sphere between block boundaries for even diameters
            double r = radius;  // Distance threshold
            double innerR = hollow ? Math.Max(0, radius - thickness) : 0;

            // Loop bounds: for radius 4, we want 8 blocks (-4 to +3 or similar)
            int minB = -radius;
            int maxB = radius - 1;

            for (int y = minB; y <= maxB; y++)
                for (int x = minB; x <= maxB; x++)
                    for (int z = minB; z <= maxB; z++)
                    {
                        // Calculate distance from center of the sphere (at 0.5, 0.5, 0.5 offset)
                        double dx = x + 0.5;
                        double dy = y + 0.5;
                        double dz = z + 0.5;
                        double dist = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                        if (dist <= r && dist >= innerR)
                            Place(grid, cx + x, cy + y, cz + z);
                    }

            return new ShapeInfo("Sphere", new()
            {
                ["Radius"] = radius.ToString(),
                ["Hollow"] = hollow.ToString(),
                ["Blocks"] = grid.BlockCount.ToString()
            });
        }

        public static ShapeInfo GenerateDome(VoxelGrid grid, int radius, bool hollow = false, int thickness = 1,
            double heightRatio = 1.0, double baseRatio = 1.0)
        {
            grid.Clear(); StartShape();
            int cx = grid.CenterX, cz = grid.CenterZ;
            int cy = grid.CenterY;

            // For radius R, diameter = 2*R
            int minXZ = -radius;
            int maxXZ = radius - 1;
            int maxY = (int)(radius * heightRatio) - 1;
            if (maxY < 0) maxY = 0;

            for (int y = 0; y <= maxY; y++)
                for (int x = minXZ; x <= maxXZ; x++)
                    for (int z = minXZ; z <= maxXZ; z++)
                    {
                        // Offset by 0.5 to center
                        double dx = (x + 0.5) / (radius * baseRatio);
                        double dy = (y + 0.5) / (radius * heightRatio);
                        double dz = (z + 0.5) / (radius * baseRatio);
                        double dist = Math.Sqrt(dx * dx + dy * dy + dz * dz);

                        double innerDist = 2.0;  // Default to "outside" inner shell
                        if (hollow && radius > thickness)
                        {
                            double innerRadiusX = (radius - thickness) * baseRatio;
                            double innerRadiusY = (radius - thickness) * heightRatio;
                            double innerRadiusZ = (radius - thickness) * baseRatio;
                            if (innerRadiusX > 0 && innerRadiusY > 0 && innerRadiusZ > 0)
                            {
                                innerDist = Math.Sqrt(
                                    ((x + 0.5) / innerRadiusX) * ((x + 0.5) / innerRadiusX) +
                                    ((y + 0.5) / innerRadiusY) * ((y + 0.5) / innerRadiusY) +
                                    ((z + 0.5) / innerRadiusZ) * ((z + 0.5) / innerRadiusZ));
                            }
                        }

                        if (dist <= 1.0 && innerDist >= 1.0)
                            Place(grid, cx + x, cy + y, cz + z);
                    }

            return new ShapeInfo("Dome", new()
            {
                ["Radius"] = radius.ToString(),
                ["Height Ratio"] = heightRatio.ToString("F1"),
                ["Hollow"] = hollow.ToString(),
                ["Blocks"] = grid.BlockCount.ToString()
            });
        }

        public static ShapeInfo GenerateOnionDome(VoxelGrid grid, int radius, int height, int thickness = 2)
        {
            grid.Clear(); StartShape();
            int cx = grid.CenterX, cz = grid.CenterZ;
            int cy = grid.CenterY;
            int halfH = height / 2;

            for (int y = 0; y < height; y++)
            {
                double t = (double)y / height;
                double profile;

                if (t < 0.6)
                {
                    double bulgeT = t / 0.6;
                    profile = Math.Sin(bulgeT * Math.PI) * 1.3;
                }
                else
                {
                    double taperT = (t - 0.6) / 0.4;
                    profile = (1 - taperT) * 0.8;
                }

                int layerRadius = (int)(radius * profile);
                if (layerRadius < 1 && y < height - 1) layerRadius = 1;

                GenerateFilledCircle(grid, cx, cy - halfH + y, cz, layerRadius, thickness, y > height * 0.3);
            }

            // Finial at top
            for (int y = 0; y < radius / 3; y++)
                Place(grid, cx, cy + halfH + y, cz);

            return new ShapeInfo("Onion Dome", new()
            {
                ["Radius"] = radius.ToString(),
                ["Height"] = height.ToString(),
                ["Blocks"] = grid.BlockCount.ToString()
            });
        }

        public static ShapeInfo GenerateWizardTowerRoof(VoxelGrid grid, int baseRadius, int height, int thickness = 1)
        {
            grid.Clear(); StartShape();
            int cx = grid.CenterX, cz = grid.CenterZ;
            int cy = grid.CenterY;
            int halfH = height / 2;

            for (int y = 0; y < height; y++)
            {
                double t = (double)y / height;
                double curve = 1 - Math.Pow(t, 0.7);
                if (t < 0.1) curve += (0.1 - t) * 2;

                int layerRadius = (int)(baseRadius * curve);
                if (layerRadius < 1 && y < height - 1) layerRadius = 1;

                GenerateFilledCircle(grid, cx, cy - halfH + y, cz, layerRadius, thickness, true);
            }

            // Finial
            for (int y = 0; y < 3; y++)
                Place(grid, cx, cy + halfH + y, cz);

            return new ShapeInfo("Wizard Tower Roof", new()
            {
                ["Base Radius"] = baseRadius.ToString(),
                ["Height"] = height.ToString(),
                ["Blocks"] = grid.BlockCount.ToString()
            });
        }

        #endregion

        #region Cylindrical Shapes

        public static ShapeInfo GenerateCylinder(VoxelGrid grid, int radius, int height, bool hollow = false,
            int thickness = 1, EdgeMode edgeMode = EdgeMode.Normal)
        {
            grid.Clear(); StartShape();
            int cx = grid.CenterX, cz = grid.CenterZ;
            int cy = grid.CenterY;
            int halfH = height / 2;

            for (int y = -halfH; y < halfH; y++)
                GenerateCircleLayer(grid, cx, cy + y, cz, radius, hollow, thickness, edgeMode);

            return new ShapeInfo("Cylinder", new()
            {
                ["Radius"] = radius.ToString(),
                ["Height"] = height.ToString(),
                ["Hollow"] = hollow.ToString(),
                ["Blocks"] = grid.BlockCount.ToString()
            });
        }

        public static ShapeInfo GenerateCone(VoxelGrid grid, int radius, int height, bool hollow = false,
            int thickness = 1, int stepHeight = 1)
        {
            grid.Clear(); StartShape();
            int cx = grid.CenterX, cz = grid.CenterZ;
            int cy = grid.CenterY;
            int halfH = height / 2;

            for (int y = 0; y < height; y++)
            {
                int stepY = y / stepHeight;
                int totalSteps = height / stepHeight;
                double t = (double)stepY / totalSteps;
                int layerRadius = (int)(radius * (1 - t));
                if (layerRadius < 0) layerRadius = 0;

                if (layerRadius > 0)
                    GenerateCircleLayer(grid, cx, cy - halfH + y, cz, layerRadius, hollow, thickness, EdgeMode.Normal);
                else
                    Place(grid, cx, cy - halfH + y, cz);
            }

            return new ShapeInfo("Cone", new()
            {
                ["Radius"] = radius.ToString(),
                ["Height"] = height.ToString(),
                ["Step Height"] = stepHeight.ToString(),
                ["Blocks"] = grid.BlockCount.ToString()
            });
        }

        public static ShapeInfo GeneratePyramid(VoxelGrid grid, int baseSize, int height, bool hollow = false,
            int thickness = 1, int stepHeight = 1)
        {
            grid.Clear(); StartShape();
            int cx = grid.CenterX, cz = grid.CenterZ;
            int cy = grid.CenterY;
            int halfH = height / 2;

            for (int y = 0; y < height; y++)
            {
                int stepY = y / stepHeight;
                int totalSteps = (height + stepHeight - 1) / stepHeight;
                double t = (double)stepY / totalSteps;
                int layerSize = (int)(baseSize * (1 - t));
                if (layerSize < 1) layerSize = 1;
                int offsetXZ = layerSize / 2;

                // Use 0 to layerSize-1 for exact dimensions
                for (int x = 0; x < layerSize; x++)
                    for (int z = 0; z < layerSize; z++)
                    {
                        bool isEdge = x == 0 || x == layerSize - 1 || z == 0 || z == layerSize - 1;
                        if (!hollow || isEdge || y < thickness || y >= height - thickness)
                            Place(grid, cx + x - offsetXZ, cy - halfH + y, cz + z - offsetXZ);
                    }
            }

            return new ShapeInfo("Pyramid", new()
            {
                ["Base Size"] = baseSize.ToString(),
                ["Height"] = height.ToString(),
                ["Step Height"] = stepHeight.ToString(),
                ["Blocks"] = grid.BlockCount.ToString()
            });
        }

        #endregion

        #region Arches and Bridges

        public static ShapeInfo GenerateArch(VoxelGrid grid, int width, int height, int depth, int thickness = 2,
            ArchStyle style = ArchStyle.Round)
        {
            grid.Clear(); StartShape();
            int cx = grid.CenterX, cz = grid.CenterZ;
            int cy = grid.CenterY;
            int halfW = width / 2;
            int halfD = depth / 2;
            int halfH = height / 2;

            for (int z = -halfD; z <= halfD; z++)
            {
                DrawArchProfile(grid, cx, cy - halfH, cz + z, halfW, height, thickness, style);
            }

            return new ShapeInfo("Arch", new()
            {
                ["Width"] = width.ToString(),
                ["Height"] = height.ToString(),
                ["Depth"] = depth.ToString(),
                ["Style"] = style.ToString(),
                ["Blocks"] = grid.BlockCount.ToString()
            });
        }

        private static void DrawArchProfile(VoxelGrid grid, int cx, int baseY, int z, int halfWidth, int height,
            int thickness, ArchStyle style)
        {
            // Draw the arch curve
            for (int t = 0; t <= 180; t += 2)
            {
                double rad = t * Math.PI / 180;
                int x, y;

                switch (style)
                {
                    case ArchStyle.Pointed:
                        if (t <= 90)
                        {
                            double angle = t * Math.PI / 90;
                            x = -(int)(halfWidth * (1 - Math.Sin(angle) * 0.5));
                            y = (int)(height * Math.Sin(angle));
                        }
                        else
                        {
                            double angle = (t - 90) * Math.PI / 90;
                            x = (int)(halfWidth * (1 - Math.Cos(angle) * 0.5));
                            y = (int)(height * Math.Cos(angle));
                        }
                        break;

                    case ArchStyle.Segmental:
                        double segRad = t * Math.PI / 180;
                        x = (int)(halfWidth * Math.Cos(segRad));
                        y = (int)(height * 0.5 * Math.Sin(segRad));
                        break;

                    default:  // Round
                        x = (int)(halfWidth * Math.Cos(rad));
                        y = (int)(height * Math.Sin(rad));
                        break;
                }

                for (int ty = 0; ty < thickness; ty++)
                    Place(grid, cx + x, baseY + y + ty, z);
            }

            // Draw legs/pillars
            for (int y = 0; y < height; y++)
            {
                for (int tx = 0; tx < thickness; tx++)
                {
                    Place(grid, cx - halfWidth - tx, baseY + y, z);
                    Place(grid, cx + halfWidth + tx, baseY + y, z);
                }
            }
        }

        public static ShapeInfo GenerateBridge(VoxelGrid grid, int length, int height, int width,
            int archesPerLevel = 3, int levels = 1, ArchStyle style = ArchStyle.Round)
        {
            grid.Clear(); StartShape();
            int cx = grid.CenterX, cz = grid.CenterZ;
            int cy = grid.CenterY;
            int halfLen = length / 2;
            int halfW = width / 2;
            int halfH = height / 2;

            int levelHeight = height / levels;
            int pillarWidth = Math.Max(2, width / 2);

            for (int level = 0; level < levels; level++)
            {
                int baseY = cy - halfH + level * levelHeight;
                int archHeight = levelHeight - 2;
                int archSpan = length / archesPerLevel;
                int archRadius = archSpan / 2 - pillarWidth / 2 - 1;

                // Draw pillars
                for (int p = 0; p <= archesPerLevel; p++)
                {
                    int pillarX = cx - halfLen + p * archSpan;

                    for (int y = 0; y < levelHeight; y++)
                        for (int z = cz - halfW; z <= cz + halfW; z++)
                            for (int px = -pillarWidth / 2; px <= pillarWidth / 2; px++)
                                Place(grid, pillarX + px, baseY + y, z);
                }

                // Draw arches between pillars
                for (int a = 0; a < archesPerLevel; a++)
                {
                    int archCenterX = cx - halfLen + a * archSpan + archSpan / 2;
                    int archBaseY = baseY + levelHeight - archHeight - 1;

                    for (int z = cz - halfW; z <= cz + halfW; z++)
                    {
                        for (int angle = 0; angle <= 180; angle += 3)
                        {
                            double rad = angle * Math.PI / 180;
                            int ax, ay;

                            switch (style)
                            {
                                case ArchStyle.Pointed:
                                    if (angle <= 90)
                                    {
                                        ax = -(int)(archRadius * Math.Cos(rad * 2));
                                        ay = (int)(archHeight * Math.Sin(rad));
                                    }
                                    else
                                    {
                                        ax = (int)(archRadius * Math.Cos((180 - angle) * Math.PI / 90));
                                        ay = (int)(archHeight * Math.Sin((180 - angle) * Math.PI / 180));
                                    }
                                    break;
                                default:
                                    ax = (int)(archRadius * Math.Cos(rad));
                                    ay = (int)(archHeight * Math.Sin(rad));
                                    break;
                            }

                            Place(grid, archCenterX + ax, archBaseY + ay, z);
                            Place(grid, archCenterX + ax, archBaseY + ay + 1, z);
                        }
                    }
                }

                // Draw deck
                int deckY = baseY + levelHeight - 1;
                for (int x = cx - halfLen; x <= cx + halfLen; x++)
                    for (int z = cz - halfW; z <= cz + halfW; z++)
                    {
                        Place(grid, x, deckY, z);
                        Place(grid, x, deckY + 1, z);
                    }
            }

            // Top deck railings
            int topY = cy + halfH;
            for (int x = cx - halfLen; x <= cx + halfLen; x++)
            {
                Place(grid, x, topY, cz - halfW);
                Place(grid, x, topY + 1, cz - halfW);
                Place(grid, x, topY, cz + halfW);
                Place(grid, x, topY + 1, cz + halfW);
            }

            return new ShapeInfo("Bridge (Aqueduct)", new()
            {
                ["Length"] = length.ToString(),
                ["Height"] = height.ToString(),
                ["Width"] = width.ToString(),
                ["Arches/Level"] = archesPerLevel.ToString(),
                ["Levels"] = levels.ToString(),
                ["Style"] = style.ToString(),
                ["Blocks"] = grid.BlockCount.ToString()
            });
        }

        #endregion

        #region Norman Architecture

        public static ShapeInfo GenerateWall(VoxelGrid grid, int length, int height, int thickness,
            int crenelWidth = 2, int crenelHeight = 2, int crenelSpacing = 3,
            bool arrowSlits = true, int arrowSlitSpacing = 8)
        {
            grid.Clear(); StartShape();
            int cx = grid.CenterX, cz = grid.CenterZ;
            int cy = grid.CenterY;
            int halfLen = length / 2;
            int halfThk = thickness / 2;
            int halfH = height / 2;

            // Main wall body
            for (int x = -halfLen; x <= halfLen; x++)
                for (int y = 0; y < height; y++)
                    for (int z = -halfThk; z <= halfThk; z++)
                    {
                        // Arrow slits
                        if (arrowSlits && y > 2 && y < height - 3)
                        {
                            int slitX = ((x + halfLen) % arrowSlitSpacing);
                            if (slitX == arrowSlitSpacing / 2 && Math.Abs(z) == halfThk)
                            {
                                if (y % 3 == 1) continue;
                            }
                        }
                        Place(grid, cx + x, cy - halfH + y, cz + z);
                    }

            // Crenellations (battlements)
            int crenelY = height;
            for (int x = -halfLen; x <= halfLen; x++)
            {
                int crenelPos = (x + halfLen) % (crenelWidth + crenelSpacing);
                if (crenelPos < crenelWidth)
                {
                    for (int y = 0; y < crenelHeight; y++)
                        for (int z = -halfThk; z <= halfThk; z++)
                            Place(grid, cx + x, cy - halfH + crenelY + y, cz + z);
                }
            }

            // Wall walk (platform behind crenellations)
            for (int x = -halfLen; x <= halfLen; x++)
                for (int z = -halfThk - 2; z < -halfThk; z++)
                    Place(grid, cx + x, cy - halfH + height - 1, cz + z);

            return new ShapeInfo("Norman Wall", new()
            {
                ["Length"] = length.ToString(),
                ["Height"] = height.ToString(),
                ["Thickness"] = thickness.ToString(),
                ["Arrow Slits"] = arrowSlits.ToString(),
                ["Blocks"] = grid.BlockCount.ToString()
            });
        }

        public static ShapeInfo GenerateTower(VoxelGrid grid, int radius, int height, int floors,
            bool battlements = true, EdgeMode edgeMode = EdgeMode.Normal)
        {
            grid.Clear(); StartShape();
            int cx = grid.CenterX, cz = grid.CenterZ;
            int cy = grid.CenterY;
            int halfH = height / 2;
            int floorHeight = height / Math.Max(1, floors);
            int wallThickness = 2;

            // Main tower walls - for radius R, diameter = 2*R
            for (int y = 0; y < height; y++)
            {
                bool isFloorLevel = (y % floorHeight == 0) && y > 0;

                for (int x = -radius; x < radius; x++)
                    for (int z = -radius; z < radius; z++)
                    {
                        double dx = x + 0.5;
                        double dz = z + 0.5;
                        double dist = Math.Sqrt(dx * dx + dz * dz);
                        bool inWall = dist <= radius && dist >= radius - wallThickness;
                        bool inFloor = dist <= radius && isFloorLevel;

                        if (inWall || inFloor)
                            Place(grid, cx + x, cy - halfH + y, cz + z);
                    }
            }

            // Battlements
            if (battlements)
            {
                int crenelHeight = 2;
                int crenelWidth = 2;
                int crenelGap = 2;

                for (int angle = 0; angle < 360; angle += 10)
                {
                    double rad = angle * Math.PI / 180;
                    int x = (int)((radius - 0.5) * Math.Cos(rad));
                    int z = (int)((radius - 0.5) * Math.Sin(rad));

                    if ((angle / 10) % ((crenelWidth + crenelGap) / 2) < crenelWidth / 2)
                    {
                        for (int crY = 0; crY < crenelHeight; crY++)
                            Place(grid, cx + x, cy + halfH + crY, cz + z);
                    }
                }
            }

            return new ShapeInfo("Norman Tower", new()
            {
                ["Radius"] = radius.ToString(),
                ["Height"] = height.ToString(),
                ["Floors"] = floors.ToString(),
                ["Battlements"] = battlements.ToString(),
                ["Blocks"] = grid.BlockCount.ToString()
            });
        }

        #endregion

        #region Complex Shapes

        public static ShapeInfo GenerateTorus(VoxelGrid grid, int majorRadius, int minorRadius)
        {
            grid.Clear(); StartShape();
            int cx = grid.CenterX, cz = grid.CenterZ;
            int cy = grid.CenterY;

            // For exact diameters
            int totalRadius = majorRadius + minorRadius;
            for (int x = -totalRadius; x < totalRadius; x++)
                for (int y = -minorRadius; y < minorRadius; y++)
                    for (int z = -totalRadius; z < totalRadius; z++)
                    {
                        double dx = x + 0.5;
                        double dy = y + 0.5;
                        double dz = z + 0.5;
                        double distFromAxis = Math.Sqrt(dx * dx + dz * dz);
                        double distFromRing = Math.Sqrt((distFromAxis - majorRadius) * (distFromAxis - majorRadius) + dy * dy);
                        if (distFromRing <= minorRadius)
                            Place(grid, cx + x, cy + y, cz + z);
                    }

            return new ShapeInfo("Torus", new()
            {
                ["Major Radius"] = majorRadius.ToString(),
                ["Minor Radius"] = minorRadius.ToString(),
                ["Blocks"] = grid.BlockCount.ToString()
            });
        }

        public static ShapeInfo GenerateEllipsoid(VoxelGrid grid, int rx, int ry, int rz,
            bool hollow = false, int thickness = 1)
        {
            grid.Clear(); StartShape();
            int cx = grid.CenterX, cz = grid.CenterZ;
            int cy = grid.CenterY;

            // For radius R, diameter = 2*R
            for (int x = -rx; x < rx; x++)
                for (int y = -ry; y < ry; y++)
                    for (int z = -rz; z < rz; z++)
                    {
                        // Offset by 0.5 to center
                        double dx = (x + 0.5) / rx;
                        double dy = (y + 0.5) / ry;
                        double dz = (z + 0.5) / rz;
                        double dist = dx * dx + dy * dy + dz * dz;

                        double innerDist = 2.0;
                        if (hollow && rx > thickness && ry > thickness && rz > thickness)
                        {
                            double innerDx = (x + 0.5) / (rx - thickness);
                            double innerDy = (y + 0.5) / (ry - thickness);
                            double innerDz = (z + 0.5) / (rz - thickness);
                            innerDist = innerDx * innerDx + innerDy * innerDy + innerDz * innerDz;
                        }

                        if (dist <= 1.0 && innerDist >= 1.0)
                            Place(grid, cx + x, cy + y, cz + z);
                    }

            return new ShapeInfo("Ellipsoid", new()
            {
                ["X Radius"] = rx.ToString(),
                ["Y Radius"] = ry.ToString(),
                ["Z Radius"] = rz.ToString(),
                ["Blocks"] = grid.BlockCount.ToString()
            });
        }

        public static ShapeInfo GenerateCuboid(VoxelGrid grid, int width, int height, int depth,
            bool hollow = false, int thickness = 1)
        {
            grid.Clear(); StartShape();
            int cx = grid.CenterX, cz = grid.CenterZ;
            int cy = grid.CenterY;

            // For exact dimensions, use 0 to dim-1, then offset to center
            int offsetX = width / 2;
            int offsetY = height / 2;
            int offsetZ = depth / 2;

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    for (int z = 0; z < depth; z++)
                    {
                        bool isEdge = x < thickness || x >= width - thickness ||
                                      y < thickness || y >= height - thickness ||
                                      z < thickness || z >= depth - thickness;
                        if (!hollow || isEdge)
                            Place(grid, cx + x - offsetX, cy + y - offsetY, cz + z - offsetZ);
                    }

            return new ShapeInfo("Cuboid", new()
            {
                ["Width"] = width.ToString(),
                ["Height"] = height.ToString(),
                ["Depth"] = depth.ToString(),
                ["Blocks"] = grid.BlockCount.ToString()
            });
        }

        public static ShapeInfo GenerateParaboloid(VoxelGrid grid, int radius, int height,
            bool hollow = false, int thickness = 2)
        {
            grid.Clear(); StartShape();
            int cx = grid.CenterX, cz = grid.CenterZ;
            int cy = grid.CenterY;
            int halfH = height / 2;

            for (int y = 0; y < height; y++)
            {
                double t = (double)y / height;
                int layerRadius = (int)(radius * Math.Sqrt(t));
                if (layerRadius < 1 && y > 0) layerRadius = 1;

                GenerateFilledCircle(grid, cx, cy - halfH + y, cz, layerRadius, thickness, hollow && y > thickness);
            }

            return new ShapeInfo("Paraboloid", new()
            {
                ["Radius"] = radius.ToString(),
                ["Height"] = height.ToString(),
                ["Hollow"] = hollow.ToString(),
                ["Blocks"] = grid.BlockCount.ToString()
            });
        }

        public static ShapeInfo GenerateHyperboloid(VoxelGrid grid, int topRadius, int waistRadius, int height,
            bool hollow = false, int thickness = 2)
        {
            grid.Clear(); StartShape();
            int cx = grid.CenterX, cz = grid.CenterZ;
            int cy = grid.CenterY;
            int halfH = height / 2;

            for (int y = -halfH; y < halfH; y++)
            {
                double t = (double)y / halfH;  // -1 to 1
                double curve = Math.Sqrt(1 + t * t);
                int layerRadius = (int)(waistRadius + (topRadius - waistRadius) * Math.Abs(t) * curve / Math.Sqrt(2));

                GenerateFilledCircle(grid, cx, cy + y, cz, layerRadius, thickness, hollow);
            }

            return new ShapeInfo("Hyperboloid", new()
            {
                ["Top Radius"] = topRadius.ToString(),
                ["Waist Radius"] = waistRadius.ToString(),
                ["Height"] = height.ToString(),
                ["Hollow"] = hollow.ToString(),
                ["Blocks"] = grid.BlockCount.ToString()
            });
        }

        #endregion

        #region Spirals

        public static ShapeInfo GenerateSpiralStaircase(VoxelGrid grid, int radius, int height,
            int stepsPerRotation = 16, int pillarRadius = 1)
        {
            grid.Clear(); StartShape();
            int cx = grid.CenterX, cz = grid.CenterZ;
            int cy = grid.CenterY;
            int halfH = height / 2;
            double anglePerStep = 2 * Math.PI / stepsPerRotation;

            for (int step = 0; step < height; step++)
            {
                double startAngle = step * anglePerStep;
                double endAngle = (step + 1) * anglePerStep;
                int angleSteps = Math.Max(1, radius * 2);

                // Use < instead of <= to avoid overlapping with next step
                for (int a = 0; a < angleSteps; a++)
                {
                    double t = (double)a / angleSteps;
                    double currentAngle = startAngle + t * (endAngle - startAngle);

                    for (int r = pillarRadius; r <= radius; r++)
                    {
                        int x = (int)Math.Round(r * Math.Cos(currentAngle));
                        int z = (int)Math.Round(r * Math.Sin(currentAngle));
                        Place(grid, cx + x, cy - halfH + step, cz + z);
                    }
                }

                // Central pillar
                for (int px = -pillarRadius + 1; px < pillarRadius; px++)
                    for (int pz = -pillarRadius + 1; pz < pillarRadius; pz++)
                        if (px * px + pz * pz < pillarRadius * pillarRadius)
                            Place(grid, cx + px, cy - halfH + step, cz + pz);
            }

            return new ShapeInfo("Spiral Staircase", new()
            {
                ["Radius"] = radius.ToString(),
                ["Height"] = height.ToString(),
                ["Steps/Rotation"] = stepsPerRotation.ToString(),
                ["Pillar Radius"] = pillarRadius.ToString(),
                ["Blocks"] = grid.BlockCount.ToString()
            });
        }

        public static ShapeInfo GenerateHelix(VoxelGrid grid, int radius, int height, int thickness,
            double rotations, int strands = 1)
        {
            grid.Clear(); StartShape();
            int cx = grid.CenterX, cz = grid.CenterZ;
            int cy = grid.CenterY;
            int halfH = height / 2;
            int steps = height * 10;
            double totalAngle = rotations * 2 * Math.PI;
            double strandOffset = 2 * Math.PI / strands;

            for (int strand = 0; strand < strands; strand++)
            {
                double baseOffset = strand * strandOffset;

                for (int i = 0; i <= steps; i++)
                {
                    double t = (double)i / steps;
                    double angle = t * totalAngle + baseOffset;
                    int y = (int)(t * height);
                    int centerX = (int)Math.Round(radius * Math.Cos(angle));
                    int centerZ = (int)Math.Round(radius * Math.Sin(angle));

                    int halfT = thickness / 2;
                    for (int tx = -halfT; tx <= halfT; tx++)
                        for (int ty = -halfT; ty <= halfT; ty++)
                            for (int tz = -halfT; tz <= halfT; tz++)
                                if (tx * tx + ty * ty + tz * tz <= halfT * halfT + 1)
                                    Place(grid, cx + centerX + tx, cy - halfH + y + ty, cz + centerZ + tz);
                }
            }

            return new ShapeInfo("Helix", new()
            {
                ["Radius"] = radius.ToString(),
                ["Height"] = height.ToString(),
                ["Rotations"] = rotations.ToString("F1"),
                ["Strands"] = strands.ToString(),
                ["Blocks"] = grid.BlockCount.ToString()
            });
        }

        #endregion

        #region Helper Methods

        private static void GenerateCircleLayer(VoxelGrid grid, int cx, int y, int cz, int radius,
            bool hollow, int thickness, EdgeMode edgeMode)
        {
            // For radius R, diameter = 2*R blocks
            int minB = -radius;
            int maxB = radius - 1;

            for (int x = minB; x <= maxB; x++)
                for (int z = minB; z <= maxB; z++)
                {
                    double dx = x + 0.5;
                    double dz = z + 0.5;
                    double dist = Math.Sqrt(dx * dx + dz * dz);
                    bool inOuter = dist <= radius;
                    bool inInner = hollow && dist < radius - thickness;

                    if (inOuter && !inInner)
                        Place(grid, cx + x, y, cz + z);
                }

            if (edgeMode != EdgeMode.Normal && hollow)
                ApplyEdgeMode(grid, cx, y, cz, radius, edgeMode);
        }

        private static void GenerateFilledCircle(VoxelGrid grid, int cx, int y, int cz, int radius,
            int thickness, bool hollow)
        {
            // For radius R, diameter = 2*R blocks
            int minB = -radius;
            int maxB = radius - 1;

            for (int x = minB; x <= maxB; x++)
                for (int z = minB; z <= maxB; z++)
                {
                    double dx = x + 0.5;
                    double dz = z + 0.5;
                    double dist = Math.Sqrt(dx * dx + dz * dz);
                    bool inOuter = dist <= radius;
                    bool inInner = hollow && dist < radius - thickness;

                    if (inOuter && !inInner)
                        Place(grid, cx + x, y, cz + z);
                }
        }

        private static void ApplyEdgeMode(VoxelGrid grid, int cx, int y, int cz, int radius, EdgeMode mode)
        {
            // Check for diagonal-only connections and fill them
            for (int x = -radius; x <= radius; x++)
                for (int z = -radius; z <= radius; z++)
                {
                    if (grid.GetBlock(cx + x, y, cz + z) == BlockType.Empty) continue;

                    bool hasUp = grid.GetBlock(cx + x, y, cz + z - 1) != BlockType.Empty;
                    bool hasDown = grid.GetBlock(cx + x, y, cz + z + 1) != BlockType.Empty;
                    bool hasLeft = grid.GetBlock(cx + x - 1, y, cz + z) != BlockType.Empty;
                    bool hasRight = grid.GetBlock(cx + x + 1, y, cz + z) != BlockType.Empty;

                    bool hasUpLeft = grid.GetBlock(cx + x - 1, y, cz + z - 1) != BlockType.Empty;
                    bool hasUpRight = grid.GetBlock(cx + x + 1, y, cz + z - 1) != BlockType.Empty;
                    bool hasDownLeft = grid.GetBlock(cx + x - 1, y, cz + z + 1) != BlockType.Empty;
                    bool hasDownRight = grid.GetBlock(cx + x + 1, y, cz + z + 1) != BlockType.Empty;

                    // Check each diagonal
                    if (hasUpLeft && !hasUp && !hasLeft)
                    {
                        if (mode == EdgeMode.HeavyInside)
                            Place(grid, cx + x - 1, y, cz + z);
                        else
                            Place(grid, cx + x, y, cz + z - 1);
                    }
                    if (hasUpRight && !hasUp && !hasRight)
                    {
                        if (mode == EdgeMode.HeavyInside)
                            Place(grid, cx + x + 1, y, cz + z);
                        else
                            Place(grid, cx + x, y, cz + z - 1);
                    }
                    if (hasDownLeft && !hasDown && !hasLeft)
                    {
                        if (mode == EdgeMode.HeavyInside)
                            Place(grid, cx + x - 1, y, cz + z);
                        else
                            Place(grid, cx + x, y, cz + z + 1);
                    }
                    if (hasDownRight && !hasDown && !hasRight)
                    {
                        if (mode == EdgeMode.HeavyInside)
                            Place(grid, cx + x + 1, y, cz + z);
                        else
                            Place(grid, cx + x, y, cz + z + 1);
                    }
                }
        }

        #endregion

        #region Point-to-Point Lines

        /// <summary>
        /// Draws straight lines connecting a series of points
        /// </summary>
        public static ShapeInfo GeneratePointToPointLine(VoxelGrid grid, List<(int x, int y, int z)> points,
            int width = 1, int height = 1, bool closedLoop = false)
        {
            grid.Clear(); StartShape();
            int cx = grid.CenterX, cy = grid.CenterY, cz = grid.CenterZ;

            if (points.Count < 2) return new ShapeInfo("Line", new() { ["Points"] = "0", ["Blocks"] = "0" });

            // Draw lines between consecutive points
            for (int i = 0; i < points.Count - 1; i++)
            {
                DrawLine3D(grid, cx + points[i].x, cy + points[i].y, cz + points[i].z,
                                 cx + points[i + 1].x, cy + points[i + 1].y, cz + points[i + 1].z, width, height);
            }

            // Close the loop if requested
            if (closedLoop && points.Count > 2)
            {
                DrawLine3D(grid, cx + points[points.Count - 1].x, cy + points[points.Count - 1].y, cz + points[points.Count - 1].z,
                                 cx + points[0].x, cy + points[0].y, cz + points[0].z, width, height);
            }

            return new ShapeInfo("Point-to-Point Line", new()
            {
                ["Points"] = points.Count.ToString(),
                ["Width"] = width.ToString(),
                ["Height"] = height.ToString(),
                ["Closed Loop"] = closedLoop.ToString(),
                ["Blocks"] = grid.BlockCount.ToString()
            });
        }

        /// <summary>
        /// Draws a smooth spline curve through a series of points using Catmull-Rom interpolation
        /// </summary>
        public static ShapeInfo GenerateSplineLine(VoxelGrid grid, List<(int x, int y, int z)> points,
            int width = 1, int height = 1, bool closedLoop = false, int segmentsPerSpan = 10)
        {
            grid.Clear(); StartShape();
            int cx = grid.CenterX, cy = grid.CenterY, cz = grid.CenterZ;

            if (points.Count < 2) return new ShapeInfo("Spline", new() { ["Points"] = "0", ["Blocks"] = "0" });

            if (points.Count == 2)
            {
                // Just two points - draw a straight line
                DrawLine3D(grid, cx + points[0].x, cy + points[0].y, cz + points[0].z,
                                 cx + points[1].x, cy + points[1].y, cz + points[1].z, width, height);
            }
            else
            {
                // Use Catmull-Rom spline interpolation
                var extendedPoints = new List<(double x, double y, double z)>();

                if (closedLoop)
                {
                    // For closed loop, wrap around
                    extendedPoints.Add((points[points.Count - 1].x, points[points.Count - 1].y, points[points.Count - 1].z));
                    foreach (var p in points)
                        extendedPoints.Add((p.x, p.y, p.z));
                    extendedPoints.Add((points[0].x, points[0].y, points[0].z));
                    extendedPoints.Add((points[1].x, points[1].y, points[1].z));
                }
                else
                {
                    // Extend endpoints by extrapolation
                    var first = points[0];
                    var second = points[1];
                    extendedPoints.Add((2 * first.x - second.x, 2 * first.y - second.y, 2 * first.z - second.z));

                    foreach (var p in points)
                        extendedPoints.Add((p.x, p.y, p.z));

                    var secondLast = points[points.Count - 2];
                    var last = points[points.Count - 1];
                    extendedPoints.Add((2 * last.x - secondLast.x, 2 * last.y - secondLast.y, 2 * last.z - secondLast.z));
                }

                // Interpolate between each pair of control points
                int numSpans = closedLoop ? points.Count : points.Count - 1;

                for (int span = 0; span < numSpans; span++)
                {
                    var p0 = extendedPoints[span];
                    var p1 = extendedPoints[span + 1];
                    var p2 = extendedPoints[span + 2];
                    var p3 = extendedPoints[span + 3];

                    (double px, double py, double pz) prevPoint = p1;

                    for (int seg = 1; seg <= segmentsPerSpan; seg++)
                    {
                        double t = (double)seg / segmentsPerSpan;

                        // Catmull-Rom interpolation
                        double t2 = t * t;
                        double t3 = t2 * t;

                        double x = 0.5 * ((2 * p1.x) + (-p0.x + p2.x) * t +
                                         (2 * p0.x - 5 * p1.x + 4 * p2.x - p3.x) * t2 +
                                         (-p0.x + 3 * p1.x - 3 * p2.x + p3.x) * t3);
                        double y = 0.5 * ((2 * p1.y) + (-p0.y + p2.y) * t +
                                         (2 * p0.y - 5 * p1.y + 4 * p2.y - p3.y) * t2 +
                                         (-p0.y + 3 * p1.y - 3 * p2.y + p3.y) * t3);
                        double z = 0.5 * ((2 * p1.z) + (-p0.z + p2.z) * t +
                                         (2 * p0.z - 5 * p1.z + 4 * p2.z - p3.z) * t2 +
                                         (-p0.z + 3 * p1.z - 3 * p2.z + p3.z) * t3);

                        DrawLine3D(grid,
                            cx + (int)Math.Round(prevPoint.px), cy + (int)Math.Round(prevPoint.py), cz + (int)Math.Round(prevPoint.pz),
                            cx + (int)Math.Round(x), cy + (int)Math.Round(y), cz + (int)Math.Round(z),
                            width, height);

                        prevPoint = (x, y, z);
                    }
                }
            }

            return new ShapeInfo("Spline Curve", new()
            {
                ["Points"] = points.Count.ToString(),
                ["Width"] = width.ToString(),
                ["Height"] = height.ToString(),
                ["Closed Loop"] = closedLoop.ToString(),
                ["Smoothness"] = segmentsPerSpan.ToString(),
                ["Blocks"] = grid.BlockCount.ToString()
            });
        }

        /// <summary>
        /// Draws a 3D line between two points using Bresenham's algorithm, with separate width and height
        /// </summary>
        /// <param name="width">Horizontal thickness (X/Z plane)</param>
        /// <param name="height">Vertical thickness (Y axis)</param>
        private static void DrawLine3D(VoxelGrid grid, int x0, int y0, int z0, int x1, int y1, int z1,
            int width = 1, int height = 1)
        {
            int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int dz = Math.Abs(z1 - z0), sz = z0 < z1 ? 1 : -1;
            int dm = Math.Max(dx, Math.Max(dy, dz));

            int x = x0, y = y0, z = z0;
            int errXY = dm / 2, errXZ = dm / 2, errYZ = dm / 2;

            // Calculate offsets for width and height
            int halfW = (width - 1) / 2;
            int halfH = (height - 1) / 2;

            for (int i = 0; i <= dm; i++)
            {
                // Place blocks with width (horizontal) and height (vertical)
                if (width <= 1 && height <= 1)
                {
                    Place(grid, x, y, z);
                }
                else
                {
                    // Determine the line's primary direction to know how to orient the "wall"
                    // For horizontal lines (X or Z primary), width expands perpendicular to travel
                    // Height always expands in Y

                    for (int ty = -halfH; ty <= halfH; ty++)
                    {
                        if (dm == dx || dm == 0)
                        {
                            // Line primarily along X - width expands in Z
                            for (int tz = -halfW; tz <= halfW; tz++)
                                Place(grid, x, y + ty, z + tz);
                        }
                        else if (dm == dz)
                        {
                            // Line primarily along Z - width expands in X
                            for (int tx = -halfW; tx <= halfW; tx++)
                                Place(grid, x + tx, y + ty, z);
                        }
                        else
                        {
                            // Line primarily along Y - width expands in both X and Z
                            for (int tx = -halfW; tx <= halfW; tx++)
                                for (int tz = -halfW; tz <= halfW; tz++)
                                    Place(grid, x + tx, y + ty, z + tz);
                        }
                    }
                }

                if (dm == 0) break;

                if (dm == dx)
                {
                    x += sx;
                    errXY -= dy; if (errXY < 0) { errXY += dx; y += sy; }
                    errXZ -= dz; if (errXZ < 0) { errXZ += dx; z += sz; }
                }
                else if (dm == dy)
                {
                    y += sy;
                    errXY -= dx; if (errXY < 0) { errXY += dy; x += sx; }
                    errYZ -= dz; if (errYZ < 0) { errYZ += dy; z += sz; }
                }
                else
                {
                    z += sz;
                    errXZ -= dx; if (errXZ < 0) { errXZ += dz; x += sx; }
                    errYZ -= dy; if (errYZ < 0) { errYZ += dz; y += sy; }
                }
            }
        }

        /// <summary>
        /// Parses a string of points in format "x1,y1,z1; x2,y2,z2; ..." into a list of coordinates
        /// </summary>
        public static List<(int x, int y, int z)> ParsePoints(string input)
        {
            var result = new List<(int x, int y, int z)>();
            if (string.IsNullOrWhiteSpace(input)) return result;

            var pointStrings = input.Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var pointStr in pointStrings)
            {
                var coords = pointStr.Trim().Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (coords.Length >= 3 &&
                    int.TryParse(coords[0].Trim(), out int x) &&
                    int.TryParse(coords[1].Trim(), out int y) &&
                    int.TryParse(coords[2].Trim(), out int z))
                {
                    result.Add((x, y, z));
                }
            }

            return result;
        }

        #endregion
    }
}