namespace Vastcore.WorldGen.Common
{
    /// <summary>
    /// 3D ボリューメトリックチャンクの座標。
    /// 既存の Vector2Int チャンク座標（2D）と区別するためのラッパー。
    /// </summary>
    public struct VolumetricChunkCoord : System.IEquatable<VolumetricChunkCoord>
    {
        public int X;
        public int Y;
        public int Z;

        public static VolumetricChunkCoord Create(int x, int y, int z)
        {
            return new VolumetricChunkCoord { X = x, Y = y, Z = z };
        }

        public bool Equals(VolumetricChunkCoord other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is VolumetricChunkCoord c && Equals(c);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 73856093) ^ (Y * 19349663) ^ (Z * 83492791);
            }
        }

        public static bool operator ==(VolumetricChunkCoord a, VolumetricChunkCoord b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(VolumetricChunkCoord a, VolumetricChunkCoord b)
        {
            return !a.Equals(b);
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
    }
}
