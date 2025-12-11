using OpenTK.Mathematics;

namespace MinecraftBuildingGuide3D.Models
{
    /// <summary>
    /// Represents a single block in the 3D grid
    /// </summary>
    public struct Block
    {
        public int X { get; set; }
        public int Y { get; set; }  // Y is vertical (up) in our coordinate system
        public int Z { get; set; }
        public BlockType Type { get; set; }
        public bool IsVisible { get; set; }

        public Block(int x, int y, int z, BlockType type = BlockType.Solid)
        {
            X = x;
            Y = y;
            Z = z;
            Type = type;
            IsVisible = true;
        }

        public Vector3 Position => new Vector3(X, Y, Z);

        public override string ToString() => $"Block({X}, {Y}, {Z})";
    }

    public enum BlockType
    {
        Empty = 0,
        Solid = 1,
        Edge = 2,      // For highlighting edges
        Center = 3,    // Center reference point
        Layer = 4      // Currently selected layer highlight
    }

    /// <summary>
    /// Color schemes for different block types
    /// </summary>
    public static class BlockColors
    {
        public static readonly Color4 Solid = new Color4(0.29f, 0.49f, 0.31f, 1.0f);      // Minecraft grass green
        public static readonly Color4 Edge = new Color4(0.2f, 0.35f, 0.22f, 1.0f);        // Darker green for edges
        public static readonly Color4 Center = new Color4(1.0f, 0.42f, 0.42f, 1.0f);      // Red
        public static readonly Color4 Layer = new Color4(0.4f, 0.6f, 0.9f, 0.8f);         // Blue highlight
        public static readonly Color4 GridLine = new Color4(0.3f, 0.3f, 0.3f, 0.5f);      // Grid lines
        public static readonly Color4 Empty = new Color4(0.95f, 0.95f, 0.95f, 0.1f);      // Nearly invisible

        public static Color4 GetColor(BlockType type)
        {
            return type switch
            {
                BlockType.Solid => Solid,
                BlockType.Edge => Edge,
                BlockType.Center => Center,
                BlockType.Layer => Layer,
                _ => Empty
            };
        }
    }
}