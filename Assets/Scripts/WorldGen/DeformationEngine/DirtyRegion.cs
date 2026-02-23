using System;
using Vastcore.WorldGen.Common;

namespace Vastcore.WorldGen.DeformationEngine
{
    /// <summary>
    /// 変形により再抽出が必要な領域。
    /// </summary>
    [Serializable]
    public struct DirtyRegion
    {
        public ChunkBounds bounds;
        public string reason;
        public long ticksUtc;
    }
}
