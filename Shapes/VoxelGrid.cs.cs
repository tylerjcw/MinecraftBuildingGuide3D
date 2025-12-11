using System;
using System.Collections.Generic;
using MinecraftBuildingGuide3D.Models;

namespace MinecraftBuildingGuide3D.Shapes
{
    /// <summary>
    /// Manages a 3D grid of voxels/blocks
    /// </summary>
    public class VoxelGrid
    {
        private readonly BlockType[,,] _grid;

        public int SizeX { get; }
        public int SizeY { get; }
        public int SizeZ { get; }
        public int CenterX => SizeX / 2;
        public int CenterY => SizeY / 2;
        public int CenterZ => SizeZ / 2;

        public int BlockCount { get; private set; }

        public VoxelGrid(int sizeX, int sizeY, int sizeZ)
        {
            SizeX = sizeX;
            SizeY = sizeY;
            SizeZ = sizeZ;
            _grid = new BlockType[sizeX, sizeY, sizeZ];
            BlockCount = 0;
        }

        public VoxelGrid(int uniformSize) : this(uniformSize, uniformSize, uniformSize) { }

        public void Clear()
        {
            Array.Clear(_grid, 0, _grid.Length);
            BlockCount = 0;
        }

        public bool IsInBounds(int x, int y, int z)
        {
            return x >= 0 && x < SizeX && y >= 0 && y < SizeY && z >= 0 && z < SizeZ;
        }

        public BlockType GetBlock(int x, int y, int z)
        {
            if (!IsInBounds(x, y, z)) return BlockType.Empty;
            return _grid[x, y, z];
        }

        public void SetBlock(int x, int y, int z, BlockType type)
        {
            if (!IsInBounds(x, y, z)) return;

            var oldType = _grid[x, y, z];
            _grid[x, y, z] = type;

            // Update block count
            if (oldType == BlockType.Empty && type != BlockType.Empty)
                BlockCount++;
            else if (oldType != BlockType.Empty && type == BlockType.Empty)
                BlockCount--;
        }

        public void SetBlockIfEmpty(int x, int y, int z, BlockType type)
        {
            if (!IsInBounds(x, y, z)) return;
            if (_grid[x, y, z] == BlockType.Empty)
            {
                _grid[x, y, z] = type;
                BlockCount++;
            }
        }

        /// <summary>
        /// Get all non-empty blocks in the grid
        /// </summary>
        public IEnumerable<Block> GetAllBlocks()
        {
            for (int y = 0; y < SizeY; y++)
                for (int z = 0; z < SizeZ; z++)
                    for (int x = 0; x < SizeX; x++)
                        if (_grid[x, y, z] != BlockType.Empty)
                            yield return new Block(x, y, z, _grid[x, y, z]);
        }

        /// <summary>
        /// Get blocks for a specific Y layer
        /// </summary>
        public IEnumerable<Block> GetBlocksAtLayer(int layerY)
        {
            if (layerY < 0 || layerY >= SizeY) yield break;

            for (int z = 0; z < SizeZ; z++)
                for (int x = 0; x < SizeX; x++)
                    if (_grid[x, layerY, z] != BlockType.Empty)
                        yield return new Block(x, layerY, z, _grid[x, layerY, z]);
        }

        /// <summary>
        /// Get blocks up to a specific Y layer (for layer-by-layer building view)
        /// </summary>
        public IEnumerable<Block> GetBlocksUpToLayer(int maxLayerY)
        {
            for (int y = 0; y <= Math.Min(maxLayerY, SizeY - 1); y++)
                for (int z = 0; z < SizeZ; z++)
                    for (int x = 0; x < SizeX; x++)
                        if (_grid[x, y, z] != BlockType.Empty)
                            yield return new Block(x, y, z, _grid[x, y, z]);
        }

        /// <summary>
        /// Check if a block face is visible (not occluded by adjacent blocks)
        /// Used for optimized rendering - only render visible faces
        /// </summary>
        public bool IsFaceVisible(int x, int y, int z, BlockFace face)
        {
            if (_grid[x, y, z] == BlockType.Empty) return false;

            int nx = x, ny = y, nz = z;
            switch (face)
            {
                case BlockFace.Top: ny++; break;
                case BlockFace.Bottom: ny--; break;
                case BlockFace.Front: nz++; break;
                case BlockFace.Back: nz--; break;
                case BlockFace.Right: nx++; break;
                case BlockFace.Left: nx--; break;
            }

            // Face is visible if neighbor is out of bounds or empty
            return !IsInBounds(nx, ny, nz) || _grid[nx, ny, nz] == BlockType.Empty;
        }

        /// <summary>
        /// Get the bounding box of all non-empty blocks
        /// </summary>
        public (int minX, int minY, int minZ, int maxX, int maxY, int maxZ) GetBoundingBox()
        {
            int minX = SizeX, minY = SizeY, minZ = SizeZ;
            int maxX = -1, maxY = -1, maxZ = -1;

            for (int y = 0; y < SizeY; y++)
                for (int z = 0; z < SizeZ; z++)
                    for (int x = 0; x < SizeX; x++)
                        if (_grid[x, y, z] != BlockType.Empty)
                        {
                            minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                            minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
                            minZ = Math.Min(minZ, z); maxZ = Math.Max(maxZ, z);
                        }

            if (maxX < 0) return (0, 0, 0, 0, 0, 0); // Empty grid
            return (minX, minY, minZ, maxX, maxY, maxZ);
        }

        /// <summary>
        /// Count blocks at each Y level
        /// </summary>
        public int[] GetLayerCounts()
        {
            int[] counts = new int[SizeY];
            for (int y = 0; y < SizeY; y++)
                for (int z = 0; z < SizeZ; z++)
                    for (int x = 0; x < SizeX; x++)
                        if (_grid[x, y, z] != BlockType.Empty)
                            counts[y]++;
            return counts;
        }
    }

    public enum BlockFace
    {
        Top,
        Bottom,
        Front,
        Back,
        Left,
        Right
    }
}